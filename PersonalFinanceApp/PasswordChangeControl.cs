using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class PasswordChangeControl : UserControl
    {
        private readonly User _user;
        private readonly AuthService _authService = new AuthService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);

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
            pnlCard.Height = 420;
            pnlCard.BackColor = CardBackColor;

            Label lblTitle = new Label
            {
                Text = "Şifre Değiştir",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 40,
                Top = 30,
                AutoSize = true
            };

            Label lblCurrent = new Label { Text = "Mevcut Şifre:", Left = 40, Top = 90, ForeColor = TextMuted, AutoSize = true };
            txtCurrentPassword.Left = 40;
            txtCurrentPassword.Top = 120;
            txtCurrentPassword.Width = 380;
            txtCurrentPassword.Font = new Font("Segoe UI", 10.5F);
            txtCurrentPassword.UseSystemPasswordChar = true;

            Label lblNew = new Label { Text = "Yeni Şifre (en az 6 karakter):", Left = 40, Top = 165, ForeColor = TextMuted, AutoSize = true };
            txtNewPassword.Left = 40;
            txtNewPassword.Top = 195;
            txtNewPassword.Width = 380;
            txtNewPassword.Font = new Font("Segoe UI", 10.5F);
            txtNewPassword.UseSystemPasswordChar = true;

            Label lblConfirm = new Label { Text = "Yeni Şifre (tekrar):", Left = 40, Top = 240, ForeColor = TextMuted, AutoSize = true };
            txtConfirmPassword.Left = 40;
            txtConfirmPassword.Top = 270;
            txtConfirmPassword.Width = 380;
            txtConfirmPassword.Font = new Font("Segoe UI", 10.5F);
            txtConfirmPassword.UseSystemPasswordChar = true;

            chkShowPasswords.Text = "Şifreleri göster";
            chkShowPasswords.Left = 40;
            chkShowPasswords.Top = 305;
            chkShowPasswords.AutoSize = true;
            chkShowPasswords.ForeColor = TextMuted;
            chkShowPasswords.CheckedChanged += (s, e) =>
            {
                bool show = chkShowPasswords.Checked;
                txtCurrentPassword.UseSystemPasswordChar = !show;
                txtNewPassword.UseSystemPasswordChar = !show;
                txtConfirmPassword.UseSystemPasswordChar = !show;
            };

            btnSave.Text = "Şifreyi Güncelle";
            btnSave.Left = 40;
            btnSave.Top = 340;
            btnSave.Width = 380;
            btnSave.Height = 40;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.BackColor = AccentColor;
            btnSave.ForeColor = Color.White;
            btnSave.Cursor = Cursors.Hand;
            btnSave.Click += BtnSave_Click;

            lblStatus.Left = 40;
            lblStatus.Top = 390;
            lblStatus.Width = 380;
            lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F);

            pnlCard.Controls.Add(lblTitle);
            pnlCard.Controls.Add(lblCurrent);
            pnlCard.Controls.Add(txtCurrentPassword);
            pnlCard.Controls.Add(lblNew);
            pnlCard.Controls.Add(txtNewPassword);
            pnlCard.Controls.Add(lblConfirm);
            pnlCard.Controls.Add(txtConfirmPassword);
            pnlCard.Controls.Add(chkShowPasswords);
            pnlCard.Controls.Add(btnSave);
            pnlCard.Controls.Add(lblStatus);

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
            string current = txtCurrentPassword.Text;
            string newPass = txtNewPassword.Text;
            string confirm = txtConfirmPassword.Text;

            if (newPass != confirm)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Yeni şifreler birbiriyle eşleşmiyor.";
                return;
            }

            bool success = _authService.ChangePassword(_user.Id, current, newPass, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Şifreniz başarıyla güncellendi.";
                txtCurrentPassword.Clear();
                txtNewPassword.Clear();
                txtConfirmPassword.Clear();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
        }
    }
}