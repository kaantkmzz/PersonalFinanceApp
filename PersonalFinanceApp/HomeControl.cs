using System.Windows.Forms.DataVisualization.Charting;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class HomeControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly Action<string>? _onNavigate;
        private readonly AccountService _accountService = new AccountService();
        private readonly AssetService _assetService = new AssetService();
        private readonly ReminderService _reminderService = new ReminderService();
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly ReportService _reportService = new ReportService();
        private readonly SavingsGoalService _savingsGoalService = new SavingsGoalService();
        private readonly NoteService _noteService = new NoteService();
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly RecurringTransactionService _recurringTransactionService = new RecurringTransactionService();
        private readonly AssetPriceAlertService _assetPriceAlertService = new AssetPriceAlertService();
        private readonly AssetPriceService _assetPriceService = new AssetPriceService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color IncomeColor => AppTheme.IncomeColor;
        private static Color ExpenseColor => AppTheme.ExpenseColor;

        private const int CardLeft1 = 20;
        private const int CardLeft2 = 360;
        private const int CardLeft3 = 700;
        private const int CardLeft4 = 1040;
        private const int CardWidth = 320;
        private const int AllCardsRight = CardLeft3 + CardWidth; // 1020

        private Label lblWalletAmount = new Label();
        private Label lblSafeAmount = new Label();
        private Label lblInvestAmount = new Label();
        private Label lblInvestPLValue = new Label();
        private Label lblInvestCostValue = new Label();
        private Label lblInvestPosValue = new Label();
        private Label lblStatus = new Label();
        private Panel pnlNotifications = new Panel();
        private Panel pnlNotes = new Panel();

        // --- Ana Sayfa widget yerleşimi (bkz. SetupWidgetGrid) ---
        // Widget'lar 4 sütunluk, 3 satırlık bir ızgaraya (12 hücre) sürükle-bırak ile yerleştirilir;
        // yerleşim kullanıcıya özgü (users.home_layout, JSON) kalıcı olarak saklanır. İlk açılışta
        // (home_layout boş) hiç widget yerleşik değildir — sadece Cüzdan/Kasa/Varlıklarım görünür.
        // Üç satır olmasının sebebi: butonların hemen altındaki boşluk (eskiden hiç kullanılmıyordu)
        // artık ilk satır olarak da widget alabiliyor — kullanıcı geri bildirimi, Varlık Bildirimleri
        // gibi geniş bir widget'ı hem "şu anki" hem "bir üstteki" konuma koyabilmek istedi.
        private class WidgetDef
        {
            public string Key = string.Empty;
            public string Title = string.Empty;
            public int ColSpan = 1;
        }

        private static readonly WidgetDef[] WidgetCatalog = new[]
        {
            new WidgetDef { Key = "notifications", Title = "Varlık Bildirimleri", ColSpan = 3 },
            new WidgetDef { Key = "notes", Title = "Notlar", ColSpan = 1 },
            new WidgetDef { Key = "reminders", Title = "Yaklaşan Hatırlatıcılar", ColSpan = 1 },
            new WidgetDef { Key = "report", Title = "Bu Ayın Özeti", ColSpan = 1 },
            new WidgetDef { Key = "transactions", Title = "Son İşlemler", ColSpan = 1 },
            new WidgetDef { Key = "goals", Title = "Hedeflerim", ColSpan = 1 },
            new WidgetDef { Key = "cashflow", Title = "Nakit Akışı Tahmini", ColSpan = 1 },
            new WidgetDef { Key = "quickadd", Title = "Hızlı İşlem Ekle", ColSpan = 1 },
            new WidgetDef { Key = "recurringUpcoming", Title = "Yaklaşan Ödemeler", ColSpan = 1 },
            new WidgetDef { Key = "topCategory", Title = "En Çok Harcanan", ColSpan = 1 },
            new WidgetDef { Key = "priceAlerts", Title = "Fiyat Alarmı Özeti", ColSpan = 1 },
            new WidgetDef { Key = "weeklyCompare", Title = "Haftalık Karşılaştırma", ColSpan = 1 },
        };

        private const int GridCols = 4;
        private const int ButtonsTop = 30 + 240 + 16;

        // Izgara 12 hücrelik (3 satır × 4 sütun) ama katalog artık 12 widget içeriyor (3+1×11=14 hücre
        // gerektiriyor) — hepsi aynı anda tek sayfaya sığmıyor. Bunun yerine aynı 12 hücrelik görünür
        // ızgara İKİ "sayfa" olarak kullanılıyor (bkz. _currentPage, SetupPageDots); depoda bir widget'ın
        // hangi sayfada olduğu, hücre numarasının içine page*CellsPerPage eklenerek kodlanıyor — böylece
        // home_layout'un JSON şeması (Dictionary<string,int>) değişmiyor, eski kayıtlar (hep <12) sessizce
        // sayfa 0 olarak çözülüyor.
        private const int CellsPerPage = 12;
        private int _currentPage = 0;
        private static int EncodeCell(int page, int cell) => page * CellsPerPage + cell;
        private static int PageOf(int stored) => stored / CellsPerPage;
        private static int CellOf(int stored) => stored % CellsPerPage;

        private readonly Dictionary<string, int> _placedWidgets = new Dictionary<string, int>();
        // Sürükleme sırasında kesikli çerçeveyle vurgulanan hücreler — sürüklenen widget'ın gerçek
        // kapladığı hücre sayısını (ör. 3 hücreli Varlık Bildirimleri) doğru yansıtsın diye tek hücre
        // yerine bir küme olarak tutuluyor (bkz. HighlightDragTarget). _activeDropAnchor aynı hedefin
        // başlangıç hücresi — hem gereksiz yeniden çizimi önlemek (bkz. HighlightDragTarget) hem de
        // "tüm uygun yuvalar" önizlemesinde (bkz. _validDropAnchors) aktif olanı atlamak için.
        private readonly HashSet<int> _highlightedCells = new HashSet<int>();
        private int? _activeDropAnchor;
        // Bir widget sürüklenmeye başladığı an, o widget'ın (boyutuna göre) bırakılabileceği TÜM
        // başlangıç hücreleri burada toplanır (bkz. StartWidgetDrag) — kullanıcı nereye
        // koyabileceğini sürüklemenin başından itibaren, imleci belirli bir hücreye götürmeden görsün diye.
        private readonly HashSet<int> _validDropAnchors = new HashSet<int>();
        private int _dragColSpan = 1;
        private readonly Dictionary<string, Panel> _widgetFrames = new Dictionary<string, Panel>();
        private readonly Panel[] _cellPanels = new Panel[12];
        private Panel pnlPageDot0 = new Panel();
        private Panel pnlPageDot1 = new Panel();
        private Button btnAddWidget = new Button();
        private Panel pnlWidgetPicker = new Panel();
        private Label lblEmptyGridHint = new Label();

        // Etikete göre çalışan sayaç animasyonu zamanlayıcıları — bir widget yenilenirken (ör. tutarları
        // gizle aç/kapa, ya da mini-widget'lar yeniden dolarken) eski bir animasyon hâlâ sürüyorsa önce
        // onu durdurmak için (bkz. AnimateLabelValue, ClearWidgetContent).
        private readonly Dictionary<Control, System.Windows.Forms.Timer> _cardAnimTimers = new Dictionary<Control, System.Windows.Forms.Timer>();

        // Transfer Et/Geçmişi butonlarının hemen altına çekildi — lblStatus artık kendi satırını
        // kaplamıyor (bkz. SetupUI, Transfer Geçmişi'nin sağına taşındı), bu yüzden araya eskiden
        // giren boşluğa gerek kalmadı.
        private const int MiniRowTop = 348;
        private const int MiniRowHeight = 210;

        // "Bu Ayın Özeti" kutusundaki iki sayfa arası geçiş: 0 = genel dağılım, 1 = varlık dağılımı.
        private int _miniReportPage = 0;
        private Panel pnlMiniPageGeneral = new Panel();
        private Panel pnlMiniPageAssets = new Panel();
        private Panel pnlMiniDot0 = new Panel();
        private Panel pnlMiniDot1 = new Panel();

        private static readonly Color[] AssetPalette =
        {
            Color.FromArgb(80, 200, 195),
            Color.FromArgb(120, 180, 255),
            Color.FromArgb(230, 200, 80),
            Color.FromArgb(190, 130, 240),
            Color.FromArgb(230, 100, 100),
            Color.FromArgb(120, 220, 150),
            Color.FromArgb(255, 170, 90),
            Color.FromArgb(140, 140, 220),
        };

        public HomeControl(User user, Action<string>? onNavigate = null)
        {
            _user = user;
            _onNavigate = onNavigate;
            InitializeComponent();
            SetupUI();
            RefreshBalances();
            _ = LoadInvestCardAsync();
            SetupWidgetGrid();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            // Sabit piksel konumlu dört sütun (bkz. CardLeft4/CardWidth) ve mini-widget satırı (bkz.
            // MiniRowTop/MiniRowHeight) esnek değil — Dock=Fill bunu MainForm.pnlContent'in AutoScroll'u
            // sayesinde bu boyutun altına küçültmüyor, aksi halde sağdaki/alttaki kartlar sessizce kırpılırdı.
            // Widget ızgarası 3 satır (bkz. SetupWidgetGrid, GridCols, _cellPanels) — yükseklik hesabı
            // eksik kalırsa alt satıra yerleştirilen widget'lar kırpılabilirdi.
            const int gridRows = 3;
            // +32: ızgaranın hemen altındaki sayfa noktalarına (bkz. SetupPageDots) yer aç.
            this.MinimumSize = new Size(CardLeft4 + CardWidth + 20, MiniRowTop + gridRows * (MiniRowHeight + 20) + 32);
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);
            this.Paint += DrawDragHighlightOverlay;

            Panel pnlWallet = new Panel { Left = CardLeft1, Top = 30, Width = CardWidth, Height = 240 };
            SetupSmoothContainer(pnlWallet, 16, CardBackColor);
            Label lblWalletIcon = new Label { Text = "💳", Font = new Font("Segoe UI Emoji", 32F), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true, BackColor = Color.Transparent };
            Label lblWalletTitle = new Label { Text = "Cüzdan", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 130, AutoSize = true, BackColor = Color.Transparent };
            lblWalletAmount.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblWalletAmount.ForeColor = Color.FromArgb(120, 220, 150);
            lblWalletAmount.Left = 20;
            lblWalletAmount.Top = 165;
            lblWalletAmount.AutoSize = true;
            lblWalletAmount.BackColor = Color.Transparent;
            EnableDoubleBuffering(lblWalletAmount);
            pnlWallet.Controls.Add(lblWalletIcon);
            pnlWallet.Controls.Add(lblWalletTitle);
            pnlWallet.Controls.Add(lblWalletAmount);

            Panel pnlSafe = new Panel { Left = CardLeft2, Top = 30, Width = CardWidth, Height = 240 };
            SetupSmoothContainer(pnlSafe, 16, CardBackColor);
            Label lblSafeIcon = new Label { Text = "🏦", Font = new Font("Segoe UI Emoji", 32F), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true, BackColor = Color.Transparent };
            Label lblSafeTitle = new Label { Text = "Kasa", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 130, AutoSize = true, BackColor = Color.Transparent };
            lblSafeAmount.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblSafeAmount.ForeColor = Color.FromArgb(120, 180, 255);
            lblSafeAmount.Left = 20;
            lblSafeAmount.Top = 165;
            lblSafeAmount.AutoSize = true;
            lblSafeAmount.BackColor = Color.Transparent;
            EnableDoubleBuffering(lblSafeAmount);
            pnlSafe.Controls.Add(lblSafeIcon);
            pnlSafe.Controls.Add(lblSafeTitle);
            pnlSafe.Controls.Add(lblSafeAmount);

            // Varlıklarım'ın artık kendi bakiyesi yok; bu kutu artık Varlıklarım ekranındaki güncel
            // portföy değerini (kâr/zararına göre yeşil/kırmızı ok ile) gösteriyor. Sağa doğru
            // genişletilip Kâr/Zarar, Toplam Maliyet, Pozisyon bilgileri de eklendi.
            const int investCardWidth = CardLeft4 + CardWidth - CardLeft3; // 660
            Panel pnlInvest = new Panel { Left = CardLeft3, Top = 30, Width = investCardWidth, Height = 240 };
            SetupSmoothContainer(pnlInvest, 16, CardBackColor);
            Label lblInvestIcon = new Label { Text = "📈", Font = new Font("Segoe UI Emoji", 32F), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true, BackColor = Color.Transparent };
            Label lblInvestTitle = new Label { Text = "Varlıklarım", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 130, AutoSize = true, BackColor = Color.Transparent };
            lblInvestAmount.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblInvestAmount.ForeColor = Color.FromArgb(120, 220, 210);
            lblInvestAmount.Left = 20;
            lblInvestAmount.Top = 165;
            lblInvestAmount.AutoSize = true;
            lblInvestAmount.BackColor = Color.Transparent;
            EnableDoubleBuffering(lblInvestAmount);
            pnlInvest.Controls.Add(lblInvestIcon);
            pnlInvest.Controls.Add(lblInvestTitle);
            pnlInvest.Controls.Add(lblInvestAmount);

            void AddInvestStatRow(string title, Label valueLabel, int top)
            {
                Label lblStatTitle = new Label { Text = title, Font = new Font("Segoe UI", 9.5F), ForeColor = TextMuted, Left = 380, Top = top, AutoSize = true, BackColor = Color.Transparent };
                valueLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
                valueLabel.ForeColor = TextLight;
                valueLabel.Left = 380;
                valueLabel.Top = top + 24;
                valueLabel.AutoSize = true;
                valueLabel.BackColor = Color.Transparent;
                pnlInvest.Controls.Add(lblStatTitle);
                pnlInvest.Controls.Add(valueLabel);
            }
            AddInvestStatRow("Kâr / Zarar", lblInvestPLValue, 32);
            AddInvestStatRow("Toplam Maliyet", lblInvestCostValue, 100);
            AddInvestStatRow("Pozisyon", lblInvestPosValue, 168);

            // Butonlar Cüzdan kutucuğunun altına, sol hizalı (üç kutu eklenince sağa hizalama anlamını yitirdi)
            const int buttonsTop = ButtonsTop;
            const int btnWidth = 190;
            const int btnGap = 14;

            Button btnTransfer = new Button
            {
                Text = "Transfer Et",
                Left = CardLeft1,
                Top = buttonsTop,
                Width = btnWidth,
                Height = 42,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10.5F)
            };
            SetupRoundedButton(btnTransfer, AccentColor, Color.White);
            btnTransfer.Click += BtnTransfer_Click;

            Button btnHistory = new Button
            {
                Text = "Transfer Geçmişi",
                Left = CardLeft1 + btnWidth + btnGap,
                Top = buttonsTop,
                Width = btnWidth,
                Height = 42,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10.5F)
            };
            SetupRoundedButton(btnHistory, CardBackColor, TextLight);
            btnHistory.Click += (s, e) =>
            {
                using (var dialog = new TransferHistoryDialog(_user.Id))
                {
                    dialog.ShowDialog();
                }
            };

            // Kendi satırında değil, "Transfer Geçmişi" butonunun sağında — widget ızgarasının
            // altına çekilebilmesi için bu satırın kendi başına bir boşluk açmasına gerek yok.
            lblStatus.Left = btnHistory.Right + 20;
            lblStatus.Top = buttonsTop + (42 - 25) / 2;
            lblStatus.Width = 500;
            lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F);

            // Varlıklarım'da pozisyonu olan kullanıcılar için kâr/zarar bildirim kartı (bkz.
            // LoadNotificationsAsync). Artık sabit boyutlu bir widget hücresi — konumu ve görünürlüğü
            // kullanıcının widget yerleşimine göre RebuildWidgetGrid tarafından yönetiliyor.
            SetupSmoothContainer(pnlNotifications, 16, CardBackColor);
            SetupSmoothContainer(pnlNotes, 16, CardBackColor);

            this.Controls.Add(pnlWallet);
            this.Controls.Add(pnlSafe);
            this.Controls.Add(pnlInvest);
            this.Controls.Add(btnTransfer);
            this.Controls.Add(btnHistory);
            this.Controls.Add(lblStatus);

            SetupMiniWidgets();
        }

        // Ana Sayfa'yı çeşitlendiren, kullanıcının sürükle-bırakla ekleyip çıkarabildiği önizleme
        // widget'ları — her biri kendi tam ekranına götürür. Artık doğrudan this.Controls'a
        // eklenmiyorlar; RebuildWidgetGrid, yerleştirilmiş olanları uygun bir "frame" içine koyar.
        private Panel pnlReminderWidget = new Panel();
        private Panel pnlMiniReportWidget = new Panel();
        private Panel pnlRecentTxWidget = new Panel();
        private Panel pnlGoalsWidget = new Panel();
        private Panel pnlCashflowWidget = new Panel();
        private Panel pnlQuickAddWidget = new Panel();
        private Panel pnlRecurringUpcomingWidget = new Panel();
        private Panel pnlTopCategoryWidget = new Panel();
        private Panel pnlPriceAlertsWidget = new Panel();
        private Panel pnlWeeklyCompareWidget = new Panel();

        private void SetupMiniWidgets()
        {
            pnlReminderWidget = CreateWidgetCard(CardLeft1, "Yaklaşan Hatırlatıcılar", "Hatırlatıcılar");
            pnlMiniReportWidget = CreateWidgetCard(CardLeft2, "Bu Ayın Özeti", "Rapor");
            pnlRecentTxWidget = CreateWidgetCard(CardLeft3, "Son İşlemler", "İşlemler");
            pnlGoalsWidget = CreateWidgetCard(CardLeft4, "Hedeflerim", "Hedefler");
            pnlRecurringUpcomingWidget = CreateWidgetCard(CardLeft1, "Yaklaşan Ödemeler", "İşlemler");
            pnlTopCategoryWidget = CreateWidgetCard(CardLeft1, "En Çok Harcanan", "Kategoriler");
            pnlPriceAlertsWidget = CreateWidgetCard(CardLeft1, "Fiyat Alarmı Özeti", "Varlıklarım");
            // Bu ikisi kendi başlığını Load* metodunda kendi çiziyor (Varlık Bildirimleri/Notlar ile
            // aynı desen) — "quickadd" tıklanabilir olmamalı (içinde kendi etkileşimli denetimleri var,
            // bkz. LoadQuickAddWidget), bu yüzden CreateWidgetCard'ın MakeClickable'ını kullanmıyoruz.
            // "weeklyCompare" da Nakit Akışı Tahmini gibi bir alt başlık gösterdiği için kendi başlığını
            // kendi çiziyor.
            SetupSmoothContainer(pnlCashflowWidget, 16, CardBackColor);
            SetupSmoothContainer(pnlQuickAddWidget, 16, CardBackColor);
            SetupSmoothContainer(pnlWeeklyCompareWidget, 16, CardBackColor);
            // SetupSmoothContainer .BackColor'ı her zaman AppBackColor'a sabitliyor, ama bu widget
            // görsel olarak CardBackColor ile boyanıyor. İçindeki kutucuklar (pnlType/pnlCategory/
            // pnlAmount) kendi köşe boşluklarını Parent.BackColor ile temizliyor — uyumsuzluk yüzünden
            // köşelerinde AppBackColor (koyu/siyahımsı) görünüyordu. Burada gerçek görsel rengi
            // yansıtacak şekilde düzeltiyoruz (kendi çizimini etkilemez, sadece çocuklarının okuduğu
            // Parent.BackColor değerini düzeltir).
            pnlQuickAddWidget.BackColor = CardBackColor;
        }

        // --- Widget ızgarası: 4 sütun × 2 satır, sürükle-bırakla düzenlenebilir ---

        private Panel GetContentPanelFor(string key) => key switch
        {
            "notifications" => pnlNotifications,
            "notes" => pnlNotes,
            "reminders" => pnlReminderWidget,
            "report" => pnlMiniReportWidget,
            "transactions" => pnlRecentTxWidget,
            "goals" => pnlGoalsWidget,
            "cashflow" => pnlCashflowWidget,
            "quickadd" => pnlQuickAddWidget,
            "recurringUpcoming" => pnlRecurringUpcomingWidget,
            "topCategory" => pnlTopCategoryWidget,
            "priceAlerts" => pnlPriceAlertsWidget,
            "weeklyCompare" => pnlWeeklyCompareWidget,
            _ => throw new ArgumentException($"Bilinmeyen widget anahtarı: {key}")
        };

        private void LoadWidgetContent(string key)
        {
            switch (key)
            {
                case "notifications": _ = LoadNotificationsAsync(); break;
                case "notes": LoadNotesWidget(); break;
                case "reminders": LoadReminderWidget(); break;
                case "report": LoadMiniReportWidget(); break;
                case "transactions": LoadRecentTransactionsWidget(); break;
                case "goals": LoadGoalsWidget(); break;
                case "cashflow": LoadCashflowWidget(); break;
                case "quickadd": LoadQuickAddWidget(); break;
                case "recurringUpcoming": LoadRecurringUpcomingWidget(); break;
                case "topCategory": LoadTopCategoryWidget(); break;
                case "priceAlerts": _ = LoadPriceAlertsWidgetAsync(); break;
                case "weeklyCompare": LoadWeeklyCompareWidget(); break;
            }
        }

        private void LoadPlacedWidgetsFromUser()
        {
            if (string.IsNullOrWhiteSpace(_user.HomeLayout)) return;
            try
            {
                var loaded = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(_user.HomeLayout);
                if (loaded == null) return;
                foreach (var kv in loaded)
                {
                    if (WidgetCatalog.Any(w => w.Key == kv.Key))
                        _placedWidgets[kv.Key] = kv.Value;
                }
            }
            catch
            {
                // Bozuk/eski bir home_layout JSON'u varsa sessizce yok sayılır — kullanıcı widget'ları
                // + panelinden yeniden ekleyebilir, bu kritik bir veri değil.
            }
        }

        private void SaveLayout()
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(_placedWidgets);
                _accountService.SetHomeLayout(_user.Id, json);
                _user.HomeLayout = json;
            }
            catch
            {
                // Yerleşim kaydı başarısız olsa bile UI'da widget'lar zaten doğru yerde duruyor —
                // bir sonraki oturumda eski yerleşime dönebilir, kritik değil.
            }
        }

        private void SetupWidgetGrid()
        {
            LoadPlacedWidgetsFromUser();

            // Vurgu/önizleme artık hücrelerin kendi Paint'inde DEĞİL, tek bir yerde (bkz. this.Paint,
            // SetupUI) çiziliyor — hücreler saydam olduğundan (BackColor=Transparent) altlarındaki
            // this'in çizdiği yuvarlak köşeli dikdörtgen olduğu gibi görünür, ve aralarındaki gerçek
            // 20px'lik boşluklar da (eskiden her hücre kendi sınırını çizdiği için) artık tek bir
            // bitişik dörtgenin parçası oluyor (bkz. GetCellsBoundingRect / GetAnchorBoundingRect).
            for (int i = 0; i < _cellPanels.Length; i++)
            {
                int col = i % GridCols, row = i / GridCols;
                Panel cell = new Panel
                {
                    Left = CardLeft1 + col * (CardWidth + 20),
                    Top = MiniRowTop + row * (MiniRowHeight + 20),
                    Width = CardWidth,
                    Height = MiniRowHeight,
                    BackColor = Color.Transparent,
                    AllowDrop = true
                };

                cell.DragEnter += OnGridDragEnterOrOver;
                cell.DragOver += OnGridDragEnterOrOver;
                cell.DragLeave += OnGridDragLeave;
                cell.DragDrop += OnGridDragDrop;

                this.Controls.Add(cell);
                _cellPanels[i] = cell;
            }

            SetupPageDots();
            SetupWidgetPicker();
            RebuildWidgetGrid();
        }

        private static DragDropEffects GetDropEffect(DragEventArgs e) =>
            (e.Data?.GetDataPresent(typeof(string)) ?? false) ? DragDropEffects.Move : DragDropEffects.None;

        // Hem boş hücreler hem de dolu widget çerçeveleri (bkz. CreateWidgetFrame) sürükleme sırasında
        // aynı üç olayı aynı şekilde ele alıyor — imlecin GERÇEK ekran konumundan hedef hücreyi hesaplıyoruz
        // (bkz. GetAnchorCellFromCursor), hangi alt-denetimin DragEnter/Over'ı tetiklediğinden bağımsız.
        // Önceden hedef, olayı tetikleyen hücrenin sabit kimliğiydi (cellIndex/myCell) — bu, önizleme
        // artık imlecin TAM ORTASINDA gösterildiği için (bkz. StartWidgetDrag) çok hücreli bir widget'ı
        // en sol sütuna bırakmayı imkansız kılıyordu: imleç kutunun ortasındayken en soldaki hücrenin
        // üzerine gelmiş gibi görünse de aslında bir sağdaki hücrenin sınırları içindeydi.
        private void OnGridDragEnterOrOver(object? sender, DragEventArgs e)
        {
            e.Effect = GetDropEffect(e);
            if (e.Data?.GetData(typeof(string)) is not string key) return;
            int span = WidgetCatalog.FirstOrDefault(w => w.Key == key)?.ColSpan ?? 1;
            HighlightDragTarget(GetAnchorCellFromCursor(span), e.Data);
        }

        private void OnGridDragLeave(object? sender, EventArgs e) => ClearDragHighlight();

        private void OnGridDragDrop(object? sender, DragEventArgs e)
        {
            ClearDragHighlight();
            if (e.Data?.GetData(typeof(string)) is not string key) return;
            int span = WidgetCatalog.FirstOrDefault(w => w.Key == key)?.ColSpan ?? 1;
            PlaceWidget(key, GetAnchorCellFromCursor(span));
        }

        // İmlecin gerçek ekran konumunu ızgara hücresine çevirir. Önizleme kutusunun SOL kenarı
        // (imleç - genişlik/2) hangi sütuna en yakınsa o sütun anchor kabul edilir — böylece kullanıcı
        // gördüğü önizleme kutusunu bir hücreye hizaladığında gerçekten de oraya bırakılır.
        private int GetAnchorCellFromCursor(int colSpan)
        {
            Point clientPos = this.PointToClient(System.Windows.Forms.Cursor.Position);
            int spanWidth = colSpan * CardWidth + (colSpan - 1) * 20;
            int leftEdgeX = clientPos.X - spanWidth / 2;
            int col = (int)Math.Round((double)(leftEdgeX - CardLeft1) / (CardWidth + 20));
            col = Math.Max(0, Math.Min(GridCols - colSpan, col));

            int totalRows = _cellPanels.Length / GridCols;
            int row = (int)Math.Round((double)(clientPos.Y - MiniRowTop) / (MiniRowHeight + 20));
            row = Math.Max(0, Math.Min(totalRows - 1, row));

            return row * GridCols + col;
        }

        // anchor'dan başlayıp colSpan hücre kaplayan bir yerleşimin (aynı satırda, boşluklar dahil)
        // gerçek piksel dörtgeni.
        private Rectangle GetAnchorBoundingRect(int anchor, int colSpan)
        {
            int col = anchor % GridCols, row = anchor / GridCols;
            int left = CardLeft1 + col * (CardWidth + 20);
            int top = MiniRowTop + row * (MiniRowHeight + 20);
            int width = colSpan * CardWidth + (colSpan - 1) * 20;
            return new Rectangle(left, top, width, MiniRowHeight);
        }

        // Vurgulanan (genişletilmiş) hücre kümesinin dış sınır dörtgeni — küme her zaman aynı satırda
        // ardışık hücrelerden oluşur (bkz. GetClampedSpanCells), bu yüzden min/max sütun yeterli.
        private Rectangle GetCellsBoundingRect(IEnumerable<int> cells)
        {
            var list = cells as IList<int> ?? cells.ToList();
            if (list.Count == 0) return Rectangle.Empty;
            int minCol = list.Min(c => c % GridCols);
            int maxCol = list.Max(c => c % GridCols);
            return GetAnchorBoundingRect(list[0] / GridCols * GridCols + minCol, maxCol - minCol + 1);
        }

        // Sürükleme sırasındaki tüm görsel geri bildirim tek yerde: (1) widget'ın bırakılabileceği TÜM
        // uygun yuvalar soluk/ince kesikli dörtgenlerle (bkz. _validDropAnchors, StartWidgetDrag), (2)
        // imlecin şu an işaret ettiği hedef daha belirgin bir dörtgenle (bkz. _highlightedCells). İkisi
        // de yuvarlak köşeli — hücreler arasındaki gerçek boşluklara rağmen (bkz. GetAnchorBoundingRect)
        // tek, bitişik bir kutu gibi görünürler.
        private void DrawDragHighlightOverlay(object? sender, PaintEventArgs e)
        {
            if (_validDropAnchors.Count == 0 && _highlightedCells.Count == 0) return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (_validDropAnchors.Count > 0)
            {
                using var mutedPen = new Pen(AppTheme.GridLineColor, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                foreach (var anchor in _validDropAnchors)
                {
                    if (anchor == _activeDropAnchor) continue; // aktif hedef aşağıda ayrı ve daha belirgin çiziliyor
                    var rect = GetAnchorBoundingRect(anchor, _dragColSpan);
                    using var path = GetRoundedRectPath(rect, 14);
                    e.Graphics.DrawPath(mutedPen, path);
                }
            }

            if (_highlightedCells.Count > 0)
            {
                var rect = GetCellsBoundingRect(_highlightedCells);
                using var pen = new Pen(AccentColor, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                using var path = GetRoundedRectPath(rect, 14);
                e.Graphics.DrawPath(pen, path);
            }
        }

        // Ana Sayfa widget ızgarasının sağ altındaki iki nokta: tıklanınca 1. ve 2. "sayfa" arasında
        // geçiş yapar (bkz. _currentPage, EncodeCell) — üstteki Cüzdan/Kasa/Varlıklarım kartları ve
        // Transfer düğmeleri sabit kalır, yalnızca widget ızgarası değişir. "Bu Ayın Özeti" mini-widget'ının
        // kendi sayfa noktalarıyla (bkz. SetupMiniPageDots) aynı görsel dil.
        private void SetupPageDots()
        {
            const int dotSize = 9;
            const int hitSize = 22;
            const int gap = 4;
            int rightEdge = CardLeft4 + CardWidth;
            int dot1Left = rightEdge - hitSize;
            int dot0Left = dot1Left - gap - hitSize;
            // Izgaranın altında (bkz. eski gridBottom hesabı) kullanıcı gözden kaçırıyordu — widget
            // alanının hemen ÜSTÜNE, sağ üst köşeye taşındı (bkz. ekran görüntüsündeki not).
            int dotsTop = MiniRowTop - 30;
            int dotOffset = (hitSize - dotSize) / 2;

            void SetupDot(Panel dot, int left, int page)
            {
                dot.Left = left; dot.Top = dotsTop; dot.Width = hitSize; dot.Height = hitSize;
                dot.BackColor = Color.Transparent;
                dot.Cursor = Cursors.Hand;
                dot.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    Color c = _currentPage == page ? AccentColor : AppTheme.GridLineColor;
                    using var brush = new SolidBrush(c);
                    e.Graphics.FillEllipse(brush, dotOffset, dotOffset, dotSize, dotSize);
                };
                dot.Click += (s, e) =>
                {
                    if (_currentPage == page) return;
                    _currentPage = page;
                    RebuildWidgetGrid();
                };
            }

            SetupDot(pnlPageDot0, dot0Left, 0);
            SetupDot(pnlPageDot1, dot1Left, 1);
            this.Controls.Add(pnlPageDot0);
            this.Controls.Add(pnlPageDot1);
            pnlPageDot0.BringToFront();
            pnlPageDot1.BringToFront();
        }

        private void SetupWidgetPicker()
        {
            btnAddWidget.Text = string.Empty;
            btnAddWidget.Width = 40;
            btnAddWidget.Height = 36;
            btnAddWidget.Cursor = Cursors.Hand;
            SetupAddWidgetButton(btnAddWidget, AccentColor);
            btnAddWidget.Click += (s, e) =>
            {
                pnlWidgetPicker.Visible = !pnlWidgetPicker.Visible;
                if (pnlWidgetPicker.Visible)
                {
                    BuildWidgetPickerRows();
                    pnlWidgetPicker.BringToFront();
                }
            };

            // "Yaklaşan Hatırlatıcılar" gibi uzun başlıklar 280px'te elipslenmek zorunda kalıyordu.
            pnlWidgetPicker.Width = 320;
            pnlWidgetPicker.Visible = false;
            SetupSmoothContainer(pnlWidgetPicker, 12, CardBackColor);

            // + butonu, sayfanın gerçek sağ üst köşesinde dursun diye (Transfer Et satırına değil)
            // pencerenin genişliğine göre konumlanıyor; yeniden boyutlanınca da yeniden hesaplanıyor.
            void PositionAddWidgetButton()
            {
                btnAddWidget.Left = this.ClientSize.Width - btnAddWidget.Width - 20;
                btnAddWidget.Top = 20;
                pnlWidgetPicker.Left = btnAddWidget.Right - pnlWidgetPicker.Width;
                pnlWidgetPicker.Top = btnAddWidget.Bottom + 8;
            }
            PositionAddWidgetButton();
            this.Resize += (s, e) => PositionAddWidgetButton();

            lblEmptyGridHint.Text = "Widget eklemek için sağ üstteki + butonuna tıklayın.";
            lblEmptyGridHint.ForeColor = TextMuted;
            lblEmptyGridHint.Font = new Font("Segoe UI", 10F);
            lblEmptyGridHint.AutoSize = true;
            lblEmptyGridHint.BackColor = Color.Transparent;
            lblEmptyGridHint.Left = CardLeft1;
            lblEmptyGridHint.Top = MiniRowTop + 20;

            this.Controls.Add(lblEmptyGridHint);
            this.Controls.Add(btnAddWidget);
            this.Controls.Add(pnlWidgetPicker);
            // WinForms'ta önce eklenen denetim üstte kalır — sekiz (şeffaf) hücre paneli bundan önce
            // eklendiği için ipucu metnini büyük ölçüde örtüyordu (sadece hücreler arası dar boşluktan
            // birkaç harf sızıyordu). Öne alarak düzeltiyoruz.
            lblEmptyGridHint.BringToFront();
            btnAddWidget.BringToFront();
        }

        // Widgetları Düzenle panelindeki satırları, güncel yerleşime göre (eklenmiş/eklenebilir) yeniden çizer.
        private void BuildWidgetPickerRows()
        {
            pnlWidgetPicker.Controls.Clear();

            Label lblTitle = new Label { Text = "Widgetları Düzenle", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, Left = 16, Top = 14, AutoSize = true, BackColor = Color.Transparent };
            Label lblHint = new Label { Text = "Sürükleyip ızgaraya bırakın", Font = new Font("Segoe UI", 8.5F), ForeColor = TextMuted, Left = 16, Top = 38, AutoSize = true, BackColor = Color.Transparent };
            pnlWidgetPicker.Controls.Add(lblTitle);
            pnlWidgetPicker.Controls.Add(lblHint);

            int top = 64;
            foreach (var def in WidgetCatalog)
            {
                bool placed = _placedWidgets.ContainsKey(def.Key);

                Panel row = new Panel { Left = 12, Top = top, Width = pnlWidgetPicker.Width - 24, Height = 38, BackColor = Color.Transparent };

                // "Yaklaşan Hatırlatıcılar" gibi uzun başlıklar eskiden 160px'e sığmayıp kesiliyordu —
                // durum etiketine (yalnızca yerleştirilmiş widget'larda dolu) ayrılan sabit pay küçültüldü
                // ve isim etiketi genişletildi; AutoEllipsis de yine de sığmayan durumlar için güvence.
                Label lblHandle = new Label { Text = "⠿", Font = new Font("Segoe UI", 10F), ForeColor = placed ? AppTheme.GridLineColor : TextMuted, Left = 4, Top = 10, Width = 18, Height = 18, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter };
                // 56px'te "Eklendi" kırpılıp "Eklend" olarak görünüyordu; genişletildi ve satırın
                // sağ kenarından biraz boşluk bırakıldı.
                Label lblStatus = new Label { Text = placed ? "Eklendi" : "", Font = new Font("Segoe UI", 8F), ForeColor = AppTheme.SuccessColor, Left = row.Width - 74, Top = 0, Width = 70, Height = 38, TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent };
                Label lblName = new Label { Text = def.Title, Font = new Font("Segoe UI", 9.5F), ForeColor = placed ? TextMuted : TextLight, Left = 26, Top = 0, Width = lblStatus.Left - 26, Height = 38, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent };

                row.Controls.Add(lblHandle);
                row.Controls.Add(lblName);
                row.Controls.Add(lblStatus);

                if (!placed)
                {
                    row.Cursor = Cursors.SizeAll;
                    // Panel, sağ sütunun (CardLeft4) tam üzerinde açılıyor — sürükleme sırasında açık
                    // kalırsa oraya bırakmayı imkansız kılıyordu (bırakma hedefi panelin kendisi oluyordu,
                    // altındaki hücre değil). Sürükleme başlar başlamaz paneli kapatıyoruz.
                    MouseEventHandler startDrag = (s, e) =>
                    {
                        pnlWidgetPicker.Visible = false;
                        StartWidgetDrag(row, def.Key, def.ColSpan);
                    };
                    row.MouseDown += startDrag;
                    lblHandle.MouseDown += startDrag;
                    lblName.MouseDown += startDrag;
                }

                pnlWidgetPicker.Controls.Add(row);
                top += 40;
            }

            pnlWidgetPicker.Height = top + 12;
        }

        // Bir widget'ı belirtilen hücreye (anchorCell) yerleştirir. Hedef hücrelerle çakışan başka
        // widget'lar varsa (3 hücre kaplayan Varlık Bildirimleri'ni taşırken bu, aynı anda 1-3 farklı
        // widget'la çakışabilir) SİLİNMEZ — önce sürüklenenin boşalttığı eski yere (uygunsa), yoksa
        // ızgaradaki ilk boş hücreye taşınmaya çalışılır; gerçekten hiç yer kalmadıysa (nadir) tahtadan
        // kaldırılır ve + panelinden tekrar eklenebilir. Önceden yalnızca "tek widget'la tam takas"
        // durumu ele alınıyordu, geri kalan her şey sessizce siliniyordu.
        // anchorCell'e bırakılırsa key'in gerçekte hangi hücreleri kaplayacağını (satır sınırına göre
        // kırpılmış) hesaplar — hem yerleştirme hem de sürükleme sırasındaki önizleme aynı mantığı kullanır.
        private List<int> GetClampedSpanCells(string key, int anchorCell)
        {
            var def = WidgetCatalog.FirstOrDefault(w => w.Key == key);
            if (def == null) return new List<int> { anchorCell };
            int col = anchorCell % GridCols, row = anchorCell / GridCols;
            if (col + def.ColSpan > GridCols) col = GridCols - def.ColSpan;
            anchorCell = row * GridCols + col;
            return Enumerable.Range(anchorCell, def.ColSpan).ToList();
        }

        // Aynı hedef zaten vurgulanıyorsa yeniden çizmiyoruz (DragOver imleç durağan olsa bile çok sık
        // tetiklenebiliyor — sürükleme sırasında fark edilir bir yavaşlamaya/"kasma"ya yol açıyordu).
        private void HighlightDragTarget(int anchorCell, IDataObject? data)
        {
            string? key = data?.GetData(typeof(string)) as string;
            var newCells = key != null ? GetClampedSpanCells(key, anchorCell) : new List<int>();
            if (newCells.Count > 0 && _highlightedCells.SetEquals(newCells)) return;

            _highlightedCells.Clear();
            foreach (var c in newCells) _highlightedCells.Add(c);
            _activeDropAnchor = newCells.Count > 0 ? newCells.Min() : (int?)null;
            InvalidateHighlightRegion(newCells.Count > 0 ? GetCellsBoundingRect(newCells) : Rectangle.Empty);
        }

        private void ClearDragHighlight()
        {
            if (_highlightedCells.Count == 0) return;
            _highlightedCells.Clear();
            _activeDropAnchor = null;
            InvalidateHighlightRegion(Rectangle.Empty);
        }

        // Önceki ve yeni hedefin dörtgenini kapsayan KÜÇÜK bölgeyi geçersiz kılar. Önceden her hedef
        // değişiminde ızgaranın 3 satırının TAMAMI geçersiz kılınıyordu (bkz. eski RefreshDragHighlightVisuals)
        // — bu, o satırlardaki BAŞKA widget'ların da (ör. "Bu Ayın Özeti"ndeki Chart denetimi; GDI+ grafik
        // çizimi ucuz değil) gereksiz yere yeniden çizilmesine yol açıp sürükleme sırasında, özellikle
        // geçerli bir hedef yokken/değişirken, gözle görülür bir donmaya neden oluyordu (ekran kaydıyla
        // doğrulandı). Sadece hedefin gerçekten bulunduğu küçük alanı geçersiz kılmak bunu ortadan kaldırıyor.
        private Rectangle _lastHighlightRect = Rectangle.Empty;
        private void InvalidateHighlightRegion(Rectangle newRect)
        {
            Rectangle combined = _lastHighlightRect.IsEmpty ? newRect
                : (newRect.IsEmpty ? _lastHighlightRect : Rectangle.Union(_lastHighlightRect, newRect));
            if (!combined.IsEmpty)
            {
                combined.Inflate(4, 4);
                this.Invalidate(combined);
            }
            _lastHighlightRect = newRect;
        }

        // Sürükleme başında/sonunda TÜM uygun yuvaları (bkz. _validDropAnchors) bir kerelik göstermek/
        // temizlemek için — bu ender çağrılır (drag başına 2 kez), bu yüzden tüm ızgarayı geçersiz
        // kılmak burada sorun değil.
        private void RefreshDragHighlightVisuals()
        {
            int totalRows = _cellPanels.Length / GridCols;
            var gridArea = new Rectangle(CardLeft1 - 4, MiniRowTop - 4, CardLeft4 + CardWidth - CardLeft1 + 8, totalRows * (MiniRowHeight + 20) + 8);
            this.Invalidate(gridArea);
            _lastHighlightRect = Rectangle.Empty;
        }

        private void PlaceWidget(string key, int anchorCell)
        {
            var def = WidgetCatalog.FirstOrDefault(w => w.Key == key);
            if (def == null) return;

            int col = anchorCell % GridCols, row = anchorCell / GridCols;
            if (col + def.ColSpan > GridCols) col = GridCols - def.ColSpan;
            anchorCell = row * GridCols + col;

            int? previousCell = (_placedWidgets.TryGetValue(key, out var pc) && PageOf(pc) == _currentPage) ? CellOf(pc) : (int?)null;
            _placedWidgets.Remove(key);

            var newCells = new HashSet<int>(Enumerable.Range(anchorCell, def.ColSpan));

            var displaced = _placedWidgets
                .Where(kv => PageOf(kv.Value) == _currentPage &&
                    Enumerable.Range(CellOf(kv.Value), WidgetCatalog.First(w => w.Key == kv.Key).ColSpan).Any(c => newCells.Contains(c)))
                .Select(kv => kv.Key)
                .ToList();

            foreach (var dKey in displaced) _placedWidgets.Remove(dKey);
            _placedWidgets[key] = EncodeCell(_currentPage, anchorCell);

            foreach (var dKey in displaced)
            {
                var dDef = WidgetCatalog.First(w => w.Key == dKey);
                int? target = null;
                if (previousCell.HasValue && dDef.ColSpan == def.ColSpan && IsCellRangeFree(previousCell.Value, dDef.ColSpan))
                {
                    target = previousCell.Value;
                    previousCell = null; // eski yer sadece bir widget'a verilebilir
                }
                else
                {
                    target = FindFreeCell(dDef.ColSpan);
                }
                if (target.HasValue) _placedWidgets[dKey] = EncodeCell(_currentPage, target.Value);
            }

            SaveLayout();
            RebuildWidgetGrid();
        }

        // [anchorCell, anchorCell+span) hücrelerinin tamamı satır sınırını aşmadan, GEÇERLİ SAYFADA boşta
        // mı? ignoreKey verilirse o widget'ın kendi (taşınmakta olan) hücreleri dolu sayılmaz — sürükleme
        // başında "tüm uygun yuvaları" hesaplarken (bkz. StartWidgetDrag) widget'ın şu anki yerinin de
        // geçerli bir hedef olarak görünmesi için.
        private bool IsCellRangeFree(int anchorCell, int span, string? ignoreKey = null)
        {
            int col = anchorCell % GridCols;
            if (col + span > GridCols) return false;
            var cells = Enumerable.Range(anchorCell, span).ToList();
            return !_placedWidgets.Any(kv => kv.Key != ignoreKey && PageOf(kv.Value) == _currentPage &&
                Enumerable.Range(CellOf(kv.Value), WidgetCatalog.First(w => w.Key == kv.Key).ColSpan).Any(c => cells.Contains(c)));
        }

        // Verilen genişlikte bir widget için ızgarada uygun ilk boş hücreyi (satır satır, soldan sağa) bulur.
        private int? FindFreeCell(int span)
        {
            int rows = _cellPanels.Length / GridCols;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c <= GridCols - span; c++)
                {
                    int anchor = r * GridCols + c;
                    if (IsCellRangeFree(anchor, span)) return anchor;
                }
            }
            return null;
        }

        // Sürükleme sırasında, widget'ın gerçek boyutunu (ör. Varlık Bildirimleri'nin 3 hücre kapladığını)
        // gösteren yarı saydam bir önizleme dikdörtgenini fareyle birlikte hareket ettirir — öncesinde
        // sürükleme sırasında yalnızca varsayılan (küçük, tek hücrelik izlenim veren) imleç görünüyordu.
        private Form? _dragPreviewForm;

        // Salt görsel bir "hayalet" pencere — fare olaylarını YAKALAMAMASI gerekiyor. Önizleme imlecin
        // sağ-alt çaprazına konurken bu sorun hiç fark edilmemişti; imlecin TAM ÜZERİNE ortalanınca
        // (bkz. StartWidgetDrag'daki not) bu TopMost pencere artık imlecin doğrudan altında oturuyor —
        // Windows'un OLE sürükle-bırak "hedef" araması, o noktada ilk bulduğu pencereyi (asıl ızgara
        // hücresi yerine bu önizlemeyi) hedef sanıp hiçbir yere bırakılmasına izin vermiyordu.
        // WS_EX_TRANSPARENT, bu pencereyi fare/vuruş testi (hit-test) için tamamen görünmez kılar;
        // sürükle-bırak hedefi her zaman altındaki gerçek denetime ulaşır.
        private class DragPreviewForm : Form
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    const int WS_EX_TRANSPARENT = 0x20;
                    var cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_TRANSPARENT;
                    return cp;
                }
            }
        }

        private void StartWidgetDrag(Control source, string key, int colSpan)
        {
            int width = colSpan * CardWidth + (colSpan - 1) * 20;
            _dragPreviewForm = new DragPreviewForm
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Size = new Size(width, MiniRowHeight),
                BackColor = AccentColor,
                Opacity = 0.35,
                TopMost = true
            };
            // Köşeleri yuvarlatmak için bir ara denendi (Region = yuvarlak köşeli yol) ama katmanlı
            // (Opacity<1 → WS_EX_LAYERED) ve sık sık taşınan (bkz. aşağıdaki GiveFeedback) bir pencereye
            // Region eklemek, Windows'un her karede yeniden bileştirme yapmasına yol açıp sürükleme
            // sırasında (özellikle geçerli bir bırakma hedefi yokken, "yasak" imleci gösterilirken) fark
            // edilir bir kasmaya neden oluyordu — köşeler bu yüzden düz bırakıldı. Statik olarak çizilen
            // (bir pencere gibi taşınmayan) kesikli kutular (bkz. DrawDragHighlightOverlay) bu sorunu
            // yaşamıyor, onlar yuvarlak kalıyor.
            _dragPreviewForm.Show();

            // Sürükleme başlar başlamaz, bu widget'ın (boyutuna göre) bırakılabileceği TÜM başlangıç
            // hücrelerini işaretliyoruz — kullanıcı imleci belirli bir hücreye götürmeden önce bile
            // nereye koyabileceğini görsün diye (bkz. this.Paint'teki soluk/kesikli önizleme kutuları).
            _dragColSpan = colSpan;
            _validDropAnchors.Clear();
            int totalRows = _cellPanels.Length / GridCols;
            for (int r = 0; r < totalRows; r++)
            {
                for (int c = 0; c <= GridCols - colSpan; c++)
                {
                    int anchor = r * GridCols + c;
                    if (IsCellRangeFree(anchor, colSpan, ignoreKey: key)) _validDropAnchors.Add(anchor);
                }
            }
            RefreshDragHighlightVisuals();

            // Önizlemenin (ve dolayısıyla imlecin sürüklerken izlendiği alanın) widget ızgarasının
            // dışına çıkmasını engelliyoruz — köşeleri düzleştirmek donmayı tamamen gidermedi, imleç
            // ızgara dışına (kenar çubuğu, başka bir pencere, masaüstü) çıktığında bu katmanlı pencere
            // hâlâ oraya taşınıp Windows'a onu YABANCI pencerelerin/masaüstünün üzerinde yeniden
            // bileştirtiyordu — bu da ekran kaydında görülen donmanın asıl kaynağıydı. Önizleme artık
            // ekran koordinatında ızgara sınırlarının içinde tutuluyor, imleç dışarı çıksa bile.
            int totalRowsForClamp = _cellPanels.Length / GridCols;
            var gridClientRect = new Rectangle(CardLeft1, MiniRowTop, CardLeft4 + CardWidth - CardLeft1, totalRowsForClamp * (MiniRowHeight + 20) - 20);
            var gridScreenTopLeft = this.PointToScreen(new Point(gridClientRect.Left, gridClientRect.Top));
            var gridScreenBottomRight = this.PointToScreen(new Point(gridClientRect.Right, gridClientRect.Bottom));

            GiveFeedbackEventHandler onGiveFeedback = (s, e) =>
            {
                e.UseDefaultCursors = true;
                if (_dragPreviewForm == null) return;
                var p = System.Windows.Forms.Cursor.Position;
                // Önceden imlecin sağ-alt çaprazına (+16,+16) konuyordu — imleç önizlemenin bir
                // köşesindeymiş gibi duruyordu. Widget'ın gerçek boyutunu imlecin merkezinde gösteriyoruz.
                int x = p.X - width / 2;
                int y = p.Y - MiniRowHeight / 2;
                x = Math.Max(gridScreenTopLeft.X, Math.Min(gridScreenBottomRight.X - width, x));
                y = Math.Max(gridScreenTopLeft.Y, Math.Min(gridScreenBottomRight.Y - MiniRowHeight, y));
                _dragPreviewForm.Location = new Point(x, y);
            };
            source.GiveFeedback += onGiveFeedback;
            try
            {
                source.DoDragDrop(key, DragDropEffects.Move);
            }
            finally
            {
                source.GiveFeedback -= onGiveFeedback;
                _dragPreviewForm?.Close();
                _dragPreviewForm?.Dispose();
                _dragPreviewForm = null;
                _validDropAnchors.Clear();
                RefreshDragHighlightVisuals();
            }
        }

        private void RemoveWidgetFromGrid(string key)
        {
            _placedWidgets.Remove(key);
            SaveLayout();
            RebuildWidgetGrid();
        }

        // Yerleştirilmiş her widget için (ilk seferinde) bir "frame" (sürükleme tutamacı + kaldırma
        // düğmesi + gerçek içerik paneli) kurar/konumlandırır; yerleştirilmemiş olanların frame'i varsa gizler.
        private void RebuildWidgetGrid()
        {
            foreach (var def in WidgetCatalog)
            {
                // Bir widget yalnızca TEK bir sayfada olabilir (bkz. EncodeCell) — geçerli sayfada
                // değilse (ya hiç yerleştirilmemiş ya da diğer sayfadaysa) çerçevesi gizlenir.
                bool isPlaced = _placedWidgets.TryGetValue(def.Key, out int stored) && PageOf(stored) == _currentPage;
                if (!isPlaced)
                {
                    if (_widgetFrames.TryGetValue(def.Key, out var hiddenFrame)) hiddenFrame.Visible = false;
                    continue;
                }
                int cell = CellOf(stored);

                if (!_widgetFrames.TryGetValue(def.Key, out var frame))
                {
                    frame = CreateWidgetFrame(def);
                    _widgetFrames[def.Key] = frame;
                    this.Controls.Add(frame);
                }

                int col = cell % GridCols, row = cell / GridCols;
                frame.Left = CardLeft1 + col * (CardWidth + 20);
                frame.Top = MiniRowTop + row * (MiniRowHeight + 20);
                frame.Width = def.ColSpan * CardWidth + (def.ColSpan - 1) * 20;
                frame.Height = MiniRowHeight;
                frame.Visible = true;
                frame.BringToFront();

                LoadWidgetContent(def.Key);
            }

            lblEmptyGridHint.BringToFront();
            btnAddWidget.BringToFront();
            if (pnlWidgetPicker.Visible) BuildWidgetPickerRows();
            pnlWidgetPicker.BringToFront();
            pnlPageDot0.BringToFront();
            pnlPageDot1.BringToFront();

            lblEmptyGridHint.Visible = !_placedWidgets.Any(kv => PageOf(kv.Value) == _currentPage);
            pnlPageDot0.Invalidate();
            pnlPageDot1.Invalidate();
        }

        // Bir widget'ı barındıran dış çerçeve: sürükleme tutamacı ve kaldırma düğmesi, içerik panelinin
        // kendi Controls.Clear() tabanlı yenilemelerinden (bkz. LoadReminderWidget vb.) etkilenmeyecek
        // şekilde bu dış panelde tutuluyor.
        private Panel CreateWidgetFrame(WidgetDef def)
        {
            // BackColor=Transparent burada WinForms'un "sahte" saydamlığına düşüyordu: köşelerdeki
            // yuvarlatma, içerik panelinin kendi rounded-rect çizimiyle oluşuyor, ama dış çerçevenin
            // KENDİSİ saydam olduğunda o köşe üçgenlerinin arkasında doğru koyu renk yerine bozuk/
            // kare görünümlü bir dolgu kalıyordu. Düz (opak) AppBackColor ile bu ortadan kalkıyor.
            Panel frame = new Panel { BackColor = AppBackColor, AllowDrop = true };

            // bkz. OnGridDragEnterOrOver'daki üstteki not — boş hücreler VE dolu çerçeveler artık aynı,
            // imlecin gerçek konumuna dayalı hesaplamayı kullanıyor (önceden burada bu çerçevenin KENDİ
            // sabit hücresi hedef alınıyordu, tutarsızdı).
            frame.DragEnter += OnGridDragEnterOrOver;
            frame.DragOver += OnGridDragEnterOrOver;
            frame.DragLeave += OnGridDragLeave;
            frame.DragDrop += OnGridDragDrop;

            Panel content = GetContentPanelFor(def.Key);
            content.Dock = DockStyle.Fill;
            frame.Controls.Add(content);

            // Aynı sebeple (frame'in kendi "saydam" arka planı, altındaki kartın gerçek CardBackColor
            // dolgusuyla eşleşmiyordu) tutamaç ve kaldırma düğmesinin arkasında koyu bir leke
            // görünüyordu — bu iki etiket artık kartın dolgu rengiyle aynı, düz bir arka plan kullanıyor.
            Label dragHandle = new Label
            {
                Text = "⠿",
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextMuted,
                BackColor = CardBackColor,
                Left = 2,
                Top = 8,
                Width = 15,
                Height = 16,
                Cursor = Cursors.SizeAll,
                TextAlign = ContentAlignment.MiddleCenter
            };
            dragHandle.MouseDown += (s, e) =>
            {
                if (_placedWidgets.ContainsKey(def.Key)) StartWidgetDrag(frame, def.Key, def.ColSpan);
            };
            frame.Controls.Add(dragHandle);
            dragHandle.BringToFront();

            Label removeBtn = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = TextMuted,
                BackColor = CardBackColor,
                Width = 18,
                Height = 18,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            void PositionRemoveBtn() { removeBtn.Left = frame.Width - 24; removeBtn.Top = 8; }
            PositionRemoveBtn();
            frame.Resize += (s, e) => PositionRemoveBtn();
            removeBtn.Click += (s, e) => RemoveWidgetFromGrid(def.Key);
            removeBtn.MouseEnter += (s, e) => removeBtn.ForeColor = AppTheme.DangerColor;
            removeBtn.MouseLeave += (s, e) => removeBtn.ForeColor = TextMuted;
            frame.Controls.Add(removeBtn);
            removeBtn.BringToFront();

            return frame;
        }

        private Panel CreateWidgetCard(int left, string titleText, string navigateTarget)
        {
            Panel card = new Panel { Left = left, Top = MiniRowTop, Width = CardWidth, Height = MiniRowHeight };
            SetupSmoothContainer(card, 16, CardBackColor);

            Label lblTitle = new Label
            {
                Text = titleText,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 18,
                Top = 14,
                // AutoSize=true idi — uzun başlıklar (ör. "Yaklaşan Tekrarlayan Ödemeler") kartın
                // sağ üstündeki kaldırma (✕) düğmesinin altına/üstüne binip kesiliyordu. Sabit
                // genişlik + "..." ile kısaltma; 24, kaldırma düğmesinin sol kenarıyla (CreateWidgetFrame'de
                // frame.Width - 24) hizalanacak şekilde seçildi.
                Width = CardWidth - 18 - 24,
                // 24px'te bu büyüklükteki (11.5F Bold) yazı tipinde "ç"/"ş"/"ğ" gibi harflerin alt
                // kuyruğu kesiliyordu (bkz. bu dosyadaki diğer 20px→24px notları, burada yazı tipi
                // daha büyük olduğundan 28 gerekti).
                Height = 28,
                AutoSize = false,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Tag = "title"
            };
            card.Controls.Add(lblTitle);

            MakeClickable(card, () => _onNavigate?.Invoke(navigateTarget));
            return card;
        }

        // Tutarları gizle açılıp kapandığında ya da veriler değiştiğinde bir mini-widget'ı yeniden
        // doldurmak için, başlık etiketi dışındaki tüm önceki içeriği (ve varsa zamanlayıcılarını) temizler.
        private void ClearWidgetContent(Panel card)
        {
            var toRemove = card.Controls.Cast<Control>().Where(c => !(c.Tag is string s && s == "title")).ToList();
            foreach (var c in toRemove)
            {
                card.Controls.Remove(c);
                if (_cardAnimTimers.TryGetValue(c, out var timer))
                {
                    timer.Stop();
                    timer.Dispose();
                    _cardAnimTimers.Remove(c);
                }
                c.Dispose();
            }
        }

        // Panel ve içindeki her denetim (WinForms'ta Click yukarı aktarılmaz) için tıklanabilir
        // el imleci ve navigasyon davranışı ekler.
        private void MakeClickable(Control root, Action onClick)
        {
            root.Cursor = Cursors.Hand;
            root.Click += (s, e) => onClick();
            foreach (Control child in root.Controls)
            {
                MakeClickable(child, onClick);
            }
        }

        private void LoadReminderWidget()
        {
            ClearWidgetContent(pnlReminderWidget);

            var upcoming = _reminderService.GetUserReminders(_user.Id)
                .Where(r => !r.IsCompleted && r.ReminderDate >= DateTime.Now)
                .OrderBy(r => r.ReminderDate)
                .Take(5)
                .ToList();

            Action goToReminders = () => _onNavigate?.Invoke("Hatırlatıcılar");

            int top = 48;
            if (upcoming.Count == 0)
            {
                AddWidgetLine(pnlReminderWidget, "Yaklaşan hatırlatıcı yok.", top, TextMuted, 18, goToReminders);
                return;
            }

            foreach (var r in upcoming)
            {
                string line = $"{r.Title}  —  {r.ReminderDate:dd.MM.yyyy}";
                AddWidgetLine(pnlReminderWidget, line, top, TextLight, 18, goToReminders);
                top += 30;
            }
        }

        private void LoadRecentTransactionsWidget()
        {
            ClearWidgetContent(pnlRecentTxWidget);

            var recent = _transactionService.GetUserTransactions(_user.Id).Take(5).ToList();
            var tr = new System.Globalization.CultureInfo("tr-TR");

            int top = 48;
            if (recent.Count == 0)
            {
                AddWidgetLine(pnlRecentTxWidget, "Henüz işlem yok.", top, TextMuted, 18, () => _onNavigate?.Invoke("İşlemler"));
                return;
            }

            foreach (var t in recent)
            {
                Color amountColor = t.Type == "income" ? IncomeColor : ExpenseColor;

                Label lblCategory = new Label
                {
                    Text = t.CategoryName,
                    ForeColor = TextLight,
                    Left = 18,
                    Top = top,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5F)
                };
                Label lblAmount = new Label
                {
                    ForeColor = amountColor,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Left = CardWidth - 150,
                    Top = top,
                    Width = 130,
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent
                };
                pnlRecentTxWidget.Controls.Add(lblCategory);
                pnlRecentTxWidget.Controls.Add(lblAmount);
                MakeClickable(lblCategory, () => _onNavigate?.Invoke("İşlemler"));
                MakeClickable(lblAmount, () => _onNavigate?.Invoke("İşlemler"));

                AnimateLabelValue(lblAmount, t.Amount, v => v.ToString("#,##0", tr) + " ₺", hiddenText: "••••••");
                top += 30;
            }
        }

        private void LoadMiniReportWidget()
        {
            ClearWidgetContent(pnlMiniReportWidget);

            // ClearWidgetContent az önce bir önceki üretimin pnlMiniPage*/pnlMiniDot* örneklerini dispose
            // etti; aynı alan adlarını (field) taze örneklerle değiştirmeden yeniden kullanmaya çalışmak
            // "disposed nesne" hatasına yol açardı — bu yüzden her yenilemede sıfırdan kuruyoruz. Sayfa
            // seçimi (_miniReportPage) ise ayrı bir int alanda tutulduğundan yenilemeler arasında korunur.
            pnlMiniPageGeneral = new Panel();
            pnlMiniPageAssets = new Panel();
            pnlMiniDot0 = new Panel();
            pnlMiniDot1 = new Panel();

            DateTime start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime end = start.AddMonths(1);
            var report = _reportService.GenerateReport(_user.Id, start, end);
            var (wallet, _) = _accountService.GetBalances(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            // Rapor ekranındaki Genel pastayla aynı kırılım: gelir + gider + hedef + yatırım + boşta kalan.
            // Önceden burada sadece gelir/gider vardı ve Rapor'daki güncel dağılımı yansıtmıyordu.
            decimal categorySum = report.TotalIncome + report.TotalExpense;
            decimal idle = wallet - categorySum;
            if (idle < 0) idle = 0;
            decimal goalTotal = report.TotalGoal;
            decimal investTotal = report.TotalInvest;

            pnlMiniPageGeneral.Left = 0; pnlMiniPageGeneral.Top = 44; pnlMiniPageGeneral.Width = CardWidth; pnlMiniPageGeneral.Height = MiniRowHeight - 44;
            pnlMiniPageGeneral.BackColor = Color.Transparent;

            Chart miniChart = new Chart { Left = 14, Top = 0, Width = 130, Height = 150, BackColor = CardBackColor };
            ChartArea area = new ChartArea("mini") { BackColor = CardBackColor };
            area.Position = new ElementPosition(0, 0, 100, 100);
            area.InnerPlotPosition = new ElementPosition(0, 0, 100, 100);
            miniChart.ChartAreas.Add(area);

            Series series = new Series { ChartType = SeriesChartType.Doughnut, ChartArea = "mini" };
            series["DoughnutRadius"] = "55";
            series["PieLabelStyle"] = "Disabled";

            void AddSlice(decimal amount, Color color)
            {
                if (amount <= 0) return;
                int idx = series.Points.AddY((double)amount);
                series.Points[idx].Color = color;
            }
            AddSlice(report.TotalIncome, IncomeColor);
            AddSlice(report.TotalExpense, ExpenseColor);
            AddSlice(goalTotal, AppTheme.GoalColor);
            AddSlice(investTotal, AppTheme.InvestColor);
            AddSlice(idle, AppTheme.IdleColor);
            if (series.Points.Count == 0)
            {
                int i1 = series.Points.AddY(1);
                series.Points[i1].Color = TextMuted;
            }
            miniChart.Series.Add(series);
            pnlMiniPageGeneral.Controls.Add(miniChart);

            Action goToReport = () => _onNavigate?.Invoke("Rapor");
            Label lblGelir = AddWidgetLine(pnlMiniPageGeneral, "Gelir: 0 ₺", 48, IncomeColor, 158, goToReport);
            Label lblGider = AddWidgetLine(pnlMiniPageGeneral, "Gider: 0 ₺", 78, ExpenseColor, 158, goToReport);
            AnimateLabelValue(lblGelir, report.TotalIncome, v => $"Gelir: {v.ToString("#,##0", tr)} ₺", hiddenText: "Gelir: ••••••");
            AnimateLabelValue(lblGider, report.TotalExpense, v => $"Gider: {v.ToString("#,##0", tr)} ₺", hiddenText: "Gider: ••••••");
            MakeClickable(miniChart, goToReport);

            // İkinci sayfa: Varlıklarım'daki varlık dağılımı (canlı fiyatlarla), aşağıda asenkron doldurulur.
            pnlMiniPageAssets.Left = 0; pnlMiniPageAssets.Top = 44; pnlMiniPageAssets.Width = CardWidth; pnlMiniPageAssets.Height = MiniRowHeight - 44;
            pnlMiniPageAssets.BackColor = Color.Transparent;
            pnlMiniPageAssets.Visible = false;

            Label lblAssetsLoading = new Label { Text = "Varlık verisi yükleniyor...", ForeColor = TextMuted, Left = 14, Top = 48, AutoSize = true, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9.5F) };
            pnlMiniPageAssets.Controls.Add(lblAssetsLoading);

            pnlMiniReportWidget.Controls.Add(pnlMiniPageGeneral);
            pnlMiniReportWidget.Controls.Add(pnlMiniPageAssets);

            SetupMiniPageDots();
            UpdateMiniReportPageVisibility();

            _ = LoadMiniAssetPageAsync();
        }

        // Kartın sağ alt köşesindeki iki nokta: tıklanınca genel/varlık sayfaları arasında geçiş yapar.
        // Görünen nokta (dotSize) ile tıklanabilir alan (hitSize) ayrı tutuluyor; eskiden ikisi de 7px'ti
        // ve tıklamak zordu — tıklama alanı görünenden büyük tutularak (nokta ortalanarak) kolaylaştırıldı.
        private void SetupMiniPageDots()
        {
            const int dotSize = 11;
            const int hitSize = 22;
            const int gap = 2;
            const int rightPad = 10;
            int dot1Left = CardWidth - rightPad - hitSize;
            int dot0Left = dot1Left - gap - hitSize;
            const int dotTop = MiniRowHeight - hitSize - 6;
            const int dotOffset = (hitSize - dotSize) / 2;

            void SetupDot(Panel dot, int left, int page)
            {
                dot.Left = left; dot.Top = dotTop; dot.Width = hitSize; dot.Height = hitSize;
                dot.BackColor = Color.Transparent;
                dot.Cursor = Cursors.Hand;
                dot.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    Color c = _miniReportPage == page ? AccentColor : AppTheme.GridLineColor;
                    using var brush = new SolidBrush(c);
                    e.Graphics.FillEllipse(brush, dotOffset, dotOffset, dotSize, dotSize);
                };
                dot.Click += (s, e) =>
                {
                    if (_miniReportPage == page) return;
                    _miniReportPage = page;
                    UpdateMiniReportPageVisibility();
                };
            }

            SetupDot(pnlMiniDot0, dot0Left, 0);
            SetupDot(pnlMiniDot1, dot1Left, 1);

            pnlMiniReportWidget.Controls.Add(pnlMiniDot0);
            pnlMiniReportWidget.Controls.Add(pnlMiniDot1);
            // Sayfa panelleri (pnlMiniPageGeneral/Assets) aynı üst denetimin çocuğu ve noktalarla aynı
            // köşede çakışıyor; WinForms'ta önce eklenen denetim üstte kaldığından noktalar görünmez
            // oluyordu — en öne alıyoruz.
            pnlMiniDot0.BringToFront();
            pnlMiniDot1.BringToFront();
        }

        private void UpdateMiniReportPageVisibility()
        {
            pnlMiniPageGeneral.Visible = _miniReportPage == 0;
            pnlMiniPageAssets.Visible = _miniReportPage == 1;
            pnlMiniDot0.Invalidate();
            pnlMiniDot1.Invalidate();
        }

        // Ard arda tema/gizle tazelemeleri LoadMiniReportWidget'ı (ve dolayısıyla bu metodu) hızlıca
        // birden fazla kez tetikleyebilir; her çağrı pnlMiniPageAssets'i TAZE bir Panel'e bağlıyor
        // (bkz. LoadMiniReportWidget) ama önceki bir çağrının "await" sonrası devamı, o sırada CURRENT
        // olan (daha yeni bir çağrının) pnlMiniPageAssets alanına yazabilirdi. ReportControl'deki aynı
        // desenli bir yarış durumuna (Genel/Varlıklarım hızlı geçişte grafiğin karışması) paralel olarak,
        // burada da istek numarası ile eski sonuçları sessizce atlıyoruz.
        private int _miniAssetRequestId = 0;

        // Varlıklarım'daki pozisyonların canlı fiyatlarla güncel değerine göre dağılım pastası.
        private async Task LoadMiniAssetPageAsync()
        {
            int requestId = ++_miniAssetRequestId;
            Panel targetPanel = pnlMiniPageAssets;

            var holdings = await _assetService.GetHoldingsWithLivePricesAsync(_user.Id);
            if (this.IsDisposed || requestId != _miniAssetRequestId) return;

            targetPanel.Controls.Clear();

            // Bu widget "Bu Ayın Özeti" kartına ait olduğundan, hangi sayfasında olursa olsun
            // tıklama her zaman Rapor ekranına götürmeli (önceden bu sayfa Varlıklarım'a atıyordu).
            Action goToAssets = () => _onNavigate?.Invoke("Rapor");

            var grouped = holdings
                .Where(h => (h.CurrentValueTry ?? 0) > 0)
                .OrderByDescending(h => h.CurrentValueTry ?? 0)
                .ToList();

            if (grouped.Count == 0)
            {
                Label lblEmpty = new Label { Text = "Henüz bir varlığınız yok.", ForeColor = TextMuted, Left = 14, Top = 48, AutoSize = true, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9.5F) };
                targetPanel.Controls.Add(lblEmpty);
                MakeClickable(lblEmpty, goToAssets);
                return;
            }

            Chart miniChart = new Chart { Left = 14, Top = 0, Width = 130, Height = 150, BackColor = CardBackColor };
            ChartArea area = new ChartArea("miniAssets") { BackColor = CardBackColor };
            area.Position = new ElementPosition(0, 0, 100, 100);
            area.InnerPlotPosition = new ElementPosition(0, 0, 100, 100);
            miniChart.ChartAreas.Add(area);

            Series series = new Series { ChartType = SeriesChartType.Doughnut, ChartArea = "miniAssets" };
            series["DoughnutRadius"] = "55";
            series["PieLabelStyle"] = "Disabled";

            for (int i = 0; i < grouped.Count; i++)
            {
                int idx = series.Points.AddY((double)(grouped[i].CurrentValueTry ?? 0));
                series.Points[idx].Color = AssetPalette[i % AssetPalette.Length];
            }
            miniChart.Series.Add(series);
            targetPanel.Controls.Add(miniChart);
            MakeClickable(miniChart, goToAssets);

            var tr = new System.Globalization.CultureInfo("tr-TR");
            int top = 8;
            foreach (var h in grouped.Take(3))
            {
                Color c = AssetPalette[grouped.IndexOf(h) % AssetPalette.Length];
                Label lblAsset = AddWidgetLine(targetPanel, $"{h.Symbol}: 0 ₺", top, c, 158, goToAssets);
                decimal value = h.CurrentValueTry ?? 0;
                AnimateLabelValue(lblAsset, value, v => $"{h.Symbol}: {v.ToString("#,##0", tr)} ₺", hiddenText: $"{h.Symbol}: ••••••");
                top += 30;
            }
        }

        // Tamamlanmamış hedeflerden rastgele 3 tanesini, Hedefler ekranındaki ile aynı stilde
        // (dolan yuvarlak çubuk) küçük bir önizleme olarak gösterir.
        private void LoadGoalsWidget()
        {
            ClearWidgetContent(pnlGoalsWidget);

            var rng = new Random();
            var pending = _savingsGoalService.GetUserGoals(_user.Id)
                .Where(g => !g.IsAchieved)
                .OrderBy(_ => rng.Next())
                .Take(3)
                .ToList();

            Action goToGoals = () => _onNavigate?.Invoke("Hedefler");

            if (pending.Count == 0)
            {
                AddWidgetLine(pnlGoalsWidget, "Tamamlanmamış hedef yok.", 48, TextMuted, 18, goToGoals);
                return;
            }

            int top = 48;
            foreach (var g in pending)
            {
                double percent = g.TargetAmount > 0 ? Math.Min(100, Math.Max(0, (double)(g.CurrentAmount / g.TargetAmount * 100))) : 0;

                // Sabit tek satır + "..." ile kısaltma: uzun hedef adları (ör. "masaüstü bilgisayar")
                // eskiden sarıp bir alttaki çubuğun üzerine biniyordu (bkz. AddWidgetLine'daki not).
                Label lblName = new Label
                {
                    Text = g.GoalName,
                    ForeColor = TextLight,
                    Left = 18,
                    Top = top,
                    Width = CardWidth - 90,
                    // 20px, "y"/"ğ" gibi alt uzantılı (descender) harflerin kuyruğunu kesiyordu.
                    Height = 24,
                    AutoSize = false,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5F)
                };
                Label lblPercent = new Label
                {
                    Text = "%0",
                    ForeColor = TextMuted,
                    Left = CardWidth - 78,
                    Top = top,
                    Width = 60,
                    Height = 24,
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5F)
                };
                pnlGoalsWidget.Controls.Add(lblName);
                pnlGoalsWidget.Controls.Add(lblPercent);
                MakeClickable(lblName, goToGoals);
                MakeClickable(lblPercent, goToGoals);

                double shownPercent = 0;
                Panel bar = CreateMiniProgressBar(18, top + 26, CardWidth - 36, () => shownPercent);
                pnlGoalsWidget.Controls.Add(bar);
                MakeClickable(bar, goToGoals);

                AnimateLabelValue(lblPercent, (decimal)percent, v => $"%{v:0}", onTick: v => { shownPercent = (double)v; bar.Invalidate(); });

                top += 48;
            }
        }

        // Hedefler ekranındaki dolan çubukla aynı görsel dil: gri iz + dolu kısım (yaklaşık tamamsa yeşil).
        // Yüzde, sabit bir değer değil bir getter olarak alınır ki yanındaki etiketle birlikte
        // (bkz. AnimateLabelValue'nun onTick'i) 0'dan gerçek değerine doğru animasyonla dolabilsin.
        private Panel CreateMiniProgressBar(int left, int top, int width, Func<double> getPercent)
        {
            const int barHeight = 8;
            Panel bar = new Panel { Left = left, Top = top, Width = width, Height = barHeight, BackColor = Color.Transparent };
            bar.Paint += (s, e) =>
            {
                double percent = getPercent();
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var trackPath = GetRoundedRectPath(new Rectangle(0, 0, bar.Width, bar.Height), barHeight / 2))
                using (var trackBrush = new SolidBrush(AppTheme.GridLineColor))
                    e.Graphics.FillPath(trackBrush, trackPath);

                int fillWidth = (int)(bar.Width * (percent / 100.0));
                if (fillWidth > barHeight)
                {
                    Color fillColor = percent >= 99.9 ? AppTheme.SuccessColor : AccentColor;
                    using var fillPath = GetRoundedRectPath(new Rectangle(0, 0, fillWidth, bar.Height), barHeight / 2);
                    using var fillBrush = new SolidBrush(fillColor);
                    e.Graphics.FillPath(fillBrush, fillPath);
                }
            };
            return bar;
        }

        // Aktif tekrarlayan işlemlerin sıklığına göre önümüzdeki 30 günün yaklaşık net etkisini
        // (Cüzdan+Kasa üzerinden) hesaplayıp tahmini bakiyeyi gösterir. "goal"/"invest" tipinde
        // tekrarlayan işlem yok (bkz. RecurringTransactionDialog, sadece income/expense üretiyor),
        // bu yüzden yalnızca bu ikisi cüzdanı etkiler.
        private void LoadCashflowWidget()
        {
            ClearWidgetContent(pnlCashflowWidget);

            Action goToTransactions = () => _onNavigate?.Invoke("İşlemler");

            Label lblHeader = new Label
            {
                Text = "Nakit Akışı Tahmini",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 18,
                Top = 14,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlCashflowWidget.Controls.Add(lblHeader);
            MakeClickable(lblHeader, goToTransactions);

            Label lblSub = new Label
            {
                Text = "30 gün sonrası tahmini bakiye",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = TextMuted,
                Left = 18,
                Top = 44,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlCashflowWidget.Controls.Add(lblSub);
            MakeClickable(lblSub, goToTransactions);

            var (wallet, safe) = _accountService.GetBalances(_user.Id);
            decimal currentTotal = wallet + safe;

            var activeRecurring = _recurringTransactionService.GetUserRecurring(_user.Id).Where(r => r.IsActive).ToList();

            decimal expectedIncome = 0, expectedExpense = 0;
            foreach (var r in activeRecurring)
            {
                int occurrences = r.Frequency switch
                {
                    "daily" => 30,
                    "weekly" => 30 / 7,
                    _ => 1 // "monthly"
                };
                if (r.Type == "income") expectedIncome += r.Amount * occurrences;
                else if (r.Type == "expense") expectedExpense += r.Amount * occurrences;
            }

            decimal projected = currentTotal + expectedIncome - expectedExpense;
            var tr = new System.Globalization.CultureInfo("tr-TR");

            Label lblProjected = new Label
            {
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = projected >= currentTotal ? IncomeColor : ExpenseColor,
                Left = 18,
                Top = 66,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlCashflowWidget.Controls.Add(lblProjected);
            MakeClickable(lblProjected, goToTransactions);
            AnimateLabelValue(lblProjected, projected, v => v.ToString("#,##0", tr) + " ₺", hiddenText: "••••••");

            if (activeRecurring.Count == 0)
            {
                AddWidgetLine(pnlCashflowWidget, "Aktif tekrarlayan işlem yok.", 112, TextMuted, 18, goToTransactions);
                return;
            }

            Label lblIncome = AddWidgetLine(pnlCashflowWidget, "Beklenen Gelir: +0 ₺", 112, IncomeColor, 18, goToTransactions);
            Label lblExpense = AddWidgetLine(pnlCashflowWidget, "Beklenen Gider: -0 ₺", 142, ExpenseColor, 18, goToTransactions);
            AnimateLabelValue(lblIncome, expectedIncome, v => $"Beklenen Gelir: +{v.ToString("#,##0", tr)} ₺", hiddenText: "Beklenen Gelir: ••••••");
            AnimateLabelValue(lblExpense, expectedExpense, v => $"Beklenen Gider: -{v.ToString("#,##0", tr)} ₺", hiddenText: "Beklenen Gider: ••••••");
        }

        // Bir tekrarlayan işlemin sıradaki işleneceği tarihi tahmin eder — ProcessDueRecurring
        // (giriş yapıldığında süresi gelmişleri otomatik işleyen metod, bkz. RecurringTransactionService)
        // ile aynı "süresi geldi mi" mantığının tarih karşılığı. Hiç işlenmemişse hemen şimdi demektir.
        private static DateTime NextRecurringDueDate(RecurringTransaction r, DateTime today)
        {
            if (r.LastProcessedDate == null) return today;
            DateTime last = r.LastProcessedDate.Value.Date;
            return r.Frequency switch
            {
                "daily" => last.AddDays(1),
                "weekly" => last.AddDays(7),
                _ => new DateTime(last.Year, last.Month, 1).AddMonths(1) // "monthly": bir sonraki ayın 1'i
            };
        }

        // Önümüzdeki 7 gün içinde (ya da süresi çoktan gelmiş) sırası gelecek aktif tekrarlayan
        // işlemleri listeler — Nakit Akışı Tahmini'nin "toplamı" yerine "hangi işlemler" detayını verir.
        private void LoadRecurringUpcomingWidget()
        {
            ClearWidgetContent(pnlRecurringUpcomingWidget);

            Action goToTransactions = () => _onNavigate?.Invoke("İşlemler");
            DateTime today = DateTime.Today;

            var upcoming = _recurringTransactionService.GetUserRecurring(_user.Id)
                .Where(r => r.IsActive)
                .Select(r => new { Recurring = r, NextDue = NextRecurringDueDate(r, today) })
                .Where(x => x.NextDue <= today.AddDays(7))
                .OrderBy(x => x.NextDue)
                .Take(5)
                .ToList();

            if (upcoming.Count == 0)
            {
                AddWidgetLine(pnlRecurringUpcomingWidget, "Yaklaşan tekrarlayan ödeme yok.", 48, TextMuted, 18, goToTransactions);
                return;
            }

            var tr = new System.Globalization.CultureInfo("tr-TR");
            int top = 48;
            foreach (var x in upcoming)
            {
                string dateText = x.NextDue <= today ? "bugün" : x.NextDue == today.AddDays(1) ? "yarın" : x.NextDue.ToString("dd.MM");
                Color amountColor = x.Recurring.Type == "income" ? IncomeColor : ExpenseColor;
                string sign = x.Recurring.Type == "income" ? "+" : "-";

                Label lblLine = new Label
                {
                    Text = $"{x.Recurring.CategoryName} — {dateText}",
                    ForeColor = TextLight,
                    Left = 18,
                    Top = top,
                    Width = CardWidth - 150,
                    Height = 24,
                    AutoSize = false,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5F)
                };
                Label lblAmount = new Label
                {
                    ForeColor = amountColor,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Left = CardWidth - 150,
                    Top = top,
                    Width = 130,
                    Height = 24,
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent
                };
                pnlRecurringUpcomingWidget.Controls.Add(lblLine);
                pnlRecurringUpcomingWidget.Controls.Add(lblAmount);
                MakeClickable(lblLine, goToTransactions);
                MakeClickable(lblAmount, goToTransactions);

                AnimateLabelValue(lblAmount, x.Recurring.Amount, v => $"{sign}{v.ToString("#,##0", tr)} ₺", hiddenText: "••••••");
                top += 30;
            }
        }

        // Bu ayki gider kategorilerini tutara göre sıralayıp ilk 3'ünü gösterir; bütçe limiti
        // tanımlı olanlarda harcama/limit oranını bir ilerleme çubuğuyla da vurgular (bkz.
        // CreateBudgetProgressBar — Hedeflerim'deki çubuktan farklı olarak %100'e YAKLAŞMAK
        // burada iyi değil kötü, bu yüzden renk yönü tersine çevrilmiş).
        private void LoadTopCategoryWidget()
        {
            ClearWidgetContent(pnlTopCategoryWidget);

            Action goToCategories = () => _onNavigate?.Invoke("Kategoriler");
            DateTime today = DateTime.Today;

            var monthlyExpense = _transactionService.GetMonthlyExpenseByCategoryId(_user.Id, today.Year, today.Month);
            var categories = _categoryService.GetUserCategoriesByType(_user.Id, "expense");

            var top3 = categories
                .Select(c => new { Category = c, Spent = monthlyExpense.TryGetValue(c.Id, out decimal s) ? s : 0 })
                .Where(x => x.Spent > 0)
                .OrderByDescending(x => x.Spent)
                .Take(3)
                .ToList();

            if (top3.Count == 0)
            {
                AddWidgetLine(pnlTopCategoryWidget, "Bu ay henüz gider yok.", 48, TextMuted, 18, goToCategories);
                return;
            }

            var tr = new System.Globalization.CultureInfo("tr-TR");
            int top = 48;
            foreach (var x in top3)
            {
                bool hasBudget = x.Category.BudgetLimit.HasValue && x.Category.BudgetLimit.Value > 0;
                string prefix = string.IsNullOrEmpty(x.Category.Icon) ? "" : x.Category.Icon + " ";

                Label lblName = new Label
                {
                    Text = prefix + x.Category.Name,
                    ForeColor = TextLight,
                    Left = 18,
                    Top = top,
                    Width = CardWidth - 150,
                    Height = 24,
                    AutoSize = false,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5F)
                };
                Label lblAmount = new Label
                {
                    Text = _user.HideAmountsEnabled
                        ? "••••••"
                        : (hasBudget
                            ? $"{x.Spent.ToString("#,##0", tr)}/{x.Category.BudgetLimit!.Value.ToString("#,##0", tr)} ₺"
                            : $"{x.Spent.ToString("#,##0", tr)} ₺"),
                    ForeColor = TextMuted,
                    Left = CardWidth - 150,
                    Top = top,
                    Width = 130,
                    Height = 24,
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5F)
                };
                pnlTopCategoryWidget.Controls.Add(lblName);
                pnlTopCategoryWidget.Controls.Add(lblAmount);
                MakeClickable(lblName, goToCategories);
                MakeClickable(lblAmount, goToCategories);

                if (hasBudget)
                {
                    double percent = (double)(x.Spent / x.Category.BudgetLimit!.Value * 100);
                    Panel bar = CreateBudgetProgressBar(18, top + 26, CardWidth - 36, () => percent);
                    pnlTopCategoryWidget.Controls.Add(bar);
                    MakeClickable(bar, goToCategories);
                    top += 48;
                }
                else
                {
                    top += 30;
                }
            }
        }

        // Hedeflerim'deki CreateMiniProgressBar ile aynı çizim ama renk anlamı ters: burada %100'e
        // ulaşmak (bütçe limitini doldurmak) bir başarı değil bir uyarı, bu yüzden yeşil yerine
        // kademeli turuncu/kırmızıya geçiyor.
        private Panel CreateBudgetProgressBar(int left, int top, int width, Func<double> getPercent)
        {
            const int barHeight = 8;
            Panel bar = new Panel { Left = left, Top = top, Width = width, Height = barHeight, BackColor = Color.Transparent };
            bar.Paint += (s, e) =>
            {
                double percent = getPercent();
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var trackPath = GetRoundedRectPath(new Rectangle(0, 0, bar.Width, bar.Height), barHeight / 2))
                using (var trackBrush = new SolidBrush(AppTheme.GridLineColor))
                    e.Graphics.FillPath(trackBrush, trackPath);

                int fillWidth = (int)(bar.Width * (Math.Min(100, percent) / 100.0));
                if (fillWidth > barHeight)
                {
                    Color fillColor = percent >= 100 ? AppTheme.DangerColor : percent >= 80 ? AppTheme.IdleColor : AccentColor;
                    using var fillPath = GetRoundedRectPath(new Rectangle(0, 0, fillWidth, bar.Height), barHeight / 2);
                    using var fillBrush = new SolidBrush(fillColor);
                    e.Graphics.FillPath(fillBrush, fillPath);
                }
            };
            return bar;
        }

        // Varlıklarım'da kurulmuş aktif fiyat alarmlarının güncel fiyatla eşiğe ne kadar yaklaştığını
        // gösterir (bkz. MainForm.CheckAssetPriceAlertsAsync — burası bildirim göndermez, sadece
        // özetler). AssetPriceService 30sn'lik bellek-içi cache kullandığından (bkz. o dosyadaki not)
        // Varlıklarım ekranıyla aynı fiyatı paylaşır, ekstra istek yükü oluşturmaz.
        private int _priceAlertsRequestId = 0;

        private async Task LoadPriceAlertsWidgetAsync()
        {
            ClearWidgetContent(pnlPriceAlertsWidget);
            int requestId = ++_priceAlertsRequestId;
            Panel target = pnlPriceAlertsWidget;

            Action goToAssets = () => _onNavigate?.Invoke("Varlıklarım");

            var alerts = _assetPriceAlertService.GetActiveByUser(_user.Id).Take(3).ToList();
            if (alerts.Count == 0)
            {
                AddWidgetLine(target, "Aktif fiyat alarmı yok.", 48, TextMuted, 18, goToAssets);
                return;
            }

            Label lblLoading = AddWidgetLine(target, "Fiyatlar yükleniyor...", 48, TextMuted, 18, goToAssets);

            var results = new List<(AssetPriceAlert Alert, decimal? Price)>();
            foreach (var alert in alerts)
            {
                decimal? price = await _assetPriceService.GetPriceTryAsync(alert.Symbol, alert.AssetType);
                results.Add((alert, price));
            }

            if (this.IsDisposed || requestId != _priceAlertsRequestId) return;
            if (!target.Controls.Contains(lblLoading)) return; // widget bu arada tekrar temizlenmiş olabilir
            target.Controls.Remove(lblLoading);
            lblLoading.Dispose();

            var tr = new System.Globalization.CultureInfo("tr-TR");
            int top = 48;
            foreach (var (alert, price) in results)
            {
                string arrow = alert.Direction == "above" ? "▲" : "▼";
                // Küsuratlı gösterim (ör. "5.000.000,00 ₺") satır genişliğini aşıp "..." ile
                // kesiliyordu — eşik zaten kullanıcının kendi girdiği yuvarlak bir hedef, tam
                // sayı yeterli.
                string thresholdText = _user.HideAmountsEnabled ? "••••••" : alert.ThresholdPrice.ToString("#,##0", tr) + " ₺";

                Label lblName = new Label
                {
                    Text = $"{alert.Symbol} {arrow} {thresholdText}",
                    ForeColor = TextLight,
                    Left = 18,
                    Top = top,
                    Width = CardWidth - 150,
                    Height = 24,
                    AutoSize = false,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5F)
                };
                Label lblPrice = new Label
                {
                    Text = !price.HasValue ? "N/A" : (_user.HideAmountsEnabled ? "••••••" : price.Value.ToString("#,##0.00", tr) + " ₺"),
                    ForeColor = TextMuted,
                    Left = CardWidth - 150,
                    Top = top,
                    Width = 130,
                    Height = 24,
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5F)
                };
                target.Controls.Add(lblName);
                target.Controls.Add(lblPrice);
                MakeClickable(lblName, goToAssets);
                MakeClickable(lblPrice, goToAssets);

                if (price.HasValue && price.Value > 0)
                {
                    double percent = alert.Direction == "above"
                        ? (double)(price.Value / alert.ThresholdPrice * 100)
                        : (double)(alert.ThresholdPrice / price.Value * 100);
                    Panel bar = CreateMiniProgressBar(18, top + 26, CardWidth - 36, () => percent);
                    target.Controls.Add(bar);
                    MakeClickable(bar, goToAssets);
                }
                top += 48;
            }
        }

        // Son 7 gün ile ondan önceki 7 günün toplam giderini karşılaştırır — Nakit Akışı Tahmini'nin
        // "gelecek" tahminine karşılık burası "yakın geçmişteki" harcama eğilimini (artıyor mu azalıyor
        // mu) gösterir.
        private void LoadWeeklyCompareWidget()
        {
            ClearWidgetContent(pnlWeeklyCompareWidget);

            Action goToReport = () => _onNavigate?.Invoke("Rapor");
            DateTime today = DateTime.Today;
            DateTime thisWeekStart = today.AddDays(-6);
            DateTime lastWeekStart = today.AddDays(-13);
            DateTime lastWeekEnd = today.AddDays(-7);

            var expenses = _transactionService.GetUserTransactions(_user.Id).Where(t => t.Type == "expense").ToList();
            decimal thisWeek = expenses.Where(t => t.TransactionDate.Date >= thisWeekStart && t.TransactionDate.Date <= today).Sum(t => t.Amount);
            decimal lastWeek = expenses.Where(t => t.TransactionDate.Date >= lastWeekStart && t.TransactionDate.Date <= lastWeekEnd).Sum(t => t.Amount);

            Label lblHeader = new Label
            {
                Text = "Haftalık Karşılaştırma",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 18,
                Top = 14,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlWeeklyCompareWidget.Controls.Add(lblHeader);
            MakeClickable(lblHeader, goToReport);

            Label lblSub = new Label
            {
                Text = "Son 7 gün vs önceki 7 gün",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = TextMuted,
                Left = 18,
                Top = 44,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlWeeklyCompareWidget.Controls.Add(lblSub);
            MakeClickable(lblSub, goToReport);

            var tr = new System.Globalization.CultureInfo("tr-TR");
            Label lblThisWeek = new Label
            {
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = ExpenseColor,
                Left = 18,
                Top = 66,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlWeeklyCompareWidget.Controls.Add(lblThisWeek);
            MakeClickable(lblThisWeek, goToReport);
            AnimateLabelValue(lblThisWeek, thisWeek, v => v.ToString("#,##0", tr) + " ₺", hiddenText: "••••••");

            decimal diff = thisWeek - lastWeek;
            double diffPercent = lastWeek > 0 ? (double)(Math.Abs(diff) / lastWeek * 100) : (thisWeek > 0 ? 100 : 0);
            string arrow = diff > 0 ? "▲" : diff < 0 ? "▼" : "▬";
            string trend = diff > 0 ? "artış" : diff < 0 ? "azalış" : "değişim yok";
            Color diffColor = diff > 0 ? ExpenseColor : diff < 0 ? IncomeColor : TextMuted;

            Label lblLastWeek = AddWidgetLine(pnlWeeklyCompareWidget, "Geçen Hafta: 0 ₺", 112, TextMuted, 18, goToReport);
            AnimateLabelValue(lblLastWeek, lastWeek, v => $"Geçen Hafta: {v.ToString("#,##0", tr)} ₺", hiddenText: "Geçen Hafta: ••••••");

            string diffText = _user.HideAmountsEnabled ? $"{arrow} ••••••" : $"{arrow} %{diffPercent:0} {trend}";
            AddWidgetLine(pnlWeeklyCompareWidget, diffText, 142, diffColor, 18, goToReport);
        }

        // Ana ekrandan ayrılmadan hızlıca gelir/gider eklemek için: Tip + Kategori + Tutar + Ekle.
        // Diğer mini-widget'ların aksine tıklanınca başka ekrana GÖTÜRMEZ (MakeClickable ile
        // sarmalanmaz) — kendi Click handler'ı doğrudan TransactionService.AddTransaction çağırır.
        private void LoadQuickAddWidget()
        {
            ClearWidgetContent(pnlQuickAddWidget);

            Label lblHeader = new Label
            {
                Text = "Hızlı İşlem Ekle",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 18,
                Top = 14,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlQuickAddWidget.Controls.Add(lblHeader);

            ComboBox cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlType = new Panel { Left = 18, Top = 48, Width = 104, Height = 34 };
            cmbType.Left = 6; cmbType.Top = 6; cmbType.Width = 92;
            pnlType.Controls.Add(cmbType);
            SetupHomeComboBox(pnlType, cmbType);
            cmbType.Items.Add("Gelir"); cmbType.Items.Add("Gider"); cmbType.SelectedIndex = 1;

            ComboBox cmbCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlCategory = new Panel { Left = 130, Top = 48, Width = CardWidth - 130 - 18, Height = 34 };
            cmbCategory.Left = 6; cmbCategory.Top = 6; cmbCategory.Width = pnlCategory.Width - 12;
            pnlCategory.Controls.Add(cmbCategory);
            SetupHomeComboBox(pnlCategory, cmbCategory);

            Panel pnlAmount = new Panel { Left = 18, Top = 92, Width = 140, Height = 34 };
            SetupSmoothContainer(pnlAmount, 8, CardBackColor);
            TextBox txtAmount = new TextBox
            {
                Left = 8,
                Top = 7,
                Width = 124,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = CardBackColor,
                ForeColor = TextLight
            };
            pnlAmount.Controls.Add(txtAmount);

            // bkz. AddBorderRingOverlay'in üzerindeki not: kutu dolgusu kartla aynı renkte,
            // çerçevesiz neredeyse görünmüyordu.
            AddBorderRingOverlay(pnlAmount, 8);

            Button btnAdd = new Button { Text = "Ekle", Left = 166, Top = 92, Width = CardWidth - 166 - 18, Height = 34, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9.5F) };
            SetupRoundedButton(btnAdd, AccentColor, Color.White);

            Label lblStatus = new Label
            {
                Left = 18,
                Top = 134,
                Width = CardWidth - 36,
                Height = 40,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = TextMuted,
                BackColor = Color.Transparent
            };

            // Türkçe binlik ayraçlı, sadece rakam kabul eden anlık biçimlendirme (bkz. TransactionControl.SmartFormatAmount).
            bool suppressFormat = false;
            txtAmount.TextChanged += (s, e) =>
            {
                if (suppressFormat || string.IsNullOrWhiteSpace(txtAmount.Text)) return;
                string digits = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
                if (string.IsNullOrEmpty(digits)) return;
                if (decimal.TryParse(digits, out decimal amt))
                {
                    string formatted = amt.ToString("#,##0", new System.Globalization.CultureInfo("tr-TR"));
                    if (txtAmount.Text == formatted) return;
                    suppressFormat = true;
                    txtAmount.Text = formatted;
                    txtAmount.SelectionStart = txtAmount.Text.Length;
                    suppressFormat = false;
                }
            };

            void LoadCategoriesForType()
            {
                string type = cmbType.SelectedItem?.ToString() == "Gelir" ? "income" : "expense";
                var categories = _categoryService.GetUserCategoriesByType(_user.Id, type);
                cmbCategory.Items.Clear();
                foreach (var c in categories) cmbCategory.Items.Add(c.Name);
                if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
            }
            cmbType.SelectedIndexChanged += (s, e) => LoadCategoriesForType();
            LoadCategoriesForType();

            btnAdd.Click += (s, e) =>
            {
                if (cmbCategory.SelectedIndex < 0)
                {
                    lblStatus.ForeColor = AppTheme.DangerColor;
                    lblStatus.Text = "Bu tipte kategori yok.";
                    return;
                }

                string rawAmount = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
                if (!decimal.TryParse(rawAmount, out decimal amount) || amount <= 0)
                {
                    lblStatus.ForeColor = AppTheme.DangerColor;
                    lblStatus.Text = "Geçersiz tutar.";
                    return;
                }

                string type = cmbType.SelectedItem?.ToString() == "Gelir" ? "income" : "expense";
                string categoryName = cmbCategory.SelectedItem?.ToString() ?? string.Empty;
                var category = _categoryService.GetOrCreateCategory(_user.Id, categoryName, type);
                bool success = _transactionService.AddTransaction(_user.Id, category.Id, amount, type, null, out string errorMessage);

                if (success)
                {
                    lblStatus.ForeColor = AppTheme.SuccessColor;
                    lblStatus.Text = "İşlem eklendi.";
                    txtAmount.Clear();

                    RefreshBalances();
                    if (_placedWidgets.ContainsKey("transactions")) LoadRecentTransactionsWidget();
                    if (_placedWidgets.ContainsKey("report")) LoadMiniReportWidget();
                    if (_placedWidgets.ContainsKey("cashflow")) LoadCashflowWidget();
                }
                else
                {
                    lblStatus.ForeColor = AppTheme.DangerColor;
                    lblStatus.Text = errorMessage;
                }
            };

            pnlQuickAddWidget.Controls.Add(pnlType);
            pnlQuickAddWidget.Controls.Add(pnlCategory);
            pnlQuickAddWidget.Controls.Add(pnlAmount);
            pnlQuickAddWidget.Controls.Add(btnAdd);
            pnlQuickAddWidget.Controls.Add(lblStatus);
        }

        // TransactionControl.SetupCustomComboBox'ın sade sürümü: mavi native seçim arka planını
        // engellemek için owner-draw + rounded container. Burada sadece DropDownList kullanılıyor,
        // bu yüzden editable kutulardaki metin kaydırma hack'ine (ShiftEditTextUp) gerek yok.
        private void SetupHomeComboBox(Panel pnl, ComboBox cmb)
        {
            SetupSmoothContainer(pnl, 8, CardBackColor);
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = CardBackColor;
            cmb.ForeColor = TextLight;
            cmb.DrawMode = DrawMode.OwnerDrawFixed;
            // 20px'te "g" gibi alt uzantılı (descender) harfler kesiliyor, "geri ödeme" "aeri ödeme"
            // gibi görünüyordu (bkz. bu dosyadaki diğer 20px→24px notları) — 24'e çıkarıldı.
            cmb.ItemHeight = 24;
            cmb.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bgColor = isSelected ? AppTheme.HoverBackColor : CardBackColor;
                e.Graphics.FillRectangle(new SolidBrush(bgColor), e.Bounds);
                TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index]?.ToString() ?? string.Empty, cmb.Font, e.Bounds, TextLight, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            };
            cmb.Region = new Region(new Rectangle(1, 1, cmb.Width - 2, cmb.Height - 2));

            // OwnerDrawFixed yalnızca metin/liste kısmını boyuyor — açılır ok DÜĞMESİ hâlâ Windows'un
            // kendi (açık/beyaz) temasıyla çiziliyor, kutunun sağ kenarında beyaz bir çizgi/köşe gibi
            // görünüyordu. Üzerini kartla aynı renkte bir panelle kapatıp kendi ok işaretimizi çiziyoruz
            // (bkz. AssetControl.SetupCustomComboBox — aynı desen).
            Panel pnlArrow = new Panel { Width = 28, BackColor = CardBackColor, Cursor = Cursors.Hand };
            pnlArrow.Height = cmb.Height - 2;
            pnlArrow.Left = cmb.Right - pnlArrow.Width;
            pnlArrow.Top = cmb.Top + 1;
            pnlArrow.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int ax = pnlArrow.Width / 2 - 4;
                int ay = pnlArrow.Height / 2 - 2;
                using var brush = new SolidBrush(TextMuted);
                e.Graphics.FillPolygon(brush, new Point[] { new Point(ax, ay), new Point(ax + 8, ay), new Point(ax + 4, ay + 5) });
            };
            pnlArrow.MouseClick += (s, e) => { cmb.DroppedDown = true; };
            pnl.MouseClick += (s, e) => { cmb.DroppedDown = true; };
            pnl.Controls.Add(pnlArrow);
            pnlArrow.BringToFront();

            // Kutu dolgusu widget kartıyla aynı renkte (CardBackColor) olduğu için sınır çizilmeden
            // kutu neredeyse görünmez oluyordu — diğer ekranlardaki kart-üstü kutular gibi ince bir
            // çerçeve ekliyoruz (bkz. AddBorderRingOverlay'in üzerindeki not — neden pnl'in kendi
            // Paint'i yerine ayrı, bölgesi daraltılmış bir katman kullandığımız orada açıklanıyor).
            AddBorderRingOverlay(pnl, 8);
        }

        // parent'ın kartı zaten tıklanabilir (bkz. CreateWidgetCard) ama bu satır SetupUI bittikten
        // SONRA eklendiği için o ilk MakeClickable taramasına dahil değil — burada ayrıca sarmalıyoruz.
        private Label AddWidgetLine(Panel parent, string text, int top, Color color, int left, Action onClick)
        {
            // AutoSize + MaximumSize (yalnızca genişlik sınırlı) satırı sarmaya çalışıyordu; dar mini-widget
            // genişliğinde bu, metnin iki satıra bölünüp bir alttaki satırın üzerine binmesine yol açıyordu
            // (ör. "Gider: X ₺" satırının Gelir satırınca örtülmesi). Sabit tek satır + gerekirse "..." ile
            // kısaltma daha güvenli.
            Label lbl = new Label
            {
                Text = text,
                ForeColor = color,
                Left = left,
                Top = top,
                Width = CardWidth - left - 16,
                // "y"/"ğ" gibi alt uzantılı harflerin kuyruğu 20px'te kesiliyordu.
                Height = 24,
                AutoSize = false,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F)
            };
            parent.Controls.Add(lbl);
            MakeClickable(lbl, onClick);
            return lbl;
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor)
        {
            pnl.BackColor = AppBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor);
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        // "+" widget ekleme butonu: yazı tipi glifiyle ("+") çizildiğinde optik olarak ortalanmış
        // durmuyordu (fontların glif kutusu genelde alt çıkıntı payı bırakır) — bunun yerine iki
        // çizgiyi elle, tam merkeze göre çiziyoruz.
        private void SetupAddWidgetButton(Button btn, Color bgColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);

                float cx = btn.Width / 2f, cy = btn.Height / 2f;
                float arm = Math.Min(btn.Width, btn.Height) / 4.2f;
                using var pen = new Pen(Color.White, 2.4f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
                e.Graphics.DrawLine(pen, cx - arm, cy, cx + arm, cy);
                e.Graphics.DrawLine(pen, cx, cy - arm, cx, cy + arm);
            };
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
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);
                if (bgColor == CardBackColor)
                {
                    using var pen = new Pen(Color.FromArgb(90, 94, 115), 1);
                    using var path2 = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                    e.Graphics.DrawPath(pen, path2);
                }
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

        // Hızlı İşlem Ekle'deki kutuların (gider/kategori/tutar) dolgusu widget kartıyla aynı renkte
        // (CardBackColor) olduğundan, bir çerçeve çizilmeden neredeyse görünmüyorlardı. Çerçeveyi
        // doğrudan pnl'in kendi Paint'inde çizmek işe yaramadı: pnl önce boyanır, İÇİNDEKİ ComboBox/
        // TextBox gibi opak alt denetimler SONRA üzerine çizilir, bu yüzden özellikle alt kenar
        // (ComboBox'ın Height ile değiştirilemeyen, yazı tipine göre sabit yüksekliği yüzünden) alt
        // denetimin arkasında kalıp görünmüyordu. Bunu tüm pnl'i kaplayan şeffaf bir katmanla
        // düzeltmek de işe yaramadı: WinForms'ta BackColor=Transparent yalnızca PARENT'ın (pnl'in)
        // düz arka planına karşı "şeffaf" davranır, KARDEŞ (sibling) denetimlerin (ComboBox/TextBox)
        // çizdiklerine karşı değil — üstteki katman onların üzerini pnl'in düz rengiyle kaplayıp
        // yazıyı tamamen görünmez kılıyordu. Çözüm: katmanın Region'ını sadece ince çerçeve
        // halkasıyla sınırlamak — halkanın dışında pencere hiç yok sayıldığından altındaki
        // denetimlerin pikselleri hiç dokunulmadan kalıyor, ve halka zaten kutuların içindeki
        // metinle çakışmayacak kadar ince (kenardan birkaç piksel).
        private void AddBorderRingOverlay(Panel pnl, int radius)
        {
            Panel ring = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Enabled = false };
            ring.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(AppTheme.CardBorderColor, 1f);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius);
                e.Graphics.DrawPath(pen, path);
            };
            void UpdateRegion()
            {
                if (pnl.Width <= 4 || pnl.Height <= 4) return;
                using var outer = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width, pnl.Height), radius);
                using var inner = GetRoundedRectPath(new Rectangle(2, 2, pnl.Width - 4, pnl.Height - 4), Math.Max(radius - 2, 1));
                var region = new Region(outer);
                region.Exclude(inner);
                ring.Region = region;
            }
            UpdateRegion();
            ring.Resize += (s, e) => UpdateRegion();
            pnl.Controls.Add(ring);
            ring.BringToFront();
        }

        public void RefreshData()
        {
            RefreshBalances();
            _ = LoadInvestCardAsync();

            // Yalnızca kullanıcının ızgaraya yerleştirdiği widget'ları tazele — yerleştirilmemiş
            // olanların içeriğini üretmenin bir anlamı yok (bkz. WidgetCatalog/_placedWidgets).
            if (_placedWidgets.ContainsKey("notifications")) _ = LoadNotificationsAsync();
            if (_placedWidgets.ContainsKey("notes")) LoadNotesWidget();
            if (_placedWidgets.ContainsKey("reminders")) LoadReminderWidget();
            if (_placedWidgets.ContainsKey("report")) LoadMiniReportWidget();
            if (_placedWidgets.ContainsKey("transactions")) LoadRecentTransactionsWidget();
            if (_placedWidgets.ContainsKey("goals")) LoadGoalsWidget();
            if (_placedWidgets.ContainsKey("cashflow")) LoadCashflowWidget();
            if (_placedWidgets.ContainsKey("quickadd")) LoadQuickAddWidget();
            if (_placedWidgets.ContainsKey("recurringUpcoming")) LoadRecurringUpcomingWidget();
            if (_placedWidgets.ContainsKey("topCategory")) LoadTopCategoryWidget();
            if (_placedWidgets.ContainsKey("priceAlerts")) _ = LoadPriceAlertsWidgetAsync();
            if (_placedWidgets.ContainsKey("weeklyCompare")) LoadWeeklyCompareWidget();
        }

        // Varlıklarım'daki pozisyonların anlık kâr/zararını "bildirim" tarzında listeler; en çok
        // hareket edenler (yüzde olarak) üstte. Pozisyon yoksa boş durum mesajı gösterilir (widget artık
        // kullanıcı tarafından yerleştirildiğinden veri yoksa bile kartı tamamen gizlemek, ızgarada
        // açıklanamayan bir boşluk bırakırdı).
        private async Task LoadNotificationsAsync()
        {
            var holdings = await _assetService.GetHoldingsWithLivePricesAsync(_user.Id);
            if (this.IsDisposed) return;

            var withChange = holdings.Where(h => h.ProfitLossPercent.HasValue).ToList();

            pnlNotifications.Controls.Clear();

            Label lblHeader = new Label
            {
                Text = "Varlık Bildirimleri",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 16,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlNotifications.Controls.Add(lblHeader);

            if (withChange.Count == 0)
            {
                AddWidgetLine(pnlNotifications, "Henüz bir bildirim yok.", 56, TextMuted, 20, () => _onNavigate?.Invoke("Varlıklarım"));
                return;
            }

            // 4 satır (56 + 4*32 + 16 = 200px), sabit 210px yükseklikli widget hücresine sığacak
            // şekilde sınırlandı (bkz. MiniRowHeight) — eskiden 5 satırdı ve dinamik yükseklik kullanıyordu.
            var ordered = withChange.OrderByDescending(h => Math.Abs(h.ProfitLossPercent!.Value)).Take(4).ToList();
            var tr = new System.Globalization.CultureInfo("tr-TR");

            int rowTop = 56;
            foreach (var h in ordered)
            {
                bool isUp = h.ProfitLossPercent!.Value >= 0;
                Color changeColor = isUp ? IncomeColor : ExpenseColor;
                string arrow = isUp ? "▲" : "▼";

                Label lblSymbol = new Label
                {
                    Text = $"{h.Symbol} — {h.Name}",
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = TextLight,
                    Left = 20,
                    Top = rowTop,
                    AutoSize = true,
                    BackColor = Color.Transparent
                };

                Label lblChange = new Label
                {
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = changeColor,
                    Left = pnlNotifications.Width - 220,
                    Top = rowTop,
                    Width = 200,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent
                };
                AnimateNotificationChange(lblChange, arrow, Math.Abs(h.ProfitLossPercent!.Value), h.ProfitLossTry!.Value);

                pnlNotifications.Controls.Add(lblSymbol);
                pnlNotifications.Controls.Add(lblChange);
                rowTop += 32;
            }
        }

        // lblChange diğer kartlardaki (Cüzdan, Kasa vb.) sayma efektiyle tutarlı olsun diye yüzde ve
        // tutarı aynı anda, tek bir zamanlayıcıyla 0'dan hedef değerine sayarak dolduruyor — ikisi de
        // aynı metne yazıldığından AnimateLabelValue'nun tek değerli formatter'ı yeterli değil.
        private void AnimateNotificationChange(Label label, string arrow, decimal targetPercent, decimal targetAmount)
        {
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

            timer.Tick += (s, e) =>
            {
                if (label.IsDisposed) { timer.Stop(); timer.Dispose(); _cardAnimTimers.Remove(label); return; }

                double t = sw.Elapsed.TotalMilliseconds / durationMs;
                bool finished = t >= 1.0;
                if (finished) t = 1.0;

                double eased = 1 - Math.Pow(1 - t, 3);
                decimal shownPercent = finished ? targetPercent : targetPercent * (decimal)eased;
                decimal shownAmount = finished ? targetAmount : Math.Round(targetAmount * (decimal)eased);
                label.Text = $"{arrow} %{shownPercent.ToString("0.0", tr)}   {shownAmount.ToString("+#,##0;-#,##0", tr)} ₺";

                if (finished)
                {
                    timer.Stop();
                    timer.Dispose();
                    _cardAnimTimers.Remove(label);
                }
            };
            _cardAnimTimers[label] = timer;
            timer.Start();
        }

        // Varlık Bildirimleri'nin sağındaki boş alana, en son düzenlenen 4 notu başlık+tarihle listeler.
        private void LoadNotesWidget()
        {
            pnlNotes.Controls.Clear();

            Label lblHeader = new Label
            {
                Text = "Notlar",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 16,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlNotes.Controls.Add(lblHeader);

            Action goToNotes = () => _onNavigate?.Invoke("Notlar");
            MakeClickable(lblHeader, goToNotes);

            var notes = _noteService.GetRecentlyUpdatedNotes(_user.Id, 4);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            // Kutu 232px sabit (pnlNotes.Height, SetupUI'da bir kez ayarlanır ve burada değiştirilmez);
            // 4 satır bu boyu dolduracak şekilde 40px aralıklı. Etiket kendi 40px'lik dilimini tamamen
            // dolduruyor (26px'te bile "g/ğ" gibi alt uzantılı harflerin kuyruğu hâlâ kesiliyordu) —
            // dar bir kutuda ortalanan metin, kutu küçüldükçe hizalama aşağı değil merkeze doğru kayar.
            const int rowHeight = 40;
            const int rowLabelHeight = rowHeight;
            int top = 56;

            if (notes.Count == 0)
            {
                AddWidgetLine(pnlNotes, "Henüz not yok.", top, TextMuted, 20, goToNotes);
            }
            else
            {
                const int dateWidth = 100;
                const int rightMargin = 20;
                int dateLeft = CardWidth - rightMargin - dateWidth;
                const int titleDateGap = 12;
                int titleWidth = dateLeft - titleDateGap - 20;

                foreach (var note in notes)
                {
                    Label lblTitle = new Label
                    {
                        Text = string.IsNullOrWhiteSpace(note.Title) ? "(Başlıksız)" : note.Title,
                        ForeColor = TextLight,
                        Left = 20,
                        Top = top,
                        Width = titleWidth,
                        Height = rowLabelHeight,
                        AutoSize = false,
                        AutoEllipsis = true,
                        TextAlign = ContentAlignment.MiddleLeft,
                        BackColor = Color.Transparent,
                        Font = new Font("Segoe UI", 10F)
                    };
                    Label lblDate = new Label
                    {
                        // "dd.MM.yyyy" bu makinenin ~150% DPI ölçeğinde 85px'te bile yıl kısmı kesiliyordu.
                        Text = note.UpdatedAt.ToString("dd.MM.yyyy", tr),
                        ForeColor = TextMuted,
                        Left = dateLeft,
                        Top = top,
                        Width = dateWidth,
                        Height = rowLabelHeight,
                        TextAlign = ContentAlignment.MiddleRight,
                        BackColor = Color.Transparent,
                        Font = new Font("Segoe UI", 9F)
                    };
                    pnlNotes.Controls.Add(lblTitle);
                    pnlNotes.Controls.Add(lblDate);
                    MakeClickable(lblTitle, goToNotes);
                    MakeClickable(lblDate, goToNotes);
                    top += rowHeight;
                }
            }
        }

        private void RefreshBalances()
        {
            var (wallet, safe) = _accountService.GetBalances(_user.Id);

            AnimateCardValue(lblWalletAmount, wallet);
            AnimateCardValue(lblSafeAmount, safe);
        }

        // Varlıklarım kutusu: güncel portföy değeri (kâr/zararına göre ok+renk), Kâr/Zarar, Toplam
        // Maliyet, Pozisyon — hepsi Varlıklarım ekranındaki (AssetControl) hesaplarla aynı.
        private async Task LoadInvestCardAsync()
        {
            var holdings = await _assetService.GetHoldingsWithLivePricesAsync(_user.Id);
            if (this.IsDisposed) return;

            decimal totalValue = holdings.Sum(h => h.CurrentValueTry ?? 0);
            decimal totalCost = holdings.Sum(h => h.AvgCostTry * h.Quantity);
            decimal totalPl = totalValue - totalCost;
            bool isProfit = totalPl >= 0;
            Color plColor = isProfit ? IncomeColor : ExpenseColor;
            string arrow = isProfit ? "▲" : "▼";

            var tr = new System.Globalization.CultureInfo("tr-TR");
            AnimateLabelValue(lblInvestAmount, totalValue, v => $"{arrow} {v.ToString("#,##0", tr)} ₺", hiddenText: $"{arrow} ••••••");
            lblInvestAmount.ForeColor = plColor;

            AnimateLabelValue(lblInvestPLValue, totalPl, v => v.ToString("+#,##0;-#,##0", tr) + " ₺", hiddenText: "••••••");
            lblInvestPLValue.ForeColor = plColor;

            AnimateCardValue(lblInvestCostValue, totalCost);

            // Pozisyon sayısı hassas bir tutar değil (bkz. AssetControl'ün Pozisyon çipi) — tutarları
            // gizle açıkken bile düz gösteriliyor.
            lblInvestPosValue.Text = holdings.Count.ToString();
        }

        // Kart tutarını 0'dan gerçek değerine sayarak (count-up) belirtir; tutarlar gizliyse animasyonsuz gösterir.
        private void AnimateCardValue(Label label, decimal targetValue, string suffix = " ₺")
        {
            var tr = new System.Globalization.CultureInfo("tr-TR");
            AnimateLabelValue(label, targetValue, v => v.ToString("#,##0", tr) + suffix, hiddenText: "••••••");
        }

        // Genel sayaç animasyonu: 0'dan hedef değere doğru (ease-out) sayarak yazdırır, özel bir
        // biçimlendirici alır (ör. "%45" ya da "1.234 ₺") — Cüzdan/Kasa/Varlıklarım kartlarıyla aynı
        // efekti Bu Ayın Özeti, Son İşlemler ve Hedeflerim mini-widget'larında da kullanmak için.
        // Tutarlar gizliyse animasyonsuz "••••••" gösterir (showDotsWhenHidden=false ise, ör. yüzdeler
        // için, gizleme durumundan etkilenmeden normal animasyonla devam eder).
        private void AnimateLabelValue(Label label, decimal targetValue, Func<decimal, string> formatter, string? hiddenText = null, Action<decimal>? onTick = null)
        {
            // Aynı etikete ait önceki animasyon hâlâ çalışıyorsa (ör. widget hızlıca yeniden yüklendiğinde)
            // önce onu durdurup atıyoruz — yoksa ikisi de aynı Label'a yazıp yarım kalmış/titreşen bir
            // görüntüye yol açabiliyor.
            if (_cardAnimTimers.TryGetValue(label, out var existingTimer))
            {
                existingTimer.Stop();
                existingTimer.Dispose();
                _cardAnimTimers.Remove(label);
            }

            if (hiddenText != null && _user.HideAmountsEnabled)
            {
                label.Text = hiddenText;
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var timer = new System.Windows.Forms.Timer { Interval = 16 };
            const int durationMs = 800;

            timer.Tick += (s, e) =>
            {
                if (label.IsDisposed) { timer.Stop(); timer.Dispose(); _cardAnimTimers.Remove(label); return; }

                double t = sw.Elapsed.TotalMilliseconds / durationMs;
                bool finished = t >= 1.0;
                if (finished) t = 1.0;

                double eased = 1 - Math.Pow(1 - t, 3);
                decimal shown = finished ? targetValue : Math.Round(targetValue * (decimal)eased);
                label.Text = formatter(shown);
                onTick?.Invoke(shown);

                if (finished)
                {
                    timer.Stop();
                    timer.Dispose();
                    _cardAnimTimers.Remove(label);
                }
            };
            _cardAnimTimers[label] = timer;
            timer.Start();
        }

        private void BtnTransfer_Click(object? sender, EventArgs e)
        {
            using (var dialog = new TransferDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;

                bool success;
                string errorMessage;

                switch ((dialog.From, dialog.To))
                {
                    case (TransferAccount.Wallet, TransferAccount.Safe):
                        success = _accountService.TransferToSafe(_user.Id, dialog.Amount, out errorMessage);
                        break;
                    case (TransferAccount.Safe, TransferAccount.Wallet):
                        success = _accountService.TransferToWallet(_user.Id, dialog.Amount, out errorMessage);
                        break;
                    default:
                        success = false;
                        errorMessage = "Geçersiz transfer yönü.";
                        break;
                }

                if (success)
                {
                    lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                    lblStatus.Text = "Transfer başarılı.";
                    RefreshBalances();
                    if (_placedWidgets.ContainsKey("notifications")) _ = LoadNotificationsAsync();
                }
                else
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = errorMessage;
                }
            }
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
