using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public enum InvestTransferDirection { WalletToInvest, SafeToInvest, InvestToWallet, InvestToSafe }

    // Cüzdan/Kasa ile Yatırım Bakiyesi arasında transfer. Mevcut TransferDialog (Cüzdan<->Kasa) ile
    // aynı görsel dile sahip, ayrı bir dialog olarak tutuluyor ki Cüzdan/Kasa akışına dokunulmasın.
    public class AssetTransferDialog : Form
    {
        private readonly int _userId;
        private readonly AccountService _accountService = new AccountService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;

        private RadioButton rbWalletToInvest = new RadioButton();
        private RadioButton rbSafeToInvest = new RadioButton();
        private RadioButton rbInvestToWallet = new RadioButton();
        private RadioButton rbInvestToSafe = new RadioButton();
        private TextBox txtAmount = new TextBox();
        private Label lblError = new Label();

        public AssetTransferDialog(int userId)
        {
            _userId = userId;
            SetupUI();
            this.Load += (s, e) => DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Yatırım Bakiyesi Transferi";
            this.Width = 400;
            this.Height = 400;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9.5F);

            Label lblDirection = new Label { Text = "Yön:", Left = 30, Top = 20, ForeColor = TextMuted, AutoSize = true };

            rbWalletToInvest.Text = "Cüzdandan Yatırıma";
            rbWalletToInvest.Left = 30; rbWalletToInvest.Top = 48; rbWalletToInvest.AutoSize = true; rbWalletToInvest.ForeColor = TextLight; rbWalletToInvest.Checked = true;

            rbSafeToInvest.Text = "Kasadan Yatırıma";
            rbSafeToInvest.Left = 30; rbSafeToInvest.Top = 80; rbSafeToInvest.AutoSize = true; rbSafeToInvest.ForeColor = TextLight;

            rbInvestToWallet.Text = "Yatırımdan Cüzdana";
            rbInvestToWallet.Left = 30; rbInvestToWallet.Top = 112; rbInvestToWallet.AutoSize = true; rbInvestToWallet.ForeColor = TextLight;

            rbInvestToSafe.Text = "Yatırımdan Kasaya";
            rbInvestToSafe.Left = 30; rbInvestToSafe.Top = 144; rbInvestToSafe.AutoSize = true; rbInvestToSafe.ForeColor = TextLight;

            Label lblAmount = new Label { Text = "Tutar:", Left = 30, Top = 185, ForeColor = TextLight, AutoSize = true };
            txtAmount.Left = 30;
            txtAmount.Top = 215;
            txtAmount.Width = 320;
            txtAmount.TextChanged += (s, e) => SmartFormatAmount(txtAmount);

            lblError.Left = 30;
            lblError.Top = 250;
            lblError.Width = 320;
            lblError.Height = 30;
            lblError.ForeColor = Color.FromArgb(255, 140, 140);

            Button btnOk = new Button
            {
                Text = "Transfer Et",
                Left = 30,
                Top = 290,
                Width = 320,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;

            this.Controls.Add(lblDirection);
            this.Controls.Add(rbWalletToInvest);
            this.Controls.Add(rbSafeToInvest);
            this.Controls.Add(rbInvestToWallet);
            this.Controls.Add(rbInvestToSafe);
            this.Controls.Add(lblAmount);
            this.Controls.Add(txtAmount);
            this.Controls.Add(lblError);
            this.Controls.Add(btnOk);
        }

        private bool _suppressAmountFormatting = false;

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

            bool success;
            string error;

            if (rbWalletToInvest.Checked) success = _accountService.TransferWalletToInvest(_userId, amount, out error);
            else if (rbSafeToInvest.Checked) success = _accountService.TransferSafeToInvest(_userId, amount, out error);
            else if (rbInvestToWallet.Checked) success = _accountService.TransferInvestToWallet(_userId, amount, out error);
            else success = _accountService.TransferInvestToSafe(_userId, amount, out error);

            if (!success)
            {
                lblError.Text = error;
                return;
            }

            this.DialogResult = DialogResult.OK;
        }
    }
}
