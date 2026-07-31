using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class TransactionControl : UserControl
    {
        private readonly User _user;
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly CategoryService _categoryService = new CategoryService();
        private CheckBox chkHideAmounts = new CheckBox();
        private List<Transaction> _cachedTransactions = new List<Transaction>();
        private bool _amountsHidden = false;

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color DangerColor = Color.FromArgb(220, 90, 90);
        private static readonly Color IncomeColor = Color.FromArgb(60, 180, 110);
        private static readonly Color ExpenseColor = Color.FromArgb(230, 100, 100);

        private Panel pnlTop = new Panel();
        private Panel pnlGrid = new Panel();
        private Panel pnlBottom = new Panel();

        private ComboBox cmbType = new ComboBox();
        private ComboBox cmbCategory = new ComboBox();
        private TextBox txtAmount = new TextBox();
        private DateTimePicker dtpDate = new DateTimePicker();
        private TextBox txtDescription = new TextBox();
        private Button btnAdd = new Button();
        private Button btnDelete = new Button();
        private DataGridView dgvTransactions = new DataGridView();
        private Label lblStatus = new Label();

        public TransactionControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadCategorySuggestions();
            LoadTransactions();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 260;
            pnlTop.BackColor = AppBackColor;

            Label lblTitle = new Label
            {
                Text = "Gelir / Gider İşlemleri",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 15,
                AutoSize = true
            };

            Label lblType = new Label { Text = "Tip:", Left = 20, Top = 70, ForeColor = TextMuted, AutoSize = true };
            cmbType.Left = 20;
            cmbType.Top = 100;
            cmbType.Width = 140;
            cmbType.Font = new Font("Segoe UI", 9.5F);
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("Gelir");
            cmbType.Items.Add("Gider");
            cmbType.SelectedIndex = 1;
            cmbType.SelectedIndexChanged += (s, e) => LoadCategorySuggestions();

            Label lblCategory = new Label { Text = "Kategori:", Left = 180, Top = 70, ForeColor = TextMuted, AutoSize = true };
            cmbCategory.Left = 180;
            cmbCategory.Top = 100;
            cmbCategory.Width = 200;
            cmbCategory.Font = new Font("Segoe UI", 9.5F);
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDown;
            cmbCategory.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbCategory.AutoCompleteSource = AutoCompleteSource.ListItems;

            Label lblAmount = new Label { Text = "Tutar:", Left = 400, Top = 70, ForeColor = TextMuted, AutoSize = true };
            txtAmount.Left = 400;
            txtAmount.Top = 100;
            txtAmount.Width = 100;
            txtAmount.Font = new Font("Segoe UI", 9.5F);

            Label lblDate = new Label { Text = "Tarih:", Left = 520, Top = 70, ForeColor = TextMuted, AutoSize = true };
            dtpDate.Left = 520;
            dtpDate.Top = 100;
            dtpDate.Width = 160;
            dtpDate.Font = new Font("Segoe UI", 9.5F);
            dtpDate.Format = DateTimePickerFormat.Short;
            dtpDate.Value = DateTime.Today;

            Label lblDescription = new Label { Text = "Açıklama (opsiyonel):", Left = 20, Top = 150, ForeColor = TextMuted, AutoSize = true };
            txtDescription.Left = 20;
            txtDescription.Top = 180;
            txtDescription.Width = 500;
            txtDescription.Font = new Font("Segoe UI", 9.5F);

            btnAdd.Text = "İşlem Ekle";
            btnAdd.Left = 540;
            btnAdd.Top = 178;
            btnAdd.Width = 140;
            btnAdd.Height = 30;
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.BackColor = AccentColor;
            btnAdd.ForeColor = Color.White;
            btnAdd.Cursor = Cursors.Hand;
            btnAdd.Click += BtnAdd_Click;

            lblStatus.Left = 20;
            lblStatus.Top = 225;
            lblStatus.Width = 660;
            lblStatus.Height = 25;
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
            lblStatus.Font = new Font("Segoe UI", 9F);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblType);
            pnlTop.Controls.Add(cmbType);
            pnlTop.Controls.Add(lblCategory);
            pnlTop.Controls.Add(cmbCategory);
            pnlTop.Controls.Add(lblAmount);
            pnlTop.Controls.Add(txtAmount);
            pnlTop.Controls.Add(lblDate);
            pnlTop.Controls.Add(dtpDate);
            pnlTop.Controls.Add(lblDescription);
            pnlTop.Controls.Add(txtDescription);
            pnlTop.Controls.Add(btnAdd);
            pnlTop.Controls.Add(lblStatus);

            // --- Orta panel: tablo ---
            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.Padding = new Padding(20, 0, 20, 0);
            pnlGrid.BackColor = AppBackColor;

            dgvTransactions.Dock = DockStyle.Fill;
            dgvTransactions.ReadOnly = true;
            dgvTransactions.AllowUserToAddRows = false;
            dgvTransactions.AllowUserToDeleteRows = false;
            dgvTransactions.AllowUserToResizeColumns = false;
            dgvTransactions.AllowUserToResizeRows = false;
            dgvTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTransactions.MultiSelect = false;
            dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTransactions.BackgroundColor = Color.White;
            dgvTransactions.BorderStyle = BorderStyle.None;
            dgvTransactions.RowHeadersVisible = false;
            dgvTransactions.GridColor = Color.FromArgb(230, 230, 235);
            dgvTransactions.Font = new Font("Segoe UI", 9.5F);
            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 60, 70);
            dgvTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvTransactions.ColumnHeadersHeight = 36;
            dgvTransactions.EnableHeadersVisualStyles = false;
            dgvTransactions.RowTemplate.Height = 30;
            dgvTransactions.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 253);

            // Sert mor seçim rengini kaldırıp, hafif/nötr bir vurgu kullanıyoruz
            dgvTransactions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 239, 246);

            dgvTransactions.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewRow row in dgvTransactions.Rows)
                {
                    Color rowColor = row.Cells["Tip"].Value?.ToString() == "Gelir" ? IncomeColor : ExpenseColor;
                    row.DefaultCellStyle.ForeColor = rowColor;
                    row.DefaultCellStyle.SelectionForeColor = rowColor;
                }
            };

            // Gelir satırlarını yeşil, gider satırlarını kırmızı yazı rengiyle gösteriyoruz
            dgvTransactions.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewRow row in dgvTransactions.Rows)
                {
                    if (row.Cells["Tip"].Value?.ToString() == "Gelir")
                    {
                        row.DefaultCellStyle.ForeColor = IncomeColor;
                    }
                    else
                    {
                        row.DefaultCellStyle.ForeColor = ExpenseColor;
                    }
                }
            };

            pnlGrid.Controls.Add(dgvTransactions);

            // --- Alt panel ---
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 60;
            pnlBottom.BackColor = AppBackColor;

            btnDelete.Text = "Seçili İşlemi Sil";
            btnDelete.Left = 20;
            btnDelete.Top = 12;
            btnDelete.Width = 160;
            btnDelete.Height = 35;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 1;
            btnDelete.FlatAppearance.BorderColor = DangerColor;
            btnDelete.BackColor = AppBackColor;
            btnDelete.ForeColor = DangerColor;
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.Click += BtnDelete_Click;
            chkHideAmounts.Text = "Tutarları Gizle";
            chkHideAmounts.Left = 200;
            chkHideAmounts.Top = 20;
            chkHideAmounts.AutoSize = true;
            chkHideAmounts.ForeColor = TextMuted;
            chkHideAmounts.CheckedChanged += (s, e) =>
            {
                _amountsHidden = chkHideAmounts.Checked;
                RefreshGrid();
            };

            pnlBottom.Controls.Add(btnDelete);
            pnlBottom.Controls.Add(chkHideAmounts);

            pnlBottom.Controls.Add(btnDelete);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private string GetSelectedType()
        {
            return cmbType.SelectedItem?.ToString() == "Gelir" ? "income" : "expense";
        }

        private void LoadCategorySuggestions()
        {
            string type = GetSelectedType();
            var categories = _categoryService.GetUserCategoriesByType(_user.Id, type);

            cmbCategory.Items.Clear();
            foreach (var cat in categories)
            {
                cmbCategory.Items.Add(cat.Name);
            }
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

        // Veritabanına tekrar gitmeden, sadece görünümü (gizli/açık) günceller
        private void RefreshGrid()
        {
            var displayList = _cachedTransactions.Select(t => new
            {
                ID = t.Id,
                Tarih = t.TransactionDate.ToString("dd.MM.yyyy"),
                Tip = t.Type == "income" ? "Gelir" : "Gider",
                Kategori = t.CategoryName,
                Tutar = _amountsHidden ? "••••••" : t.Amount.ToString("#,##0", new System.Globalization.CultureInfo("tr-TR")) + " ₺",
                Açıklama = t.Description
            }).ToList();

            dgvTransactions.DataSource = displayList;
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string categoryName = cmbCategory.Text.Trim();

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                lblStatus.Text = "Lütfen bir kategori adı girin.";
                return;
            }

            if (!decimal.TryParse(txtAmount.Text, out decimal amount))
            {
                lblStatus.Text = "Geçersiz tutar.";
                return;
            }

            string type = GetSelectedType();
            string description = txtDescription.Text;
            DateTime date = dtpDate.Value;

            var category = _categoryService.GetOrCreateCategory(_user.Id, categoryName, type);

            bool success = _transactionService.AddTransaction(
                _user.Id, category.Id, amount, type, description, date, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "İşlem başarıyla eklendi.";
                txtAmount.Clear();
                txtDescription.Clear();
                LoadCategorySuggestions();
                LoadTransactions();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvTransactions.CurrentRow == null)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen silmek için bir işlem seçin.";
                return;
            }

            int transactionId = Convert.ToInt32(dgvTransactions.CurrentRow.Cells["ID"].Value);

            var confirm = MessageBox.Show("Bu işlemi silmek istediğinize emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                bool success = _transactionService.DeleteTransaction(transactionId, _user.Id, out string errorMessage);

                if (success)
                {
                    lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                    lblStatus.Text = "İşlem silindi.";
                    LoadTransactions();
                }
                else
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = errorMessage;
                }
            }
        }
    }
}