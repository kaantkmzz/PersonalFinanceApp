using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class RegisterForm : Form
    {
        private readonly AuthService _authService = new AuthService();

        private TextBox txtUsername = new TextBox();
        private TextBox txtEmail = new TextBox();
        private TextBox txtPassword = new TextBox();
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
            this.Font = new Font("Segoe UI", 9F);
            this.Text = "Kayıt Ol";
            this.Width = 460;
            this.Height = 460;
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblUsername = new Label { Text = "Kullanıcı Adı:", Left = 30, Top = 30, Width = 250, AutoSize = true };
            txtUsername.Left = 30;
            txtUsername.Top = 55;
            txtUsername.Width = 340;

            Label lblEmail = new Label { Text = "E-posta:", Left = 30, Top = 95, Width = 250, AutoSize = true };
            txtEmail.Left = 30;
            txtEmail.Top = 120;
            txtEmail.Width = 340;

            Label lblPassword = new Label { Text = "Şifre (en az 6 karakter):", Left = 30, Top = 160, Width = 250, AutoSize = true };
            txtPassword.Left = 30;
            txtPassword.Top = 185;
            txtPassword.Width = 340;
            txtPassword.PasswordChar = '*';

            btnRegister.Text = "Kayıt Ol";
            btnRegister.Left = 30;
            btnRegister.Top = 230;
            btnRegister.Width = 160;
            btnRegister.Height = 35;
            btnRegister.Click += BtnRegister_Click;

            lblStatus.Left = 30;
            lblStatus.Top = 275;
            lblStatus.Width = 400;
            lblStatus.Height = 60;
            lblStatus.ForeColor = Color.Red;

            this.Controls.Add(lblUsername);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblEmail);
            this.Controls.Add(txtEmail);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnRegister);
            this.Controls.Add(lblStatus);

            this.AcceptButton = btnRegister;
            SetupEnterNavigation();
        }

        // Enter tuşu ile alanlar arası geçişi ve son alanda kayıt işlemini tetikleme
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
                lblStatus.ForeColor = Color.Green;
                lblStatus.Text = "Kayıt başarılı! Bu pencereyi kapatıp giriş yapabilirsiniz.";
            }
            else
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = errorMessage;
            }
        }
    }
}