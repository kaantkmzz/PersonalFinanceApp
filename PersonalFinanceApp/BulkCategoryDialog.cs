using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    // İşlemler ekranındaki "Toplu İşlem" modunda seçili işlemlerin kategorisini tek seferde
    // değiştirmek için kullanılan küçük seçim diyaloğu.
    public class BulkCategoryDialog : Form
    {
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly System.Collections.Generic.List<Category> _categories;
        private ComboBox cmbCategory = new ComboBox();

        public int? SelectedCategoryId { get; private set; }

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;

        public BulkCategoryDialog(User user, string type)
        {
            _categories = _categoryService.GetUserCategoriesByType(user.Id, type);
            SetupUI();
            this.Load += (s, e) => Helpers.DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Kategori Değiştir";
            this.Size = new Size(360, 220);
            this.BackColor = AppBackColor;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9.5F);

            Label lblTitle = new Label { Text = "Yeni Kategori Seçin", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 20, AutoSize = true };

            Panel pnlCombo = new Panel { Left = 20, Top = 60, Width = 300, Height = 38 };
            SetupSmoothContainer(pnlCombo, 8, CardBackColor);
            cmbCategory.Left = 5; cmbCategory.Top = 8; cmbCategory.Width = 290;
            cmbCategory.Font = new Font("Segoe UI", 10F); cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var c in _categories) cmbCategory.Items.Add(c.Name);
            // Otomatik olarak ilk kategoriyi (listede ne varsa) seçiyordu — kullanıcı hiç dokunmadan
            // "Uygula"ya basarsa yanlışlıkla o kategoriye taşınabiliyordu. Artık boş başlıyor, kullanıcı
            // bilinçli olarak seçmeden Uygula zaten devre dışı (bkz. aşağıdaki SelectedIndexChanged).
            pnlCombo.Controls.Add(cmbCategory);
            SetupCustomComboBox(pnlCombo, cmbCategory);

            Button btnOk = new Button { Text = "Uygula", Left = 20, Top = 130, Width = 140, Height = 38, Cursor = Cursors.Hand, Enabled = false };
            SetupRoundedButton(btnOk, AccentColor, Color.White);
            cmbCategory.SelectedIndexChanged += (s, e) => btnOk.Enabled = cmbCategory.SelectedIndex >= 0;
            btnOk.Click += (s, e) =>
            {
                if (cmbCategory.SelectedIndex < 0) return;
                SelectedCategoryId = _categories[cmbCategory.SelectedIndex].Id;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            Button btnCancel = new Button { Text = "İptal", Left = 180, Top = 130, Width = 140, Height = 38, Cursor = Cursors.Hand };
            SetupRoundedButton(btnCancel, Color.FromArgb(80, 85, 105), Color.White);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lblTitle);
            this.Controls.Add(pnlCombo);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            if (_categories.Count == 0)
            {
                Label lblEmpty = new Label { Text = "Bu tipte kategori yok.", ForeColor = TextMuted, Left = 20, Top = 108, AutoSize = true };
                this.Controls.Add(lblEmpty);
                btnOk.Enabled = false;
            }
        }

        // ComboBox'ın varsayılan beyaz sistem çizimini tema renklerine göre kendi çizdiğimiz
        // sürümle değiştirir (bkz. TransactionControl.SetupCustomComboBox). Bu dialogda çevreleyen
        // panel zaten SetupSmoothContainer ile boyanmış olduğundan burada sadece ComboBox'ın
        // kendisini (arka plan + açılır liste + kırpma bölgesi) ele alıyoruz.
        private void SetupCustomComboBox(Panel pnl, ComboBox cmb)
        {
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = CardBackColor;
            cmb.ForeColor = TextLight;
            cmb.DrawMode = DrawMode.OwnerDrawFixed;
            cmb.ItemHeight = 24;
            cmb.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bgColor = isSelected ? AppTheme.HoverBackColor : CardBackColor;
                e.Graphics.FillRectangle(new SolidBrush(bgColor), e.Bounds);
                TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index]?.ToString() ?? string.Empty, cmb.Font, e.Bounds, TextLight, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            };
            cmb.Region = new Region(new Rectangle(1, 1, cmb.Width - 2, cmb.Height - 2));

            // OwnerDrawFixed açılır ok düğmesini kapsamıyor — Windows'un kendi (beyaz) temasıyla
            // çizilen o düğmeyi kartla aynı renkte bir panelle kapatıp kendi okumuzu çiziyoruz.
            Panel pnlArrow = new Panel { Width = 28, BackColor = CardBackColor, Cursor = Cursors.Hand };
            pnlArrow.Height = cmb.Height - 2;
            pnlArrow.Left = cmb.Right - pnlArrow.Width;
            pnlArrow.Top = cmb.Top + 1;
            pnlArrow.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int ax = pnlArrow.Width / 2 - 4;
                int ay = pnlArrow.Height / 2 - 2;
                using var brush = new SolidBrush(TextMuted);
                e.Graphics.FillPolygon(brush, new Point[] { new Point(ax, ay), new Point(ax + 8, ay), new Point(ax + 4, ay + 5) });
            };
            pnlArrow.MouseClick += (s, e) => { cmb.DroppedDown = true; };
            pnl.MouseClick += (s, e) => { cmb.DroppedDown = true; };
            pnl.Controls.Add(pnlArrow);
            pnlArrow.BringToFront();
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } }; }
        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius) { var path = new System.Drawing.Drawing2D.GraphicsPath(); int d = Math.Max(radius * 2, 1); path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path; }
    }
}
