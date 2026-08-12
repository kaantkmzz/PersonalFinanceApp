using System.Windows.Forms.DataVisualization.Charting;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class ReportControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly ReportService _reportService = new ReportService();
        private readonly AccountService _accountService = new AccountService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color IncomeColor => AppTheme.IncomeColor;
        private static Color ExpenseColor => AppTheme.ExpenseColor;
        private static Color WalletColor => AppTheme.WalletColor;
        private static Color SafeColor => AppTheme.SafeColor;
        private static Color IdleColor => AppTheme.IdleColor;
        private static Color SliceBorderColor => AppTheme.SliceBorderColor;

        private ComboBox cmbMonth = new ComboBox();
        private NumericUpDown nudYear = new NumericUpDown();
        private Button btnView = new Button();
        private Button btnExportReport = new Button();
        private MonthlyReport? _currentReport;

        private Label lblIncome = new Label();
        private Label lblExpense = new Label();
        private Label lblNet = new Label();
        private Label lblWalletBalance = new Label();
        private Label lblSafeBalance = new Label();

        private Chart chart = new Chart();
        private int _hoveredPointIndex = -1;
        private Panel pnlLegendWrapper = new Panel();
        private FlowLayoutPanel pnlLegendFlow = new FlowLayoutPanel();

        // Grafik açılış (saat 12'den başlayıp bir tur dönerek beliren) animasyonu
        private Panel pnlChartReveal = new Panel();
        private System.Windows.Forms.Timer _revealTimer = new System.Windows.Forms.Timer();
        private System.Diagnostics.Stopwatch _revealStopwatch = new System.Diagnostics.Stopwatch();
        private Bitmap? _chartSnapshot;
        private float _revealSweep;
        private const int RevealDurationMs = 700;

        // İlk açılışta chart, gerçek (dock edilmiş) boyutuna ulaşmadan önce geçici bir boyutla
        // bir-iki kez daha yeniden boyutlanıyor (bkz. pnlLeft'teki not). Animasyonu o geçici boyutla
        // yakalayıp sonra aniden "zıplamaması" için, her Resize'da bu bekleme sayacını sıfırlıyoruz;
        // sayaç kesintisiz doluyorsa boyut artık oturmuş demektir ve o zaman görüntü yakalanır.
        private System.Windows.Forms.Timer _revealSettleTimer = new System.Windows.Forms.Timer { Interval = 60 };

        public ReportControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            chart.Resize += (s, e) => RequestChartReveal();
            _revealSettleTimer.Tick += (s, e) => { _revealSettleTimer.Stop(); StartChartRevealAnimation(); };
            LoadReport();
            this.Disposed += (s, e) => { _chartSnapshot?.Dispose(); _revealTimer.Dispose(); _revealSettleTimer.Dispose(); };
        }

        private void RequestChartReveal()
        {
            _revealSettleTimer.Stop();
            _revealSettleTimer.Start();
        }

        public void RefreshData()
        {
            LoadReport();
        }
        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.Size = new Size(1400, 800);
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            // --- Sol taraf: başlık + grafik, kalan alanı otomatik dolduruyor ---
            // Size, pnlLeft henüz bu.Controls'e eklenmeden (yani Dock=Fill henüz uygulanmadan) chart'a
            // Bottom-dock'lu legend şeridiyle birlikte 0/negatif yükseklik verilmesini (Chart control'ün
            // OnResize'da attığı sert bir ArgumentException) önlemek için geçici olarak makul bir başlangıç
            // boyutu veriyoruz.
            Panel pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                Size = new Size(900, 700),
                BackColor = AppBackColor,
                Padding = new Padding(20, 70, 20, 20)
            };

            Label lblTitle = new Label
            {
                Text = "Rapor",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 15,
                AutoSize = true
            };

            chart.Size = new Size(600, 500);
            chart.Dock = DockStyle.Fill;
            chart.BackColor = AppBackColor;
            chart.MouseMove += Chart_MouseMove;
            chart.MouseLeave += (s, e) => ClearHover();
            chart.MouseClick += Chart_MouseClick;

            ChartArea chartArea = new ChartArea("main");
            chartArea.BackColor = AppBackColor;
            chart.ChartAreas.Add(chartArea);

            // Yüzdelik dağılımı, dilimlerin altında değil; grafiğin altında tek renk başına
            // bir kutucuk halinde, ortalanmış özel bir şerit olarak gösteriyoruz (bkz. BuildLegend).
            pnlLegendWrapper.Height = 40;
            pnlLegendWrapper.Dock = DockStyle.Bottom;
            pnlLegendWrapper.BackColor = AppBackColor;
            pnlLegendFlow.FlowDirection = FlowDirection.LeftToRight;
            pnlLegendFlow.AutoSize = true;
            pnlLegendFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlLegendFlow.WrapContents = false;
            pnlLegendFlow.BackColor = AppBackColor;
            pnlLegendFlow.Top = 8;
            pnlLegendWrapper.Controls.Add(pnlLegendFlow);
            pnlLegendWrapper.Resize += (s, e) => CenterLegend();

            // Grafiğin üstünü kaplayan, açılış animasyonu sırasında chart'ın anlık görüntüsünü
            // saat 12'den başlayan bir dilim maskesiyle kademeli olarak ortaya çıkaran katman.
            // Çift arabellek (double buffer) olmadan her kare doğrudan ekrana çizildiği için
            // titreme/kasma oluyordu — bu yüzden burada da diğer canlı çizilen paneller gibi etkinleştiriyoruz.
            pnlChartReveal.Dock = DockStyle.Fill;
            pnlChartReveal.BackColor = AppBackColor;
            pnlChartReveal.Visible = false;
            pnlChartReveal.Paint += PnlChartReveal_Paint;
            EnableDoubleBuffering(pnlChartReveal);

            _revealTimer.Interval = 16;
            _revealTimer.Tick += RevealTimer_Tick;

            pnlLeft.Controls.Add(chart);
            pnlLeft.Controls.Add(pnlChartReveal);
            pnlLeft.Controls.Add(pnlLegendWrapper);
            pnlLeft.Controls.Add(lblTitle);

            // --- Sağ taraf: tarih + özet kartlar, sabit genişlikte, hep sağda kalır ---
            Panel pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 460,
                BackColor = AppBackColor,
                Padding = new Padding(20, 15, 20, 20)
            };

            string[] months = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
            cmbMonth.Items.AddRange(months);
            cmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            cmbMonth.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMonth.Left = 0;
            cmbMonth.Top = 0;
            cmbMonth.Width = 130;

            nudYear.Minimum = 2000;
            nudYear.Maximum = 2100;
            nudYear.Value = DateTime.Today.Year;
            nudYear.Left = 140;
            nudYear.Top = 0;
            nudYear.Width = 90;

            btnView.Text = "Görüntüle";
            btnView.Left = 240;
            btnView.Top = -2;
            btnView.Width = 110;
            btnView.Height = 30;
            btnView.Cursor = Cursors.Hand;
            btnView.Click += (s, e) => LoadReport();
            SetupFilledButton(btnView, AccentColor, Color.White);

            Panel cardIncome = CreateSummaryCard("Toplam Gelir", 0, 50, IncomeColor, lblIncome);
            Panel cardExpense = CreateSummaryCard("Toplam Gider", 210, 50, ExpenseColor, lblExpense);
            Panel cardNet = CreateSummaryCard("Net Bakiye", 0, 155, TextLight, lblNet);
            Panel cardWallet = CreateSummaryCard("Cüzdan", 210, 155, WalletColor, lblWalletBalance);
            Panel cardSafe = CreateSummaryCard("Kasa", 0, 260, SafeColor, lblSafeBalance);
            
            btnExportReport.Text = "Raporu CSV'ye Aktar";
            btnExportReport.Left = 0;
            btnExportReport.Top = 360;
            btnExportReport.Width = 405;
            btnExportReport.Height = 34;
            btnExportReport.Cursor = Cursors.Hand;
            btnExportReport.Click += BtnExportReport_Click;
            SetupOutlinedButton(btnExportReport, TextMuted, TextLight);

            pnlRight.Controls.Add(cmbMonth);
            pnlRight.Controls.Add(nudYear);
            pnlRight.Controls.Add(btnView);
            pnlRight.Controls.Add(cardIncome);
            pnlRight.Controls.Add(cardExpense);
            pnlRight.Controls.Add(cardNet);
            pnlRight.Controls.Add(cardWallet);
            pnlRight.Controls.Add(cardSafe);
            pnlRight.Controls.Add(btnExportReport);

            // Sıra önemli: önce Fill (sol), sonra Right (sağ) — böylece sağ blok sabit genişliğini korur, sol kalanı doldurur
            this.Controls.Add(pnlLeft);
            this.Controls.Add(pnlRight);
        }

        private Panel CreateSummaryCard(string title, int left, int top, Color valueColor, Label valueLabel)
        {
            Panel card = new Panel { Left = left, Top = top, Width = 195, Height = 90, BackColor = AppBackColor };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(card.Parent?.BackColor ?? AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
                using var brush = new SolidBrush(CardBackColor);
                e.Graphics.FillPath(brush, path);
            };

            Label lblCardTitle = new Label
            {
                Text = title,
                Left = 15,
                Top = 14,
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent
            };

            valueLabel.Left = 15;
            valueLabel.Top = 40;
            valueLabel.AutoSize = true;
            valueLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            valueLabel.ForeColor = valueColor;
            valueLabel.BackColor = Color.Transparent;
            EnableDoubleBuffering(valueLabel);

            card.Controls.Add(lblCardTitle);
            card.Controls.Add(valueLabel);

            return card;
        }

        // Kart tutarını 0'dan gerçek değerine sayarak (count-up) belirtir; tutarlar gizliyse animasyonsuz gösterir.
        private void AnimateCardValue(Label label, decimal targetValue, string suffix = " ₺")
        {
            if (_user.HideAmountsEnabled)
            {
                label.Text = "••••••";
                return;
            }

            var tr = new System.Globalization.CultureInfo("tr-TR");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var timer = new System.Windows.Forms.Timer { Interval = 16 };
            const int durationMs = 800;

            timer.Tick += (s, e) =>
            {
                if (label.IsDisposed) { timer.Stop(); timer.Dispose(); return; }

                double t = sw.Elapsed.TotalMilliseconds / durationMs;
                bool finished = t >= 1.0;
                if (finished) t = 1.0;

                double eased = 1 - Math.Pow(1 - t, 3);
                decimal shown = finished ? targetValue : Math.Round(targetValue * (decimal)eased);
                label.Text = shown.ToString("#,##0", tr) + suffix;

                if (finished)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        private void LoadReport()
        {
            int year = (int)nudYear.Value;
            int month = cmbMonth.SelectedIndex + 1;

            var current = _reportService.GenerateMonthlyReport(_user.Id, year, month);
            _currentReport = current;
            var tr = new System.Globalization.CultureInfo("tr-TR");

            var (wallet, safe) = _accountService.GetBalances(_user.Id);

            AnimateCardValue(lblIncome, current.TotalIncome);
            AnimateCardValue(lblExpense, current.TotalExpense);
            AnimateCardValue(lblNet, current.NetBalance);
            AnimateCardValue(lblWalletBalance, wallet);
            AnimateCardValue(lblSafeBalance, safe);

            _hoveredPointIndex = -1;
            chart.Series.Clear();
            chart.Annotations.Clear();

            Series series = new Series("İşlemler") { ChartType = SeriesChartType.Doughnut };
            series["PieLabelStyle"] = "Outside";
            series["PieLineColor"] = $"{TextMuted.R},{TextMuted.G},{TextMuted.B}";
            series["DoughnutRadius"] = "62";
            series["PieDrawingStyle"] = "SoftEdge";
            series["PieStartAngle"] = "270";
            series.Label = "#VALX #PERCENT{P0}";
            series.Font = new Font("Segoe UI", 9F);
            series.LabelForeColor = TextLight;

            decimal categorySum = current.IncomeBreakdown.Sum(x => x.TotalAmount) + current.ExpenseBreakdown.Sum(x => x.TotalAmount);
            decimal idle = wallet - categorySum;
            if (idle < 0) idle = 0;
            decimal pieTotal = categorySum + idle;

            // Yüzdesi çok küçük dilimler (etiketleri dip dibe binen) tek tek gösterilmez;
            // aynı renkteki (gelir/gider) küçük kategoriler "Diğer" adıyla tek dilimde birleştirilir.
            decimal smallSliceThreshold = pieTotal * 0.04m;

            void AddPoint(string name, decimal amount, Color color, List<(string Name, decimal Amount)>? details = null)
            {
                int index = series.Points.AddXY(name, amount);
                series.Points[index].Color = color;
                series.Points[index].BorderColor = SliceBorderColor;
                series.Points[index].BorderWidth = 2;
                if (details != null) series.Points[index].Tag = details;
            }

            void AddGroupedPoints(IEnumerable<(string Name, decimal Amount)> items, Color color)
            {
                var large = items.Where(i => i.Amount >= smallSliceThreshold).ToList();
                var small = items.Where(i => i.Amount < smallSliceThreshold).ToList();

                foreach (var item in large)
                    AddPoint(item.Name, item.Amount, color);

                if (small.Count == 1)
                    AddPoint(small[0].Name, small[0].Amount, color);
                else if (small.Count > 1)
                    AddPoint("Diğer", small.Sum(i => i.Amount), color, small);
            }

            AddGroupedPoints(current.IncomeBreakdown.Select(x => (x.CategoryName, x.TotalAmount)), IncomeColor);
            AddGroupedPoints(current.ExpenseBreakdown.Select(x => (x.CategoryName, x.TotalAmount)), ExpenseColor);

            if (idle > 0 || series.Points.Count == 0)
            {
                AddPoint("Boşta", idle, IdleColor);
            }

            series.ChartArea = "main";
            chart.Series.Add(series);

            BuildLegend(current, idle, pieTotal, tr);

            // Animasyonu hemen değil, boyut oturana kadar erteleyerek başlatıyoruz (bkz. RequestChartReveal).
            RequestChartReveal();
        }

        // Grafiği saat 12 konumundan başlayıp tam tur atarak kademeli ortaya çıkarır.
        private void StartChartRevealAnimation()
        {
            if (chart.Width <= 0 || chart.Height <= 0) return;

            _revealTimer.Stop();
            _chartSnapshot?.Dispose();
            _chartSnapshot = new Bitmap(chart.Width, chart.Height);
            chart.DrawToBitmap(_chartSnapshot, new Rectangle(0, 0, chart.Width, chart.Height));

            _revealSweep = 0f;
            chart.Visible = false;
            pnlChartReveal.Visible = true;
            pnlChartReveal.Invalidate();

            _revealStopwatch.Restart();
            _revealTimer.Start();
        }

        private void RevealTimer_Tick(object? sender, EventArgs e)
        {
            double t = _revealStopwatch.Elapsed.TotalMilliseconds / RevealDurationMs;
            bool finished = t >= 1.0;
            if (finished) t = 1.0;

            double eased = 1 - Math.Pow(1 - t, 3); // ease-out: hızlı başlar, yumuşak biter
            _revealSweep = (float)(eased * 360.0);
            pnlChartReveal.Invalidate();

            if (finished)
            {
                _revealTimer.Stop();
                chart.Visible = true;
                pnlChartReveal.Visible = false;
            }
        }

        private void PnlChartReveal_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(AppBackColor);
            if (_chartSnapshot == null || _revealSweep <= 0.01f) return;

            var rect = new Rectangle(0, 0, pnlChartReveal.Width, pnlChartReveal.Height);
            float diag = (float)Math.Sqrt((double)rect.Width * rect.Width + (double)rect.Height * rect.Height) * 1.2f;
            var pieRect = new RectangleF(rect.Width / 2f - diag / 2f, rect.Height / 2f - diag / 2f, diag, diag);

            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddPie(pieRect.X, pieRect.Y, pieRect.Width, pieRect.Height, -90f, Math.Min(_revealSweep, 359.9f));

            var oldClip = e.Graphics.Clip;
            e.Graphics.SetClip(path);
            e.Graphics.DrawImage(_chartSnapshot, rect);
            e.Graphics.Clip = oldClip;
        }

        // Dilimlerin altında kategori kategori değil; her renk için TEK bir kutucukta
        // o rengin toplam yüzdesini gösteren, grafiğin altında ortalanmış özel bir şerit.
        private void BuildLegend(MonthlyReport current, decimal idle, decimal pieTotal, System.Globalization.CultureInfo tr)
        {
            pnlLegendFlow.Controls.Clear();
            if (pieTotal <= 0) return;

            decimal incomeTotal = current.IncomeBreakdown.Sum(x => x.TotalAmount);
            decimal expenseTotal = current.ExpenseBreakdown.Sum(x => x.TotalAmount);

            if (incomeTotal > 0) AddLegendEntry(IncomeColor, "Gelir", incomeTotal, pieTotal);
            if (expenseTotal > 0) AddLegendEntry(ExpenseColor, "Gider", expenseTotal, pieTotal);
            if (idle > 0) AddLegendEntry(IdleColor, "Boşta", idle, pieTotal);

            CenterLegend();
        }

        private void AddLegendEntry(Color color, string label, decimal amount, decimal pieTotal)
        {
            int percent = (int)Math.Round(amount / pieTotal * 100);
            Font legendFont = new Font("Segoe UI", 9.5F);

            const int dotSize = 10;
            Size textSize = TextRenderer.MeasureText($"{label} %{percent}", legendFont);
            int dotTopMargin = Math.Max(0, (textSize.Height - dotSize) / 2);

            Panel dot = new Panel { Width = dotSize, Height = dotSize, Margin = new Padding(4, dotTopMargin, 4, 0) };
            dot.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(color);
                e.Graphics.FillEllipse(brush, 0, 0, dot.Width - 1, dot.Height - 1);
            };

            Label lbl = new Label
            {
                Text = $"{label} %{percent}",
                AutoSize = true,
                ForeColor = TextLight,
                Font = legendFont,
                Margin = new Padding(0, 0, 18, 0)
            };

            pnlLegendFlow.Controls.Add(dot);
            pnlLegendFlow.Controls.Add(lbl);
        }

        private void CenterLegend()
        {
            pnlLegendFlow.Left = Math.Max(0, (pnlLegendWrapper.Width - pnlLegendFlow.PreferredSize.Width) / 2);
        }

        private void Chart_MouseMove(object? sender, MouseEventArgs e)
        {
            var result = chart.HitTest(e.X, e.Y);
            int newIndex = (result.ChartElementType == ChartElementType.DataPoint && result.Series != null) ? result.PointIndex : -1;

            if (newIndex == _hoveredPointIndex) return;

            var series = chart.Series.FirstOrDefault();
            if (series == null) return;

            if (_hoveredPointIndex >= 0 && _hoveredPointIndex < series.Points.Count)
            {
                series.Points[_hoveredPointIndex].BorderColor = SliceBorderColor;
                series.Points[_hoveredPointIndex].BorderWidth = 2;
            }

            if (newIndex >= 0 && newIndex < series.Points.Count)
            {
                series.Points[newIndex].BorderColor = Color.White;
                series.Points[newIndex].BorderWidth = 4;
            }

            _hoveredPointIndex = newIndex;
        }

        private void ClearHover()
        {
            var series = chart.Series.FirstOrDefault();
            if (series != null && _hoveredPointIndex >= 0 && _hoveredPointIndex < series.Points.Count)
            {
                series.Points[_hoveredPointIndex].BorderColor = SliceBorderColor;
                series.Points[_hoveredPointIndex].BorderWidth = 2;
            }
            _hoveredPointIndex = -1;
        }

        private void Chart_MouseClick(object? sender, MouseEventArgs e)
        {
            var result = chart.HitTest(e.X, e.Y);

            if (result.ChartElementType == ChartElementType.DataPoint && result.Series != null && result.PointIndex >= 0)
            {
                var point = result.Series.Points[result.PointIndex];
                double value = point.YValues[0];
                var tr = new System.Globalization.CultureInfo("tr-TR");

                string amountText = _user.HideAmountsEnabled ? "••••••" : value.ToString("#,##0", tr) + " ₺";

                if (point.Tag is List<(string Name, decimal Amount)> details && details.Count > 0)
                {
                    var lines = details
                        .OrderByDescending(d => d.Amount)
                        .Select(d => $"   •  {d.Name}: {(_user.HideAmountsEnabled ? "••••••" : d.Amount.ToString("#,##0", tr) + " ₺")}");
                    MessageBox.Show($"{point.AxisLabel} — Toplam: {amountText}\n\n{string.Join("\n", lines)}", "Kategori Bilgisi");
                }
                else
                {
                    MessageBox.Show($"{point.AxisLabel}\nTutar: {amountText}", "Kategori Bilgisi");
                }
            }
        }

        private void BtnExportReport_Click(object? sender, EventArgs e)
        {
            if (_currentReport == null) return;

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV Dosyası (*.csv)|*.csv";
                dialog.FileName = $"rapor_{_currentReport.Year}_{_currentReport.Month:00}.csv";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var tr = new System.Globalization.CultureInfo("tr-TR");

                        using (var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8))
                        {
                            writer.WriteLine("Tip;Kategori;Tutar");

                            foreach (var item in _currentReport.IncomeBreakdown)
                            {
                                writer.WriteLine($"Gelir;{item.CategoryName};{item.TotalAmount.ToString("0.00", tr)}");
                            }
                            foreach (var item in _currentReport.ExpenseBreakdown)
                            {
                                writer.WriteLine($"Gider;{item.CategoryName};{item.TotalAmount.ToString("0.00", tr)}");
                            }

                            writer.WriteLine();
                            writer.WriteLine($"Toplam Gelir;;{_currentReport.TotalIncome.ToString("0.00", tr)}");
                            writer.WriteLine($"Toplam Gider;;{_currentReport.TotalExpense.ToString("0.00", tr)}");
                            writer.WriteLine($"Net Bakiye;;{_currentReport.NetBalance.ToString("0.00", tr)}");
                        }

                        MessageBox.Show("Rapor CSV olarak dışa aktarıldı.", "Bilgi");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Dışa aktarma başarısız: {ex.Message}", "Hata");
                    }
                }
            }
        }

        private void SetupFilledButton(Button btn, Color bgColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;

            bool isHovered = false;
            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);

                using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                using (var brush = new SolidBrush(isHovered ? ControlPaint.Light(bgColor) : bgColor))
                    e.Graphics.FillPath(brush, path);

                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private void SetupOutlinedButton(Button btn, Color borderColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;

            bool isHovered = false;
            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);

                using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                using (var pen = new Pen(isHovered ? TextLight : borderColor, 1.2f))
                    e.Graphics.DrawPath(pen, path);

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

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                control,
                new object[] { true });
        }
    }
}