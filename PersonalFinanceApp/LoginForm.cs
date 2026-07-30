using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class LoginForm : Form
    {
        private readonly AuthService _authService = new AuthService();

        private Panel pnlCard = new Panel();
        private TextBox txtUsernameOrEmail = new TextBox();
        private TextBox txtPassword = new TextBox();
        private Button btnLogin = new Button();
        private Button btnGoToRegister = new Button();
        private Label lblStatus = new Label();

        public LoginForm()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Kişisel Finans Takip Sistemi";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(900, 600);
            this.BackColor = Color.FromArgb(230, 232, 242);
            this.Font = new Font("Segoe UI", 10F);

            pnlCard.Width = 460;
            pnlCard.Height = 380;
            pnlCard.BackColor = Color.White;
            pnlCard.BorderStyle = BorderStyle.FixedSingle;

            Label lblTitle = new Label
            {
                Text = "Kişisel Finans Takip Sistemi",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = true,
                Left = 40,
                Top = 30
            };

            Label lblUsername = new Label { Text = "Kullanıcı Adı / E-posta:", Left = 40, Top = 90, Font = new Font("Segoe UI", 11F), AutoSize = true };
            txtUsernameOrEmail.Left = 40;
            txtUsernameOrEmail.Top = 115;
            txtUsernameOrEmail.Width = 380;
            txtUsernameOrEmail.Font = new Font("Segoe UI", 11F);

            Label lblPassword = new Label { Text = "Şifre:", Left = 40, Top = 160, Font = new Font("Segoe UI", 11F), AutoSize = true };
            txtPassword.Left = 40;
            txtPassword.Top = 185;
            txtPassword.Width = 380;
            txtPassword.Font = new Font("Segoe UI", 11F);
            txtPassword.PasswordChar = '*';

            btnLogin.Text = "Giriş Yap";
            btnLogin.Left = 40;
            btnLogin.Top = 235;
            btnLogin.Width = 180;
            btnLogin.Height = 42;
            btnLogin.Font = new Font("Segoe UI", 10.5F);
            btnLogin.Click += BtnLogin_Click;

            btnGoToRegister.Text = "Kayıt Ol";
            btnGoToRegister.Left = 240;
            btnGoToRegister.Top = 235;
            btnGoToRegister.Width = 180;
            btnGoToRegister.Height = 42;
            btnGoToRegister.Font = new Font("Segoe UI", 10.5F);
            btnGoToRegister.Click += BtnGoToRegister_Click;

            lblStatus.Left = 40;
            lblStatus.Top = 295;
            lblStatus.Width = 380;
            lblStatus.Height = 60;
            lblStatus.ForeColor = Color.Red;
            lblStatus.Font = new Font("Segoe UI", 9.5F);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            pnlCard.Controls.Add(lblTitle);
            pnlCard.Controls.Add(lblUsername);
            pnlCard.Controls.Add(txtUsernameOrEmail);
            pnlCard.Controls.Add(lblPassword);
            pnlCard.Controls.Add(txtPassword);
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
        }

        private void CenterCard()
        {
            pnlCard.Left = (this.ClientSize.Width - pnlCard.Width) / 2;
            pnlCard.Top = (this.ClientSize.Height - pnlCard.Height) / 2;
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            string usernameOrEmail = txtUsernameOrEmail.Text;
            string password = txtPassword.Text;

            User? user = _authService.Login(usernameOrEmail, password, out string errorMessage);

            if (user != null)
            {
                this.Hide();
                MainForm mainForm = new MainForm(user);
                mainForm.ShowDialog();
                this.Close();
            }
            else
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnGoToRegister_Click(object? sender, EventArgs e)
        {
            RegisterForm registerForm = new RegisterForm();
            registerForm.ShowDialog();
        }
    }
}