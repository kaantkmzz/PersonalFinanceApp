using System;
using System.Drawing;
using System.Windows.Forms;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class PasswordChangeControl : UserControl
    {
        private readonly User _user;
        private readonly AuthService _authService = new AuthService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color FieldBackColor => AppTheme.HoverBackColor;

        private Panel pnlCard = new Panel();
        private TextBox txtCurrentPassword = new TextBox();
        private TextBox txtNewPassword = new TextBox();
        private TextBox txtConfirmPassword = new TextBox();
        private CheckBox chkShowPasswords = new CheckBox();
        private Button btnSave = new Button();
        private Label lblStatus = new Label();

        public PasswordChangeControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            pnlCard.Width = 460;
            pnlCard.Height = 440;
            SetupSmoothContainer(pnlCard, 16, CardBackColor);

            Label lblTitle = new Label { Text = "Şifre Değiştir", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 40, Top = 30, AutoSize = true };

            Label lblCurrent = new Label { Text = "Mevcut Şifre:", Left = 40, Top = 90, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlCurrent = new Panel { Left = 40, Top = 120, Width = 380, Height = 40 };
            SetupSmoothContainer(pnlCurrent, 8, FieldBackColor, CardBackColor);
            txtCurrentPassword.Left = 10; txtCurrentPassword.Top = 10; txtCurrentPassword.Width = 360;
            txtCurrentPassword.Font = new Font("Segoe UI", 10.5F); txtCurrentPassword.BorderStyle = BorderStyle.None;
            txtCurrentPassword.BackColor = FieldBackColor; txtCurrentPassword.ForeColor = TextLight; txtCurrentPassword.UseSystemPasswordChar = true;
            pnlCurrent.Controls.Add(txtCurrentPassword);

            Label lblNew = new Label { Text = "Yeni Şifre (en az 6 karakter):", Left = 40, Top = 175, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlNew = new Panel { Left = 40, Top = 205, Width = 380, Height = 40 };
            SetupSmoothContainer(pnlNew, 8, FieldBackColor, CardBackColor);
            txtNewPassword.Left = 10; txtNewPassword.Top = 10; txtNewPassword.Width = 360;
            txtNewPassword.Font = new Font("Segoe UI", 10.5F); txtNewPassword.BorderStyle = BorderStyle.None;
            txtNewPassword.BackColor = FieldBackColor; txtNewPassword.ForeColor = TextLight; txtNewPassword.UseSystemPasswordChar = true;
            pnlNew.Controls.Add(txtNewPassword);

            Label lblConfirm = new Label { Text = "Yeni Şifre (tekrar):", Left = 40, Top = 260, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlConfirm = new Panel { Left = 40, Top = 290, Width = 380, Height = 40 };
            SetupSmoothContainer(pnlConfirm, 8, FieldBackColor, CardBackColor);
            txtConfirmPassword.Left = 10; txtConfirmPassword.Top = 10; txtConfirmPassword.Width = 360;
            txtConfirmPassword.Font = new Font("Segoe UI", 10.5F); txtConfirmPassword.BorderStyle = BorderStyle.None;
            txtConfirmPassword.BackColor = FieldBackColor; txtConfirmPassword.ForeColor = TextLight; txtConfirmPassword.UseSystemPasswordChar = true;
            pnlConfirm.Controls.Add(txtConfirmPassword);

            chkShowPasswords.Text = "Şifreleri göster";
            chkShowPasswords.Left = 40; chkShowPasswords.Top = 345; chkShowPasswords.AutoSize = true; chkShowPasswords.ForeColor = TextMuted; chkShowPasswords.BackColor = Color.Transparent;
            chkShowPasswords.CheckedChanged += (s, e) =>
            {
                bool show = chkShowPasswords.Checked;
                txtCurrentPassword.UseSystemPasswordChar = !show;
                txtNewPassword.UseSystemPasswordChar = !show;
                txtConfirmPassword.UseSystemPasswordChar = !show;
            };

            btnSave.Text = "💾 Şifreyi Güncelle";
            btnSave.Left = 200; btnSave.Top = 340; btnSave.Width = 220; btnSave.Height = 36; btnSave.Cursor = Cursors.Hand;
            SetupRoundedButton(btnSave, AccentColor, Color.White);
            btnSave.Click += BtnSave_Click;

            lblStatus.Left = 40; lblStatus.Top = 395; lblStatus.Width = 380; lblStatus.Height = 25; lblStatus.Font = new Font("Segoe UI", 9F); lblStatus.BackColor = Color.Transparent;

            pnlCard.Controls.Add(lblTitle);
            pnlCard.Controls.Add(lblCurrent); pnlCard.Controls.Add(pnlCurrent);
            pnlCard.Controls.Add(lblNew); pnlCard.Controls.Add(pnlNew);
            pnlCard.Controls.Add(lblConfirm); pnlCard.Controls.Add(pnlConfirm);
            pnlCard.Controls.Add(chkShowPasswords); pnlCard.Controls.Add(btnSave); pnlCard.Controls.Add(lblStatus);

            this.Controls.Add(pnlCard);
            this.Resize += (s, e) => CenterCard();
            this.Load += (s, e) => CenterCard();
            CenterCard();
        }

        private void CenterCard()
        {
            pnlCard.Left = (this.ClientSize.Width - pnlCard.Width) / 2;
            pnlCard.Top = (this.ClientSize.Height - pnlCard.Height) / 2;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string current = txtCurrentPassword.Text; string newPass = txtNewPassword.Text; string confirm = txtConfirmPassword.Text;
            if (newPass != confirm) { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = "Yeni şifreler birbiriyle eşleşmiyor."; return; }
            if (_authService.ChangePassword(_user.Id, current, newPass, out string errorMessage))
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "Şifreniz başarıyla güncellendi.";
                txtCurrentPassword.Clear(); txtNewPassword.Clear(); txtConfirmPassword.Clear();
            }
            else { lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = errorMessage; }
        }

        protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } }
        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) => SetupSmoothContainer(pnl, radius, bgColor, AppBackColor);
        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor, Color clearColor) { pnl.BackColor = clearColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(clearColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } }; pnl.SizeChanged += (s, e) => pnl.Invalidate(); }
        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius) { var path = new System.Drawing.Drawing2D.GraphicsPath(); int d = Math.Max(radius * 2, 1); path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path; }
    }
}