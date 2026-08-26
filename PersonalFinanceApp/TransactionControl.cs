using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class TransactionControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly CategoryService _categoryService = new CategoryService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color DangerColor => AppTheme.DangerColor;

        private Panel pnlTop = new Panel();
        private Panel pnlGrid = new Panel();
        private Panel pnlBottom = new Panel();

        private ComboBox cmbType = new ComboBox();
        private ComboBox cmbCategory = new ComboBox();
        private TextBox txtAmount = new TextBox();
        private TextBox txtDescription = new TextBox();

        private Button btnAdd = new Button();
        private Button btnExport = new Button();
        private Button btnRecurring = new Button();
        private TextBox txtSearch = new TextBox();
        private Button btnSearch = new Button();
        private Label lblStatus = new Label();
        private Label lblSearch = new Label();
        private Panel pnlSearch = new Panel();

        private DataGridView dgvTransactions = new DataGridView();
        private List<Transaction> _cachedTransactions = new List<Transaction>();

        private CheckBox chkDateFilter = new CheckBox();
        private DarkDatePicker dtpStart = new DarkDatePicker();
        private DarkDatePicker dtpEnd = new DarkDatePicker();

        private Button btnBulkMode = new Button();
        private Button btnBulkDelete = new Button();
        private Button btnBulkCategory = new Button();
        private bool _bulkModeActive = false;

        public TransactionControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadCategorySuggestions();
            LoadTransactions();
        }

        public void RefreshData()
        {
            LoadTransactions();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            // --- ÜST PANEL ---
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 258;
            pnlTop.BackColor = AppBackColor;

            Label lblTitle = new Label { Text = "İşlemler", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true };

            // Girdi Alanları
            Label lblType = new Label { Text = "Tip:", Left = 20, Top = 75, ForeColor = TextMuted, AutoSize = true };
            Panel pnlType = new Panel { Left = 20, Top = 100, Width = 140, Height = 36 };
            // Name: tema değişince ekran yeniden kurulduğunda yarım kalmış form verisi kaybolmasın
            // diye MainForm.CaptureFormState/RestoreFormState bu isimle eşleştiriyor.
            cmbType.Name = "TransactionType";
            cmbType.Left = 5; cmbType.Top = 7; cmbType.Width = 135;
            cmbType.Font = new Font("Segoe UI", 9.5F); cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("Gelir"); cmbType.Items.Add("Gider"); cmbType.SelectedIndex = 1;
            cmbType.SelectedIndexChanged += (s, e) => LoadCategorySuggestions();
            pnlType.Controls.Add(cmbType);
            SetupCustomComboBox(pnlType, cmbType); // Beyazlık ve Mavi renk düzeltildi

            Label lblCategory = new Label { Text = "Kategori:", Left = 180, Top = 75, ForeColor = TextMuted, AutoSize = true };
            Panel pnlCategory = new Panel { Left = 180, Top = 100, Width = 200, Height = 36 };
            cmbCategory.Name = "TransactionCategory";
            cmbCategory.Left = 5; cmbCategory.Top = 7; cmbCategory.Width = 195;
            cmbCategory.Font = new Font("Segoe UI", 9.5F); cmbCategory.DropDownStyle = ComboBoxStyle.DropDown;
            cmbCategory.AutoCompleteMode = AutoCompleteMode.SuggestAppend; cmbCategory.AutoCompleteSource = AutoCompleteSource.ListItems;
            pnlCategory.Controls.Add(cmbCategory);
            SetupCustomComboBox(pnlCategory, cmbCategory);

            Label lblAmount = new Label { Text = "Tutar:", Left = 400, Top = 75, ForeColor = TextMuted, AutoSize = true };
            Panel pnlAmount = new Panel { Left = 400, Top = 100, Width = 120, Height = 36 };
            SetupSmoothContainer(pnlAmount, 8, CardBackColor);
            txtAmount.Name = "TransactionAmount";
            txtAmount.Left = 10; txtAmount.Top = 8; txtAmount.Width = 100;
            txtAmount.Font = new Font("Segoe UI", 10.5F); txtAmount.BorderStyle = BorderStyle.None;
            txtAmount.BackColor = CardBackColor; txtAmount.ForeColor = TextLight;
            txtAmount.TextChanged += (s, e) => SmartFormatAmount(txtAmount);
            pnlAmount.Controls.Add(txtAmount);

            Label lblDescription = new Label { Text = "Açıklama (opsiyonel):", Left = 20, Top = 150, ForeColor = TextMuted, AutoSize = true };
            Panel pnlDesc = new Panel { Left = 20, Top = 175, Width = 500, Height = 36 };
            SetupSmoothContainer(pnlDesc, 8, CardBackColor);
            txtDescription.Name = "TransactionDescription";
            txtDescription.Left = 10; txtDescription.Top = 8; txtDescription.Width = 480;
            txtDescription.Font = new Font("Segoe UI", 10.5F); txtDescription.BorderStyle = BorderStyle.None;
            txtDescription.BackColor = CardBackColor; txtDescription.ForeColor = TextLight;
            pnlDesc.Controls.Add(txtDescription);

            // Tarih Aralığı Filtresi — tablonun (ve arama kutusunun) sağ kenarına hizalı, Ara
            // kutusunun tam üstündeki satırda durur (bkz. PositionSearchArea/PositionDateRangeArea,
            // pencere yeniden boyutlanınca da hizası korunur).
            // CheckBox'ın sistem-çizimli kutusu Label'lardan daha yüksek olduğu için 75→100 arasındaki
            // 25px'lik boşluğa sığmıyor, metin alttaki tarih kutusunun içine taşıyordu — Top yukarı
            // alındı. BackColor=Transparent koyu temada arkasında görünen açık dikdörtgeni kaldırır
            // (bkz. LoginForm.chkRememberMe — aynı desen).
            chkDateFilter.Text = "Tarih Aralığı"; chkDateFilter.ForeColor = TextMuted; chkDateFilter.BackColor = Color.Transparent; chkDateFilter.Top = 68; chkDateFilter.AutoSize = true;
            chkDateFilter.CheckedChanged += (s, e) =>
            {
                dtpStart.Enabled = dtpEnd.Enabled = chkDateFilter.Checked;
                RefreshGrid();
            };

            dtpStart.Top = 100; dtpStart.Width = 145; dtpStart.Enabled = false;
            dtpStart.Value = DateTime.Today.AddMonths(-1);
            dtpStart.ValueChanged += (s, e) => { if (chkDateFilter.Checked) RefreshGrid(); };

            // Sabit genişlikte ve ortalanmış: eskiden AutoSize ile tire karakteri beklenenden geniş
            // ölçülüp sağdaki kutunun içine taşıyordu.
            Label lblDateSep = new Label { Text = "—", Top = 100, Width = 20, Height = 36, ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false };

            dtpEnd.Top = 100; dtpEnd.Width = 145; dtpEnd.Enabled = false;
            dtpEnd.Value = DateTime.Today;
            dtpEnd.ValueChanged += (s, e) => { if (chkDateFilter.Checked) RefreshGrid(); };

            btnAdd.Text = "➕ İşlem Ekle";
            btnAdd.Left = 540;
            btnAdd.Top = 175;
            btnAdd.Width = 140;
            btnAdd.Height = 36;
            btnAdd.Cursor = Cursors.Hand;
            SetupRoundedButton(btnAdd, AccentColor, Color.White, false);
            btnAdd.Click += BtnAdd_Click;

            // Arama kutusu, tablonun sağ kenarına hizalı (bkz. PositionSearchArea, pencere
            // yeniden boyutlanınca da tablonun sonuna hizalı kalması için)
            lblSearch.Text = "Ara:"; lblSearch.Top = 150; lblSearch.ForeColor = TextMuted; lblSearch.AutoSize = true;
            pnlSearch.Top = 175; pnlSearch.Width = 140; pnlSearch.Height = 36;
            SetupSmoothContainer(pnlSearch, 8, CardBackColor);
            txtSearch.Left = 10; txtSearch.Top = 8; txtSearch.Width = 120;
            txtSearch.Font = new Font("Segoe UI", 10.5F); txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = CardBackColor; txtSearch.ForeColor = TextLight;
            txtSearch.TextChanged += (s, e) => RefreshGrid();
            pnlSearch.Controls.Add(txtSearch);

            btnSearch.Text = "🔍";
            btnSearch.Top = 175; btnSearch.Width = 40; btnSearch.Height = 36; btnSearch.Cursor = Cursors.Hand;
            SetupRoundedButton(btnSearch, AccentColor, Color.White, false);
            btnSearch.Click += (s, e) => RefreshGrid();

            lblStatus.Left = 20;
            lblStatus.Top = 222;
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
            lblStatus.Font = new Font("Segoe UI", 9F);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblType); pnlTop.Controls.Add(pnlType);
            pnlTop.Controls.Add(lblCategory); pnlTop.Controls.Add(pnlCategory);
            pnlTop.Controls.Add(lblAmount); pnlTop.Controls.Add(pnlAmount);
            pnlTop.Controls.Add(chkDateFilter); pnlTop.Controls.Add(dtpStart); pnlTop.Controls.Add(lblDateSep); pnlTop.Controls.Add(dtpEnd);
            pnlTop.Controls.Add(lblDescription); pnlTop.Controls.Add(pnlDesc);
            pnlTop.Controls.Add(btnAdd);
            pnlTop.Controls.Add(lblSearch); pnlTop.Controls.Add(pnlSearch); pnlTop.Controls.Add(btnSearch);
            pnlTop.Controls.Add(lblStatus);

            // Arama kutusunu tablonun (pnlGrid) sağ kenarıyla hizalar; pnlGrid'in sağ dolgusu (40)
            // ile aynı hizada dursun diye pencere yeniden boyutlanınca da yeniden konumlandırılır.
            void PositionSearchArea()
            {
                int rightEdge = pnlTop.Width - 40;
                btnSearch.Left = rightEdge - btnSearch.Width;
                pnlSearch.Left = btnSearch.Left - 10 - pnlSearch.Width;
                lblSearch.Left = pnlSearch.Left;
            }
            PositionSearchArea();

            // Tarih Aralığı filtresini de aynı sağ kenara hizalar — Ara kutusunun bir üst satırında,
            // aynı hizada durur.
            void PositionDateRangeArea()
            {
                int rightEdge = pnlTop.Width - 40;
                dtpEnd.Left = rightEdge - dtpEnd.Width;
                lblDateSep.Left = dtpEnd.Left - lblDateSep.Width;
                dtpStart.Left = lblDateSep.Left - dtpStart.Width;
                chkDateFilter.Left = dtpStart.Left;
            }
            PositionDateRangeArea();

            this.Resize += (s, e) => { PositionSearchArea(); PositionDateRangeArea(); };

            // --- ORTA PANEL (Tablo) ---
            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.Padding = new Padding(20, 0, 40, 0);
            pnlGrid.BackColor = AppBackColor;

            Panel pnlGridWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2, 6, 2, 6) };
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);

            dgvTransactions.Dock = DockStyle.Fill;
            dgvTransactions.ReadOnly = true; dgvTransactions.AllowUserToAddRows = false; dgvTransactions.AllowUserToDeleteRows = false;
            dgvTransactions.AllowUserToResizeColumns = false; dgvTransactions.AllowUserToResizeRows = false;
            dgvTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgvTransactions.MultiSelect = false;
            dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; dgvTransactions.RowHeadersVisible = false;
            dgvTransactions.Font = new Font("Segoe UI", 9.5F); dgvTransactions.RowTemplate.Height = 44;
            dgvTransactions.CellDoubleClick += DgvTransactions_CellDoubleClick;

            dgvTransactions.BorderStyle = BorderStyle.None; dgvTransactions.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvTransactions.GridColor = AppTheme.GridLineColor; dgvTransactions.BackgroundColor = CardBackColor;

            // Renkler sadeleştirildi (Sadece düz beyaz)
            dgvTransactions.DefaultCellStyle.BackColor = CardBackColor; dgvTransactions.DefaultCellStyle.ForeColor = TextLight;
            dgvTransactions.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvTransactions.DefaultCellStyle.SelectionBackColor = AccentColor;
            dgvTransactions.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.HeaderBackColor; dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvTransactions.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.HeaderBackColor; dgvTransactions.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); dgvTransactions.EnableHeadersVisualStyles = false; dgvTransactions.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None; dgvTransactions.ColumnHeadersHeight = 40;

            dgvTransactions.CellPainting += DgvTransactions_CellPainting;

            // Kaydırma çubuğu Windows'un yerleşik denetimi olduğundan BackColor/ForeColor'a uymuyor;
            // handle oluşunca koyu temaya göre boyatıyoruz.
            dgvTransactions.HandleCreated += (s, e) => DarkTitleBarHelper.SetDataGridViewScrollBarDarkMode(dgvTransactions, AppTheme.IsDark);

            pnlGridWrapper.Controls.Add(dgvTransactions);
            pnlGrid.Controls.Add(pnlGridWrapper);

            // --- ALT PANEL ---
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 80;
            pnlBottom.Padding = new Padding(20, 15, 40, 15);
            pnlBottom.BackColor = AppBackColor;

            btnExport.Text = "📄 CSV'ye Aktar";
            btnExport.Top = 20; btnExport.Height = 38; btnExport.Cursor = Cursors.Hand;
            btnExport.Width = TextRenderer.MeasureText(btnExport.Text, btnExport.Font).Width + 44;
            btnExport.Left = 20;
            SetupRoundedButton(btnExport, Color.FromArgb(80, 85, 105), Color.White, false);
            btnExport.Click += BtnExport_Click;

            btnRecurring.Text = "🔄 Tekrarlanan İşlemler";
            btnRecurring.Top = 20; btnRecurring.Height = 38; btnRecurring.Cursor = Cursors.Hand;
            btnRecurring.Width = TextRenderer.MeasureText(btnRecurring.Text, btnRecurring.Font).Width + 44;
            btnRecurring.Left = btnExport.Left + btnExport.Width + 20;
            SetupRoundedButton(btnRecurring, Color.FromArgb(80, 85, 105), Color.White, false);
            btnRecurring.Click += (s, e) => { using (var dialog = new RecurringTransactionDialog(_user)) { dialog.ShowDialog(); } LoadTransactions(); };

            btnBulkMode.Top = 20; btnBulkMode.Height = 38; btnBulkMode.Cursor = Cursors.Hand;
            SetupRoundedButton(btnBulkMode, Color.FromArgb(80, 85, 105), Color.White, false);
            btnBulkMode.Click += BtnBulkMode_Click;

            btnBulkDelete.Text = "🗑️ Seçilenleri Sil";
            btnBulkDelete.Top = 20; btnBulkDelete.Height = 38; btnBulkDelete.Cursor = Cursors.Hand;
            btnBulkDelete.Width = TextRenderer.MeasureText(btnBulkDelete.Text, btnBulkDelete.Font).Width + 44;
            SetupRoundedButton(btnBulkDelete, DangerColor, Color.White, false);
            btnBulkDelete.Click += BtnBulkDelete_Click;
            btnBulkDelete.Visible = false;

            btnBulkCategory.Text = "🔀 Kategori Değiştir";
            btnBulkCategory.Top = 20; btnBulkCategory.Height = 38; btnBulkCategory.Cursor = Cursors.Hand;
            btnBulkCategory.Width = TextRenderer.MeasureText(btnBulkCategory.Text, btnBulkCategory.Font).Width + 44;
            SetupRoundedButton(btnBulkCategory, Color.FromArgb(80, 85, 105), Color.White, false);
            btnBulkCategory.Click += BtnBulkCategory_Click;
            btnBulkCategory.Visible = false;

            SetBulkModeButtonText();
            PositionBottomButtons();

            pnlBottom.Controls.Add(btnExport); pnlBottom.Controls.Add(btnRecurring);
            pnlBottom.Controls.Add(btnBulkMode); pnlBottom.Controls.Add(btnBulkDelete); pnlBottom.Controls.Add(btnBulkCategory);

            this.Controls.Add(pnlGrid); this.Controls.Add(pnlBottom); this.Controls.Add(pnlTop);
        }

        private void DgvTransactions_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex > 0 && e.ColumnIndex < dgvTransactions.ColumnCount)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);
                using (Pen p = new Pen(AppTheme.RowSeparatorColor, 1)) { e.Graphics!.DrawLine(p, e.CellBounds.Left, e.CellBounds.Top + 10, e.CellBounds.Left, e.CellBounds.Bottom - 10); }
                e.Handled = true;
            }
        }

        private string GetSelectedType() => cmbType.SelectedItem?.ToString() == "Gelir" ? "income" : "expense";
        private static string TypeToTr(string type) => type switch { "income" => "Gelir", "goal" => "Hedef", "invest" => "Yatırım", _ => "Gider" };

        private void LoadCategorySuggestions()
        {
            string type = GetSelectedType();
            var categories = _categoryService.GetUserCategoriesByType(_user.Id, type);
            cmbCategory.Items.Clear();
            foreach (var cat in categories) cmbCategory.Items.Add(cat.Name);
            cmbCategory.Text = string.Empty;
            var autoCompleteList = new AutoCompleteStringCollection();
            autoCompleteList.AddRange(categories.Select(c => c.Name).ToArray());
            cmbCategory.AutoCompleteCustomSource = autoCompleteList;
        }

        private void LoadTransactions()
        {
            _cachedTransactions = _transactionService.GetUserTransactions(_user.Id);
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            var tr = new System.Globalization.CultureInfo("tr-TR");

            string searchText = txtSearch.Text.Trim();
            IEnumerable<Transaction> visibleTransactions = string.IsNullOrEmpty(searchText)
                ? _cachedTransactions
                : _cachedTransactions.Where(t =>
                    tr.CompareInfo.IndexOf(t.CategoryName ?? string.Empty, searchText, System.Globalization.CompareOptions.IgnoreCase) >= 0 ||
                    tr.CompareInfo.IndexOf(t.Description ?? string.Empty, searchText, System.Globalization.CompareOptions.IgnoreCase) >= 0
                  );

            // Satır bazlı tarih-aralığı sorgusu repository'de yok (sadece toplam/kategori kırılımı
            // sorgusu var, bkz. Rapor ekranı) — mevcut arama filtresi deseniyle tutarlı olsun diye
            // burada da bellek-içi (in-memory) filtreliyoruz.
            if (chkDateFilter.Checked)
            {
                DateTime start = dtpStart.Value.Date;
                DateTime end = dtpEnd.Value.Date.AddDays(1);
                visibleTransactions = visibleTransactions.Where(t => t.TransactionDate >= start && t.TransactionDate < end);
            }

            // Tarih sütununu Açıklama'nın hemen önüne aldık
            var displayList = visibleTransactions.Select(t => new
            {
                ID = t.Id,
                Tip = TypeToTr(t.Type),
                Kategori = t.CategoryName,
                Tutar = _user.HideAmountsEnabled ? "••••••" : t.Amount.ToString("#,##0", tr) + " ₺",
                Tarih = t.TransactionDate.ToString("dd.MM.yyyy HH:mm"),
                Açıklama = t.Description
            }).ToList();

            dgvTransactions.DataSource = displayList;
            if (dgvTransactions.Columns["ID"] != null) dgvTransactions.Columns["ID"]!.Visible = false;

            if (dgvTransactions.Columns["Tip"] != null)
            {
                // Genişlikleri daha estetik olacak şekilde dağıttık
                dgvTransactions.Columns["Tip"]!.FillWeight = 30; // Yamuk durmaması için biraz açtık
                dgvTransactions.Columns["Kategori"]!.FillWeight = 70;
                dgvTransactions.Columns["Tutar"]!.FillWeight = 45;
                dgvTransactions.Columns["Tarih"]!.FillWeight = 60;
                dgvTransactions.Columns["Açıklama"]!.FillWeight = 95;
            }
        }

        private bool _suppressAmountFormatting = false;

        // Tutar kutusuna yazılan rakamları "10.000" gibi binlik ayraçlarla biçimlendirir (Onboarding ekranındakiyle aynı mantık).
        private void SmartFormatAmount(TextBox txt)
        {
            if (_suppressAmountFormatting || string.IsNullOrWhiteSpace(txt.Text)) return;
            string value = new string(txt.Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(value)) return;
            if (decimal.TryParse(value, out decimal amount))
            {
                string formatted = amount.ToString("#,##0", new System.Globalization.CultureInfo("tr-TR"));
                if (txt.Text == formatted) return;
                _suppressAmountFormatting = true;
                txt.Text = formatted;
                txt.SelectionStart = txt.Text.Length;
                _suppressAmountFormatting = false;
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string categoryName = cmbCategory.Text.Trim();
            if (string.IsNullOrWhiteSpace(categoryName)) { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = "Lütfen bir kategori adı girin."; return; }
            string rawAmount = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
            if (!decimal.TryParse(rawAmount, out decimal amount)) { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = "Geçersiz tutar."; return; }

            string type = GetSelectedType();
            string description = txtDescription.Text;
            var category = _categoryService.GetOrCreateCategory(_user.Id, categoryName, type);
            bool success = _transactionService.AddTransaction(_user.Id, category.Id, amount, type, description, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "İşlem başarıyla eklendi.";
                txtAmount.Clear(); txtDescription.Clear(); LoadCategorySuggestions(); LoadTransactions();
            }
            else { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = errorMessage; }
        }

        private void DgvTransactions_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var idCell = dgvTransactions.Rows[e.RowIndex].Cells["ID"];
            if (idCell?.Value == null || !int.TryParse(idCell.Value.ToString(), out int transactionId)) return;

            var transaction = _cachedTransactions.FirstOrDefault(t => t.Id == transactionId);
            if (transaction == null) return;

            using (var dialog = new TransactionEditDialog(_user, transaction))
            {
                dialog.ShowDialog();
                if (dialog.WasUpdated)
                {
                    lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "İşlem güncellendi.";
                    LoadTransactions();
                }
                else if (dialog.WasDeleted)
                {
                    lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "İşlem silindi.";
                    LoadTransactions();
                }
            }
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV Dosyası (*.csv)|*.csv";
                dialog.FileName = $"islemler_{DateTime.Today:yyyy_MM_dd}.csv";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var tr = new System.Globalization.CultureInfo("tr-TR");
                        using (var writer = new System.IO.StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8))
                        {
                            writer.WriteLine("Tarih;Tip;Kategori;Tutar;Açıklama");
                            foreach (var t in _cachedTransactions)
                            {
                                string tip = TypeToTr(t.Type);
                                string tarih = t.TransactionDate.ToString("dd.MM.yyyy");
                                string tutar = t.Amount.ToString("0.00", tr);
                                string aciklama = (t.Description ?? "").Replace(";", ",");
                                writer.WriteLine($"{tarih};{tip};{t.CategoryName};{tutar};{aciklama}");
                            }
                        }
                        lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "İşlemler CSV olarak dışa aktarıldı.";
                    }
                    catch (Exception ex) { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = $"Dışa aktarma başarısız: {ex.Message}"; }
                }
            }
        }

        private void SetBulkModeButtonText()
        {
            btnBulkMode.Text = _bulkModeActive ? "✖️ Toplu İşlemi Kapat" : "☑️ Toplu İşlem";
            btnBulkMode.Width = TextRenderer.MeasureText(btnBulkMode.Text, btnBulkMode.Font).Width + 44;
        }

        // btnBulkMode'un metni (dolayısıyla genişliği) moda göre değiştiği için, sağındaki
        // butonların konumu her toggle'da yeniden hesaplanır.
        private void PositionBottomButtons()
        {
            btnBulkMode.Left = btnRecurring.Left + btnRecurring.Width + 20;
            btnBulkDelete.Left = btnBulkMode.Left + btnBulkMode.Width + 20;
            btnBulkCategory.Left = btnBulkDelete.Left + btnBulkDelete.Width + 20;
        }

        // Arama kutusu her keystroke'ta dgvTransactions.DataSource'u yeniden bağlayıp seçimi
        // sessizce sıfırlıyor; toplu seçim sırasında bu şaşırtıcı olacağından, toplu mod açıkken
        // aramayı devre dışı bırakmak en basit ve güvenli çözüm.
        private void BtnBulkMode_Click(object? sender, EventArgs e)
        {
            _bulkModeActive = !_bulkModeActive;
            dgvTransactions.MultiSelect = _bulkModeActive;
            txtSearch.Enabled = !_bulkModeActive;
            btnSearch.Enabled = !_bulkModeActive;
            btnBulkDelete.Visible = _bulkModeActive;
            btnBulkCategory.Visible = _bulkModeActive;
            SetBulkModeButtonText();
            PositionBottomButtons();

            if (!_bulkModeActive) dgvTransactions.ClearSelection();
        }

        private void BtnBulkDelete_Click(object? sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count == 0)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen en az bir işlem seçin.";
                return;
            }

            var confirm = MessageBox.Show($"{dgvTransactions.SelectedRows.Count} işlemi silmek istediğinize emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            var selectedIds = dgvTransactions.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => Convert.ToInt32(r.Cells["ID"].Value)).ToList();

            int deleted = 0;
            foreach (var id in selectedIds)
            {
                if (_transactionService.DeleteTransaction(id, _user.Id, out _)) deleted++;
            }

            lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
            lblStatus.Text = $"{deleted} işlem silindi.";
            LoadTransactions();
        }

        private void BtnBulkCategory_Click(object? sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count == 0)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen en az bir işlem seçin.";
                return;
            }

            var selectedIds = dgvTransactions.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => Convert.ToInt32(r.Cells["ID"].Value)).ToList();
            var selectedTx = _cachedTransactions.Where(t => selectedIds.Contains(t.Id)).ToList();

            var distinctTypes = selectedTx.Select(t => t.Type).Distinct().ToList();
            if (distinctTypes.Count > 1 || (distinctTypes.Count == 1 && distinctTypes[0] != "income" && distinctTypes[0] != "expense"))
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Toplu kategori değişikliği için aynı tipte (gelir/gider) işlemler seçin.";
                return;
            }

            string type = distinctTypes[0];
            using var dialog = new BulkCategoryDialog(_user, type);
            if (dialog.ShowDialog() == DialogResult.OK && dialog.SelectedCategoryId.HasValue)
            {
                int updated = 0;
                foreach (var t in selectedTx)
                {
                    if (_transactionService.UpdateTransaction(t.Id, _user.Id, dialog.SelectedCategoryId.Value, t.Amount, t.Type, t.Description, t.TransactionDate, out _))
                        updated++;
                }
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = $"{updated} işlemin kategorisi değiştirildi.";
                LoadTransactions();
            }
        }

        protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } }

        // --- GÖRSEL YARDIMCI METOTLAR ---

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct COMBOBOXINFO
        {
            public int cbSize;
            public RECT rcItem;
            public RECT rcButton;
            public IntPtr stateButton;
            public IntPtr hwndCombo;
            public IntPtr hwndItem;
            public IntPtr hwndList;
        }

        private const int CB_GETCOMBOBOXINFO = 0x0164;
        private const int EM_SETRECT = 0x00B3;

        [DllImport("user32.dll")]
        private static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref COMBOBOXINFO pcbi);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

        // Düzenlenebilir ComboBox'ın native metin kutusunun biçimlendirme dikdörtgenini üstten
        // genişleterek yazının dikey olarak birkaç piksel yukarı kaymasını sağlar.
        private static void ShiftEditTextUp(ComboBox cmb, int pixels)
        {
            var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf<COMBOBOXINFO>() };
            if (!GetComboBoxInfo(cmb.Handle, ref info) || info.hwndItem == IntPtr.Zero) return;

            if (!GetClientRect(info.hwndItem, out RECT rect)) return;
            rect.Top -= pixels;
            SendMessage(info.hwndItem, EM_SETRECT, IntPtr.Zero, ref rect);
        }

        private void SetupCustomComboBox(Panel pnl, ComboBox cmb)
        {
            SetupSmoothContainer(pnl, 8, CardBackColor);
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = CardBackColor;
            cmb.ForeColor = TextLight;

            // 1. Mavi arka planı engellemek için
            cmb.DrawMode = DrawMode.OwnerDrawFixed;
            cmb.ItemHeight = 22;
            cmb.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bgColor = isSelected ? AppTheme.HoverBackColor : CardBackColor;
                e.Graphics.FillRectangle(new SolidBrush(bgColor), e.Bounds);
                TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index]?.ToString() ?? string.Empty, cmb.Font, e.Bounds, TextLight, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            };

            // 2. Dıştaki ince beyaz çerçeveyi tıraşlıyoruz
            cmb.Region = new Region(new Rectangle(1, 1, cmb.Width - 2, cmb.Height - 2));

            // Düzenlenebilir (editable) kutularda native metin kutusu alt sınıra çok yakın yazıyor;
            // bu da "g, y, ç" gibi alt çıkıntılı (descender) harflerin üstteki kırpmayla kesilmesine
            // yol açıyordu. Çözüm olarak metnin kendisini native edit kontrolü içinde birkaç piksel
            // yukarı kaydırıyoruz (EM_SETRECT), böylece hem çerçeve gizli kalıyor hem de harfler tam görünüyor.
            if (cmb.DropDownStyle == ComboBoxStyle.DropDown)
            {
                cmb.HandleCreated += (s, e) => ShiftEditTextUp(cmb, 3);
                if (cmb.IsHandleCreated) ShiftEditTextUp(cmb, 3);
            }

            // 3. Oku ve beyaz çizgiyi gizlemek için örtü paneli (Overlay)
            Panel pnlArrow = new Panel();
            pnlArrow.Width = 28;
            pnlArrow.Height = cmb.Height - 2;
            pnlArrow.Left = cmb.Right - pnlArrow.Width - 1;
            pnlArrow.Top = cmb.Top + 1;
            pnlArrow.BackColor = CardBackColor;
            pnlArrow.Cursor = Cursors.Hand;

            // 4. Kendi okumuzu örtü panelinin üzerine çiziyoruz
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

            // Paneli en öne getir ki diğer her şeyi kapatsın
            pnl.Controls.Add(pnlArrow);
            pnlArrow.BringToFront();
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } }; pnl.SizeChanged += (s, e) => pnl.Invalidate(); }
        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor, bool isOutlined) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius) { var path = new System.Drawing.Drawing2D.GraphicsPath(); int d = Math.Max(radius * 2, 1); path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path; }
    }
}