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
        };

        private const int GridCols = 4;
        private const int ButtonsTop = 30 + 240 + 16;

        private readonly Dictionary<string, int> _placedWidgets = new Dictionary<string, int>();
        // Sürükleme sırasında kesikli çerçeveyle vurgulanan hücreler — sürüklenen widget'ın gerçek
        // kapladığı hücre sayısını (ör. 3 hücreli Varlık Bildirimleri) doğru yansıtsın diye tek hücre
        // yerine bir küme olarak tutuluyor (bkz. HighlightDragTarget).
        private readonly HashSet<int> _highlightedCells = new HashSet<int>();
        private readonly Dictionary<string, Panel> _widgetFrames = new Dictionary<string, Panel>();
        // 6 widget toplamda tam olarak 8 hücreyi (3+1+1+1+1+1) dolduruyor — 3. bir satır hiçbir zaman
        // gerekmiyordu ve her zaman boş bir alan olarak kalıyordu (bkz. kullanıcı geri bildirimi);
        // bunun yerine mevcut 2 satır yukarı (bkz. MiniRowTop) kaydırılarak boşluk giderildi.
        private readonly Panel[] _cellPanels = new Panel[8];
        private Button btnAddWidget = new Button();
        private Panel pnlWidgetPicker = new Panel();
        private Label lblEmptyGridHint = new Label();

        // Etikete göre çalışan sayaç animasyonu zamanlayıcıları — bir widget yenilenirken (ör. tutarları
        // gizle aç/kapa, ya da mini-widget'lar yeniden dolarken) eski bir animasyon hâlâ sürüyorsa önce
        // onu durdurmak için (bkz. AnimateLabelValue, ClearWidgetContent).
        private readonly Dictionary<Control, System.Windows.Forms.Timer> _cardAnimTimers = new Dictionary<Control, System.Windows.Forms.Timer>();

        // Eskiden 630'du; butonların/durum satırının hemen altında kullanılmayan büyük bir boşluk
        // bırakıyordu (bkz. yukarıdaki not) — ızgara artık o boşluğu da kapsayacak şekilde yukarı çekildi.
        private const int MiniRowTop = 400;
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
            // Widget ızgarası 2 satır (bkz. SetupWidgetGrid, GridCols, _cellPanels) — yükseklik hesabı
            // eksik kalırsa alt satıra yerleştirilen widget'lar kırpılabilirdi.
            const int gridRows = 2;
            this.MinimumSize = new Size(CardLeft4 + CardWidth + 20, MiniRowTop + gridRows * (MiniRowHeight + 20));
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

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

            lblStatus.Left = CardLeft1;
            lblStatus.Top = buttonsTop + 55;
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

        private void SetupMiniWidgets()
        {
            pnlReminderWidget = CreateWidgetCard(CardLeft1, "Yaklaşan Hatırlatıcılar", "Hatırlatıcılar");
            pnlMiniReportWidget = CreateWidgetCard(CardLeft2, "Bu Ayın Özeti", "Rapor");
            pnlRecentTxWidget = CreateWidgetCard(CardLeft3, "Son İşlemler", "İşlemler");
            pnlGoalsWidget = CreateWidgetCard(CardLeft4, "Hedeflerim", "Hedefler");
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

                int cellIndex = i;
                cell.Paint += (s, e) =>
                {
                    if (_highlightedCells.Contains(cellIndex))
                    {
                        using var pen = new Pen(AccentColor, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                        e.Graphics.DrawRectangle(pen, 1, 1, cell.Width - 2, cell.Height - 2);
                    }
                };
                // Sürüklenen widget 3 hücre kaplıyorsa (ör. Varlık Bildirimleri), kesikli kutu da o üç
                // hücrenin tamamını göstermeli — önceden yalnızca imlecin üzerinde olduğu tek hücre
                // vurgulanıyordu, gerçek yerleşim boyutunu yanıltıcı şekilde 1 birim gösteriyordu.
                cell.DragEnter += (s, e) => { e.Effect = GetDropEffect(e); HighlightDragTarget(cellIndex, e.Data); };
                cell.DragLeave += (s, e) => ClearDragHighlight();
                cell.DragDrop += (s, e) =>
                {
                    ClearDragHighlight();
                    if (e.Data?.GetData(typeof(string)) is string key) PlaceWidget(key, cellIndex);
                };

                this.Controls.Add(cell);
                _cellPanels[i] = cell;
            }

            SetupWidgetPicker();
            RebuildWidgetGrid();
        }

        private static DragDropEffects GetDropEffect(DragEventArgs e) =>
            (e.Data?.GetDataPresent(typeof(string)) ?? false) ? DragDropEffects.Move : DragDropEffects.None;

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

        private void HighlightDragTarget(int anchorCell, IDataObject? data)
        {
            _highlightedCells.Clear();
            if (data?.GetData(typeof(string)) is string key)
            {
                foreach (var c in GetClampedSpanCells(key, anchorCell)) _highlightedCells.Add(c);
            }
            RefreshDragHighlightVisuals();
        }

        private void ClearDragHighlight()
        {
            _highlightedCells.Clear();
            RefreshDragHighlightVisuals();
        }

        private void RefreshDragHighlightVisuals()
        {
            foreach (var cell in _cellPanels) cell.Invalidate();
            foreach (var frame in _widgetFrames.Values) frame.Invalidate();
        }

        private void PlaceWidget(string key, int anchorCell)
        {
            var def = WidgetCatalog.FirstOrDefault(w => w.Key == key);
            if (def == null) return;

            int col = anchorCell % GridCols, row = anchorCell / GridCols;
            if (col + def.ColSpan > GridCols) col = GridCols - def.ColSpan;
            anchorCell = row * GridCols + col;

            int? previousCell = _placedWidgets.TryGetValue(key, out var pc) ? pc : (int?)null;
            _placedWidgets.Remove(key);

            var newCells = new HashSet<int>(Enumerable.Range(anchorCell, def.ColSpan));

            var displaced = _placedWidgets
                .Where(kv => Enumerable.Range(kv.Value, WidgetCatalog.First(w => w.Key == kv.Key).ColSpan).Any(c => newCells.Contains(c)))
                .Select(kv => kv.Key)
                .ToList();

            foreach (var dKey in displaced) _placedWidgets.Remove(dKey);
            _placedWidgets[key] = anchorCell;

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
                if (target.HasValue) _placedWidgets[dKey] = target.Value;
            }

            SaveLayout();
            RebuildWidgetGrid();
        }

        // [anchorCell, anchorCell+span) hücrelerinin tamamı satır sınırını aşmadan boşta mı?
        private bool IsCellRangeFree(int anchorCell, int span)
        {
            int col = anchorCell % GridCols;
            if (col + span > GridCols) return false;
            var cells = Enumerable.Range(anchorCell, span).ToList();
            return !_placedWidgets.Any(kv => Enumerable.Range(kv.Value, WidgetCatalog.First(w => w.Key == kv.Key).ColSpan).Any(c => cells.Contains(c)));
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

        private void StartWidgetDrag(Control source, string key, int colSpan)
        {
            int width = colSpan * CardWidth + (colSpan - 1) * 20;
            _dragPreviewForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Size = new Size(width, MiniRowHeight),
                BackColor = AccentColor,
                Opacity = 0.35,
                TopMost = true
            };
            _dragPreviewForm.Show();

            GiveFeedbackEventHandler onGiveFeedback = (s, e) =>
            {
                e.UseDefaultCursors = true;
                if (_dragPreviewForm == null) return;
                var p = System.Windows.Forms.Cursor.Position;
                _dragPreviewForm.Location = new Point(p.X + 16, p.Y + 16);
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
                bool isPlaced = _placedWidgets.TryGetValue(def.Key, out int cell);
                if (!isPlaced)
                {
                    if (_widgetFrames.TryGetValue(def.Key, out var hiddenFrame)) hiddenFrame.Visible = false;
                    continue;
                }

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

            lblEmptyGridHint.Visible = _placedWidgets.Count == 0;
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

            frame.DragEnter += (s, e) =>
            {
                e.Effect = GetDropEffect(e);
                if (_placedWidgets.TryGetValue(def.Key, out int myCell)) HighlightDragTarget(myCell, e.Data);
            };
            frame.DragLeave += (s, e) => ClearDragHighlight();
            frame.DragDrop += (s, e) =>
            {
                ClearDragHighlight();
                if (e.Data?.GetData(typeof(string)) is string key && _placedWidgets.TryGetValue(def.Key, out int myCell))
                    PlaceWidget(key, myCell);
            };

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
                AutoSize = true,
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
                Text = "🔔 Varlık Bildirimleri",
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
