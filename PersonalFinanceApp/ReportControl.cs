using System.Windows.Forms.DataVisualization.Charting;
using PersonalFinanceApp.Helpers;
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
        private static Color SafeColor => AppTheme.SafeColor;
        private static Color IdleColor => AppTheme.IdleColor;
        private static Color GoalColor => AppTheme.GoalColor;
        private static Color SliceBorderColor => AppTheme.SliceBorderColor;
        // Dilim/lejant üzerine gelindiğinde vurgu çerçevesi: dark modda beyaz, light modda (beyaz
        // arka planla karışmaması için) koyu renk.
        private static Color HoverHighlightColor => AppTheme.TextLight;

        private ComboBox cmbMonth = new ComboBox();
        private NumericUpDown nudYear = new NumericUpDown();
        private Button btnView = new Button();
        private Button btnExportReport = new Button();
        private MonthlyReport? _currentReport;
        private decimal _currentWallet;
        private decimal _currentSafe;

        // null = birleşik (gelir+gider+hedef+boşta) görünüm; "income"/"expense"/"goal" = o türün kendi kırılımı
        private string? _drillDownType;

        private Label lblTitle = new Label();
        private Label lblIncome = new Label();
        private Label lblExpense = new Label();
        private Label lblNet = new Label();
        private Label lblSafeBalance = new Label();
        private readonly Dictionary<Label, System.Windows.Forms.Timer> _cardAnimTimers = new Dictionary<Label, System.Windows.Forms.Timer>();

        private Chart chart = new Chart();
        private int _hoveredPointIndex = -1;
        private Panel pnlLegendWrapper = new Panel();
        private FlowLayoutPanel pnlLegendFlow = new FlowLayoutPanel();
        // Geri dönüş oku artık pnlLegendFlow'un İÇİNDE (sarma akışının bir parçası) değil; ayrı, sabit
        // konumlu bir kontrol. Böylece kategori öğeleri birden fazla satıra taştığında (ör. tek başına
        // kalan küçük bir yüzdelik), her satır okun genişliğinden etkilenmeden aynı X'te başlar.
        private Panel? _backButton;

        // Lejant öğesinin (Gelir/Gider/Hedef/Boşta ya da kırılımdaki tek bir kategori) üzerine gelindiğinde
        // grafikte hangi dilim(ler)in beyaz çizgiyle vurgulanacağını tutar (bkz. BuildChart, AddLegendEntry).
        private readonly Dictionary<string, List<int>> _typeToPointIndices = new Dictionary<string, List<int>>();
        private int _idlePointIndex = -1;
        private readonly HashSet<int> _legendHoverIndices = new HashSet<int>();

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
            this.Disposed += (s, e) =>
            {
                _chartSnapshot?.Dispose();
                _revealTimer.Dispose();
                _revealSettleTimer.Dispose();
                foreach (var t in _cardAnimTimers.Values) { t.Stop(); t.Dispose(); }
                _cardAnimTimers.Clear();
            };
        }

        private void RequestChartReveal()
        {
            _revealSettleTimer.Stop();
            _revealSettleTimer.Start();
        }

        // Dışarıdan tetiklenen bir tazeleme (ör. tutarları gizle aç/kapa) verileri yeniden çeksin
        // ama kullanıcının o an incelediği kırılım (drill-down) görünümünü sıfırlamasın.
        public void RefreshData()
        {
            LoadReport(resetDrillDown: false);
        }

        // Tema değişikliğinde ekran tamamen yeniden kurulduğu için kırılım durumu MainForm
        // tarafından yakalanıp yeni örneğe geri uygulanır (bkz. MainForm.RebuildForThemeChange).
        public string? DrillDownType => _drillDownType;

        public void RestoreDrillDownType(string? type)
        {
            _drillDownType = type;
            RenderReport();
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

            lblTitle.Text = "Rapor";
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = TextLight;
            lblTitle.Left = 20;
            lblTitle.Top = 15;
            lblTitle.AutoSize = true;

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
            pnlLegendWrapper.Height = 76;
            pnlLegendWrapper.Dock = DockStyle.Bottom;
            pnlLegendWrapper.BackColor = AppBackColor;
            pnlLegendFlow.FlowDirection = FlowDirection.LeftToRight;
            pnlLegendFlow.AutoSize = true;
            pnlLegendFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlLegendFlow.WrapContents = true;
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

            // --- Sağ taraf: periyot seçimi + özet kartlar, sabit genişlikte, hep sağda kalır ---
            Panel pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 460,
                BackColor = AppBackColor,
                Padding = new Padding(20, 15, 20, 20)
            };

            Label lblMonth = new Label { Text = "Ay:", Left = 0, Top = 0, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlMonth = new Panel { Left = 0, Top = 24, Width = 195, Height = 40 };
            cmbMonth.Left = 5; cmbMonth.Top = 5; cmbMonth.Width = 190;
            cmbMonth.Font = new Font("Segoe UI", 9.5F); cmbMonth.DropDownStyle = ComboBoxStyle.DropDownList;
            var trMonths = new System.Globalization.CultureInfo("tr-TR");
            for (int m = 1; m <= 12; m++) cmbMonth.Items.Add(trMonths.DateTimeFormat.GetMonthName(m));
            cmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            pnlMonth.Controls.Add(cmbMonth);
            SetupCustomComboBox(pnlMonth, cmbMonth);

            Label lblYear = new Label { Text = "Yıl:", Left = 210, Top = 0, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlYear = new Panel { Left = 210, Top = 24, Width = 195, Height = 40 };
            SetupSmoothContainer(pnlYear, 8, CardBackColor);
            nudYear.Left = 10; nudYear.Top = 8; nudYear.Width = 175;
            nudYear.Font = new Font("Segoe UI", 9.5F); nudYear.BorderStyle = BorderStyle.None;
            nudYear.BackColor = CardBackColor; nudYear.ForeColor = TextLight;
            nudYear.Minimum = 2000; nudYear.Maximum = 2100; nudYear.Value = DateTime.Today.Year;
            nudYear.TextAlign = HorizontalAlignment.Left;
            pnlYear.Controls.Add(nudYear);

            btnView.Text = "Görüntüle";
            btnView.Left = 0; btnView.Top = 68; btnView.Width = 405; btnView.Height = 34;
            btnView.Cursor = Cursors.Hand;
            btnView.Click += (s, e) => { _drillDownType = null; LoadReport(); };
            SetupRoundedButton(btnView, AccentColor, Color.White);

            Panel cardIncome = CreateSummaryCard("Toplam Gelir", 0, 120, IncomeColor, lblIncome);
            Panel cardExpense = CreateSummaryCard("Toplam Gider", 210, 120, ExpenseColor, lblExpense);
            Panel cardNet = CreateSummaryCard("Net Bakiye", 0, 225, TextLight, lblNet);
            Panel cardSafe = CreateSummaryCard("Kasa", 210, 225, SafeColor, lblSafeBalance);

            btnExportReport.Text = "Raporu CSV'ye Aktar";
            btnExportReport.Left = 0;
            btnExportReport.Top = 335;
            btnExportReport.Width = 405;
            btnExportReport.Height = 34;
            btnExportReport.Cursor = Cursors.Hand;
            btnExportReport.Click += BtnExportReport_Click;
            SetupOutlinedButton(btnExportReport, TextMuted, TextLight);

            pnlRight.Controls.Add(lblMonth);
            pnlRight.Controls.Add(pnlMonth);
            pnlRight.Controls.Add(lblYear);
            pnlRight.Controls.Add(pnlYear);
            pnlRight.Controls.Add(btnView);
            pnlRight.Controls.Add(cardIncome);
            pnlRight.Controls.Add(cardExpense);
            pnlRight.Controls.Add(cardNet);
            pnlRight.Controls.Add(cardSafe);
            pnlRight.Controls.Add(btnExportReport);

            // Sıra önemli: önce Fill (sol), sonra Right (sağ) — böylece sağ blok sabit genişliğini korur, sol kalanı doldurur
            this.Controls.Add(pnlLeft);
            this.Controls.Add(pnlRight);
        }

        private void SetupCustomComboBox(Panel pnl, ComboBox cmb)
        {
            SetupSmoothContainer(pnl, 8, CardBackColor);
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = CardBackColor;
            cmb.ForeColor = TextLight;

            cmb.DrawMode = DrawMode.OwnerDrawFixed;
            // ItemHeight, kapalı kutunun kendi çizim yüksekliğini de belirliyor; 22px "Ağustos" gibi
            // alt-uzantılı harf (ğ) içeren ay adlarının kesilmesine yol açıyordu, bu yüzden büyütüldü.
            cmb.ItemHeight = 26;
            cmb.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bgColor = isSelected ? AppTheme.HoverBackColor : CardBackColor;
                e.Graphics.FillRectangle(new SolidBrush(bgColor), e.Bounds);
                TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index]?.ToString() ?? string.Empty, cmb.Font, e.Bounds, TextLight, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };

            cmb.Region = new Region(new Rectangle(1, 1, cmb.Width - 2, cmb.Height - 2));

            Panel pnlArrow = new Panel();
            pnlArrow.Width = 28;
            pnlArrow.Height = cmb.Height - 2;
            pnlArrow.Left = cmb.Right - pnlArrow.Width - 1;
            pnlArrow.Top = cmb.Top + 1;
            pnlArrow.BackColor = CardBackColor;
            pnlArrow.Cursor = Cursors.Hand;

            pnlArrow.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int ax = pnlArrow.Width / 2 - 5;
                int ay = pnlArrow.Height / 2 - 2;
                using (var brush = new SolidBrush(TextMuted))
                    e.Graphics.FillPolygon(brush, new Point[] { new Point(ax, ay), new Point(ax + 10, ay), new Point(ax + 5, ay + 6) });
            };

            pnlArrow.MouseClick += (s, e) => { cmb.DroppedDown = true; };
            pnl.MouseClick += (s, e) => { cmb.DroppedDown = true; };

            pnl.Controls.Add(pnlArrow);
            pnlArrow.BringToFront();
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
            // Aynı karta ait önceki animasyon hâlâ çalışıyorsa (ör. rapor art arda hızlıca yenilendiğinde
            // ya da animasyon sürerken "tutarları gizle" açıldığında), o zamanlayıcıyı iptal etmeden yeni
            // bir tane daha başlatmak ikisinin de aynı Label'a yazmasına ve gizleme durumunun görmezden
            // gelinmesine yol açıyordu. Önce eskisini durdurup atıyoruz.
            if (_cardAnimTimers.TryGetValue(label, out var existingTimer))
            {
                existingTimer.Stop();
                existingTimer.Dispose();
                _cardAnimTimers.Remove(label);
            }

            if (_user.HideAmountsEnabled)
            {
                label.Text = "••••••";
                return;
            }

            var tr = new System.Globalization.CultureInfo("tr-TR");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var timer = new System.Windows.Forms.Timer { Interval = 16 };
            const int durationMs = 800;
            _cardAnimTimers[label] = timer;

            timer.Tick += (s, e) =>
            {
                if (label.IsDisposed || _user.HideAmountsEnabled)
                {
                    timer.Stop();
                    timer.Dispose();
                    _cardAnimTimers.Remove(label);
                    if (!label.IsDisposed && _user.HideAmountsEnabled) label.Text = "••••••";
                    return;
                }

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
                    _cardAnimTimers.Remove(label);
                }
            };
            timer.Start();
        }

        // Seçili ay/yıla ait veriyi veritabanından çeker. resetDrillDown=false olduğunda (ör. dışarıdan
        // gelen tutar gizleme tazelemesinde) kullanıcının o an incelediği kırılım görünümü korunur.
        private void LoadReport(bool resetDrillDown = true)
        {
            int year = (int)nudYear.Value;
            int month = cmbMonth.SelectedIndex + 1;
            DateTime start = new DateTime(year, month, 1);
            DateTime end = start.AddMonths(1);

            _currentReport = _reportService.GenerateReport(_user.Id, start, end);
            var (wallet, safe) = _accountService.GetBalances(_user.Id);
            _currentWallet = wallet;
            _currentSafe = safe;

            AnimateCardValue(lblIncome, _currentReport.TotalIncome);
            AnimateCardValue(lblExpense, _currentReport.TotalExpense);
            AnimateCardValue(lblNet, _currentReport.NetBalance);
            AnimateCardValue(lblSafeBalance, safe);

            if (resetDrillDown) _drillDownType = null;
            RenderReport();
        }

        // Önbellekteki (_currentReport) veriden grafiği ve lejantı yeniden çizer; yeni veri çekmez.
        // Hem ilk yüklemede hem de lejant tıklanıp birleşik/kırılım görünümü değiştiğinde kullanılır.
        private void RenderReport()
        {
            if (_currentReport == null) return;
            var tr = new System.Globalization.CultureInfo("tr-TR");
            var current = _currentReport;

            _hoveredPointIndex = -1;

            decimal categorySum = current.IncomeBreakdown.Sum(x => x.TotalAmount) + current.ExpenseBreakdown.Sum(x => x.TotalAmount);
            decimal idle = _currentWallet - categorySum;
            if (idle < 0) idle = 0;
            decimal goalTotal = current.GoalBreakdown.Sum(x => x.TotalAmount);
            decimal pieTotal = categorySum + idle + goalTotal;

            lblTitle.Text = _drillDownType switch
            {
                "income" => "Rapor — Gelir",
                "expense" => "Rapor — Gider",
                "goal" => "Rapor — Hedef",
                _ => "Rapor"
            };

            BuildChart(current, idle, pieTotal);
            BuildLegend(current, idle, goalTotal, pieTotal, tr);

            // Animasyonu hemen değil, boyut oturana kadar erteleyerek başlatıyoruz (bkz. RequestChartReveal).
            RequestChartReveal();
        }

        private void BuildChart(MonthlyReport current, decimal idle, decimal pieTotal)
        {
            chart.Series.Clear();
            chart.Annotations.Clear();
            _typeToPointIndices.Clear();
            _idlePointIndex = -1;

            Series series = new Series("İşlemler") { ChartType = SeriesChartType.Doughnut };
            series["PieLabelStyle"] = "Outside";
            series["PieLineColor"] = $"{TextMuted.R},{TextMuted.G},{TextMuted.B}";
            series["DoughnutRadius"] = "62";
            series["PieDrawingStyle"] = "SoftEdge";
            series["PieStartAngle"] = "270";
            series.Label = "#VALX #PERCENT{P0}";
            series.Font = new Font("Segoe UI", 9F);
            series.LabelForeColor = TextLight;

            int AddPoint(string name, decimal amount, Color color, List<(string Name, decimal Amount)>? details = null)
            {
                int index = series.Points.AddXY(name, amount);
                series.Points[index].Color = color;
                series.Points[index].BorderColor = SliceBorderColor;
                series.Points[index].BorderWidth = 2;
                if (details != null) series.Points[index].Tag = details;
                return index;
            }

            if (_drillDownType == null)
            {
                // Yüzdesi çok küçük dilimler (etiketleri dip dibe binen) tek tek gösterilmez;
                // aynı renkteki (gelir/gider/hedef) küçük kategoriler "Diğer" adıyla tek dilimde birleştirilir.
                decimal smallSliceThreshold = pieTotal * 0.04m;

                void AddGroupedPoints(IEnumerable<(string Name, decimal Amount)> items, Color color, string type)
                {
                    var large = items.Where(i => i.Amount >= smallSliceThreshold).ToList();
                    var small = items.Where(i => i.Amount < smallSliceThreshold).ToList();

                    if (!_typeToPointIndices.TryGetValue(type, out var indices))
                    {
                        indices = new List<int>();
                        _typeToPointIndices[type] = indices;
                    }

                    foreach (var item in large)
                        indices.Add(AddPoint(item.Name, item.Amount, color));

                    if (small.Count == 1)
                        indices.Add(AddPoint(small[0].Name, small[0].Amount, color));
                    else if (small.Count > 1)
                        indices.Add(AddPoint("Diğer", small.Sum(i => i.Amount), color, small));
                }

                AddGroupedPoints(current.IncomeBreakdown.Select(x => (x.CategoryName, x.TotalAmount)), IncomeColor, "income");
                AddGroupedPoints(current.ExpenseBreakdown.Select(x => (x.CategoryName, x.TotalAmount)), ExpenseColor, "expense");
                AddGroupedPoints(current.GoalBreakdown.Select(x => (x.CategoryName, x.TotalAmount)), GoalColor, "goal");

                if (idle > 0 || series.Points.Count == 0)
                    _idlePointIndex = AddPoint("Boşta", idle, IdleColor);
            }
            else
            {
                // Kırılım görünümü: seçilen türün (gelir/gider/hedef) HER kategorisi kendi dilimi olarak,
                // tümü aynı temel rengin farklı bir tonuyla — kaç kategori olursa olsun (10, 100 fark etmez).
                var items = _drillDownType switch
                {
                    "income" => current.IncomeBreakdown,
                    "expense" => current.ExpenseBreakdown,
                    _ => current.GoalBreakdown
                };
                Color baseColor = _drillDownType switch
                {
                    "income" => IncomeColor,
                    "expense" => ExpenseColor,
                    _ => GoalColor
                };

                var ordered = items.OrderByDescending(i => i.TotalAmount).ToList();
                var shades = GenerateShades(baseColor, ordered.Count);

                // Kırılım görünümünde her kategori kendi dilimi olduğundan, çok sayıda küçük dilim
                // aynı bölgede toplanıp dış etiketlerin (ve bağlantı çizgilerinin) üst üste binmesine
                // yol açabiliyordu. Payı %3'ün altında kalan dilimler için etiketi TAMAMEN kapatıyoruz
                // ("Disabled") — sadece metni değil, işaret eden çizgiyi de kaldırır; dilim yine
                // grafikte kendi rengiyle görünmeye devam ediyor.
                decimal typeTotal = ordered.Sum(i => i.TotalAmount);
                decimal labelThreshold = typeTotal * 0.03m;
                for (int i = 0; i < ordered.Count; i++)
                {
                    int idx = AddPoint(ordered[i].CategoryName, ordered[i].TotalAmount, shades[i]);
                    if (ordered[i].TotalAmount < labelThreshold)
                        series.Points[idx]["PieLabelStyle"] = "Disabled";
                }

                if (ordered.Count == 0)
                    AddPoint("Veri yok", 1, Color.FromArgb(60, 64, 84));
            }

            series.ChartArea = "main";
            chart.Series.Add(series);
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

        // Dilimlerin altında kategori kategori değil; her renk için TEK bir kutucukta o rengin toplam
        // yüzdesini gösteren, grafiğin altında ortalanmış özel bir şerit. Gelir/Gider/Hedef tıklanabilir:
        // tıklanınca o türün kendi kategori kırılımına geçilir (bkz. BuildChart, BuildDrillDownLegend).
        private void BuildLegend(MonthlyReport current, decimal idle, decimal goalTotal, decimal pieTotal, System.Globalization.CultureInfo tr)
        {
            pnlLegendFlow.Controls.Clear();

            if (_backButton != null)
            {
                pnlLegendWrapper.Controls.Remove(_backButton);
                _backButton.Dispose();
                _backButton = null;
            }

            if (_drillDownType != null)
            {
                BuildDrillDownLegend(current);
            }
            else
            {
                if (pieTotal <= 0) return;

                decimal incomeTotal = current.IncomeBreakdown.Sum(x => x.TotalAmount);
                decimal expenseTotal = current.ExpenseBreakdown.Sum(x => x.TotalAmount);

                if (incomeTotal > 0) AddLegendEntry(IncomeColor, "Gelir", incomeTotal, pieTotal, () => DrillInto("income"), _typeToPointIndices.GetValueOrDefault("income"));
                if (expenseTotal > 0) AddLegendEntry(ExpenseColor, "Gider", expenseTotal, pieTotal, () => DrillInto("expense"), _typeToPointIndices.GetValueOrDefault("expense"));
                if (goalTotal > 0) AddLegendEntry(GoalColor, "Hedef", goalTotal, pieTotal, () => DrillInto("goal"), _typeToPointIndices.GetValueOrDefault("goal"));
                if (idle > 0) AddLegendEntry(IdleColor, "Boşta", idle, pieTotal, null, _idlePointIndex >= 0 ? new List<int> { _idlePointIndex } : null);
            }

            CenterLegend();
        }

        private void BuildDrillDownLegend(MonthlyReport current)
        {
            var items = _drillDownType switch
            {
                "income" => current.IncomeBreakdown,
                "expense" => current.ExpenseBreakdown,
                _ => current.GoalBreakdown
            };
            Color baseColor = _drillDownType switch
            {
                "income" => IncomeColor,
                "expense" => ExpenseColor,
                _ => GoalColor
            };

            AddBackLegendEntry();

            decimal typeTotal = items.Sum(x => x.TotalAmount);
            if (typeTotal <= 0 || items.Count == 0) return;

            var ordered = items.OrderByDescending(i => i.TotalAmount).ToList();
            var shades = GenerateShades(baseColor, ordered.Count);
            // BuildChart de aynı kaynak listeyi aynı şekilde (OrderByDescending) sıralayıp dilimleri
            // 0'dan başlayarak sırayla eklediği için, buradaki i. öğe doğrudan i. dilime karşılık gelir.
            for (int i = 0; i < ordered.Count; i++)
                AddLegendEntry(shades[i], ordered[i].CategoryName, ordered[i].TotalAmount, typeTotal, null, new List<int> { i });
        }

        // Kırılım görünümünden birleşik görünüme dönüş: sade bir ok butonu (metin yerine daha
        // belirgin, dolgulu ve tıklanabilir olduğu açıkça belli olan bir görünüm).
        private void AddBackLegendEntry()
        {
            Panel btnBack = new Panel
            {
                Width = 34,
                Height = 34,
                Cursor = Cursors.Hand,
                BackColor = AppBackColor
            };

            bool isHovered = false;
            btnBack.MouseEnter += (s, e) => { isHovered = true; btnBack.Invalidate(); };
            btnBack.MouseLeave += (s, e) => { isHovered = false; btnBack.Invalidate(); };

            btnBack.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btnBack.Parent?.BackColor ?? AppBackColor);

                using (var brush = new SolidBrush(isHovered ? ControlPaint.Light(AccentColor) : AccentColor))
                    e.Graphics.FillEllipse(brush, 0, 0, btnBack.Width - 1, btnBack.Height - 1);

                using var pen = new Pen(Color.White, 2.6f)
                {
                    StartCap = System.Drawing.Drawing2D.LineCap.Round,
                    EndCap = System.Drawing.Drawing2D.LineCap.Round,
                    LineJoin = System.Drawing.Drawing2D.LineJoin.Round
                };
                int cx = btnBack.Width / 2, cy = btnBack.Height / 2;
                e.Graphics.DrawLines(pen, new PointF[]
                {
                    new PointF(cx + 6, cy - 8),
                    new PointF(cx - 7, cy),
                    new PointF(cx + 6, cy + 8)
                });
            };

            btnBack.Click += (s, e) => { _drillDownType = null; RenderReport(); };
            pnlLegendWrapper.Controls.Add(btnBack);
            _backButton = btnBack;
        }

        private void DrillInto(string type)
        {
            _drillDownType = type;
            RenderReport();
        }

        private void AddLegendEntry(Color color, string label, decimal amount, decimal totalForPercent, Action? onClick, List<int>? highlightIndices = null)
        {
            int percent = totalForPercent > 0 ? (int)Math.Round(amount / totalForPercent * 100) : 0;
            Font legendFont = new Font("Segoe UI", 9.5F);

            const int dotSize = 16;
            Size textSize = TextRenderer.MeasureText($"{label} %{percent}", legendFont);
            int dotTopMargin = Math.Max(0, (textSize.Height - dotSize) / 2);

            Panel dot = new Panel { Width = dotSize, Height = dotSize, Margin = new Padding(0, dotTopMargin, 4, 0) };
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
                Margin = new Padding(0, 0, 0, 0)
            };

            if (onClick != null)
            {
                dot.Cursor = Cursors.Hand;
                lbl.Cursor = Cursors.Hand;
                dot.Click += (s, e) => onClick();
                lbl.Click += (s, e) => onClick();
            }

            // Lejant öğesinin üzerine gelince, grafikte bir dilimin üstüne gelindiğinde olduğu gibi
            // ilgili dilim(ler)in etrafını beyaz çizgiyle vurguluyoruz (o türe ait tek bir dilim
            // olabileceği gibi, "Gelir" gibi bir üst kategori için birden fazla dilim de olabilir).
            if (highlightIndices != null && highlightIndices.Count > 0)
            {
                dot.MouseEnter += (s, e) => SetLegendHover(highlightIndices);
                dot.MouseLeave += (s, e) => ClearLegendHover();
                lbl.MouseEnter += (s, e) => SetLegendHover(highlightIndices);
                lbl.MouseLeave += (s, e) => ClearLegendHover();
            }

            // Nokta ve etiketini tek bir kapsayıcıya koyup pnlLegendFlow'a TEK parça olarak ekliyoruz;
            // aksi halde satır kaydırma (WrapContents) ikisinin arasından bölüp noktayı bir üst satırda,
            // yazıyı bir alt satırda bırakabiliyordu.
            FlowLayoutPanel entry = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                BackColor = AppBackColor,
                Margin = new Padding(4, 4, 18, 4)
            };
            entry.Controls.Add(dot);
            entry.Controls.Add(lbl);

            pnlLegendFlow.Controls.Add(entry);
        }

        // Tek bir temel rengin (ör. yeşil) N farklı tonunu üretir; kaç kategori olursa olsun
        // (10, 20, 100...) hiçbiri birebir aynı olmaz. Açıklık ekseninde eşit aralıklı, doygunlukta
        // hafif rastgele görünümlü (altın oran tabanlı) bir kayma ile art arda gelen dilimlerin
        // birbirine çok benzemesi engelleniyor.
        private static List<Color> GenerateShades(Color baseColor, int count)
        {
            var colors = new List<Color>();
            if (count <= 0) return colors;
            if (count == 1) { colors.Add(baseColor); return colors; }

            RgbToHsl(baseColor, out double h, out double s, out double l);

            const double minL = 0.30, maxL = 0.74;
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / (count - 1);
                double lShade = minL + (maxL - minL) * t;
                double satJitter = (i * 0.6180339887) % 1.0;
                double sShade = Math.Clamp(s * (0.8 + 0.35 * satJitter), 0.25, 1.0);
                colors.Add(HslToRgb(h, sShade, lShade));
            }
            return colors;
        }

        private static void RgbToHsl(Color c, out double h, out double s, out double l)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            l = (max + min) / 2.0;

            if (max == min)
            {
                h = 0; s = 0;
            }
            else
            {
                double d = max - min;
                s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
                if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g) h = (b - r) / d + 2;
                else h = (r - g) / d + 4;
                h /= 6.0;
            }
        }

        private static Color HslToRgb(double h, double s, double l)
        {
            double r, g, b;
            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                r = HueToRgb(p, q, h + 1.0 / 3.0);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0 / 3.0);
            }
            return Color.FromArgb(
                (int)Math.Round(Math.Clamp(r, 0, 1) * 255),
                (int)Math.Round(Math.Clamp(g, 0, 1) * 255),
                (int)Math.Round(Math.Clamp(b, 0, 1) * 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
            return p;
        }

        private void CenterLegend()
        {
            // Geri oku artık akışın (wrap) dışında, sabit bir referans noktası: bu sayede kategori
            // öğeleri birden fazla satıra taştığında (ör. tek başına kalan küçük bir yüzdelik), her
            // satır okun genişliğinden etkilenmeksizin aynı X'te başlar ve üst satırla hizalı kalır.
            int reserved = _backButton != null ? _backButton.Width + 10 : 0;

            // Lejant tek satıra sığmadığında (ör. çok sayıda kategori) sağ kenardan taşıp kırpılmak
            // yerine alt satıra kaysın diye genişliği sarmalayıcı panelin genişliğiyle sınırlıyoruz.
            pnlLegendFlow.MaximumSize = new Size(Math.Max(100, pnlLegendWrapper.Width - 20 - reserved), 0);

            int contentWidth = pnlLegendFlow.PreferredSize.Width + reserved;
            int startLeft = Math.Max(0, (pnlLegendWrapper.Width - contentWidth) / 2);

            pnlLegendFlow.Left = startLeft + reserved;
            pnlLegendFlow.Top = Math.Max(4, (pnlLegendWrapper.Height - pnlLegendFlow.PreferredSize.Height) / 2);

            if (_backButton != null)
            {
                _backButton.Left = startLeft;
                _backButton.Top = Math.Max(4, pnlLegendFlow.Top - (_backButton.Height - 24) / 2);
            }
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
                series.Points[newIndex].BorderColor = HoverHighlightColor;
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

        // Lejantta bir öğenin (ör. "Gelir %30" ya da kırılımda tek bir kategori) üzerine gelindiğinde,
        // grafik üzerinde fareyle bir dilime gelindiğindeki aynı beyaz çerçeve vurgusunu, birden fazla
        // dilim olsa bile (ör. "Gelir" birkaç kategoriye yayılmışsa) hepsine birden uygular.
        private void SetLegendHover(List<int> indices)
        {
            var series = chart.Series.FirstOrDefault();
            if (series == null) return;

            ClearLegendHover();

            foreach (var idx in indices)
            {
                if (idx < 0 || idx >= series.Points.Count) continue;
                series.Points[idx].BorderColor = HoverHighlightColor;
                series.Points[idx].BorderWidth = 4;
                _legendHoverIndices.Add(idx);
            }
        }

        private void ClearLegendHover()
        {
            var series = chart.Series.FirstOrDefault();
            if (series != null)
            {
                foreach (var idx in _legendHoverIndices)
                {
                    if (idx < 0 || idx >= series.Points.Count) continue;
                    series.Points[idx].BorderColor = SliceBorderColor;
                    series.Points[idx].BorderWidth = 2;
                }
            }
            _legendHoverIndices.Clear();
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
                dialog.FileName = $"rapor_{_currentReport.PeriodStart:yyyy_MM}.csv";

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
                            foreach (var item in _currentReport.GoalBreakdown)
                            {
                                writer.WriteLine($"Hedef;{item.CategoryName};{item.TotalAmount.ToString("0.00", tr)}");
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

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor)
        {
            pnl.BackColor = AppBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius);
                using var brush = new SolidBrush(bgColor);
                e.Graphics.FillPath(brush, path);
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                using var brush = new SolidBrush(bgColor);
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
