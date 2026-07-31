using System.Windows.Forms.DataVisualization.Charting;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class ReportControl : UserControl
    {
        private readonly User _user;
        private readonly ReportService _reportService = new ReportService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color IncomeColor = Color.FromArgb(60, 180, 110);
        private static readonly Color ExpenseColor = Color.FromArgb(230, 100, 100);

        private ComboBox cmbMonth = new ComboBox();
        private NumericUpDown nudYear = new NumericUpDown();
        private Button btnView = new Button();

        private Label lblIncome = new Label();
        private Label lblExpense = new Label();
        private Label lblNet = new Label();
        private Label lblTopCategory = new Label();

        private FlowLayoutPanel pnlMessages = new FlowLayoutPanel();
        private Chart chart = new Chart();

        public ReportControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadReport();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            Label lblTitle = new Label
            {
                Text = "Aylık Rapor",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 15,
                AutoSize = true
            };

            string[] months = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
            cmbMonth.Items.AddRange(months);
            cmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            cmbMonth.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMonth.Left = 20;
            cmbMonth.Top = 70;
            cmbMonth.Width = 140;

            nudYear.Minimum = 2000;
            nudYear.Maximum = 2100;
            nudYear.Value = DateTime.Today.Year;
            nudYear.Left = 170;
            nudYear.Top = 70;
            nudYear.Width = 90;

            btnView.Text = "Görüntüle";
            btnView.Left = 280;
            btnView.Top = 68;
            btnView.Width = 120;
            btnView.Height = 30;
            btnView.FlatStyle = FlatStyle.Flat;
            btnView.FlatAppearance.BorderSize = 0;
            btnView.BackColor = AccentColor;
            btnView.ForeColor = Color.White;
            btnView.Cursor = Cursors.Hand;
            btnView.Click += (s, e) => LoadReport();

            Panel pnlSummary = new Panel { Left = 20, Top = 115, Width = 500, Height = 110, BackColor = CardBackColor };
            lblIncome.Left = 20; lblIncome.Top = 15; lblIncome.AutoSize = true; lblIncome.ForeColor = IncomeColor; lblIncome.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblExpense.Left = 20; lblExpense.Top = 45; lblExpense.AutoSize = true; lblExpense.ForeColor = ExpenseColor; lblExpense.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblNet.Left = 20; lblNet.Top = 75; lblNet.AutoSize = true; lblNet.ForeColor = TextLight; lblNet.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblTopCategory.Left = 260; lblTopCategory.Top = 15; lblTopCategory.AutoSize = true; lblTopCategory.ForeColor = TextMuted; lblTopCategory.Font = new Font("Segoe UI", 9.5F); lblTopCategory.MaximumSize = new Size(220, 0);
            pnlSummary.Controls.Add(lblIncome);
            pnlSummary.Controls.Add(lblExpense);
            pnlSummary.Controls.Add(lblNet);
            pnlSummary.Controls.Add(lblTopCategory);

            Label lblMessagesTitle = new Label { Text = "Geçen Aya Göre Değişimler", Left = 20, Top = 240, AutoSize = true, ForeColor = TextLight, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            pnlMessages.Left = 20;
            pnlMessages.Top = 270;
            pnlMessages.Width = 500;
            pnlMessages.Height = 220;
            pnlMessages.FlowDirection = FlowDirection.TopDown;
            pnlMessages.WrapContents = false;
            pnlMessages.AutoScroll = true;
            pnlMessages.BackColor = AppBackColor;

            chart.Left = 540;
            chart.Top = 70;
            chart.Width = 420;
            chart.Height = 420;
            chart.BackColor = AppBackColor;

            ChartArea chartArea = new ChartArea("main");
            chartArea.BackColor = AppBackColor;
            chart.ChartAreas.Add(chartArea);

            Legend legend = new Legend("legend") { BackColor = AppBackColor, ForeColor = TextLight, Docking = Docking.Bottom };
            chart.Legends.Add(legend);

            this.Controls.Add(lblTitle);
            this.Controls.Add(cmbMonth);
            this.Controls.Add(nudYear);
            this.Controls.Add(btnView);
            this.Controls.Add(pnlSummary);
            this.Controls.Add(lblMessagesTitle);
            this.Controls.Add(pnlMessages);
            this.Controls.Add(chart);
        }

        private void LoadReport()
        {
            int year = (int)nudYear.Value;
            int month = cmbMonth.SelectedIndex + 1;

            var current = _reportService.GenerateMonthlyReport(_user.Id, year, month);
            var previous = _reportService.GetPreviousMonthReport(_user.Id, year, month);

            var tr = new System.Globalization.CultureInfo("tr-TR");

            lblIncome.Text = $"Toplam Gelir: {current.TotalIncome.ToString("#,##0", tr)} ₺";
            lblExpense.Text = $"Toplam Gider: {current.TotalExpense.ToString("#,##0", tr)} ₺";
            lblNet.Text = $"Net Bakiye: {current.NetBalance.ToString("#,##0", tr)} ₺";

            var topCategory = _reportService.GetTopExpenseCategory(current);
            if (topCategory != null && current.TotalExpense > 0)
            {
                double pct = (double)(topCategory.TotalAmount / current.TotalExpense) * 100;
                lblTopCategory.Text = $"En çok harcanan:\n{topCategory.CategoryName} (%{pct:0.0})";
            }
            else
            {
                lblTopCategory.Text = "Bu ay gider kaydı yok.";
            }

            pnlMessages.Controls.Clear();
            var messages = _reportService.GetComparisonMessages(current, previous);

            if (messages.Count == 0)
            {
                pnlMessages.Controls.Add(new Label
                {
                    Text = "Anlamlı bir değişiklik tespit edilmedi.",
                    ForeColor = TextMuted,
                    AutoSize = true
                });
            }
            else
            {
                foreach (var msg in messages)
                {
                    pnlMessages.Controls.Add(new Label
                    {
                        Text = "• " + msg,
                        ForeColor = TextMuted,
                        AutoSize = true,
                        MaximumSize = new Size(480, 0),
                        Margin = new Padding(0, 0, 0, 8)
                    });
                }
            }

            chart.Series.Clear();
            if (current.ExpenseBreakdown.Count > 0)
            {
                Series series = new Series("Giderler") { ChartType = SeriesChartType.Pie };
                foreach (var item in current.ExpenseBreakdown)
                {
                    series.Points.AddXY(item.CategoryName, item.TotalAmount);
                }
                series["PieLabelStyle"] = "Outside";
                series.ChartArea = "main";
                series.Legend = "legend";
                chart.Series.Add(series);
            }
        }
    }
}