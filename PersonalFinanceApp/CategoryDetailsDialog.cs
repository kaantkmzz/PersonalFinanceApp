using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public class CategoryDetailsDialog : Form
    {
        private readonly User _user;
        private readonly int _categoryId;
        private string _categoryName;

        private readonly CategoryService _categoryService = new CategoryService();
        private readonly TransactionService _transactionService = new TransactionService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color DangerColor = Color.FromArgb(220, 90, 90);

        private Label lblTitle = new Label();
        private DataGridView dgvTransactions = new DataGridView();
        private TextBox txtRename = new TextBox();
        private Label lblStatus = new Label();

        public CategoryDetailsDialog(User user, int categoryId, string categoryName)
        {
            _user = user;
            _categoryId = categoryId;
            _categoryName = categoryName;

            SetupDialog();
            LoadCategoryTransactions();
        }

        private void SetupDialog()
        {
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppBackColor;
            this.Text = "Kategori Detayları";
            this.Font = new Font("Segoe UI", 9F);

            // Başlık
            lblTitle.Text = $"Kategori: {_categoryName}";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = TextLight;
            lblTitle.Location = new Point(20, 20);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Tablo (Sadece bu kategoriye ait işlemler)
            Panel pnlGridWrapper = new Panel { Left = 20, Top = 70, Width = 645, Height = 280, Padding = new Padding(2, 6, 2, 6) };
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);

            dgvTransactions.Dock = DockStyle.Fill;
            dgvTransactions.ReadOnly = true; dgvTransactions.AllowUserToAddRows = false;
            dgvTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTransactions.RowHeadersVisible = false;
            dgvTransactions.Font = new Font("Segoe UI", 9.5F);
            dgvTransactions.RowTemplate.Height = 40;
            dgvTransactions.BorderStyle = BorderStyle.None;
            dgvTransactions.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvTransactions.BackgroundColor = CardBackColor;
            dgvTransactions.DefaultCellStyle.BackColor = CardBackColor;
            dgvTransactions.DefaultCellStyle.ForeColor = TextLight;
            dgvTransactions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 65, 85);
            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 39, 54);
            dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvTransactions.EnableHeadersVisualStyles = false;

            pnlGridWrapper.Controls.Add(dgvTransactions);
            this.Controls.Add(pnlGridWrapper);

            // Alt Kısım: Yeniden Adlandırma ve Silme (Eski Kategoriler ekranından taşındı)
            Panel pnlRename = new Panel { Left = 20, Top = 370, Width = 200, Height = 36 };
            SetupSmoothContainer(pnlRename, 8, CardBackColor);
            txtRename.Left = 10; txtRename.Top = 8; txtRename.Width = 180;
            txtRename.Font = new Font("Segoe UI", 10.5F); txtRename.BorderStyle = BorderStyle.None;
            txtRename.BackColor = CardBackColor; txtRename.ForeColor = TextLight;
            txtRename.Text = _categoryName;
            pnlRename.Controls.Add(txtRename);
            this.Controls.Add(pnlRename);

            Button btnRename = new Button { Text = "✏️ Yeniden Adlandır", Left = 230, Top = 370, Width = 160, Height = 36, Cursor = Cursors.Hand };
            SetupRoundedButton(btnRename, Color.FromArgb(80, 85, 105), Color.White);
            btnRename.Click += BtnRename_Click;
            this.Controls.Add(btnRename);

            Button btnDelete = new Button { Text = "🗑️ Kategoriyi Sil", Left = 505, Top = 370, Width = 160, Height = 36, Cursor = Cursors.Hand };
            SetupRoundedButton(btnDelete, DangerColor, Color.White);
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);

            lblStatus.Left = 20; lblStatus.Top = 420; lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9F);
            this.Controls.Add(lblStatus);
        }

        private void LoadCategoryTransactions()
        {
            var tr = new System.Globalization.CultureInfo("tr-TR");
            var allTransactions = _transactionService.GetUserTransactions(_user.Id);

            // Sadece bu kategoriye ait işlemleri filtrele
            var displayList = allTransactions
                .Where(t => t.CategoryName == _categoryName)
                .Select(t => new {
                    Tarih = t.TransactionDate.ToString("dd.MM.yyyy HH:mm"),
                    Tutar = t.Amount.ToString("#,##0", tr) + " ₺",
                    Açıklama = t.Description
                }).ToList();

            dgvTransactions.DataSource = displayList;
        }

        private void BtnRename_Click(object? sender, EventArgs e)
        {
            string newName = txtRename.Text.Trim();
            if (string.IsNullOrEmpty(newName)) return;

            if (_categoryService.UpdateCategoryName(_categoryId, _user.Id, newName, out string errorMessage))
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Kategori başarıyla yeniden adlandırıldı.";
                _categoryName = newName;
                lblTitle.Text = $"Kategori: {_categoryName}";
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Bu kategoriyi silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    _categoryService.DeleteCategory(_categoryId, _user.Id);
                    this.Close(); // Silindiyse pencereyi kapat
                }
                catch (Exception)
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = "Bu kategoriye ait işlemler olduğu için silinemedi.";
                }
            }
        }

        // Görsel Yardımcılar
        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(pnl.Parent.BackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) e.Graphics.FillPath(brush, path); } }; }
        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent.BackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) e.Graphics.FillPath(brush, path); } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius) { var path = new System.Drawing.Drawing2D.GraphicsPath(); int d = radius * 2; path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path; }
    }
}