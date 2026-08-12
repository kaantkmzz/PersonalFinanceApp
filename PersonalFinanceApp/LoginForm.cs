using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;
using PersonalFinanceApp.Helpers;

namespace PersonalFinanceApp
{
    public partial class LoginForm : Form
    {
        private readonly AuthService _authService = new AuthService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color FieldBackColor => AppTheme.HoverBackColor;

        private Panel pnlCard = new Panel();
        private TextBox txtUsernameOrEmail = new TextBox();
        private TextBox txtPassword = new TextBox();
        private Panel pnlTogglePassword = new Panel();
        private bool _passwordVisible = false;
        private CheckBox chkRememberMe = new CheckBox();
        private Button btnLogin = new Button();
        private Button btnGoToRegister = new Button();
        private Label lblStatus = new Label();

        public LoginForm()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Kişisel Finans Takip Sistemi";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(900, 600);
            this.Font = new Font("Segoe UI", 10F);
            this.Resize += (s, e) => CenterCard();
            this.FormClosing += LoginForm_FormClosing;

            BuildCard();
            this.Load += (s, e) => DarkTitleBarHelper.SetTitleBarDarkMode(this, AppTheme.IsDark);
            TryAutoLogin();
        }

        // Kartı ve içindeki tüm denetimleri geçerli AppTheme renkleriyle sıfırdan kurar.
        // Oturum kapatılıp bu ekrana dönüldüğünde tema (dark/light) değişmiş olabileceğinden,
        // önceki kartı atıp yeniden kuruyoruz (canlı renk mutasyonu yerine MainForm'daki
        // tema değişimi ile aynı "yeniden kur" yaklaşımı).
        private void BuildCard()
        {
            this.SuspendLayout();

            this.Controls.Remove(pnlCard);
            pnlCard.Dispose();
            pnlCard = new Panel { Width = 520, Height = 460 };
            SetupSmoothContainer(pnlCard, 16, CardBackColor, AppBackColor);

            this.BackColor = AppBackColor;

            Label lblTitle = new Label
            {
                Text = "Kişisel Finans Takip Sistemi",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = TextLight,
                BackColor = Color.Transparent,
                AutoSize = true,
                Left = 45,
                Top = 35
            };

            Label lblUsername = new Label { Text = "Kullanıcı Adı / E-posta:", Left = 45, Top = 100, Font = new Font("Segoe UI", 11F), ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlUsername = new Panel { Left = 45, Top = 130, Width = 430, Height = 42 };
            SetupSmoothContainer(pnlUsername, 10, FieldBackColor, CardBackColor);
            txtUsernameOrEmail = new TextBox { Left = 14, Top = 12, Width = 402, Font = new Font("Segoe UI", 10.5F), BorderStyle = BorderStyle.None, BackColor = FieldBackColor, ForeColor = TextLight };
            pnlUsername.Controls.Add(txtUsernameOrEmail);

            Label lblPassword = new Label { Text = "Şifre:", Left = 45, Top = 190, Font = new Font("Segoe UI", 11F), ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlPassword = new Panel { Left = 45, Top = 220, Width = 430, Height = 42 };
            SetupSmoothContainer(pnlPassword, 10, FieldBackColor, CardBackColor);
            txtPassword = new TextBox { Left = 14, Top = 12, Width = 366, Font = new Font("Segoe UI", 10.5F), BorderStyle = BorderStyle.None, BackColor = FieldBackColor, ForeColor = TextLight, UseSystemPasswordChar = true };
            pnlPassword.Controls.Add(txtPassword);

            _passwordVisible = false;
            pnlTogglePassword = new Panel { Size = new Size(26, 26), Left = pnlPassword.Width - 34, Top = 8, Cursor = Cursors.Hand, BackColor = Color.Transparent };
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

            chkRememberMe = new CheckBox { Text = "Beni hatırla", Left = 45, Top = 275, AutoSize = true, ForeColor = TextMuted, BackColor = Color.Transparent };

            btnLogin = new Button { Text = "Giriş Yap", Left = 45, Top = 320, Width = 205, Height = 44, Font = new Font("Segoe UI", 10.5F), Cursor = Cursors.Hand };
            SetupRoundedButton(btnLogin, AccentColor, Color.White);
            btnLogin.Click += BtnLogin_Click;

            btnGoToRegister = new Button { Text = "Kayıt Ol", Left = 270, Top = 320, Width = 205, Height = 44, Font = new Font("Segoe UI", 10.5F), Cursor = Cursors.Hand };
            SetupRoundedButton(btnGoToRegister, FieldBackColor, TextLight);
            btnGoToRegister.Click += BtnGoToRegister_Click;

            lblStatus = new Label
            {
                Left = 45,
                Top = 385,
                Width = 430,
                Height = 70,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(255, 120, 120),
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlCard.Controls.Add(lblTitle);
            pnlCard.Controls.Add(lblUsername);
            pnlCard.Controls.Add(pnlUsername);
            pnlCard.Controls.Add(lblPassword);
            pnlCard.Controls.Add(pnlPassword);
            pnlCard.Controls.Add(chkRememberMe);
            pnlCard.Controls.Add(btnLogin);
            pnlCard.Controls.Add(btnGoToRegister);
            pnlCard.Controls.Add(lblStatus);

            this.Controls.Add(pnlCard);
            this.AcceptButton = btnLogin;

            txtUsernameOrEmail.PreviewKeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) e.IsInputKey = true; };
            txtUsernameOrEmail.KeyDown += (s, e) =>
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
                    btnLogin.PerformClick();
                }
            };

            CenterCard();

            if (this.IsHandleCreated)
                DarkTitleBarHelper.SetTitleBarDarkMode(this, AppTheme.IsDark);

            this.ResumeLayout(true);
        }

        private void CenterCard()
        {
            pnlCard.Left = (this.ClientSize.Width - pnlCard.Width) / 2;
            pnlCard.Top = (this.ClientSize.Height - pnlCard.Height) / 2;
        }

        // Uygulama tamamen kapanmadan önce kullanıcıya onay soruyoruz
        private void LoginForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            var result = MessageBox.Show("Uygulamadan çıkmak istediğinize emin misiniz?", "Çıkış Onayı",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            PerformLogin();
        }

        private bool PerformLogin()
        {
            string usernameOrEmail = txtUsernameOrEmail.Text;
            string password = txtPassword.Text;

            User? user = _authService.Login(usernameOrEmail, password, out string errorMessage);

            if (user != null)
            {
                if (chkRememberMe.Checked)
                {
                    RememberMeHelper.Save(usernameOrEmail, password);
                }
                else
                {
                    RememberMeHelper.Clear();
                }

                this.Hide();

                if (!user.OnboardingCompleted)
                {
                    using (var onboarding = new OnboardingForm(user.Id))
                    {
                        onboarding.ShowDialog();
                    }
                    user.OnboardingCompleted = true;
                }

                var recurringService = new RecurringTransactionService();
                var (addedRecurring, failedRecurring) = recurringService.ProcessDueRecurring(user.Id);

                var infoMessages = new List<string>();
                if (addedRecurring.Count > 0) infoMessages.Add("Şu tekrarlanan işlemler eklendi:\n- " + string.Join("\n- ", addedRecurring));
                if (failedRecurring.Count > 0) infoMessages.Add("Şu tekrarlanan işlemler eklenemedi:\n- " + string.Join("\n- ", failedRecurring));

                if (infoMessages.Count > 0)
                {
                    MessageBox.Show(string.Join("\n\n", infoMessages), "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                MainForm mainForm = new MainForm(user);
                mainForm.ShowDialog();

                if (mainForm.ExitRequested)
                {
                    Environment.Exit(0);
                    return true;
                }

                if (this.IsDisposed)
                {
                    return true;
                }

                BuildCard();
                lblStatus.Text = string.Empty;
                this.Show();

                return true;
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 120, 120);
                lblStatus.Text = errorMessage;
                return false;
            }
        }

        // Uygulama açılışında, daha önce "Beni hatırla" işaretlenmişse otomatik giriş dener
        private void TryAutoLogin()
        {
            var saved = RememberMeHelper.Load();
            if (saved != null)
            {
                txtUsernameOrEmail.Text = saved.Value.Username;
                txtPassword.Text = saved.Value.Password;
                chkRememberMe.Checked = true;

                bool success = PerformLogin();

                if (!success)
                {
                    // Kayıtlı bilgiler artık geçersizse (örn. şifre değişmiş), temizleyip normal girişe dön
                    RememberMeHelper.Clear();
                    txtUsernameOrEmail.Clear();
                    txtPassword.Clear();
                    chkRememberMe.Checked = false;
                    lblStatus.Text = string.Empty;
                }
            }
        }

        private void BtnGoToRegister_Click(object? sender, EventArgs e)
        {
            RegisterForm registerForm = new RegisterForm();
            registerForm.ShowDialog();
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
