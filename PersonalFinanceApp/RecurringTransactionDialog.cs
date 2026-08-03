using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class RecurringTransactionDialog : Form
    {
        private readonly User _user;
        private readonly RecurringTransactionService _recurringService = new RecurringTransactionService();

        private static readonly Color AppBackColor = Color.FromArgb(37, 41, 59);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color DangerColor = Color.FromArgb(220, 90, 90);

        private ComboBox cmbType = new ComboBox();
        private TextBox txtCategory = new TextBox();
        private TextBox txtAmount = new TextBox();
        private TextBox txtDescription = new TextBox();
        private Button btnAdd = new Button();
        private Label lblStatus = new Label();

        private DataGridView dgvRecurring = new DataGridView();
        private List<RecurringTransaction> _cachedRecurring = new List<RecurringTransaction>();
        private Button btnDelete = new Button();
        private bool _isUpdatingProgrammatically = false;

        private class RecurringRow
        {
            public int ID { get; set; }
            public string Tip { get; set; } = string.Empty;
            public string Kategori { get; set; } = string.Empty;
            public string Tutar { get; set; } = string.Empty;
            public bool Aktif { get; set; }
        }

        public RecurringTransactionDialog(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadRecurring();
            this.Load += (s, e) => DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Tekrarlanan İşlemler";
            this.Width = 560;
            this.Height = 560;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9.5F);

            Label lblInfo = new Label
            {
                Text = "Buraya eklenen işlemler, her ay giriş yaptığınızda otomatik olarak eklenir.",
                Left = 20,
                Top = 15,
                Width = 500,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5F)
            };

            Label lblTypeField = new Label { Text = "Tip:", Left = 20, Top = 50, ForeColor = TextMuted, AutoSize = true };
            cmbType.Left = 20;
            cmbType.Top = 78;
            cmbType.Width = 100;
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("Gelir");
            cmbType.Items.Add("Gider");
            cmbType.SelectedIndex = 1;

            Label lblCategoryField = new Label { Text = "Kategori:", Left = 130, Top = 50, ForeColor = TextMuted, AutoSize = true };
            txtCategory.Left = 130;
            txtCategory.Top = 78;
            txtCategory.Width = 140;

            Label lblAmountField = new Label { Text = "Tutar:", Left = 280, Top = 50, ForeColor = TextMuted, AutoSize = true };
            txtAmount.Left = 280;
            txtAmount.Top = 78;
            txtAmount.Width = 100;

            Label lblDescField = new Label { Text = "Açıklama:", Left = 390, Top = 50, ForeColor = TextMuted, AutoSize = true };
            txtDescription.Left = 390;
            txtDescription.Top = 78;
            txtDescription.Width = 130;

            btnAdd.Text = "Ekle";
            btnAdd.Left = 20;
            btnAdd.Top = 112;
            btnAdd.Width = 500;
            btnAdd.Height = 32;
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.BackColor = AccentColor;
            btnAdd.ForeColor = Color.White;
            btnAdd.Cursor = Cursors.Hand;
            btnAdd.Click += BtnAdd_Click;

            lblStatus.Left = 20;
            lblStatus.Top = 150;
            lblStatus.Width = 500;
            lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F);

            dgvRecurring.Left = 20;
            dgvRecurring.Top = 185;
            dgvRecurring.Width = 500;
            dgvRecurring.Height = 300;
            dgvRecurring.AllowUserToAddRows = false;
            dgvRecurring.AllowUserToDeleteRows = false;
            dgvRecurring.AllowUserToResizeColumns = false;
            dgvRecurring.AllowUserToResizeRows = false;
            dgvRecurring.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRecurring.MultiSelect = false;
            dgvRecurring.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRecurring.BackgroundColor = Color.White;
            dgvRecurring.BorderStyle = BorderStyle.None;
            dgvRecurring.RowHeadersVisible = false;
            dgvRecurring.Font = new Font("Segoe UI", 9F);
            dgvRecurring.RowTemplate.Height = 30;
            dgvRecurring.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dgvRecurring.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 239, 246);
            dgvRecurring.DefaultCellStyle.SelectionForeColor = Color.FromArgb(40, 40, 40);

            dgvRecurring.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvRecurring.IsCurrentCellDirty)
                {
                    dgvRecurring.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            dgvRecurring.CellValueChanged += DgvRecurring_CellValueChanged;

            btnDelete.Text = "Seçili İşlemi Sil";
            btnDelete.Left = 20;
            btnDelete.Top = 495;
            btnDelete.Width = 500;
            btnDelete.Height = 34;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 1;
            btnDelete.FlatAppearance.BorderColor = DangerColor;
            btnDelete.BackColor = AppBackColor;
            btnDelete.ForeColor = DangerColor;
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.Click += BtnDelete_Click;

            this.Controls.Add(lblInfo);
            this.Controls.Add(lblTypeField);
            this.Controls.Add(cmbType);
            this.Controls.Add(lblCategoryField);
            this.Controls.Add(txtCategory);
            this.Controls.Add(lblAmountField);
            this.Controls.Add(txtAmount);
            this.Controls.Add(lblDescField);
            this.Controls.Add(txtDescription);
            this.Controls.Add(btnAdd);
            this.Controls.Add(lblStatus);
            this.Controls.Add(dgvRecurring);
            this.Controls.Add(btnDelete);
        }

        private void LoadRecurring()
        {
            _cachedRecurring = _recurringService.GetUserRecurring(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            _isUpdatingProgrammatically = true;

            var displayList = _cachedRecurring.Select(r => new RecurringRow
            {
                ID = r.Id,
                Tip = r.Type == "income" ? "Gelir" : "Gider",
                Kategori = r.CategoryName,
                Tutar = r.Amount.ToString("#,##0", tr) + " ₺",
                Aktif = r.IsActive
            }).ToList();

            dgvRecurring.DataSource = displayList;

            if (dgvRecurring.Columns["ID"] != null)
            {
                dgvRecurring.Columns["ID"].Visible = false;
                dgvRecurring.Columns["Tip"].ReadOnly = true;
                dgvRecurring.Columns["Kategori"].ReadOnly = true;
                dgvRecurring.Columns["Tutar"].ReadOnly = true;
            }

            _isUpdatingProgrammatically = false;
        }

        private void DgvRecurring_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_isUpdatingProgrammatically || e.RowIndex < 0) return;
            if (dgvRecurring.Columns[e.ColumnIndex].Name != "Aktif") return;

            var row = dgvRecurring.Rows[e.RowIndex];
            int recurringId = Convert.ToInt32(row.Cells["ID"].Value);
            bool newValue = (bool)row.Cells["Aktif"].Value;

            _recurringService.SetActive(recurringId, _user.Id, newValue);
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string type = cmbType.SelectedItem?.ToString() == "Gelir" ? "income" : "expense";
            string category = txtCategory.Text.Trim();
            string description = txtDescription.Text;

            if (!decimal.TryParse(txtAmount.Text, out decimal amount))
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Geçersiz tutar.";
                return;
            }

            bool success = _recurringService.AddRecurring(_user.Id, category, type, amount, description, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Tekrarlanan işlem eklendi.";
                txtCategory.Clear();
                txtAmount.Clear();
                txtDescription.Clear();
                LoadRecurring();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvRecurring.CurrentRow == null)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen silmek için bir işlem seçin.";
                return;
            }

            var confirm = MessageBox.Show("Bu tekrarlanan işlemi silmek istediğinize emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                int recurringId = Convert.ToInt32(dgvRecurring.CurrentRow.Cells["ID"].Value);
                _recurringService.DeleteRecurring(recurringId, _user.Id);
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "İşlem silindi.";
                LoadRecurring();
            }
        }
    }
}