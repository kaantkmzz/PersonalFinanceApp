using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using PersonalFinanceApp.Helpers;
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

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color DangerColor => AppTheme.DangerColor;

        private ComboBox cmbType = new ComboBox();
        private ComboBox cmbCategory = new ComboBox();
        private TextBox txtAmount = new TextBox();
        private TextBox txtDescription = new TextBox();
        private DateTimePicker dtpDate = new DateTimePicker();
        private Label lblStatus = new Label();

        public bool WasUpdated { get; private set; }
        public bool WasDeleted { get; private set; }

        public TransactionEditDialog(User user, Transaction transaction)
        {
            _user = user;
            _transaction = transaction;

            SetupDialog();
            LoadCategories();
            this.Load += (s, e) => DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupDialog()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new Size(400, 660);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppBackColor;
            this.Text = "İşlemi Düzenle";
            this.Font = new Font("Segoe UI", 9F);

            Label lblTitle = new Label { Text = "İşlemi Düzenle", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true };
            this.Controls.Add(lblTitle);

            // --- Tip ---
            // Not: Label ile kutu arasındaki boşluk, bu makinenin ~150% DPI ölçeğinde etiket
            // metninin gerçek piksel yüksekliğinin eski (22px) boşluktan büyük olması yüzünden
            // yetersizdi — etiketin alt kısmı kutunun üstüne biniyordu. Boşluklar büyütüldü.
            Label lblType = new Label { Text = "Tip:", Left = 20, Top = 68, ForeColor = TextMuted, AutoSize = true };
            Panel pnlType = new Panel { Left = 20, Top = 98, Width = 360, Height = 38 };
            cmbType.Left = 5; cmbType.Top = 8; cmbType.Width = 350;
            cmbType.Font = new Font("Segoe UI", 9.5F); cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("Gelir"); cmbType.Items.Add("Gider"); cmbType.Items.Add("Hedef"); cmbType.Items.Add("Yatırım");
            cmbType.SelectedIndex = _transaction.Type switch { "income" => 0, "goal" => 2, "invest" => 3, _ => 1 };
            cmbType.SelectedIndexChanged += (s, e) => LoadCategories();
            pnlType.Controls.Add(cmbType);
            SetupCustomComboBox(pnlType, cmbType);
            this.Controls.Add(lblType); this.Controls.Add(pnlType);

            // --- Kategori ---
            Label lblCategory = new Label { Text = "Kategori:", Left = 20, Top = 150, ForeColor = TextMuted, AutoSize = true };
            Panel pnlCategory = new Panel { Left = 20, Top = 180, Width = 360, Height = 38 };
            cmbCategory.Left = 5; cmbCategory.Top = 8; cmbCategory.Width = 350;
            cmbCategory.Font = new Font("Segoe UI", 9.5F); cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            pnlCategory.Controls.Add(cmbCategory);
            SetupCustomComboBox(pnlCategory, cmbCategory);
            this.Controls.Add(lblCategory); this.Controls.Add(pnlCategory);

            // --- Tutar ---
            Label lblAmount = new Label { Text = "Tutar:", Left = 20, Top = 232, ForeColor = TextMuted, AutoSize = true };
            Panel pnlAmount = new Panel { Left = 20, Top = 262, Width = 360, Height = 38 };
            SetupSmoothContainer(pnlAmount, 8, CardBackColor);
            txtAmount.Left = 12; txtAmount.Top = 9; txtAmount.Width = 336;
            txtAmount.Font = new Font("Segoe UI", 10.5F); txtAmount.BorderStyle = BorderStyle.None;
            txtAmount.BackColor = CardBackColor; txtAmount.ForeColor = TextLight;
            txtAmount.Text = _transaction.Amount.ToString("#,##0", new System.Globalization.CultureInfo("tr-TR"));
            txtAmount.TextChanged += (s, e) => SmartFormatAmount(txtAmount);
            pnlAmount.Controls.Add(txtAmount);
            this.Controls.Add(lblAmount); this.Controls.Add(pnlAmount);

            // --- Tarih ---
            Label lblDate = new Label { Text = "Tarih:", Left = 20, Top = 314, ForeColor = TextMuted, AutoSize = true };
            Panel pnlDate = new Panel { Left = 20, Top = 344, Width = 360, Height = 38 };
            SetupSmoothContainer(pnlDate, 8, CardBackColor);
            dtpDate.Left = 8; dtpDate.Top = 6; dtpDate.Width = 344;
            dtpDate.Font = new Font("Segoe UI", 10F);
            dtpDate.Format = DateTimePickerFormat.Custom;
            dtpDate.CustomFormat = "dd.MM.yyyy HH:mm";
            dtpDate.Value = _transaction.TransactionDate;
            // Not: aşağıdaki DisableVisualStyle+BackColor denemesi kapalı kutunun beyaz zeminini
            // düzeltmiyor (comctl32'nin DateTimePicker'ına özgü bilinen bir sınırlama — ShowUpDown
            // modu ve Application.SetColorMode(Dark) de denendi, ikisi de etkisiz kaldı, bkz.
            // TransactionControl.SetupDarkDateTimePicker). Yalnızca açılır TAKVİM (Calendar* +
            // EnableDarkCalendarPopup) gerçekten koyulaşıyor.
            dtpDate.CalendarForeColor = TextLight;
            dtpDate.CalendarMonthBackground = CardBackColor;
            dtpDate.CalendarTitleBackColor = AppTheme.HeaderBackColor;
            dtpDate.CalendarTitleForeColor = TextLight;
            dtpDate.CalendarTrailingForeColor = TextMuted;
            dtpDate.HandleCreated += (s, e) =>
            {
                DarkTitleBarHelper.DisableVisualStyle(dtpDate);
                dtpDate.BackColor = CardBackColor;
                dtpDate.ForeColor = TextLight;
            };
            DarkTitleBarHelper.EnableDarkCalendarPopup(dtpDate);
            pnlDate.Controls.Add(dtpDate);
            this.Controls.Add(lblDate); this.Controls.Add(pnlDate);

            // --- Açıklama ---
            Label lblDescription = new Label { Text = "Açıklama (opsiyonel):", Left = 20, Top = 396, ForeColor = TextMuted, AutoSize = true };
            Panel pnlDesc = new Panel { Left = 20, Top = 426, Width = 360, Height = 38 };
            SetupSmoothContainer(pnlDesc, 8, CardBackColor);
            txtDescription.Left = 12; txtDescription.Top = 9; txtDescription.Width = 336;
            txtDescription.Font = new Font("Segoe UI", 10.5F); txtDescription.BorderStyle = BorderStyle.None;
            txtDescription.BackColor = CardBackColor; txtDescription.ForeColor = TextLight;
            txtDescription.Text = _transaction.Description ?? string.Empty;
            pnlDesc.Controls.Add(txtDescription);
            this.Controls.Add(lblDescription); this.Controls.Add(pnlDesc);

            // --- Butonlar ---
            Button btnSave = new Button { Text = "Kaydet", Left = 20, Top = 488, Width = 172, Height = 40, Cursor = Cursors.Hand };
            SetupRoundedButton(btnSave, AccentColor, Color.White);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            Button btnCancel = new Button { Text = "İptal", Left = 208, Top = 488, Width = 172, Height = 40, Cursor = Cursors.Hand };
            SetupRoundedButton(btnCancel, Color.FromArgb(80, 85, 105), Color.White);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            Button btnDelete = new Button { Text = "🗑️ İşlemi Sil", Left = 20, Top = 536, Width = 360, Height = 40, Cursor = Cursors.Hand };
            SetupRoundedButton(btnDelete, DangerColor, Color.White);
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);

            lblStatus.Left = 20; lblStatus.Top = 584; lblStatus.Width = 360; lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
            this.Controls.Add(lblStatus);
        }

        private string GetSelectedType() => cmbType.SelectedItem?.ToString() switch { "Gelir" => "income", "Hedef" => "goal", "Yatırım" => "invest", _ => "expense" };

        private bool _suppressAmountFormatting = false;

        // Tutar kutusuna yazılan rakamları "10.000" gibi binlik ayraçlarla biçimlendirir.
        private void SmartFormatAmount(TextBox txt)
        {
            if (_suppressAmountFormatting || string.IsNullOrWhiteSpace(txt.Text)) return;
            string value = new string(txt.Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(value)) return;
            if (decimal.TryParse(value, out decimal amount))
            {
                string formatted = amount.ToString("#,##0", new System.Globalization.CultureInfo("tr-TR"));
                if (txt.Text == formatted) return;
                _suppressAmountFormatting = true;
                txt.Text = formatted;
                txt.SelectionStart = txt.Text.Length;
                _suppressAmountFormatting = false;
            }
        }

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
            string rawAmount = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
            if (!decimal.TryParse(rawAmount, out decimal amount))
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

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Bu işlemi silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            if (_transactionService.DeleteTransaction(_transaction.Id, _user.Id, out string errorMessage))
            {
                WasDeleted = true;
                this.Close();
            }
            else
            {
                lblStatus.Text = errorMessage;
            }
        }

        // --- GÖRSEL YARDIMCI METOTLAR ---
        private void SetupCustomComboBox(Panel pnl, ComboBox cmb)
        {
            SetupSmoothContainer(pnl, 8, CardBackColor);
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = CardBackColor;
            cmb.ForeColor = TextLight;

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

            cmb.Region = new Region(new Rectangle(1, 1, cmb.Width - 2, cmb.Height - 2));

            Panel pnlArrow = new Panel();
            pnlArrow.Width = 28;
            pnlArrow.Height = cmb.Height - 2;
            pnlArrow.Left = cmb.Right - pnlArrow.Width - 1;
            pnlArrow.Top = cmb.Top + 1;
            pnlArrow.BackColor = CardBackColor;
            pnlArrow.Cursor = Cursors.Hand;

            pnlArrow.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int ax = pnlArrow.Width / 2 - 5;
                int ay = pnlArrow.Height / 2 - 2;
                using (var brush = new SolidBrush(TextMuted))
                    e.Graphics.FillPolygon(brush, new Point[] { new Point(ax, ay), new Point(ax + 10, ay), new Point(ax + 5, ay + 6) });
            };

            pnlArrow.MouseClick += (s, e) => { cmb.DroppedDown = true; };
            pnl.MouseClick += (s, e) => { cmb.DroppedDown = true; };

            pnl.Controls.Add(pnlArrow);
            pnlArrow.BringToFront();
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor)
        {
            pnl.BackColor = AppBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor);
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Font = new Font("Segoe UI", 10F);
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = Math.Max(radius * 2, 1);
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
