using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class RegisterForm : Form
    {
        private readonly AuthService _authService = new AuthService();

        private Panel pnlCard = new Panel();
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
            this.Text = "Kayıt Ol";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(900, 600);
            this.BackColor = Color.FromArgb(230, 232, 242);
            this.Font = new Font("Segoe UI", 10F);

            pnlCard.Width = 460;
            pnlCard.Height = 460;
            pnlCard.BackColor = Color.White;
            pnlCard.BorderStyle = BorderStyle.FixedSingle;

            Label lblTitle = new Label
            {
                Text = "Yeni Hesap Oluştur",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = true,
                Left = 40,
                Top = 30
            };

            Label lblUsername = new Label { Text = "Kullanıcı Adı:", Left = 40, Top = 90, Font = new Font("Segoe UI", 11F), AutoSize = true };
            txtUsername.Left = 40;
            txtUsername.Top = 115;
            txtUsername.Width = 380;
            txtUsername.Font = new Font("Segoe UI", 11F);

            Label lblEmail = new Label { Text = "E-posta:", Left = 40, Top = 160, Font = new Font("Segoe UI", 11F), AutoSize = true };
            txtEmail.Left = 40;
            txtEmail.Top = 185;
            txtEmail.Width = 380;
            txtEmail.Font = new Font("Segoe UI", 11F);

            Label lblPassword = new Label { Text = "Şifre (en az 6 karakter):", Left = 40, Top = 230, Font = new Font("Segoe UI", 11F), AutoSize = true };
            txtPassword.Left = 40;
            txtPassword.Top = 255;
            txtPassword.Width = 380;
            txtPassword.Font = new Font("Segoe UI", 11F);
            txtPassword.PasswordChar = '*';

            btnRegister.Text = "Kayıt Ol";
            btnRegister.Left = 40;
            btnRegister.Top = 305;
            btnRegister.Width = 380;
            btnRegister.Height = 42;
            btnRegister.Font = new Font("Segoe UI", 10.5F);
            btnRegister.Click += BtnRegister_Click;

            lblStatus.Left = 40;
            lblStatus.Top = 360;
            lblStatus.Width = 380;
            lblStatus.Height = 80;
            lblStatus.ForeColor = Color.Red;
            lblStatus.Font = new Font("Segoe UI", 9.5F);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            pnlCard.Controls.Add(lblTitle);
            pnlCard.Controls.Add(lblUsername);
            pnlCard.Controls.Add(txtUsername);
            pnlCard.Controls.Add(lblEmail);
            pnlCard.Controls.Add(txtEmail);
            pnlCard.Controls.Add(lblPassword);
            pnlCard.Controls.Add(txtPassword);
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