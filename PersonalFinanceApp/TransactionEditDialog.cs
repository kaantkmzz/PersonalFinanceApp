using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public class TransactionEditDialog : Form
    {
        private readonly User _user;
        private readonly Transaction _transaction;
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly CategoryService _categoryService = new CategoryService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);

        private ComboBox cmbType = new ComboBox();
        private ComboBox cmbCategory = new ComboBox();
        private TextBox txtAmount = new TextBox();
        private TextBox txtDescription = new TextBox();
        private DateTimePicker dtpDate = new DateTimePicker();
        private Label lblStatus = new Label();

        public bool WasUpdated { get; private set; }

        public TransactionEditDialog(User user, Transaction transaction)
        {
            _user = user;
            _transaction = transaction;

            SetupDialog();
            LoadCategories();
        }

        private void SetupDialog()
        {
            this.Size = new Size(420, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppBackColor;
            this.Text = "İşlemi Düzenle";
            this.Font = new Font("Segoe UI", 9F);

            Label lblTitle = new Label { Text = "İşlemi Düzenle", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true };
            this.Controls.Add(lblTitle);

            Label lblType = new Label { Text = "Tip:", Left = 20, Top = 60, ForeColor = TextMuted, AutoSize = true };
            cmbType.Left = 20; cmbType.Top = 82; cmbType.Width = 360;
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("Gelir"); cmbType.Items.Add("Gider");
            cmbType.SelectedIndex = _transaction.Type == "income" ? 0 : 1;
            cmbType.SelectedIndexChanged += (s, e) => LoadCategories();
            this.Controls.Add(lblType); this.Controls.Add(cmbType);

            Label lblCategory = new Label { Text = "Kategori:", Left = 20, Top = 115, ForeColor = TextMuted, AutoSize = true };
            cmbCategory.Left = 20; cmbCategory.Top = 137; cmbCategory.Width = 360;
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            this.Controls.Add(lblCategory); this.Controls.Add(cmbCategory);

            Label lblAmount = new Label { Text = "Tutar:", Left = 20, Top = 170, ForeColor = TextMuted, AutoSize = true };
            txtAmount.Left = 20; txtAmount.Top = 192; txtAmount.Width = 360;
            txtAmount.Text = _transaction.Amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            this.Controls.Add(lblAmount); this.Controls.Add(txtAmount);

            Label lblDate = new Label { Text = "Tarih:", Left = 20, Top = 225, ForeColor = TextMuted, AutoSize = true };
            dtpDate.Left = 20; dtpDate.Top = 247; dtpDate.Width = 360;
            dtpDate.Format = DateTimePickerFormat.Custom;
            dtpDate.CustomFormat = "dd.MM.yyyy HH:mm";
            dtpDate.Value = _transaction.TransactionDate;
            this.Controls.Add(lblDate); this.Controls.Add(dtpDate);

            Label lblDescription = new Label { Text = "Açıklama (opsiyonel):", Left = 20, Top = 280, ForeColor = TextMuted, AutoSize = true };
            txtDescription.Left = 20; txtDescription.Top = 302; txtDescription.Width = 360;
            txtDescription.Text = _transaction.Description ?? string.Empty;
            this.Controls.Add(lblDescription); this.Controls.Add(txtDescription);

            Button btnSave = new Button { Text = "Kaydet", Left = 20, Top = 340, Width = 170, Height = 36, Cursor = Cursors.Hand, BackColor = AccentColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            Button btnCancel = new Button { Text = "İptal", Left = 210, Top = 340, Width = 170, Height = 36, Cursor = Cursors.Hand, BackColor = Color.FromArgb(80, 85, 105), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            lblStatus.Left = 20; lblStatus.Top = 385; lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
            this.Controls.Add(lblStatus);
        }

        private string GetSelectedType() => cmbType.SelectedItem?.ToString() == "Gelir" ? "income" : "expense";

        private void LoadCategories()
        {
            string type = GetSelectedType();
            var categories = _categoryService.GetUserCategoriesByType(_user.Id, type);
            cmbCategory.Items.Clear();
            foreach (var cat in categories) cmbCategory.Items.Add(cat.Name);

            var match = categories.FirstOrDefault(c => c.Id == _transaction.CategoryId);
            if (match != null) cmbCategory.SelectedItem = match.Name;
            else if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cmbCategory.SelectedItem == null)
            {
                lblStatus.Text = "Lütfen bir kategori seçin.";
                return;
            }
            if (!decimal.TryParse(txtAmount.Text, out decimal amount))
            {
                lblStatus.Text = "Geçersiz tutar.";
                return;
            }

            string type = GetSelectedType();
            string categoryName = cmbCategory.SelectedItem.ToString()!;
            var category = _categoryService.GetOrCreateCategory(_user.Id, categoryName, type);

            bool success = _transactionService.UpdateTransaction(
                _transaction.Id, _user.Id, category.Id, amount, type,
                txtDescription.Text, dtpDate.Value, out string errorMessage);

            if (success)
            {
                WasUpdated = true;
                this.Close();
            }
            else
            {
                lblStatus.Text = errorMessage;
            }
        }
    }
}
