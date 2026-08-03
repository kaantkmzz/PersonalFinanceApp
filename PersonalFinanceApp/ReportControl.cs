using System.Windows.Forms.DataVisualization.Charting;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class ReportControl : UserControl
    {
        private readonly User _user;
        private readonly ReportService _reportService = new ReportService();
        private readonly AccountService _accountService = new AccountService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color IncomeColor = Color.FromArgb(60, 180, 110);
        private static readonly Color ExpenseColor = Color.FromArgb(230, 100, 100);
        private static readonly Color WalletColor = Color.FromArgb(120, 220, 150);
        private static readonly Color SafeColor = Color.FromArgb(120, 180, 255);
        private static readonly Color IdleColor = Color.FromArgb(230, 200, 80);
        private static readonly Color SliceBorderColor = Color.FromArgb(24, 26, 38);

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
            this.Size = new Size(1400, 800);
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            // --- Sol taraf: başlık + grafik, kalan alanı otomatik dolduruyor ---
            Panel pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
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

            Legend legend = new Legend("legend") { BackColor = AppBackColor, ForeColor = TextLight, Docking = Docking.Bottom };
            chart.Legends.Add(legend);

            pnlLeft.Controls.Add(chart);
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
            btnView.FlatStyle = FlatStyle.Flat;
            btnView.FlatAppearance.BorderSize = 0;
            btnView.BackColor = AccentColor;
            btnView.ForeColor = Color.White;
            btnView.Cursor = Cursors.Hand;
            btnView.Click += (s, e) => LoadReport();

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
            btnExportReport.FlatStyle = FlatStyle.Flat;
            btnExportReport.FlatAppearance.BorderSize = 1;
            btnExportReport.FlatAppearance.BorderColor = TextMuted;
            btnExportReport.BackColor = AppBackColor;
            btnExportReport.ForeColor = TextLight;
            btnExportReport.Cursor = Cursors.Hand;
            btnExportReport.Click += BtnExportReport_Click;

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
            Panel card = new Panel { Left = left, Top = top, Width = 195, Height = 90, BackColor = CardBackColor };

            Label lblCardTitle = new Label
            {
                Text = title,
                Left = 15,
                Top = 14,
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9F)
            };

            valueLabel.Left = 15;
            valueLabel.Top = 40;
            valueLabel.AutoSize = true;
            valueLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            valueLabel.ForeColor = valueColor;

            card.Controls.Add(lblCardTitle);
            card.Controls.Add(valueLabel);

            return card;
        }

        private void LoadReport()
        {
            int year = (int)nudYear.Value;
            int month = cmbMonth.SelectedIndex + 1;

            var current = _reportService.GenerateMonthlyReport(_user.Id, year, month);
            _currentReport = current;
            var tr = new System.Globalization.CultureInfo("tr-TR");

            lblIncome.Text = current.TotalIncome.ToString("#,##0", tr) + " ₺";
            lblExpense.Text = current.TotalExpense.ToString("#,##0", tr) + " ₺";
            lblNet.Text = current.NetBalance.ToString("#,##0", tr) + " ₺";

            var (wallet, safe) = _accountService.GetBalances(_user.Id);
            lblWalletBalance.Text = wallet.ToString("#,##0", tr) + " ₺";
            lblSafeBalance.Text = safe.ToString("#,##0", tr) + " ₺";

            _hoveredPointIndex = -1;
            chart.Series.Clear();

            Series series = new Series("İşlemler") { ChartType = SeriesChartType.Pie };
            series["PieLabelStyle"] = "Inside";
            series.Label = "#PERCENT{P0}";
            series.LabelForeColor = Color.White;

            decimal categorySum = 0;

            foreach (var item in current.IncomeBreakdown)
            {
                int index = series.Points.AddXY(item.CategoryName, item.TotalAmount);
                series.Points[index].Color = IncomeColor;
                series.Points[index].BorderColor = SliceBorderColor;
                series.Points[index].BorderWidth = 2;
                categorySum += item.TotalAmount;
            }

            foreach (var item in current.ExpenseBreakdown)
            {
                int index = series.Points.AddXY(item.CategoryName, item.TotalAmount);
                series.Points[index].Color = ExpenseColor;
                series.Points[index].BorderColor = SliceBorderColor;
                series.Points[index].BorderWidth = 2;
                categorySum += item.TotalAmount;
            }

            decimal idle = wallet - categorySum;
            if (idle < 0) idle = 0;

            if (idle > 0 || series.Points.Count == 0)
            {
                int idleIndex = series.Points.AddXY("Boşta", idle);
                series.Points[idleIndex].Color = IdleColor;
                series.Points[idleIndex].BorderColor = SliceBorderColor;
                series.Points[idleIndex].BorderWidth = 2;
            }

            series.ChartArea = "main";
            series.Legend = "legend";
            chart.Series.Add(series);
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

                MessageBox.Show(
                    $"{point.AxisLabel}\nTutar: {value.ToString("#,##0", tr)} ₺",
                    "Kategori Bilgisi");
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
    }
}