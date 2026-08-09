using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class CategoryControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly TransactionService _transactionService = new TransactionService();

        private readonly AccountService _accountService = new AccountService(); // Kasa bakiyesini çekmek için
        private Panel pnlSafeCapsule = new Panel();
        private Label lblSafeBalance = new Label();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color DangerColor = Color.FromArgb(220, 90, 90);

        private Panel pnlTop = new Panel();
        private Panel pnlGrid = new Panel();
        private Panel pnlBottom = new Panel();

        private ComboBox cmbFilterType = new ComboBox();
        private TextBox txtNewCategory = new TextBox();
        private ComboBox cmbNewCategoryType = new ComboBox();
        private Button btnAdd = new Button();
        private Label lblStatus = new Label();

        private DataGridView dgvCategories = new DataGridView();
        private List<Category> _cachedCategories = new List<Category>();

        public CategoryControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadCategories();

            this.Load += (s, e) => RefreshSafeBalance();
        }

        public void RefreshData()
        {
            LoadCategories();
            RefreshSafeBalance();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            // --- ÜST PANEL ---
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 190;
            pnlTop.BackColor = AppBackColor;

            Label lblTitle = new Label { Text = "Kategoriler", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true };
            // --- KASA KAPSÜLÜ ---
            pnlSafeCapsule.Height = 36;
            pnlSafeCapsule.Top = 15; // Başlıkla aynı hizada
                                     // Kasa için mavi tonlarında yumuşak şeffaf bir cam efekti
            SetupTranslucentCapsule(pnlSafeCapsule, Color.FromArgb(25, 52, 152, 219), Color.FromArgb(80, 52, 152, 219));

            lblSafeBalance.Dock = DockStyle.Fill;
            lblSafeBalance.TextAlign = ContentAlignment.MiddleCenter;
            lblSafeBalance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblSafeBalance.ForeColor = Color.FromArgb(170, 215, 255);
            lblSafeBalance.BackColor = Color.Transparent; // Altındaki şeffaflığın görünmesi için
            pnlSafeCapsule.Controls.Add(lblSafeBalance);

            pnlTop.Controls.Add(pnlSafeCapsule);
            pnlTop.Resize += (s, e) => { pnlSafeCapsule.Left = pnlTop.Width - pnlSafeCapsule.Width - 40; }; // Sağa yaslı kalması için

            Label lblFilter = new Label { Text = "Göster:", Left = 20, Top = 70, ForeColor = TextMuted, AutoSize = true };
            Panel pnlFilter = new Panel { Left = 20, Top = 95, Width = 140, Height = 36 };
            cmbFilterType.Left = 5; cmbFilterType.Top = 7; cmbFilterType.Width = 135;
            cmbFilterType.Font = new Font("Segoe UI", 9.5F); cmbFilterType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterType.Items.Add("Tümü"); cmbFilterType.Items.Add("Gelir"); cmbFilterType.Items.Add("Gider"); cmbFilterType.SelectedIndex = 0;
            cmbFilterType.SelectedIndexChanged += (s, e) => LoadCategories();
            pnlFilter.Controls.Add(cmbFilterType);
            SetupCustomComboBox(pnlFilter, cmbFilterType); // Beyazlık ve mavi renk düzeltildi

            Label lblNew = new Label { Text = "Yeni Kategori Adı:", Left = 180, Top = 70, ForeColor = TextMuted, AutoSize = true };
            Panel pnlNewCat = new Panel { Left = 180, Top = 95, Width = 220, Height = 36 };
            SetupSmoothContainer(pnlNewCat, 8, CardBackColor);
            txtNewCategory.Left = 10; txtNewCategory.Top = 8; txtNewCategory.Width = 200;
            txtNewCategory.Font = new Font("Segoe UI", 10.5F); txtNewCategory.BorderStyle = BorderStyle.None;
            txtNewCategory.BackColor = CardBackColor; txtNewCategory.ForeColor = TextLight;
            pnlNewCat.Controls.Add(txtNewCategory);

            Label lblNewType = new Label { Text = "Tip:", Left = 420, Top = 70, ForeColor = TextMuted, AutoSize = true };
            Panel pnlNewType = new Panel { Left = 420, Top = 95, Width = 120, Height = 36 };
            cmbNewCategoryType.Left = 5; cmbNewCategoryType.Top = 7; cmbNewCategoryType.Width = 115;
            cmbNewCategoryType.Font = new Font("Segoe UI", 9.5F); cmbNewCategoryType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbNewCategoryType.Items.Add("Gelir"); cmbNewCategoryType.Items.Add("Gider"); cmbNewCategoryType.SelectedIndex = 1;
            pnlNewType.Controls.Add(cmbNewCategoryType);
            SetupCustomComboBox(pnlNewType, cmbNewCategoryType); // Beyazlık ve mavi renk düzeltildi

            btnAdd.Text = "➕ Ekle";
            btnAdd.Left = 560; btnAdd.Top = 95; btnAdd.Width = 100; btnAdd.Height = 36; btnAdd.Cursor = Cursors.Hand;
            SetupRoundedButton(btnAdd, AccentColor, Color.White);
            btnAdd.Click += BtnAdd_Click;

            lblStatus.Left = 20;
            lblStatus.Top = 145;
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Font = new Font("Segoe UI", 9F);

            pnlTop.Controls.Add(lblTitle); pnlTop.Controls.Add(lblFilter); pnlTop.Controls.Add(pnlFilter);
            pnlTop.Controls.Add(lblNew); pnlTop.Controls.Add(pnlNewCat); pnlTop.Controls.Add(lblNewType); pnlTop.Controls.Add(pnlNewType);
            pnlTop.Controls.Add(btnAdd); pnlTop.Controls.Add(lblStatus);

            // --- ORTA PANEL (Tablo) ---
            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.Padding = new Padding(20, 0, 40, 0);
            pnlGrid.BackColor = AppBackColor;

            Panel pnlGridWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2, 6, 2, 6) };
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);

            dgvCategories.Dock = DockStyle.Fill;
            dgvCategories.ReadOnly = true; dgvCategories.AllowUserToAddRows = false; dgvCategories.AllowUserToDeleteRows = false;
            dgvCategories.AllowUserToResizeColumns = false; dgvCategories.AllowUserToResizeRows = false;
            dgvCategories.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgvCategories.MultiSelect = false;
            dgvCategories.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; dgvCategories.RowHeadersVisible = false;
            dgvCategories.Font = new Font("Segoe UI", 9.5F); dgvCategories.RowTemplate.Height = 44;
            // Çift tıklama olayını bağlıyoruz
            dgvCategories.CellDoubleClick += DgvCategories_CellDoubleClick;

            dgvCategories.BorderStyle = BorderStyle.None; dgvCategories.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvCategories.GridColor = Color.FromArgb(55, 60, 80); dgvCategories.BackgroundColor = CardBackColor;

            // Renkler sadeleştirildi (Sadece düz beyaz)
            dgvCategories.DefaultCellStyle.BackColor = CardBackColor; dgvCategories.DefaultCellStyle.ForeColor = TextLight;
            dgvCategories.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvCategories.DefaultCellStyle.SelectionBackColor = AccentColor;
            dgvCategories.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvCategories.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 39, 54); dgvCategories.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvCategories.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 39, 54); dgvCategories.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvCategories.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); dgvCategories.EnableHeadersVisualStyles = false; dgvCategories.ColumnHeadersHeight = 40;

            dgvCategories.CellPainting += DgvCategories_CellPainting;

            pnlGridWrapper.Controls.Add(dgvCategories);
            pnlGrid.Controls.Add(pnlGridWrapper);

            this.Controls.Add(pnlGrid); this.Controls.Add(pnlBottom); this.Controls.Add(pnlTop);
        }

        private void DgvCategories_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex > 0 && e.ColumnIndex < dgvCategories.ColumnCount)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);
                using (Pen p = new Pen(Color.FromArgb(50, 55, 75), 1)) { e.Graphics!.DrawLine(p, e.CellBounds.Left, e.CellBounds.Top + 10, e.CellBounds.Left, e.CellBounds.Bottom - 10); }
                e.Handled = true;
            }
        }

        private void DgvCategories_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var idCell = dgvCategories.Rows[e.RowIndex].Cells["ID"];
                var adCell = dgvCategories.Rows[e.RowIndex].Cells["Ad"];

                if (idCell?.Value != null && adCell?.Value != null && int.TryParse(idCell.Value.ToString(), out int categoryId))
                {
                    string categoryName = adCell.Value.ToString() ?? string.Empty;

                    // Yeni oluşturduğumuz pencereyi açıyoruz
                    using (var dialog = new CategoryDetailsDialog(_user, categoryId, categoryName))
                    {
                        dialog.ShowDialog();
                        LoadCategories(); // Kullanıcı pencereyi kapattığında tabloyu yenile (Silinmiş veya adı değişmiş olabilir)
                    }
                }
            }
        }

        private void LoadCategories()
        {
            string filter = cmbFilterType.SelectedItem?.ToString() ?? "Tümü";
            if (filter == "Tümü") _cachedCategories = _categoryService.GetUserCategories(_user.Id);
            else _cachedCategories = _categoryService.GetUserCategoriesByType(_user.Id, filter == "Gelir" ? "income" : "expense");

            var totals = _transactionService.GetCategoryTotals(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            var displayList = _cachedCategories.Select(c => new
            {
                ID = c.Id,
                Ad = c.Name,
                Tip = c.Type == "income" ? "Gelir" : "Gider",
                ToplamTutar = (totals.TryGetValue(c.Id, out decimal total) ? total : 0).ToString("#,##0", tr) + " ₺"
            }).ToList();

            dgvCategories.DataSource = displayList;

            if (dgvCategories.Columns["ID"] != null)
            {
                dgvCategories.Columns["ID"]!.FillWeight = 30; dgvCategories.Columns["Ad"]!.FillWeight = 90;
                dgvCategories.Columns["Tip"]!.FillWeight = 60; dgvCategories.Columns["ToplamTutar"]!.FillWeight = 90;
                dgvCategories.Columns["ToplamTutar"]!.HeaderText = "Toplam Tutar";
            }
        }

        // Bakiyeyi veritabanından çekip etikete (Label) yazan metot
        private void RefreshSafeBalance()
        {
            // _accountService.GetBalances metodundan 2. değeri (kasa) alıyoruz
            var (_, safe) = _accountService.GetBalances(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            lblSafeBalance.Text = _user.HideAmountsEnabled ? "🏦 Kasa: ••••••" : $"🏦 Kasa: {safe.ToString("#,##0", tr)} ₺";

            // Metin uzunluğuna göre kapsülün genişliğini dinamik ayarlıyoruz
            Size size = TextRenderer.MeasureText(lblSafeBalance.Text, lblSafeBalance.Font);
            pnlSafeCapsule.Width = size.Width + 40;
            pnlSafeCapsule.Left = pnlTop.Width - pnlSafeCapsule.Width - 40;

            pnlSafeCapsule.Invalidate();
            lblSafeBalance.Invalidate();
        }

        // Şeffaf kapsül çizimini yapan yardımcı metot (Bu sınıfta olmadığı için eklememiz gerekiyor)
        private void SetupTranslucentCapsule(Panel pnl, Color fillColor, Color borderColor)
        {
            pnl.BackColor = AppBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor);
                var rect = new Rectangle(1, 1, pnl.Width - 3, pnl.Height - 3);
                using (var path = GetRoundedRectPath(rect, pnl.Height / 2))
                {
                    using (var brush = new SolidBrush(fillColor)) e.Graphics.FillPath(brush, path);
                    using (var pen = new Pen(borderColor, 2f)) e.Graphics.DrawPath(pen, path);
                }
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }
        

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string name = txtNewCategory.Text.Trim();
            string type = cmbNewCategoryType.SelectedItem?.ToString() == "Gelir" ? "income" : "expense";
            if (_categoryService.AddCategory(_user.Id, name, type, out string errorMessage))
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "Kategori eklendi."; txtNewCategory.Clear(); LoadCategories();
            }
            else { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = errorMessage; }
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
                TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index]?.ToString() ?? string.Empty, cmb.Font, e.Bounds, TextLight, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };

            // 2. BEYAZ ÇERÇEVE ÇÖZÜMÜ: Sadece dıştaki 1 piksellik beyaz çizgiyi tıraşlıyoruz
            cmb.Region = new Region(new Rectangle(1, 1, cmb.Width - 2, cmb.Height - 2));

            // 3. OK ÇÖZÜMÜ: Varsayılan oku ve ayırıcı beyaz çizgiyi gizlemek için üstüne küçük bir örtü panel ekliyoruz
            Panel pnlArrow = new Panel();
            pnlArrow.Width = 28; // Varsayılan oku kapatacak kadar geniş
            pnlArrow.Height = cmb.Height - 2;
            pnlArrow.Left = cmb.Right - pnlArrow.Width - 1; // ComboBox'ın tam sağ köşesinin üstüne oturt
            pnlArrow.Top = cmb.Top + 1;
            pnlArrow.BackColor = CardBackColor; // Arka planla aynı renk (yamalı durmaması için)
            pnlArrow.Cursor = Cursors.Hand;

            // 4. Özel okumuzu bu yeni panelin üzerine çiziyoruz
            pnlArrow.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int ax = pnlArrow.Width / 2 - 5;
                int ay = pnlArrow.Height / 2 - 2;
                using (var brush = new SolidBrush(TextMuted))
                    e.Graphics.FillPolygon(brush, new Point[] { new Point(ax, ay), new Point(ax + 10, ay), new Point(ax + 5, ay + 6) });
            };

            // Tıklamaları ComboBox'a aktarıyoruz ki oka basıldığında menü açılsın
            pnlArrow.MouseClick += (s, e) => { cmb.DroppedDown = true; };
            pnl.MouseClick += (s, e) => { cmb.DroppedDown = true; };

            // Örtü panelini ana panele ekleyip Z ekseninde "En Öne" (BringToFront) getiriyoruz
            pnl.Controls.Add(pnlArrow);
            pnlArrow.BringToFront();
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } }; pnl.SizeChanged += (s, e) => pnl.Invalidate(); }
        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius) { var path = new System.Drawing.Drawing2D.GraphicsPath(); int d = Math.Max(radius * 2, 1); path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path; }
    }
}