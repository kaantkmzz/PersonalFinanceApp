using PersonalFinanceApp.Services;
using PersonalFinanceApp.Helpers;

namespace PersonalFinanceApp
{
    public partial class RegisterForm : Form
    {
        private readonly AuthService _authService = new AuthService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color FieldBackColor => AppTheme.HoverBackColor;

        private Panel pnlCard = new Panel();
        private TextBox txtUsername = new TextBox();
        private TextBox txtEmail = new TextBox();
        private TextBox txtPassword = new TextBox();
        private Panel pnlTogglePassword = new Panel();
        private bool _passwordVisible = false;
        private Button btnRegister = new Button();
        private Label lblStatus = new Label();

        public RegisterForm()
        {
            InitializeComponent();
            SetupUI();
            this.Load += (s, e) => DarkTitleBarHelper.SetTitleBarDarkMode(this, AppTheme.IsDark);
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Kayıt Ol";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(900, 600);
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 10F);

            // --- Sol üstte, LoginForm'a geri dönüş linki ---
            Button btnBackToLogin = new Button
            {
                Text = "← Giriş Ekranına Dön",
                Left = 30,
                Top = 25,
                Width = 200,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = AppBackColor,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnBackToLogin.FlatAppearance.BorderSize = 0;
            btnBackToLogin.Click += (s, e) => this.Close();
            this.Controls.Add(btnBackToLogin);

            pnlCard.Width = 520;
            pnlCard.Height = 540;
            SetupSmoothContainer(pnlCard, 16, CardBackColor, AppBackColor);

            Label lblTitle = new Label
            {
                Text = "Yeni Hesap Oluştur",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = TextLight,
                BackColor = Color.Transparent,
                AutoSize = true,
                Left = 45,
                Top = 35
            };

            Label lblUsername = new Label { Text = "Kullanıcı Adı:", Left = 45, Top = 100, Font = new Font("Segoe UI", 11F), ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlUsername = new Panel { Left = 45, Top = 130, Width = 430, Height = 42 };
            SetupSmoothContainer(pnlUsername, 10, FieldBackColor, CardBackColor);
            txtUsername.Left = 14; txtUsername.Top = 12; txtUsername.Width = 402;
            txtUsername.Font = new Font("Segoe UI", 10.5F); txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.BackColor = FieldBackColor; txtUsername.ForeColor = TextLight;
            pnlUsername.Controls.Add(txtUsername);

            Label lblEmail = new Label { Text = "E-posta:", Left = 45, Top = 190, Font = new Font("Segoe UI", 11F), ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlEmail = new Panel { Left = 45, Top = 220, Width = 430, Height = 42 };
            SetupSmoothContainer(pnlEmail, 10, FieldBackColor, CardBackColor);
            txtEmail.Left = 14; txtEmail.Top = 12; txtEmail.Width = 402;
            txtEmail.Font = new Font("Segoe UI", 10.5F); txtEmail.BorderStyle = BorderStyle.None;
            txtEmail.BackColor = FieldBackColor; txtEmail.ForeColor = TextLight;
            pnlEmail.Controls.Add(txtEmail);

            Label lblPassword = new Label { Text = "Şifre (en az 6 karakter):", Left = 45, Top = 280, Font = new Font("Segoe UI", 11F), ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlPassword = new Panel { Left = 45, Top = 310, Width = 430, Height = 42 };
            SetupSmoothContainer(pnlPassword, 10, FieldBackColor, CardBackColor);
            txtPassword.Left = 14; txtPassword.Top = 12; txtPassword.Width = 366;
            txtPassword.Font = new Font("Segoe UI", 10.5F); txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.BackColor = FieldBackColor; txtPassword.ForeColor = TextLight;
            txtPassword.UseSystemPasswordChar = true;
            pnlPassword.Controls.Add(txtPassword);

            pnlTogglePassword.Size = new Size(26, 26);
            pnlTogglePassword.Left = pnlPassword.Width - 34;
            pnlTogglePassword.Top = 8;
            pnlTogglePassword.Cursor = Cursors.Hand;
            pnlTogglePassword.BackColor = Color.Transparent;
            pnlTogglePassword.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                DrawEyeIcon(e.Graphics, pnlTogglePassword.ClientRectangle, !_passwordVisible);
            };
            pnlTogglePassword.Click += (s, e) =>
            {
                _passwordVisible = !_passwordVisible;
                txtPassword.UseSystemPasswordChar = !_passwordVisible;
                pnlTogglePassword.Invalidate();
            };
            pnlPassword.Controls.Add(pnlTogglePassword);

            btnRegister.Text = "Kayıt Ol";
            btnRegister.Left = 45;
            btnRegister.Top = 375;
            btnRegister.Width = 430;
            btnRegister.Height = 44;
            btnRegister.Font = new Font("Segoe UI", 10.5F);
            btnRegister.Cursor = Cursors.Hand;
            SetupRoundedButton(btnRegister, AccentColor, Color.White);
            btnRegister.Click += BtnRegister_Click;

            lblStatus.Left = 45;
            lblStatus.Top = 430;
            lblStatus.Width = 430;
            lblStatus.Height = 90;
            lblStatus.BackColor = Color.Transparent;
            lblStatus.ForeColor = Color.FromArgb(255, 120, 120);
            lblStatus.Font = new Font("Segoe UI", 9.5F);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            pnlCard.Controls.Add(lblTitle);
            pnlCard.Controls.Add(lblUsername);
            pnlCard.Controls.Add(pnlUsername);
            pnlCard.Controls.Add(lblEmail);
            pnlCard.Controls.Add(pnlEmail);
            pnlCard.Controls.Add(lblPassword);
            pnlCard.Controls.Add(pnlPassword);
            pnlCard.Controls.Add(btnRegister);
            pnlCard.Controls.Add(lblStatus);

            this.Controls.Add(pnlCard);

            this.AcceptButton = btnRegister;
            SetupEnterNavigation();

            this.Resize += (s, e) => CenterCard();
            CenterCard();
        }

        private void CenterCard()
        {
            pnlCard.Left = (this.ClientSize.Width - pnlCard.Width) / 2;
            pnlCard.Top = (this.ClientSize.Height - pnlCard.Height) / 2;
        }

        private void SetupEnterNavigation()
        {
            txtUsername.PreviewKeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) e.IsInputKey = true; };
            txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    txtEmail.Focus();
                }
            };

            txtEmail.PreviewKeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) e.IsInputKey = true; };
            txtEmail.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    txtPassword.Focus();
                }
            };

            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    btnRegister.PerformClick();
                }
            };
        }

        private void BtnRegister_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string email = txtEmail.Text;
            string password = txtPassword.Text;

            bool success = _authService.Register(username, email, password, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Kayıt başarılı! Giriş ekranına dönüp giriş yapabilirsiniz.";
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 120, 120);
                lblStatus.Text = errorMessage;
            }
        }

        // Tutarları gizle/göster butonuyla aynı mantıkta çalışan göz ikonu (açık/kapalı göz)
        private static void DrawEyeIcon(Graphics g, Rectangle r, bool hidden)
        {
            Color iconColor = TextMuted;
            using var pen = new Pen(iconColor, 1.6f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            int w = (int)(r.Width * 0.72);
            int h = (int)(r.Height * 0.4);
            var eyeRect = new Rectangle(r.Left + (r.Width - w) / 2, r.Top + (r.Height - h) / 2, w, h);
            g.DrawEllipse(pen, eyeRect);

            int pupilSize = Math.Max(2, h / 2);
            var pupilRect = new Rectangle(eyeRect.Left + (w - pupilSize) / 2, eyeRect.Top + (h - pupilSize) / 2, pupilSize, pupilSize);
            using var brush = new SolidBrush(iconColor);
            g.FillEllipse(brush, pupilRect);

            if (hidden)
            {
                g.DrawLine(pen, r.Left + r.Width / 2 - w / 2 - 1, r.Top + r.Height / 2 + h / 2 + 1, r.Left + r.Width / 2 + w / 2 + 1, r.Top + r.Height / 2 - h / 2 - 1);
            }
        }

        // --- GÖRSEL YARDIMCI METOTLAR ---
        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor, Color clearColor)
        {
            pnl.BackColor = clearColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(clearColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius);
                using var brush = new SolidBrush(bgColor);
                e.Graphics.FillPath(brush, path);
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                using var brush = new SolidBrush(bgColor);
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
