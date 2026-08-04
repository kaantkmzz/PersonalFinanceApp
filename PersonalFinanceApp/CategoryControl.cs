using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class CategoryControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly CategoryService _categoryService = new CategoryService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
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

        private TextBox txtRename = new TextBox();
        private Button btnRename = new Button();
        private Button btnDelete = new Button();

        private DataGridView dgvCategories = new DataGridView();
        private List<Category> _cachedCategories = new List<Category>();

        private readonly TransactionService _transactionService = new TransactionService();
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
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 190;
            pnlTop.BackColor = AppBackColor;

            Label lblTitle = new Label
            {
                Text = "Kategoriler",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 15,
                AutoSize = true
            };

            Label lblFilter = new Label { Text = "Göster:", Left = 20, Top = 70, ForeColor = TextMuted, AutoSize = true };
            cmbFilterType.Left = 20;
            cmbFilterType.Top = 100;
            cmbFilterType.Width = 140;
            cmbFilterType.Font = new Font("Segoe UI", 9.5F);
            cmbFilterType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterType.Items.Add("Tümü");
            cmbFilterType.Items.Add("Gelir");
            cmbFilterType.Items.Add("Gider");
            cmbFilterType.SelectedIndex = 0;
            cmbFilterType.SelectedIndexChanged += (s, e) => LoadCategories();

            Label lblNew = new Label { Text = "Yeni Kategori Adı:", Left = 200, Top = 70, ForeColor = TextMuted, AutoSize = true };
            txtNewCategory.Left = 200;
            txtNewCategory.Top = 100;
            txtNewCategory.Width = 220;
            txtNewCategory.Font = new Font("Segoe UI", 9.5F);

            Label lblNewType = new Label { Text = "Tip:", Left = 440, Top = 70, ForeColor = TextMuted, AutoSize = true };
            cmbNewCategoryType.Left = 440;
            cmbNewCategoryType.Top = 100;
            cmbNewCategoryType.Width = 120;
            cmbNewCategoryType.Font = new Font("Segoe UI", 9.5F);
            cmbNewCategoryType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbNewCategoryType.Items.Add("Gelir");
            cmbNewCategoryType.Items.Add("Gider");
            cmbNewCategoryType.SelectedIndex = 1;

            btnAdd.Text = "Ekle";
            btnAdd.Left = 580;
            btnAdd.Top = 98;
            btnAdd.Width = 100;
            btnAdd.Height = 30;
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.BackColor = AccentColor;
            btnAdd.ForeColor = Color.White;
            btnAdd.Cursor = Cursors.Hand;
            btnAdd.Click += BtnAdd_Click;

            lblStatus.Left = 20;
            lblStatus.Top = 140;
            lblStatus.Width = 660;
            lblStatus.Height = 25;
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
            lblStatus.Font = new Font("Segoe UI", 9F);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblFilter);
            pnlTop.Controls.Add(cmbFilterType);
            pnlTop.Controls.Add(lblNew);
            pnlTop.Controls.Add(txtNewCategory);
            pnlTop.Controls.Add(lblNewType);
            pnlTop.Controls.Add(cmbNewCategoryType);
            pnlTop.Controls.Add(btnAdd);
            pnlTop.Controls.Add(lblStatus);

            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.Padding = new Padding(20, 0, 20, 0);
            pnlGrid.BackColor = AppBackColor;

            dgvCategories.Dock = DockStyle.Fill;
            dgvCategories.ReadOnly = true;
            dgvCategories.AllowUserToAddRows = false;
            dgvCategories.AllowUserToDeleteRows = false;
            dgvCategories.AllowUserToResizeColumns = false;
            dgvCategories.AllowUserToResizeRows = false;
            dgvCategories.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCategories.MultiSelect = false;
            dgvCategories.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCategories.BackgroundColor = Color.White;
            dgvCategories.BorderStyle = BorderStyle.None;
            dgvCategories.RowHeadersVisible = false;
            dgvCategories.GridColor = Color.FromArgb(230, 230, 235);
            dgvCategories.Font = new Font("Segoe UI", 9.5F);
            dgvCategories.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            dgvCategories.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 60, 70);
            dgvCategories.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvCategories.ColumnHeadersHeight = 36;
            dgvCategories.EnableHeadersVisualStyles = false;
            dgvCategories.RowTemplate.Height = 30;
            dgvCategories.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dgvCategories.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 239, 246);
            dgvCategories.DefaultCellStyle.SelectionForeColor = Color.FromArgb(40, 40, 40);
            dgvCategories.SelectionChanged += (s, e) => LoadSelectedIntoRenameBox();

            pnlGrid.Controls.Add(dgvCategories);

            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 65;
            pnlBottom.BackColor = AppBackColor;

            txtRename.Left = 20;
            txtRename.Top = 16;
            txtRename.Width = 220;
            txtRename.Font = new Font("Segoe UI", 9.5F);
            txtRename.PlaceholderText = "Yeni ad girin...";

            btnRename.Text = "Yeniden Adlandır";
            btnRename.Left = 250;
            btnRename.Top = 14;
            btnRename.Width = 180;
            btnRename.Height = 40;
            btnRename.FlatStyle = FlatStyle.Flat;
            btnRename.FlatAppearance.BorderSize = 1;
            btnRename.FlatAppearance.BorderColor = TextMuted;
            btnRename.BackColor = AppBackColor;
            btnRename.ForeColor = TextLight;
            btnRename.Cursor = Cursors.Hand;
            btnRename.Click += BtnRename_Click;

            btnDelete.Text = "Seçili Kategoriyi Sil";
            btnDelete.Left = 450;
            btnDelete.Top = 14;
            btnDelete.Width = 190;
            btnDelete.Height = 40;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 1;
            btnDelete.FlatAppearance.BorderColor = DangerColor;
            btnDelete.BackColor = AppBackColor;
            btnDelete.ForeColor = DangerColor;
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.Click += BtnDelete_Click;

            pnlBottom.Controls.Add(txtRename);
            pnlBottom.Controls.Add(btnRename);
            pnlBottom.Controls.Add(btnDelete);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private void LoadCategories()
        {
            string filter = cmbFilterType.SelectedItem?.ToString() ?? "Tümü";

            if (filter == "Tümü")
            {
                _cachedCategories = _categoryService.GetUserCategories(_user.Id);
            }
            else
            {
                string type = filter == "Gelir" ? "income" : "expense";
                _cachedCategories = _categoryService.GetUserCategoriesByType(_user.Id, type);
            }

            var totals = _transactionService.GetCategoryTotals(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            var displayList = _cachedCategories.Select(c => new
            {
                ID = c.Id,
                Ad = c.Name,
                Tip = c.Type == "income" ? "Gelir" : "Gider",
                ToplamTutar = _user.HideAmountsEnabled
                    ? "••••••"
                    : (totals.TryGetValue(c.Id, out decimal total) ? total : 0).ToString("#,##0", tr) + " ₺"
            }).ToList();

            dgvCategories.DataSource = displayList;

            // ID sütununu daraltıp diğerlerine (özellikle Toplam Tutar'a) daha fazla yer açıyoruz
            if (dgvCategories.Columns["ID"] != null)
            {
                dgvCategories.Columns["ID"].FillWeight = 30;
                dgvCategories.Columns["Ad"].FillWeight = 90;
                dgvCategories.Columns["Tip"].FillWeight = 60;
                dgvCategories.Columns["ToplamTutar"].FillWeight = 90;
                dgvCategories.Columns["ToplamTutar"].HeaderText = "Toplam Tutar";
            }
        }

        private void LoadSelectedIntoRenameBox()
        {
            if (dgvCategories.CurrentRow != null)
            {
                txtRename.Text = dgvCategories.CurrentRow.Cells["Ad"].Value?.ToString() ?? string.Empty;
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string name = txtNewCategory.Text.Trim();
            string type = cmbNewCategoryType.SelectedItem?.ToString() == "Gelir" ? "income" : "expense";

            bool success = _categoryService.AddCategory(_user.Id, name, type, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Kategori eklendi.";
                txtNewCategory.Clear();
                LoadCategories();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnRename_Click(object? sender, EventArgs e)
        {
            if (dgvCategories.CurrentRow == null)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen yeniden adlandırmak için bir kategori seçin.";
                return;
            }

            string newName = txtRename.Text.Trim();
            var idCell = dgvCategories.CurrentRow.Cells["ID"];
            if (idCell?.Value == null)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Geçersiz kategori seçimi.";
                return;
            }
            if (!int.TryParse(idCell.Value.ToString(), out int categoryId))
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Geçersiz kategori ID.";
                return;
            }

            bool success = _categoryService.UpdateCategoryName(categoryId, _user.Id, newName, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Kategori adı güncellendi.";
                LoadCategories();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvCategories.CurrentRow == null)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen silmek için bir kategori seçin.";
                return;
            }

            var confirm = MessageBox.Show(
                "Bu kategoriyi silmek istediğinize emin misiniz?",
                "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                var idCellDel = dgvCategories.CurrentRow.Cells["ID"];
                if (idCellDel?.Value == null)
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = "Geçersiz kategori seçimi.";
                    return;
                }
                if (!int.TryParse(idCellDel.Value.ToString(), out int categoryId))
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = "Geçersiz kategori ID.";
                    return;
                }

                try
                {
                    _categoryService.DeleteCategory(categoryId, _user.Id);
                    lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                    lblStatus.Text = "Kategori silindi.";
                    txtRename.Clear();
                    LoadCategories();
                }
                catch (Exception)
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = "Bu kategoriye bağlı işlemler olduğu için silinemedi. Önce o işlemleri silin.";
                }
            }
        }
    }
}