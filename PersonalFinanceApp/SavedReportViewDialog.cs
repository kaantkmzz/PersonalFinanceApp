using System.Windows.Forms.DataVisualization.Charting;
using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp
{
    // Geçmiş Raporlar listesinden "Görüntüle" ile açılan, kaydedilmiş bir rapor anlık görüntüsünü
    // (o dönemin gelir/gider/hedef kırılımını) gösteren salt-okunur pencere.
    public class SavedReportViewDialog : Form
    {
        private readonly ReportHistoryEntry _entry;

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color IncomeColor => AppTheme.IncomeColor;
        private static Color ExpenseColor => AppTheme.ExpenseColor;
        private static Color GoalColor => AppTheme.GoalColor;
        private static Color SliceBorderColor => AppTheme.SliceBorderColor;

        private Chart chart = new Chart();
        private FlowLayoutPanel pnlLegendFlow = new FlowLayoutPanel();

        public SavedReportViewDialog(ReportHistoryEntry entry)
        {
            _entry = entry;
            SetupUI();
            this.Load += (s, e) => { DarkTitleBarHelper.EnableDarkTitleBar(this); BuildChart(); };
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new Size(760, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppBackColor;
            this.Text = "Rapor Görüntüle";
            this.Font = new Font("Segoe UI", 9F);

            var tr = new System.Globalization.CultureInfo("tr-TR");

            Label lblTitle = new Label
            {
                Text = $"{ReportPeriodHelper.GetLabel(_entry.PeriodType)} Rapor",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 15,
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            Label lblRange = new Label
            {
                Text = $"{_entry.PeriodStart:dd.MM.yyyy HH:mm} — {_entry.PeriodEnd:dd.MM.yyyy HH:mm}",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = TextMuted,
                Left = 20,
                Top = 58,
                AutoSize = true
            };
            this.Controls.Add(lblRange);

            Panel cardIncome = CreateSummaryCard("Toplam Gelir", 20, 90, IncomeColor, _entry.TotalIncome.ToString("#,##0", tr) + " ₺");
            Panel cardExpense = CreateSummaryCard("Toplam Gider", 275, 90, ExpenseColor, _entry.TotalExpense.ToString("#,##0", tr) + " ₺");
            Panel cardNet = CreateSummaryCard("Net Bakiye", 530, 90, TextLight, _entry.NetBalance.ToString("#,##0", tr) + " ₺");
            this.Controls.Add(cardIncome);
            this.Controls.Add(cardExpense);
            this.Controls.Add(cardNet);

            chart.Left = 20;
            chart.Top = 200;
            chart.Width = 720;
            chart.Height = 400;
            chart.BackColor = AppBackColor;

            ChartArea chartArea = new ChartArea("main");
            chartArea.BackColor = AppBackColor;
            chart.ChartAreas.Add(chartArea);
            this.Controls.Add(chart);

            pnlLegendFlow.FlowDirection = FlowDirection.LeftToRight;
            pnlLegendFlow.AutoSize = true;
            pnlLegendFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlLegendFlow.WrapContents = true;
            pnlLegendFlow.MaximumSize = new Size(720, 60);
            pnlLegendFlow.Left = 20;
            pnlLegendFlow.Top = 598;
            pnlLegendFlow.BackColor = AppBackColor;
            this.Controls.Add(pnlLegendFlow);

            Button btnClose = new Button { Text = "Kapat", Left = 560, Top = 630, Width = 180, Height = 36, Cursor = Cursors.Hand };
            SetupRoundedButton(btnClose, Color.FromArgb(80, 85, 105), Color.White);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private Panel CreateSummaryCard(string title, int left, int top, Color valueColor, string valueText)
        {
            Panel card = new Panel { Left = left, Top = top, Width = 235, Height = 90, BackColor = AppBackColor };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(card.Parent?.BackColor ?? AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
                using var brush = new SolidBrush(CardBackColor);
                e.Graphics.FillPath(brush, path);
            };

            Label lblCardTitle = new Label { Text = title, Left = 15, Top = 14, AutoSize = true, ForeColor = TextMuted, Font = new Font("Segoe UI", 9F), BackColor = Color.Transparent };
            Label lblValue = new Label { Text = valueText, Left = 15, Top = 40, AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = valueColor, BackColor = Color.Transparent };

            card.Controls.Add(lblCardTitle);
            card.Controls.Add(lblValue);
            return card;
        }

        private void BuildChart()
        {
            var tr = new System.Globalization.CultureInfo("tr-TR");
            decimal pieTotal = _entry.IncomeBreakdown.Sum(x => x.TotalAmount) + _entry.ExpenseBreakdown.Sum(x => x.TotalAmount) + _entry.GoalBreakdown.Sum(x => x.TotalAmount);

            Series series = new Series("İşlemler") { ChartType = SeriesChartType.Doughnut };
            series["PieLabelStyle"] = "Outside";
            series["PieLineColor"] = $"{TextMuted.R},{TextMuted.G},{TextMuted.B}";
            series["DoughnutRadius"] = "62";
            series["PieDrawingStyle"] = "SoftEdge";
            series["PieStartAngle"] = "270";
            series.Label = "#VALX #PERCENT{P0}";
            series.Font = new Font("Segoe UI", 9F);
            series.LabelForeColor = TextLight;

            decimal smallSliceThreshold = pieTotal * 0.04m;

            void AddPoint(string name, decimal amount, Color color)
            {
                int index = series.Points.AddXY(name, amount);
                series.Points[index].Color = color;
                series.Points[index].BorderColor = SliceBorderColor;
                series.Points[index].BorderWidth = 2;
            }

            void AddGroupedPoints(IEnumerable<CategorySummary> items, Color color)
            {
                var list = items.ToList();
                var large = list.Where(i => i.TotalAmount >= smallSliceThreshold).ToList();
                var small = list.Where(i => i.TotalAmount < smallSliceThreshold).ToList();

                foreach (var item in large)
                    AddPoint(item.CategoryName, item.TotalAmount, color);

                if (small.Count == 1)
                    AddPoint(small[0].CategoryName, small[0].TotalAmount, color);
                else if (small.Count > 1)
                    AddPoint("Diğer", small.Sum(i => i.TotalAmount), color);
            }

            AddGroupedPoints(_entry.IncomeBreakdown, IncomeColor);
            AddGroupedPoints(_entry.ExpenseBreakdown, ExpenseColor);
            AddGroupedPoints(_entry.GoalBreakdown, GoalColor);

            if (series.Points.Count == 0)
                AddPoint("Bu dönemde işlem yok", 1, Color.FromArgb(60, 64, 84));

            series.ChartArea = "main";
            chart.Series.Add(series);

            BuildLegend(pieTotal, tr);
        }

        private void BuildLegend(decimal pieTotal, System.Globalization.CultureInfo tr)
        {
            pnlLegendFlow.Controls.Clear();
            if (pieTotal <= 0) return;

            decimal incomeTotal = _entry.IncomeBreakdown.Sum(x => x.TotalAmount);
            decimal expenseTotal = _entry.ExpenseBreakdown.Sum(x => x.TotalAmount);
            decimal goalTotal = _entry.GoalBreakdown.Sum(x => x.TotalAmount);

            if (incomeTotal > 0) AddLegendEntry(IncomeColor, "Gelir", incomeTotal, pieTotal);
            if (expenseTotal > 0) AddLegendEntry(ExpenseColor, "Gider", expenseTotal, pieTotal);
            if (goalTotal > 0) AddLegendEntry(GoalColor, "Hedef", goalTotal, pieTotal);
        }

        private void AddLegendEntry(Color color, string label, decimal amount, decimal pieTotal)
        {
            int percent = (int)Math.Round(amount / pieTotal * 100);
            Font legendFont = new Font("Segoe UI", 9.5F);

            const int dotSize = 16;
            Size textSize = TextRenderer.MeasureText($"{label} %{percent}", legendFont);
            int dotTopMargin = Math.Max(0, (textSize.Height - dotSize) / 2);

            Panel dot = new Panel { Width = dotSize, Height = dotSize, Margin = new Padding(4, dotTopMargin, 4, 4) };
            dot.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(color);
                e.Graphics.FillEllipse(brush, 0, 0, dot.Width - 1, dot.Height - 1);
            };

            Label lbl = new Label { Text = $"{label} %{percent}", AutoSize = true, ForeColor = TextLight, Font = legendFont, Margin = new Padding(0, 0, 18, 4) };

            pnlLegendFlow.Controls.Add(dot);
            pnlLegendFlow.Controls.Add(lbl);
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Font = new Font("Segoe UI", 10F);
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = Math.Max(radius * 2, 1);
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
