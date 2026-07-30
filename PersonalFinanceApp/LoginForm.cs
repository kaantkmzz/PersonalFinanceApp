using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class LoginForm : Form
    {
        private readonly AuthService _authService = new AuthService();

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
            this.AutoScaleMode = AutoScaleMode.None;
            this.Font = new Font("Segoe UI", 9F);
            this.Text = "Kişisel Finans Takip Sistemi";
            this.Width = 420;
            this.Height = 320;
            this.StartPosition = FormStartPosition.CenterScreen;

            int controlWidth = 340;
            int centerLeft = (this.ClientSize.Width - controlWidth) / 2;

            Label lblUsername = new Label { Text = "Kullanıcı Adı / E-posta:", Left = centerLeft, Top = 30, Width = controlWidth, AutoSize = true };
            txtUsernameOrEmail.Left = centerLeft;
            txtUsernameOrEmail.Top = 55;
            txtUsernameOrEmail.Width = controlWidth;

            Label lblPassword = new Label { Text = "Şifre:", Left = centerLeft, Top = 95, Width = controlWidth, AutoSize = true };
            txtPassword.Left = centerLeft;
            txtPassword.Top = 120;
            txtPassword.Width = controlWidth;
            txtPassword.PasswordChar = '*';

            int buttonWidth = 160;
            int buttonGap = 20;
            int totalButtonsWidth = buttonWidth * 2 + buttonGap;
            int buttonsStartLeft = (this.ClientSize.Width - totalButtonsWidth) / 2;

            btnLogin.Text = "Giriş Yap";
            btnLogin.Left = buttonsStartLeft;
            btnLogin.Top = 165;
            btnLogin.Width = buttonWidth;
            btnLogin.Height = 35;
            btnLogin.Click += BtnLogin_Click;

            btnGoToRegister.Text = "Kayıt Ol";
            btnGoToRegister.Left = buttonsStartLeft + buttonWidth + buttonGap;
            btnGoToRegister.Top = 165;
            btnGoToRegister.Width = buttonWidth;
            btnGoToRegister.Height = 35;
            btnGoToRegister.Click += BtnGoToRegister_Click;

            lblStatus.Left = centerLeft;
            lblStatus.Top = 215;
            lblStatus.Width = controlWidth;
            lblStatus.Height = 60;
            lblStatus.ForeColor = Color.Red;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            this.Controls.Add(lblUsername);
            this.Controls.Add(txtUsernameOrEmail);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
            this.Controls.Add(btnGoToRegister);
            this.Controls.Add(lblStatus);

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