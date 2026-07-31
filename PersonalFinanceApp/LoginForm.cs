using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;
using PersonalFinanceApp.Helpers;

namespace PersonalFinanceApp
{
    public partial class LoginForm : Form
    {
        private readonly AuthService _authService = new AuthService();

        private static readonly Color AppBackColor = Color.FromArgb(24, 27, 38);
        private static readonly Color CardBackColor = Color.FromArgb(37, 41, 59);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);

        private Panel pnlCard = new Panel();
        private TextBox txtUsernameOrEmail = new TextBox();
        private TextBox txtPassword = new TextBox();
        private CheckBox chkShowPassword = new CheckBox();
        private Button btnLogin = new Button();
        private Button btnGoToRegister = new Button();
        private Label lblStatus = new Label();
        private CheckBox chkRememberMe = new CheckBox();

        public LoginForm()
        {
            InitializeComponent();
            SetupUI();
            TryAutoLogin();   // ← BU SATIRI EKLE
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Kişisel Finans Takip Sistemi";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(900, 600);
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 10F);

            pnlCard.Width = 520;
            pnlCard.Height = 440;
            pnlCard.BackColor = CardBackColor;

            Label lblTitle = new Label
            {
                Text = "Kişisel Finans Takip Sistemi",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = TextLight,
                AutoSize = true,
                Left = 45,
                Top = 35
            };

            Label lblUsername = new Label { Text = "Kullanıcı Adı / E-posta:", Left = 45, Top = 100, Font = new Font("Segoe UI", 11F), ForeColor = TextMuted, AutoSize = true };
            txtUsernameOrEmail.Left = 45;
            txtUsernameOrEmail.Top = 135;
            txtUsernameOrEmail.Width = 430;

            Label lblPassword = new Label { Text = "Şifre:", Left = 45, Top = 180, Font = new Font("Segoe UI", 11F), ForeColor = TextMuted, AutoSize = true };
            txtPassword.Left = 45;
            txtPassword.Top = 210;
            txtPassword.Width = 430;

            chkShowPassword.Text = "Şifreyi göster";
            chkShowPassword.Left = 45;
            chkShowPassword.Top = 245;
            chkShowPassword.AutoSize = true;
            chkShowPassword.ForeColor = TextMuted;
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                txtPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '*';
            };

            chkRememberMe.Text = "Beni hatırla";
            chkRememberMe.Left = 250;
            chkRememberMe.Top = 245;
            chkRememberMe.AutoSize = true;
            chkRememberMe.ForeColor = TextMuted;

            btnLogin.Text = "Giriş Yap";
            btnLogin.Left = 45;
            btnLogin.Top = 290;
            btnLogin.Width = 205;
            btnLogin.Height = 44;
            btnLogin.Font = new Font("Segoe UI", 10.5F);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.BackColor = AccentColor;
            btnLogin.ForeColor = Color.White;
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Click += BtnLogin_Click;

            btnGoToRegister.Text = "Kayıt Ol";
            btnGoToRegister.Left = 270;
            btnGoToRegister.Top = 290;
            btnGoToRegister.Width = 205;
            btnGoToRegister.Height = 44;
            btnGoToRegister.Font = new Font("Segoe UI", 10.5F);
            btnGoToRegister.FlatStyle = FlatStyle.Flat;
            btnGoToRegister.FlatAppearance.BorderSize = 1;
            btnGoToRegister.FlatAppearance.BorderColor = TextMuted;
            btnGoToRegister.BackColor = CardBackColor;
            btnGoToRegister.ForeColor = TextLight;
            btnGoToRegister.Cursor = Cursors.Hand;
            btnGoToRegister.Click += BtnGoToRegister_Click;

            lblStatus.Left = 45;
            lblStatus.Top = 355;
            lblStatus.Width = 430;
            lblStatus.Height = 70;
            lblStatus.ForeColor = Color.FromArgb(255, 120, 120);
            lblStatus.Font = new Font("Segoe UI", 9.5F);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            pnlCard.Controls.Add(lblTitle);
            pnlCard.Controls.Add(lblUsername);
            pnlCard.Controls.Add(txtUsernameOrEmail);
            pnlCard.Controls.Add(lblPassword);
            pnlCard.Controls.Add(txtPassword);
            pnlCard.Controls.Add(chkShowPassword);
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

            this.Resize += (s, e) => CenterCard();
            CenterCard();

            this.FormClosing += LoginForm_FormClosing;
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
                MainForm mainForm = new MainForm(user);
                mainForm.ShowDialog();

                if (this.IsDisposed)
                {
                    return true;
                }

                txtUsernameOrEmail.Clear();
                txtPassword.Clear();
                chkRememberMe.Checked = false;
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
    }
}