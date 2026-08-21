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
    public class CategoryDetailsDialog : Form
    {
        private readonly User _user;
        private readonly int _categoryId;
        private string _categoryName;

        private readonly CategoryService _categoryService = new CategoryService();
        private readonly TransactionService _transactionService = new TransactionService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color DangerColor => AppTheme.DangerColor;

        private Label lblTitle = new Label();
        private DataGridView dgvTransactions = new DataGridView();
        private TextBox txtRename = new TextBox();
        private TextBox txtBudgetLimit = new TextBox();
        private Label lblBudget = new Label();
        private Control[] _budgetControls = Array.Empty<Control>();
        private Label lblStatus = new Label();
        private Category? _category;
        private string? _selectedColor;
        private readonly List<Panel> _colorSwatches = new List<Panel>();

        public CategoryDetailsDialog(User user, int categoryId, string categoryName)
        {
            _user = user;
            _categoryId = categoryId;
            _categoryName = categoryName;

            SetupDialog();
            LoadCategoryTransactions();
            LoadCategoryInfo();
            this.Load += (s, e) => DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupDialog()
        {
            this.Size = new Size(700, 520);
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
            Panel pnlGridWrapper = new Panel { Left = 20, Top = 70, Width = 645, Height = 250, Padding = new Padding(2, 6, 2, 6) };
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
            dgvTransactions.DefaultCellStyle.SelectionBackColor = AppTheme.HoverBackColor;
            dgvTransactions.DefaultCellStyle.SelectionForeColor = TextLight;
            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.HeaderBackColor;
            dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvTransactions.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.HeaderBackColor;
            dgvTransactions.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvTransactions.EnableHeadersVisualStyles = false; dgvTransactions.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            pnlGridWrapper.Controls.Add(dgvTransactions);
            this.Controls.Add(pnlGridWrapper);

            // Kategori ayarları satırı: bütçe limiti (sadece gider kategorilerinde görünür).
            // Bu satır ileride (renk/ikon fazında) sağ tarafa ek alanlarla genişleyecek şekilde
            // ayrılmış durumda, bu yüzden alttaki yeniden adlandırma/sil satırları tekrar kaymayacak.
            lblBudget.Text = "Aylık Bütçe Limiti (₺):"; lblBudget.Left = 20; lblBudget.Top = 332; lblBudget.ForeColor = TextMuted; lblBudget.AutoSize = true;
            this.Controls.Add(lblBudget);

            Panel pnlBudget = new Panel { Left = 20, Top = 355, Width = 160, Height = 36 };
            SetupSmoothContainer(pnlBudget, 8, CardBackColor);
            txtBudgetLimit.Left = 10; txtBudgetLimit.Top = 8; txtBudgetLimit.Width = 140;
            txtBudgetLimit.Font = new Font("Segoe UI", 10.5F); txtBudgetLimit.BorderStyle = BorderStyle.None;
            txtBudgetLimit.BackColor = CardBackColor; txtBudgetLimit.ForeColor = TextLight;
            txtBudgetLimit.PlaceholderText = "Limit yok";
            pnlBudget.Controls.Add(txtBudgetLimit);
            this.Controls.Add(pnlBudget);

            Button btnSaveBudget = new Button { Text = "Kaydet", Left = 190, Top = 355, Height = 36, Cursor = Cursors.Hand };
            btnSaveBudget.Width = TextRenderer.MeasureText(btnSaveBudget.Text, btnSaveBudget.Font).Width + 36;
            SetupRoundedButton(btnSaveBudget, Color.FromArgb(80, 85, 105), Color.White);
            btnSaveBudget.Click += BtnSaveBudget_Click;
            this.Controls.Add(btnSaveBudget);
            _budgetControls = new Control[] { lblBudget, pnlBudget, btnSaveBudget };

            // Renk seçimi, bütçe satırının sağında aynı yükseklikte.
            Label lblColorIcon = new Label { Text = "Renk:", Left = 400, Top = 332, ForeColor = TextMuted, AutoSize = true };
            this.Controls.Add(lblColorIcon);

            const int swatchSize = 20, swatchGap = 4;
            for (int i = 0; i < AvatarHelper.Palette.Length && i < 6; i++)
            {
                Color swatchColor = AvatarHelper.Palette[i];
                string hex = AvatarHelper.ToHex(swatchColor);
                Panel swatch = new Panel
                {
                    Left = 400 + i * (swatchSize + swatchGap),
                    Top = 359,
                    Width = swatchSize,
                    Height = swatchSize,
                    Cursor = Cursors.Hand,
                    Tag = hex
                };
                swatch.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.Clear(this.BackColor);
                    bool isSelected = _selectedColor == hex;
                    if (isSelected)
                    {
                        using var ring = new Pen(TextLight, 2f);
                        e.Graphics.DrawEllipse(ring, 0, 0, swatch.Width - 1, swatch.Height - 1);
                    }
                    int inset = isSelected ? 4 : 1;
                    using var brush = new SolidBrush(swatchColor);
                    e.Graphics.FillEllipse(brush, inset, inset, swatch.Width - inset * 2, swatch.Height - inset * 2);
                };
                swatch.Click += (s, e) =>
                {
                    _selectedColor = (_selectedColor == hex) ? null : hex; // tekrar tıklayınca rengi kaldır
                    foreach (var sw in _colorSwatches) sw.Invalidate();
                    SaveColorIcon();
                };
                _colorSwatches.Add(swatch);
                this.Controls.Add(swatch);
            }

            // Alt Kısım: Yeniden Adlandırma ve Silme (Eski Kategoriler ekranından taşındı)
            Panel pnlRename = new Panel { Left = 20, Top = 410, Width = 200, Height = 36 };
            SetupSmoothContainer(pnlRename, 8, CardBackColor);
            txtRename.Left = 10; txtRename.Top = 8; txtRename.Width = 180;
            txtRename.Font = new Font("Segoe UI", 10.5F); txtRename.BorderStyle = BorderStyle.None;
            txtRename.BackColor = CardBackColor; txtRename.ForeColor = TextLight;
            txtRename.Text = _categoryName;
            pnlRename.Controls.Add(txtRename);
            this.Controls.Add(pnlRename);

            Button btnRename = new Button { Text = "✏️ Yeniden Adlandır", Top = 410, Height = 36, Cursor = Cursors.Hand };
            btnRename.Width = TextRenderer.MeasureText(btnRename.Text, btnRename.Font).Width + 44;
            btnRename.Left = 230;
            SetupRoundedButton(btnRename, Color.FromArgb(80, 85, 105), Color.White);
            btnRename.Click += BtnRename_Click;
            this.Controls.Add(btnRename);

            Button btnDelete = new Button { Text = "🗑️ Kategoriyi Sil", Top = 410, Height = 36, Cursor = Cursors.Hand };
            btnDelete.Width = TextRenderer.MeasureText(btnDelete.Text, btnDelete.Font).Width + 44;
            btnDelete.Left = 665 - btnDelete.Width;
            SetupRoundedButton(btnDelete, DangerColor, Color.White);
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);

            lblStatus.Left = 20; lblStatus.Top = 460; lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9F);
            this.Controls.Add(lblStatus);
        }

        private bool _loadingCategoryInfo;

        private void LoadCategoryInfo()
        {
            _loadingCategoryInfo = true;

            _category = _categoryService.GetUserCategories(_user.Id).FirstOrDefault(c => c.Id == _categoryId);

            bool isExpense = _category?.Type == "expense";
            foreach (var c in _budgetControls) c.Visible = isExpense;

            if (isExpense && _category?.BudgetLimit != null)
            {
                txtBudgetLimit.Text = _category.BudgetLimit.Value.ToString("0.##");
            }

            _selectedColor = _category?.Color;
            foreach (var sw in _colorSwatches) sw.Invalidate();

            _loadingCategoryInfo = false;
        }

        private void SaveColorIcon()
        {
            if (_loadingCategoryInfo) return;
            _categoryService.SetColorIcon(_categoryId, _user.Id, _selectedColor, null);
        }

        private void BtnSaveBudget_Click(object? sender, EventArgs e)
        {
            string text = txtBudgetLimit.Text.Trim();
            decimal? limit = null;

            if (!string.IsNullOrEmpty(text))
            {
                if (!decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal parsed))
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = "Geçerli bir tutar giriniz.";
                    return;
                }
                limit = parsed;
            }

            if (_categoryService.SetBudgetLimit(_categoryId, _user.Id, limit, out string errorMessage))
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = limit.HasValue ? "Bütçe limiti kaydedildi." : "Bütçe limiti kaldırıldı.";
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
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
        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) e.Graphics.FillPath(brush, path); } }; }
        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) e.Graphics.FillPath(brush, path); } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius) { var path = new System.Drawing.Drawing2D.GraphicsPath(); int d = radius * 2; path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path; }
    }
}