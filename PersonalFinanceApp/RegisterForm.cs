using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class RegisterForm : Form
    {
        private readonly AuthService _authService = new AuthService();

        private static readonly Color AppBackColor = Color.FromArgb(24, 27, 38);
        private static readonly Color CardBackColor = Color.FromArgb(37, 41, 59);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);

        private Panel pnlCard = new Panel();
        private TextBox txtUsername = new TextBox();
        private TextBox txtEmail = new TextBox();
        private TextBox txtPassword = new TextBox();
        private CheckBox chkShowPassword = new CheckBox();
        private Button btnRegister = new Button();
        private Label lblStatus = new Label();

        public RegisterForm()
        {
            InitializeComponent();
            SetupUI();
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
            pnlCard.Height = 520;
            pnlCard.BackColor = CardBackColor;

            Label lblTitle = new Label
            {
                Text = "Yeni Hesap Oluştur",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = TextLight,
                AutoSize = true,
                Left = 45,
                Top = 35
            };

            Label lblUsername = new Label { Text = "Kullanıcı Adı:", Left = 45, Top = 100, Font = new Font("Segoe UI", 11F), ForeColor = TextMuted, AutoSize = true };
            txtUsername.Left = 45;
            txtUsername.Top = 135;
            txtUsername.Width = 430;
            txtUsername.Font = new Font("Segoe UI", 11F);

            Label lblEmail = new Label { Text = "E-posta:", Left = 45, Top = 180, Font = new Font("Segoe UI", 11F), ForeColor = TextMuted, AutoSize = true };
            txtEmail.Left = 45;
            txtEmail.Top = 210;
            txtEmail.Width = 430;
            txtEmail.Font = new Font("Segoe UI", 11F);

            Label lblPassword = new Label { Text = "Şifre (en az 6 karakter):", Left = 45, Top = 255, Font = new Font("Segoe UI", 11F), ForeColor = TextMuted, AutoSize = true };
            txtPassword.Left = 45;
            txtPassword.Top = 285;
            txtPassword.Width = 430;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Font = new Font("Segoe UI", 11F);

            chkShowPassword.Text = "Şifreyi göster";
            chkShowPassword.Left = 45;
            chkShowPassword.Top = 320;
            chkShowPassword.AutoSize = true;
            chkShowPassword.ForeColor = TextMuted;
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
            };

            btnRegister.Text = "Kayıt Ol";
            btnRegister.Left = 45;
            btnRegister.Top = 360;
            btnRegister.Width = 430;
            btnRegister.Height = 44;
            btnRegister.Font = new Font("Segoe UI", 10.5F);
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.BackColor = AccentColor;
            btnRegister.ForeColor = Color.White;
            btnRegister.Cursor = Cursors.Hand;
            btnRegister.Click += BtnRegister_Click;

            lblStatus.Left = 45;
            lblStatus.Top = 415;
            lblStatus.Width = 430;
            lblStatus.Height = 90;
            lblStatus.ForeColor = Color.FromArgb(255, 120, 120);
            lblStatus.Font = new Font("Segoe UI", 9.5F);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            pnlCard.Controls.Add(lblTitle);
            pnlCard.Controls.Add(lblUsername);
            pnlCard.Controls.Add(txtUsername);
            pnlCard.Controls.Add(lblEmail);
            pnlCard.Controls.Add(txtEmail);
            pnlCard.Controls.Add(lblPassword);
            pnlCard.Controls.Add(txtPassword);
            pnlCard.Controls.Add(chkShowPassword);
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
    }
}