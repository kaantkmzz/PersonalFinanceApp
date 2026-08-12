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

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
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