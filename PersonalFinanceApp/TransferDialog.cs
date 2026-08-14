using PersonalFinanceApp.Helpers;

namespace PersonalFinanceApp
{
    public enum TransferDirection { WalletToSafe, SafeToWallet }

    public partial class TransferDialog : Form
    {
        public decimal Amount { get; private set; }
        public TransferDirection Direction { get; private set; }

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color AccentColor => AppTheme.AccentColor;

        private RadioButton rbToSafe = new RadioButton();
        private RadioButton rbToWallet = new RadioButton();
        private TextBox txtAmount = new TextBox();
        private Label lblError = new Label();

        public TransferDialog()
        {
            InitializeComponent();
            SetupUI();
            this.Load += (s, e) => DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Transfer";
            this.Width = 380;
            this.Height = 300;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9.5F);

            rbToSafe.Text = "Cüzdandan Kasaya";
            rbToSafe.Left = 30;
            rbToSafe.Top = 25;
            rbToSafe.AutoSize = true;
            rbToSafe.ForeColor = TextLight;
            rbToSafe.Checked = true;

            rbToWallet.Text = "Kasadan Cüzdana";
            rbToWallet.Left = 30;
            rbToWallet.Top = 60;
            rbToWallet.AutoSize = true;
            rbToWallet.ForeColor = TextLight;

            Label lblAmount = new Label { Text = "Tutar:", Left = 30, Top = 105, ForeColor = TextLight, AutoSize = true };
            txtAmount.Left = 30;
            txtAmount.Top = 135;
            txtAmount.Width = 300;
            txtAmount.TextChanged += (s, e) => SmartFormatAmount(txtAmount);

            lblError.Left = 30;
            lblError.Top = 170;
            lblError.Width = 300;
            lblError.Height = 30;
            lblError.ForeColor = Color.FromArgb(255, 140, 140);

            Button btnOk = new Button
            {
                Text = "Transfer Et",
                Left = 30,
                Top = 210,
                Width = 300,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;

            this.Controls.Add(rbToSafe);
            this.Controls.Add(rbToWallet);
            this.Controls.Add(lblAmount);
            this.Controls.Add(txtAmount);
            this.Controls.Add(lblError);
            this.Controls.Add(btnOk);
        }

        private bool _suppressAmountFormatting = false;

        // Tutar kutusuna yazılan rakamları "10.000" gibi binlik ayraçlarla biçimlendirir.
        private void SmartFormatAmount(TextBox txt)
        {
            if (_suppressAmountFormatting || string.IsNullOrWhiteSpace(txt.Text)) return;
            string value = new string(txt.Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(value)) return;
            if (decimal.TryParse(value, out decimal amount))
            {
                string formatted = amount.ToString("#,##0", new System.Globalization.CultureInfo("tr-TR"));
                if (txt.Text == formatted) return;
                _suppressAmountFormatting = true;
                txt.Text = formatted;
                txt.SelectionStart = txt.Text.Length;
                _suppressAmountFormatting = false;
            }
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string rawAmount = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
            if (!decimal.TryParse(rawAmount, out decimal amount) || amount <= 0)
            {
                lblError.Text = "Geçerli bir tutar girin.";
                return;
            }

            Amount = amount;
            Direction = rbToSafe.Checked ? TransferDirection.WalletToSafe : TransferDirection.SafeToWallet;
            this.DialogResult = DialogResult.OK;
        }
    }
}