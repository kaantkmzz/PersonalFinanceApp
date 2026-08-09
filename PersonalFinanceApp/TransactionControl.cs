using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class TransactionControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly AccountService _accountService = new AccountService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color DangerColor = Color.FromArgb(220, 90, 90);

        private Panel pnlTop = new Panel();
        private Panel pnlGrid = new Panel();
        private Panel pnlBottom = new Panel();

        private Panel pnlWalletCapsule = new Panel();
        private Label lblWalletBalance = new Label();

        private ComboBox cmbType = new ComboBox();
        private ComboBox cmbCategory = new ComboBox();
        private TextBox txtAmount = new TextBox();
        private TextBox txtDescription = new TextBox();

        private Button btnAdd = new Button();
        private Button btnDelete = new Button();
        private Button btnExport = new Button();
        private Button btnRecurring = new Button();
        private Label lblStatus = new Label();

        private DataGridView dgvTransactions = new DataGridView();
        private List<Transaction> _cachedTransactions = new List<Transaction>();

        public TransactionControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadCategorySuggestions();
            LoadTransactions();
            RefreshWalletBalance();
        }

        public void RefreshData()
        {
            LoadTransactions();
            RefreshWalletBalance();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            // --- ÜST PANEL ---
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 230;
            pnlTop.BackColor = AppBackColor;

            Label lblTitle = new Label { Text = "Gelir / Gider İşlemleri", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true };

            // --- CÜZDAN KAPSÜLÜ ---
            pnlWalletCapsule.Height = 36;
            pnlWalletCapsule.Top = 15; // Başlıkla (Gelir/Gider İşlemleri) aynı hizaya alırsak daha şık durur
                                       // Alpha (şeffaflık) kanalı içeren renklerle yumuşak bir yeşil cam efekti:
            SetupTranslucentCapsule(pnlWalletCapsule, Color.FromArgb(25, 46, 204, 113), Color.FromArgb(80, 46, 204, 113));

            lblWalletBalance.Dock = DockStyle.Fill;
            lblWalletBalance.TextAlign = ContentAlignment.MiddleCenter;
            lblWalletBalance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblWalletBalance.ForeColor = Color.FromArgb(170, 255, 190);
            lblWalletBalance.BackColor = Color.Transparent; // ÇOK ÖNEMLİ: Altındaki şeffaf çizimin görünmesini sağlar
            pnlWalletCapsule.Controls.Add(lblWalletBalance);

            // Girdi Alanları
            Label lblType = new Label { Text = "Tip:", Left = 20, Top = 75, ForeColor = TextMuted, AutoSize = true };
            Panel pnlType = new Panel { Left = 20, Top = 100, Width = 140, Height = 36 };
            cmbType.Left = 5; cmbType.Top = 7; cmbType.Width = 135;
            cmbType.Font = new Font("Segoe UI", 9.5F); cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("Gelir"); cmbType.Items.Add("Gider"); cmbType.SelectedIndex = 1;
            cmbType.SelectedIndexChanged += (s, e) => LoadCategorySuggestions();
            pnlType.Controls.Add(cmbType);
            SetupCustomComboBox(pnlType, cmbType); // Beyazlık ve Mavi renk düzeltildi

            Label lblCategory = new Label { Text = "Kategori:", Left = 180, Top = 75, ForeColor = TextMuted, AutoSize = true };
            Panel pnlCategory = new Panel { Left = 180, Top = 100, Width = 200, Height = 36 };
            cmbCategory.Left = 5; cmbCategory.Top = 7; cmbCategory.Width = 195;
            cmbCategory.Font = new Font("Segoe UI", 9.5F); cmbCategory.DropDownStyle = ComboBoxStyle.DropDown;
            cmbCategory.AutoCompleteMode = AutoCompleteMode.SuggestAppend; cmbCategory.AutoCompleteSource = AutoCompleteSource.ListItems;
            pnlCategory.Controls.Add(cmbCategory);
            SetupCustomComboBox(pnlCategory, cmbCategory);

            Label lblAmount = new Label { Text = "Tutar:", Left = 400, Top = 75, ForeColor = TextMuted, AutoSize = true };
            Panel pnlAmount = new Panel { Left = 400, Top = 100, Width = 120, Height = 36 };
            SetupSmoothContainer(pnlAmount, 8, CardBackColor);
            txtAmount.Left = 10; txtAmount.Top = 8; txtAmount.Width = 100;
            txtAmount.Font = new Font("Segoe UI", 10.5F); txtAmount.BorderStyle = BorderStyle.None;
            txtAmount.BackColor = CardBackColor; txtAmount.ForeColor = TextLight;
            pnlAmount.Controls.Add(txtAmount);

            Label lblDescription = new Label { Text = "Açıklama (opsiyonel):", Left = 20, Top = 150, ForeColor = TextMuted, AutoSize = true };
            Panel pnlDesc = new Panel { Left = 20, Top = 175, Width = 500, Height = 36 };
            SetupSmoothContainer(pnlDesc, 8, CardBackColor);
            txtDescription.Left = 10; txtDescription.Top = 8; txtDescription.Width = 480;
            txtDescription.Font = new Font("Segoe UI", 10.5F); txtDescription.BorderStyle = BorderStyle.None;
            txtDescription.BackColor = CardBackColor; txtDescription.ForeColor = TextLight;
            pnlDesc.Controls.Add(txtDescription);

            btnAdd.Text = "➕ İşlem Ekle";
            btnAdd.Left = 540;
            btnAdd.Top = 175;
            btnAdd.Width = 140;
            btnAdd.Height = 36;
            btnAdd.Cursor = Cursors.Hand;
            SetupRoundedButton(btnAdd, AccentColor, Color.White, false);
            btnAdd.Click += BtnAdd_Click;

            lblStatus.Left = 695; // 540 (Buton Left) + 140 (Buton Yüksekliği) + 15 boşluk
            lblStatus.Top = 184;  // Buton ile dikeyde hizalı olması için
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
            lblStatus.Font = new Font("Segoe UI", 9F);

            pnlTop.Controls.Add(lblTitle); pnlTop.Controls.Add(pnlWalletCapsule);
            pnlTop.Controls.Add(lblType); pnlTop.Controls.Add(pnlType);
            pnlTop.Controls.Add(lblCategory); pnlTop.Controls.Add(pnlCategory);
            pnlTop.Controls.Add(lblAmount); pnlTop.Controls.Add(pnlAmount);
            pnlTop.Controls.Add(lblDescription); pnlTop.Controls.Add(pnlDesc);
            pnlTop.Controls.Add(btnAdd); pnlTop.Controls.Add(lblStatus);

            pnlTop.Resize += (s, e) => { pnlWalletCapsule.Left = pnlTop.Width - pnlWalletCapsule.Width - 40; };

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

            dgvTransactions.BorderStyle = BorderStyle.None; dgvTransactions.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvTransactions.GridColor = Color.FromArgb(55, 60, 80); dgvTransactions.BackgroundColor = CardBackColor;

            // Renkler sadeleştirildi (Sadece düz beyaz)
            dgvTransactions.DefaultCellStyle.BackColor = CardBackColor; dgvTransactions.DefaultCellStyle.ForeColor = TextLight;
            dgvTransactions.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvTransactions.DefaultCellStyle.SelectionBackColor = AccentColor;
            dgvTransactions.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 39, 54); dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvTransactions.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 39, 54); dgvTransactions.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); dgvTransactions.EnableHeadersVisualStyles = false; dgvTransactions.ColumnHeadersHeight = 40;

            dgvTransactions.CellPainting += DgvTransactions_CellPainting;

            pnlGridWrapper.Controls.Add(dgvTransactions);
            pnlGrid.Controls.Add(pnlGridWrapper);

            // --- ALT PANEL ---
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 80;
            pnlBottom.Padding = new Padding(20, 15, 40, 15);
            pnlBottom.BackColor = AppBackColor;

            btnDelete.Text = "🗑️ Seçili İşlemi Sil";
            btnDelete.Left = 20; btnDelete.Top = 20; btnDelete.Width = 160; btnDelete.Height = 38; btnDelete.Cursor = Cursors.Hand;
            SetupRoundedButton(btnDelete, DangerColor, Color.White, false);
            btnDelete.Click += BtnDelete_Click;

            btnExport.Text = "📄 CSV'ye Aktar";
            btnExport.Left = 200; btnExport.Top = 20; btnExport.Width = 160; btnExport.Height = 38; btnExport.Cursor = Cursors.Hand;
            SetupRoundedButton(btnExport, Color.FromArgb(80, 85, 105), Color.White, false);
            btnExport.Click += BtnExport_Click;

            btnRecurring.Text = "🔄 Tekrarlanan İşlemler";
            btnRecurring.Left = 380; btnRecurring.Top = 20; btnRecurring.Width = 210; btnRecurring.Height = 38; btnRecurring.Cursor = Cursors.Hand;
            SetupRoundedButton(btnRecurring, Color.FromArgb(80, 85, 105), Color.White, false);
            btnRecurring.Click += (s, e) => { using (var dialog = new RecurringTransactionDialog(_user)) { dialog.ShowDialog(); } };

            pnlBottom.Controls.Add(btnDelete); pnlBottom.Controls.Add(btnExport); pnlBottom.Controls.Add(btnRecurring);

            this.Controls.Add(pnlGrid); this.Controls.Add(pnlBottom); this.Controls.Add(pnlTop);
        }

        private void RefreshWalletBalance()
        {
            var (wallet, _) = _accountService.GetBalances(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");
            lblWalletBalance.Text = _user.HideAmountsEnabled ? "💳 Cüzdan: ••••••" : $"💳 Cüzdan: {wallet.ToString("#,##0", tr)} ₺";
            Size size = TextRenderer.MeasureText(lblWalletBalance.Text, lblWalletBalance.Font);
            pnlWalletCapsule.Width = size.Width + 40;
            pnlWalletCapsule.Left = pnlTop.Width - pnlWalletCapsule.Width - 40;
        }

        private void DgvTransactions_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex > 0 && e.ColumnIndex < dgvTransactions.ColumnCount)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);
                using (Pen p = new Pen(Color.FromArgb(50, 55, 75), 1)) { e.Graphics.DrawLine(p, e.CellBounds.Left, e.CellBounds.Top + 10, e.CellBounds.Left, e.CellBounds.Bottom - 10); }
                e.Handled = true;
            }
        }

        private string GetSelectedType() { return cmbType.SelectedItem?.ToString() == "Gelir" ? "income" : "expense"; }

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

            // Tarih sütununu Açıklama'nın hemen önüne aldık
            var displayList = _cachedTransactions.Select(t => new
            {
                ID = t.Id,
                Tip = t.Type == "income" ? "Gelir" : "Gider",
                Kategori = t.CategoryName,
                Tutar = _user.HideAmountsEnabled ? "••••••" : t.Amount.ToString("#,##0", tr) + " ₺",
                Tarih = t.TransactionDate.ToString("dd.MM.yyyy HH:mm"),
                Açıklama = t.Description
            }).ToList();

            dgvTransactions.DataSource = displayList;
            if (dgvTransactions.Columns["ID"] != null) dgvTransactions.Columns["ID"].Visible = false;

            if (dgvTransactions.Columns["Tip"] != null)
            {
                // Genişlikleri daha estetik olacak şekilde dağıttık
                dgvTransactions.Columns["Tip"].FillWeight = 30; // Yamuk durmaması için biraz açtık
                dgvTransactions.Columns["Kategori"].FillWeight = 70;
                dgvTransactions.Columns["Tutar"].FillWeight = 45;
                dgvTransactions.Columns["Tarih"].FillWeight = 60;
                dgvTransactions.Columns["Açıklama"].FillWeight = 95;
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string categoryName = cmbCategory.Text.Trim();
            if (string.IsNullOrWhiteSpace(categoryName)) { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = "Lütfen bir kategori adı girin."; return; }
            if (!decimal.TryParse(txtAmount.Text, out decimal amount)) { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = "Geçersiz tutar."; return; }

            string type = GetSelectedType();
            string description = txtDescription.Text;
            var category = _categoryService.GetOrCreateCategory(_user.Id, categoryName, type);
            bool success = _transactionService.AddTransaction(_user.Id, category.Id, amount, type, description, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "İşlem başarıyla eklendi.";
                txtAmount.Clear(); txtDescription.Clear(); LoadCategorySuggestions(); LoadTransactions(); RefreshWalletBalance();
            }
            else { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = errorMessage; }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvTransactions.CurrentRow == null) { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = "Lütfen silmek için bir işlem seçin."; return; }
            var idCell = dgvTransactions.CurrentRow.Cells["ID"];
            if (idCell?.Value == null || !int.TryParse(idCell.Value.ToString(), out int transactionId)) return;

            if (MessageBox.Show("Bu işlemi silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_transactionService.DeleteTransaction(transactionId, _user.Id, out string errorMessage))
                {
                    lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "İşlem silindi."; LoadTransactions(); RefreshWalletBalance();
                }
                else { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = errorMessage; }
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
                                string tip = t.Type == "income" ? "Gelir" : "Gider";
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

        protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } }

        // --- GÖRSEL YARDIMCI METOTLAR ---
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
                Color bgColor = isSelected ? Color.FromArgb(60, 65, 85) : CardBackColor;
                e.Graphics.FillRectangle(new SolidBrush(bgColor), e.Bounds);
                TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index].ToString(), cmb.Font, e.Bounds, TextLight, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };

            // 2. Sadece dıştaki ince beyaz çerçeveyi tıraşlıyoruz
            cmb.Region = new Region(new Rectangle(1, 1, cmb.Width - 2, cmb.Height - 2));

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

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(pnl.Parent.BackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } }; pnl.SizeChanged += (s, e) => pnl.Invalidate(); }
        private void SetupTranslucentCapsule(Panel pnl, Color fillColor, Color borderColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(pnl.Parent.BackColor); var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1); using (var path = GetRoundedRectPath(rect, pnl.Height / 2)) { using (var brush = new SolidBrush(fillColor)) e.Graphics.FillPath(brush, path); using (var pen = new Pen(borderColor, 2f)) e.Graphics.DrawPath(pen, path); } }; pnl.SizeChanged += (s, e) => pnl.Invalidate(); }
        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor, bool isOutlined) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent.BackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius) { var path = new System.Drawing.Drawing2D.GraphicsPath(); int d = Math.Max(radius * 2, 1); path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path; }
    }
}