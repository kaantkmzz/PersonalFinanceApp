using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class HomeControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly AccountService _accountService = new AccountService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);

        private Label lblWalletAmount = new Label();
        private Label lblSafeAmount = new Label();
        private Label lblStatus = new Label();

        public HomeControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            RefreshBalances();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            Label lblWelcome = new Label
            {
                Text = $"Hoş geldin, {_user.Username}",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = TextLight,
                AutoSize = true,
                Left = 20,
                Top = 20
            };

            Panel pnlWallet = new Panel { Left = 20, Top = 100, Width = 320, Height = 240, BackColor = CardBackColor };
            Label lblWalletIcon = new Label { Text = "💳", Font = new Font("Segoe UI Emoji", 32F), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true };
            Label lblWalletTitle = new Label { Text = "Cüzdan", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 130, AutoSize = true };
            lblWalletAmount.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblWalletAmount.ForeColor = Color.FromArgb(120, 220, 150);
            lblWalletAmount.Left = 20;
            lblWalletAmount.Top = 165;
            lblWalletAmount.AutoSize = true;
            pnlWallet.Controls.Add(lblWalletIcon);
            pnlWallet.Controls.Add(lblWalletTitle);
            pnlWallet.Controls.Add(lblWalletAmount);

            Panel pnlSafe = new Panel { Left = 360, Top = 100, Width = 320, Height = 240, BackColor = CardBackColor };
            Label lblSafeIcon = new Label { Text = "🏦", Font = new Font("Segoe UI Emoji", 32F), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true };
            Label lblSafeTitle = new Label { Text = "Kasa", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 130, AutoSize = true };
            lblSafeAmount.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblSafeAmount.ForeColor = Color.FromArgb(120, 180, 255);
            lblSafeAmount.Left = 20;
            lblSafeAmount.Top = 165;
            lblSafeAmount.AutoSize = true;
            pnlSafe.Controls.Add(lblSafeIcon);
            pnlSafe.Controls.Add(lblSafeTitle);
            pnlSafe.Controls.Add(lblSafeAmount);

            Button btnTransfer = new Button
            {
                Text = "Transfer Et",
                Left = 20,
                Top = 340,
                Width = 200,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10.5F)
            };


            btnTransfer.FlatAppearance.BorderSize = 0;
            btnTransfer.Click += BtnTransfer_Click;

            Button btnHistory = new Button
            {
                Text = "Transfer Geçmişi",
                Left = 240,
                Top = 340,
                Width = 200,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = CardBackColor,
                ForeColor = TextLight,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10.5F)
            };
            btnHistory.FlatAppearance.BorderSize = 1;
            btnHistory.FlatAppearance.BorderColor = Color.FromArgb(90, 94, 115);
            btnHistory.Click += (s, e) =>
            {
                using (var dialog = new TransferHistoryDialog(_user.Id))
                {
                    dialog.ShowDialog();
                }
            };

            lblStatus.Left = 20;
            lblStatus.Top = 395;
            lblStatus.Width = 500;
            lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F);

            this.Controls.Add(lblWelcome);
            this.Controls.Add(pnlWallet);
            this.Controls.Add(pnlSafe);
            this.Controls.Add(btnTransfer);
            this.Controls.Add(btnHistory);
            this.Controls.Add(lblStatus);
        }

        public void RefreshData()
        {
            RefreshBalances();
        }

        private void RefreshBalances()
        {
            var (wallet, safe) = _accountService.GetBalances(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            if (_user.HideAmountsEnabled)
            {
                lblWalletAmount.Text = "••••••";
                lblSafeAmount.Text = "••••••";
            }
            else
            {
                lblWalletAmount.Text = wallet.ToString("#,##0", tr) + " ₺";
                lblSafeAmount.Text = safe.ToString("#,##0", tr) + " ₺";
            }
        }

        private void BtnTransfer_Click(object? sender, EventArgs e)
        {
            using (var dialog = new TransferDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    bool success;
                    string errorMessage;

                    if (dialog.Direction == TransferDirection.WalletToSafe)
                        success = _accountService.TransferToSafe(_user.Id, dialog.Amount, out errorMessage);
                    else
                        success = _accountService.TransferToWallet(_user.Id, dialog.Amount, out errorMessage);

                    if (success)
                    {
                        lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                        lblStatus.Text = "Transfer başarılı.";
                        RefreshBalances();
                    }
                    else
                    {
                        lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                        lblStatus.Text = errorMessage;
                    }
                    Button btnTransfer = new Button
                    {
                        Text = "Transfer Et",
                        Left = 20,
                        Top = 340,
                        Width = 200,
                        Height = 42,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = AccentColor,
                        ForeColor = Color.White,
                        Cursor = Cursors.Hand,
                        Font = new Font("Segoe UI", 10.5F)

                    };
                    btnTransfer.FlatAppearance.BorderSize = 0;
                    btnTransfer.Click += BtnTransfer_Click;

                    Button btnHistory = new Button
                    {
                        Text = "Transfer Geçmişi",
                        Left = 240,
                        Top = 340,
                        Width = 200,
                        Height = 42,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = CardBackColor,
                        ForeColor = TextLight,
                        Cursor = Cursors.Hand,
                        Font = new Font("Segoe UI", 10.5F)
                    };

                    btnHistory.FlatAppearance.BorderSize = 1;
                    btnHistory.FlatAppearance.BorderColor = Color.FromArgb(90, 94, 115);
                    btnHistory.Click += (s, e) =>
                    {
                        using (var dialog = new TransferHistoryDialog(_user.Id))
                        {
                            dialog.ShowDialog();
                        }
                    };
                }
            }
        }
    }
}