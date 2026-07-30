using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class TransactionControl : UserControl
    {
        private readonly User _user;
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly CategoryService _categoryService = new CategoryService();

        private static readonly Color ContentBackColor = Color.FromArgb(230, 232, 242);

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
            this.Dock = DockStyle.Fill;
            this.BackColor = ContentBackColor;
            this.Font = new Font("Segoe UI", 9F);

            // --- Üst panel: başlık + form alanları (sabit yükseklik) ---
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 220;
            pnlTop.BackColor = ContentBackColor;

            Label lblTitle = new Label
            {
                Text = "Gelir / Gider İşlemleri",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Left = 40,
                Top = 20,
                AutoSize = true
            };

            Label lblType = new Label { Text = "Tip:", Left = 40, Top = 80, AutoSize = true };
            cmbType.Left = 40;
            cmbType.Top = 100;
            cmbType.Width = 140;
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("Gelir");
            cmbType.Items.Add("Gider");
            cmbType.SelectedIndex = 1;
            cmbType.SelectedIndexChanged += (s, e) => LoadCategorySuggestions();

            Label lblCategory = new Label { Text = "Kategori:", Left = 200, Top = 80, AutoSize = true };
            cmbCategory.Left = 200;
            cmbCategory.Top = 100;
            cmbCategory.Width = 200;
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDown;
            cmbCategory.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbCategory.AutoCompleteSource = AutoCompleteSource.ListItems;

            Label lblAmount = new Label { Text = "Tutar:", Left = 420, Top = 80, AutoSize = true };
            txtAmount.Left = 420;
            txtAmount.Top = 100;
            txtAmount.Width = 100;

            Label lblDate = new Label { Text = "Tarih:", Left = 540, Top = 80, AutoSize = true };
            dtpDate.Left = 540;
            dtpDate.Top = 100;
            dtpDate.Width = 160;
            dtpDate.Format = DateTimePickerFormat.Short;
            dtpDate.Value = DateTime.Today;

            Label lblDescription = new Label { Text = "Açıklama (opsiyonel):", Left = 40, Top = 135, AutoSize = true };
            txtDescription.Left = 40;
            txtDescription.Top = 155;
            txtDescription.Width = 500;

            btnAdd.Text = "İşlem Ekle";
            btnAdd.Left = 560;
            btnAdd.Top = 153;
            btnAdd.Width = 140;
            btnAdd.Height = 30;
            btnAdd.Click += BtnAdd_Click;

            lblStatus.Left = 40;
            lblStatus.Top = 190;
            lblStatus.Width = 660;
            lblStatus.Height = 25;
            lblStatus.ForeColor = Color.Red;

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

            // --- Orta panel: tablo, kalan alanı otomatik doldurur ---
            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.Padding = new Padding(40, 0, 40, 0);
            pnlGrid.BackColor = ContentBackColor;

            dgvTransactions.Dock = DockStyle.Fill;
            dgvTransactions.ReadOnly = true;
            dgvTransactions.AllowUserToAddRows = false;
            dgvTransactions.AllowUserToDeleteRows = false;
            dgvTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTransactions.MultiSelect = false;
            dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTransactions.BackgroundColor = Color.White;

            pnlGrid.Controls.Add(dgvTransactions);

            // --- Alt panel: silme butonu, hep en altta sabit ---
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 60;
            pnlBottom.BackColor = ContentBackColor;

            btnDelete.Text = "Seçili İşlemi Sil";
            btnDelete.Left = 40;
            btnDelete.Top = 12;
            btnDelete.Width = 160;
            btnDelete.Height = 35;
            btnDelete.Click += BtnDelete_Click;

            pnlBottom.Controls.Add(btnDelete);

            // Sıra önemli: Fill önce, sonra Bottom, en son Top (WinForms docking mantığı için)
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
            var transactions = _transactionService.GetUserTransactions(_user.Id);

            var displayList = transactions.Select(t => new
            {
                ID = t.Id,
                Tarih = t.TransactionDate.ToString("dd.MM.yyyy"),
                Tip = t.Type == "income" ? "Gelir" : "Gider",
                Kategori = t.CategoryName,
                Tutar = t.Amount.ToString("0.00") + " ₺",
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
                lblStatus.ForeColor = Color.Green;
                lblStatus.Text = "İşlem başarıyla eklendi.";
                txtAmount.Clear();
                txtDescription.Clear();
                LoadCategorySuggestions();
                LoadTransactions();
            }
            else
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvTransactions.CurrentRow == null)
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = "Lütfen silmek için bir işlem seçin.";
                return;
            }

            int transactionId = (int)dgvTransactions.CurrentRow.Cells["ID"].Value;

            var confirm = MessageBox.Show("Bu işlemi silmek istediğinize emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                bool success = _transactionService.DeleteTransaction(transactionId, _user.Id, out string errorMessage);

                if (success)
                {
                    lblStatus.ForeColor = Color.Green;
                    lblStatus.Text = "İşlem silindi.";
                    LoadTransactions();
                }
                else
                {
                    lblStatus.ForeColor = Color.Red;
                    lblStatus.Text = errorMessage;
                }
            }
        }
    }
}