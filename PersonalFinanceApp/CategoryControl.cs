using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class CategoryControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly TransactionService _transactionService = new TransactionService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color DangerColor => AppTheme.DangerColor;

        private Panel pnlTop = new Panel();
        private Panel pnlGrid = new Panel();
        private Panel pnlBottom = new Panel();
        private Button btnExport = new Button();

        private ComboBox cmbFilterType = new ComboBox();
        private TextBox txtNewCategory = new TextBox();
        private ComboBox cmbNewCategoryType = new ComboBox();
        private Button btnAdd = new Button();
        private TextBox txtSearch = new TextBox();
        private Button btnSearch = new Button();
        private Label lblStatus = new Label();
        private Label lblSearch = new Label();
        private Panel pnlSearch = new Panel();

        private DataGridView dgvCategories = new DataGridView();
        private List<Category> _cachedCategories = new List<Category>();

        public CategoryControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadCategories();
        }

        public void RefreshData()
        {
            LoadCategories();
            pnlTop.Invalidate(true);
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

            Label lblTitle = new Label { Text = "Kategoriler", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = TextLight, BackColor = AppBackColor, Left = 20, Top = 15, AutoSize = true };

            Label lblFilter = new Label { Text = "Göster:", Left = 20, Top = 70, ForeColor = TextMuted, AutoSize = true };
            Panel pnlFilter = new Panel { Left = 20, Top = 95, Width = 140, Height = 36 };
            cmbFilterType.Left = 5; cmbFilterType.Top = 7; cmbFilterType.Width = 135;
            cmbFilterType.Font = new Font("Segoe UI", 9.5F); cmbFilterType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterType.Items.Add("Tümü"); cmbFilterType.Items.Add("Gelir"); cmbFilterType.Items.Add("Gider"); cmbFilterType.Items.Add("Hedef"); cmbFilterType.Items.Add("Yatırım"); cmbFilterType.SelectedIndex = 0;
            cmbFilterType.SelectedIndexChanged += (s, e) => LoadCategories();
            pnlFilter.Controls.Add(cmbFilterType);
            SetupCustomComboBox(pnlFilter, cmbFilterType); // Beyazlık ve mavi renk düzeltildi

            Label lblNew = new Label { Text = "Yeni Kategori Adı:", Left = 180, Top = 70, ForeColor = TextMuted, BackColor = AppBackColor, AutoSize = true };
            Panel pnlNewCat = new Panel { Left = 180, Top = 95, Width = 220, Height = 36 };
            SetupSmoothContainer(pnlNewCat, 8, CardBackColor);
            txtNewCategory.Name = "NewCategoryName";
            txtNewCategory.Left = 10; txtNewCategory.Top = 8; txtNewCategory.Width = 200;
            txtNewCategory.Font = new Font("Segoe UI", 10.5F); txtNewCategory.BorderStyle = BorderStyle.None;
            txtNewCategory.BackColor = CardBackColor; txtNewCategory.ForeColor = TextLight;
            pnlNewCat.Controls.Add(txtNewCategory);

            Label lblNewType = new Label { Text = "Tip:", Left = 420, Top = 70, ForeColor = TextMuted, AutoSize = true };
            Panel pnlNewType = new Panel { Left = 420, Top = 95, Width = 120, Height = 36 };
            cmbNewCategoryType.Name = "NewCategoryType";
            cmbNewCategoryType.Left = 5; cmbNewCategoryType.Top = 7; cmbNewCategoryType.Width = 115;
            cmbNewCategoryType.Font = new Font("Segoe UI", 9.5F); cmbNewCategoryType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbNewCategoryType.Items.Add("Gelir"); cmbNewCategoryType.Items.Add("Gider"); cmbNewCategoryType.Items.Add("Hedef"); cmbNewCategoryType.SelectedIndex = 1;
            pnlNewType.Controls.Add(cmbNewCategoryType);
            SetupCustomComboBox(pnlNewType, cmbNewCategoryType); // Beyazlık ve mavi renk düzeltildi

            btnAdd.Text = "➕ Ekle";
            btnAdd.Left = 560; btnAdd.Top = 95; btnAdd.Width = 100; btnAdd.Height = 36; btnAdd.Cursor = Cursors.Hand;
            SetupRoundedButton(btnAdd, AccentColor, Color.White);
            btnAdd.Click += BtnAdd_Click;

            // Arama kutusu, tablonun sağ kenarına hizalı (bkz. PositionSearchArea, pencere
            // yeniden boyutlanınca da tablonun sonuna hizalı kalması için)
            lblSearch.Text = "Ara:"; lblSearch.Top = 70; lblSearch.ForeColor = TextMuted; lblSearch.AutoSize = true;
            pnlSearch.Top = 95; pnlSearch.Width = 160; pnlSearch.Height = 36;
            SetupSmoothContainer(pnlSearch, 8, CardBackColor);
            txtSearch.Left = 10; txtSearch.Top = 8; txtSearch.Width = 140;
            txtSearch.Font = new Font("Segoe UI", 10.5F); txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = CardBackColor; txtSearch.ForeColor = TextLight;
            txtSearch.TextChanged += (s, e) => LoadCategories();
            pnlSearch.Controls.Add(txtSearch);

            btnSearch.Text = "🔍";
            btnSearch.Top = 95; btnSearch.Width = 40; btnSearch.Height = 36; btnSearch.Cursor = Cursors.Hand;
            SetupRoundedButton(btnSearch, AccentColor, Color.White);
            btnSearch.Click += (s, e) => LoadCategories();

            lblStatus.Left = 20;
            lblStatus.Top = 145;
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
            lblStatus.Font = new Font("Segoe UI", 9F);

            pnlTop.Controls.Add(lblTitle); pnlTop.Controls.Add(lblFilter); pnlTop.Controls.Add(pnlFilter);
            pnlTop.Controls.Add(lblNew); pnlTop.Controls.Add(pnlNewCat); pnlTop.Controls.Add(lblNewType); pnlTop.Controls.Add(pnlNewType);
            pnlTop.Controls.Add(btnAdd); pnlTop.Controls.Add(lblSearch); pnlTop.Controls.Add(pnlSearch); pnlTop.Controls.Add(btnSearch);
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
            this.Resize += (s, e) => PositionSearchArea();

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
            dgvCategories.GridColor = AppTheme.GridLineColor; dgvCategories.BackgroundColor = CardBackColor;

            // Renkler sadeleştirildi (Sadece düz beyaz)
            dgvCategories.DefaultCellStyle.BackColor = CardBackColor; dgvCategories.DefaultCellStyle.ForeColor = TextLight;
            dgvCategories.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvCategories.DefaultCellStyle.SelectionBackColor = AccentColor;
            dgvCategories.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvCategories.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.HeaderBackColor; dgvCategories.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvCategories.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.HeaderBackColor; dgvCategories.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvCategories.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); dgvCategories.EnableHeadersVisualStyles = false; dgvCategories.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None; dgvCategories.ColumnHeadersHeight = 40;

            dgvCategories.CellPainting += DgvCategories_CellPainting;

            dgvCategories.HandleCreated += (s, e) => DarkTitleBarHelper.SetDataGridViewScrollBarDarkMode(dgvCategories, AppTheme.IsDark);

            // Kendi çizdiğimiz (owner-paint, bkz. DgvCategories_CellPainting) hücreler çift arabelleğe
            // alınmadan çiziliyordu — bu da yeniden çizim sırasında önceki karenin kalıntılarının
            // (ör. satır ayraç çizgisinin) bir sonraki karede beyazımsı bir iz olarak kalmasına yol
            // açıyordu (aralıklı görünüp kaybolan çizgi beyazlığı). AssetControl/NoteControl/
            // SavingsGoalControl'daki aynı düzeltme burada eksikti.
            EnableDoubleBuffering(dgvCategories);

            pnlGridWrapper.Controls.Add(dgvCategories);
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
            SetupRoundedButton(btnExport, Color.FromArgb(80, 85, 105), Color.White);
            btnExport.Click += BtnExport_Click;

            pnlBottom.Controls.Add(btnExport);

            this.Controls.Add(pnlGrid); this.Controls.Add(pnlBottom); this.Controls.Add(pnlTop);
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV Dosyası (*.csv)|*.csv";
                dialog.FileName = $"kategoriler_{DateTime.Today:yyyy_MM_dd}.csv";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var totals = _transactionService.GetCategoryTotals(_user.Id);
                        var tr = new System.Globalization.CultureInfo("tr-TR");
                        using (var writer = new System.IO.StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8))
                        {
                            writer.WriteLine("Ad;Tip;Toplam Tutar");
                            foreach (var c in _cachedCategories)
                            {
                                decimal total = totals.TryGetValue(c.Id, out decimal t) ? t : 0;
                                writer.WriteLine($"{c.Name};{TypeToTr(c.Type)};{total.ToString("0.00", tr)}");
                            }
                        }
                        lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "Kategoriler CSV olarak dışa aktarıldı.";
                    }
                    catch (Exception ex) { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = $"Dışa aktarma başarısız: {ex.Message}"; }
                }
            }
        }

        private void DgvCategories_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex <= 0 || e.ColumnIndex >= dgvCategories.ColumnCount) return;

            bool isAdColumn = dgvCategories.Columns[e.ColumnIndex].Name == "Ad";
            if (isAdColumn)
            {
                string renkHex = dgvCategories.Rows[e.RowIndex].Cells["RenkHex"]?.Value?.ToString() ?? "";

                e.PaintBackground(e.CellBounds, true);
                e.Graphics!.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int textLeft = e.CellBounds.Left + 8;

                if (!string.IsNullOrEmpty(renkHex))
                {
                    Color dotColor = AvatarHelper.ParseColor(renkHex, TextMuted);
                    int dotSize = 10;
                    int dotTop = e.CellBounds.Top + (e.CellBounds.Height - dotSize) / 2;
                    using (var brush = new SolidBrush(dotColor))
                        e.Graphics.FillEllipse(brush, textLeft, dotTop, dotSize, dotSize);
                    textLeft += dotSize + 6;
                }

                var textRect = new Rectangle(textLeft, e.CellBounds.Top, e.CellBounds.Right - textLeft, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, e.Value?.ToString() ?? "", e.CellStyle!.Font, textRect, e.CellStyle.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
            else
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);
            }

            using (Pen p = new Pen(AppTheme.RowSeparatorColor, 1)) { e.Graphics!.DrawLine(p, e.CellBounds.Left, e.CellBounds.Top + 10, e.CellBounds.Left, e.CellBounds.Bottom - 10); }
            e.Handled = true;
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
            else _cachedCategories = _categoryService.GetUserCategoriesByType(_user.Id, TrToType(filter));

            var totals = _transactionService.GetCategoryTotals(_user.Id);
            var monthlyExpense = _transactionService.GetMonthlyExpenseByCategoryId(_user.Id, DateTime.Today.Year, DateTime.Today.Month);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            string searchText = txtSearch.Text.Trim();
            var visibleCategories = string.IsNullOrEmpty(searchText)
                ? _cachedCategories
                : _cachedCategories.Where(c => tr.CompareInfo.IndexOf(c.Name, searchText, System.Globalization.CompareOptions.IgnoreCase) >= 0).ToList();

            var displayList = visibleCategories.Select(c => new
            {
                ID = c.Id,
                Ad = c.Name,
                Tip = TypeToTr(c.Type),
                ToplamTutar = _user.HideAmountsEnabled
                    ? "••••••"
                    : (totals.TryGetValue(c.Id, out decimal total) ? total : 0).ToString("#,##0", tr) + " ₺",
                Butce = c.BudgetLimit == null
                    ? ""
                    : _user.HideAmountsEnabled
                        ? "••• / •••"
                        : $"{(monthlyExpense.TryGetValue(c.Id, out decimal spent) ? spent : 0).ToString("#,##0", tr)} / {c.BudgetLimit.Value.ToString("#,##0", tr)} ₺",
                RenkHex = c.Color ?? ""
            }).ToList();

            dgvCategories.DataSource = displayList;

            if (dgvCategories.Columns["ID"] != null)
            {
                dgvCategories.Columns["ID"]!.FillWeight = 30; dgvCategories.Columns["Ad"]!.FillWeight = 80;
                dgvCategories.Columns["Tip"]!.FillWeight = 50; dgvCategories.Columns["ToplamTutar"]!.FillWeight = 80;
                dgvCategories.Columns["ToplamTutar"]!.HeaderText = "Toplam Tutar";
                dgvCategories.Columns["Butce"]!.FillWeight = 70;
                dgvCategories.Columns["Butce"]!.HeaderText = "Bu Ayki Bütçe";
                dgvCategories.Columns["RenkHex"]!.Visible = false;
            }
        }

        // Şeffaf kapsül çizimini yapan yardımcı metot (Bu sınıfta olmadığı için eklememiz gerekiyor)
        private static string TypeToTr(string type) => type switch { "income" => "Gelir", "goal" => "Hedef", "invest" => "Yatırım", _ => "Gider" };
        private static string TrToType(string tr) => tr switch { "Gelir" => "income", "Hedef" => "goal", "Yatırım" => "invest", _ => "expense" };

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string name = txtNewCategory.Text.Trim();
            string type = TrToType(cmbNewCategoryType.SelectedItem?.ToString() ?? "Gider");
            if (_categoryService.AddCategory(_user.Id, name, type, out string errorMessage))
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "Kategori eklendi."; txtNewCategory.Clear(); LoadCategories();
            }
            else { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = errorMessage; }
        }

        
        

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
                Color bgColor = isSelected ? AppTheme.HoverBackColor : CardBackColor;
                e.Graphics.FillRectangle(new SolidBrush(bgColor), e.Bounds);
                TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index]?.ToString() ?? string.Empty, cmb.Font, e.Bounds, TextLight, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
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