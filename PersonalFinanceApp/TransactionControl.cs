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
        private TextBox txtDescription = new TextBox();
        private Button btnAdd = new Button();
        private Button btnDelete = new Button();
        private Button btnExport = new Button();
        private Button btnRecurring = new Button();
        private Label lblStatus = new Label();
        private Label lblWalletBalance = new Label();

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
            LoadCategorySuggestions();
            LoadTransactions();
            RefreshWalletBalance();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 230;
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

            lblWalletBalance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblWalletBalance.ForeColor = Color.FromArgb(120, 220, 150);
            lblWalletBalance.Left = 500;
            lblWalletBalance.Top = 22;
            lblWalletBalance.AutoSize = true;

            Label lblType = new Label { Text = "Tip:", Left = 20, Top = 75, ForeColor = TextMuted, AutoSize = true };
            cmbType.Left = 20;
            cmbType.Top = 105;
            cmbType.Width = 140;
            cmbType.Font = new Font("Segoe UI", 9.5F);
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("Gelir");
            cmbType.Items.Add("Gider");
            cmbType.SelectedIndex = 1;
            cmbType.SelectedIndexChanged += (s, e) => LoadCategorySuggestions();

            Label lblCategory = new Label { Text = "Kategori:", Left = 180, Top = 75, ForeColor = TextMuted, AutoSize = true };
            cmbCategory.Left = 180;
            cmbCategory.Top = 105;
            cmbCategory.Width = 200;
            cmbCategory.Font = new Font("Segoe UI", 9.5F);
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDown;
            cmbCategory.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbCategory.AutoCompleteSource = AutoCompleteSource.ListItems;

            Label lblAmount = new Label { Text = "Tutar:", Left = 400, Top = 75, ForeColor = TextMuted, AutoSize = true };
            txtAmount.Left = 400;
            txtAmount.Top = 105;
            txtAmount.Width = 120;
            txtAmount.Font = new Font("Segoe UI", 9.5F);

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
            lblStatus.Top = 205;
            lblStatus.Width = 660;
            lblStatus.Height = 25;
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
            lblStatus.Font = new Font("Segoe UI", 9F);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblWalletBalance);
            pnlTop.Controls.Add(lblType);
            pnlTop.Controls.Add(cmbType);
            pnlTop.Controls.Add(lblCategory);
            pnlTop.Controls.Add(cmbCategory);
            pnlTop.Controls.Add(lblAmount);
            pnlTop.Controls.Add(txtAmount);
            pnlTop.Controls.Add(lblDescription);
            pnlTop.Controls.Add(txtDescription);
            pnlTop.Controls.Add(btnAdd);
            pnlTop.Controls.Add(lblStatus);

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
            dgvTransactions.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
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

            pnlGrid.Controls.Add(dgvTransactions);

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

            btnExport.Text = "CSV'ye Aktar";
            btnExport.Left = 400;
            btnExport.Top = 12;
            btnExport.Width = 180;
            btnExport.Height = 35;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 1;
            btnExport.FlatAppearance.BorderColor = TextMuted;
            btnExport.BackColor = AppBackColor;
            btnExport.ForeColor = TextLight;
            btnExport.Cursor = Cursors.Hand;
            btnExport.Click += BtnExport_Click;

            btnRecurring.Text = "Tekrarlanan İşlemler";
            btnRecurring.Left = 600;
            btnRecurring.Top = 12;
            btnRecurring.Width = 190;
            btnRecurring.Height = 35;
            btnRecurring.FlatStyle = FlatStyle.Flat;
            btnRecurring.FlatAppearance.BorderSize = 1;
            btnRecurring.FlatAppearance.BorderColor = TextMuted;
            btnRecurring.BackColor = AppBackColor;
            btnRecurring.ForeColor = TextLight;
            btnRecurring.Cursor = Cursors.Hand;
            btnRecurring.Click += (s, e) =>
            {
                using (var dialog = new RecurringTransactionDialog(_user))
                {
                    dialog.ShowDialog();
                }
            };

            

            pnlBottom.Controls.Add(btnDelete);
            pnlBottom.Controls.Add(btnExport);
            pnlBottom.Controls.Add(btnRecurring);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private void RefreshWalletBalance()
        {
            var (wallet, _) = _accountService.GetBalances(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");
            lblWalletBalance.Text = $"Cüzdan: {wallet.ToString("#,##0", tr)} ₺";
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

        private void RefreshGrid()
        {
            var tr = new System.Globalization.CultureInfo("tr-TR");

            var displayList = _cachedTransactions.Select(t => new
            {
                ID = t.Id,
                Tip = t.Type == "income" ? "Gelir" : "Gider",
                Kategori = t.CategoryName,
                Tutar = _user.HideAmountsEnabled
                    ? "••••••"
                    : t.Amount.ToString("#,##0", new System.Globalization.CultureInfo("tr-TR")) + " ₺",
                Açıklama = t.Description
            }).ToList();

            dgvTransactions.DataSource = displayList;
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string categoryName = cmbCategory.Text.Trim();

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen bir kategori adı girin.";
                return;
            }

            if (!decimal.TryParse(txtAmount.Text, out decimal amount))
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Geçersiz tutar.";
                return;
            }

            string type = GetSelectedType();
            string description = txtDescription.Text;

            var category = _categoryService.GetOrCreateCategory(_user.Id, categoryName, type);

            bool success = _transactionService.AddTransaction(
                _user.Id, category.Id, amount, type, description, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "İşlem başarıyla eklendi.";
                txtAmount.Clear();
                txtDescription.Clear();
                LoadCategorySuggestions();
                LoadTransactions();
                RefreshWalletBalance();
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

            var idCell = dgvTransactions.CurrentRow.Cells["ID"];
            if (idCell?.Value == null)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Geçersiz işlem seçimi.";
                return;
            }
            if (!int.TryParse(idCell.Value.ToString(), out int transactionId))
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Geçersiz işlem ID.";
                return;
            }

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
                    RefreshWalletBalance();
                }
                else
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = errorMessage;
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

                        using (var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8))
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

                        lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                        lblStatus.Text = "İşlemler CSV olarak dışa aktarıldı.";
                    }
                    catch (Exception ex)
                    {
                        lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                        lblStatus.Text = $"Dışa aktarma başarısız: {ex.Message}";
                    }
                }
            }
        }
    }
}