using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class OnboardingForm : Form
    {
        private readonly int _userId;
        private readonly AccountService _accountService = new AccountService();

        private static readonly Color AppBackColor = Color.FromArgb(24, 27, 38);
        private static readonly Color CardBackColor = Color.FromArgb(37, 41, 59);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color TextLight = Color.White;

        private Panel pnlCard = new Panel();
        private Panel pnlStep1 = new Panel();
        private Panel pnlStep2 = new Panel();

        private TextBox txtMonthlyIncome = new TextBox();
        private TextBox txtInitialSavings = new TextBox();
        private Label lblError1 = new Label();
        private Label lblError2 = new Label();

        private decimal _monthlyIncomeValue;

        public OnboardingForm(int userId)
        {
            _userId = userId;
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Hoş Geldiniz";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(900, 600);
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 10F);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ControlBox = false;

            pnlCard.Width = 480;
            pnlCard.Height = 360;
            pnlCard.BackColor = CardBackColor;

            // --- Adım 1: Cüzdan / Aylık Gelir ---
            pnlStep1.Dock = DockStyle.Fill;
            pnlStep1.BackColor = CardBackColor;

            Label lblIcon1 = new Label { Text = "👛", Font = new Font("Segoe UI", 40F), AutoSize = true, Left = 200, Top = 40 };
            Label lblPrompt1 = new Label { Text = "Aylık toplam gelirinizi yazınız", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, AutoSize = true, Left = 60, Top = 130 };

            txtMonthlyIncome.Left = 60;
            txtMonthlyIncome.Top = 195;
            txtMonthlyIncome.Width = 360;
            txtMonthlyIncome.Font = new Font("Segoe UI", 12F);
            txtMonthlyIncome.TextAlign = HorizontalAlignment.Center;

            lblError1.Left = 60;
            lblError1.Top = 230;
            lblError1.Width = 360;
            lblError1.Height = 30;
            lblError1.ForeColor = Color.FromArgb(255, 140, 140);
            lblError1.TextAlign = ContentAlignment.MiddleCenter;

            Button btnNext = new Button
            {
                Text = "Devam Et",
                Left = 140,
                Top = 280,
                Width = 200,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Click += BtnNext_Click;

            pnlStep1.Controls.Add(lblIcon1);
            pnlStep1.Controls.Add(lblPrompt1);
            pnlStep1.Controls.Add(txtMonthlyIncome);
            pnlStep1.Controls.Add(lblError1);
            pnlStep1.Controls.Add(btnNext);

            // --- Adım 2: Kasa / Toplam Birikim ---
            pnlStep2.Dock = DockStyle.Fill;
            pnlStep2.BackColor = CardBackColor;
            pnlStep2.Visible = false;

            Label lblIcon2 = new Label { Text = "🏦", Font = new Font("Segoe UI", 40F), AutoSize = true, Left = 200, Top = 40 };
            Label lblPrompt2 = new Label { Text = "Toplam birikiminizi yazınız", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, AutoSize = true, Left = 80, Top = 130 };

            txtInitialSavings.Left = 60;
            txtInitialSavings.Top = 195;
            txtInitialSavings.Width = 360;
            txtInitialSavings.Font = new Font("Segoe UI", 12F);
            txtInitialSavings.TextAlign = HorizontalAlignment.Center;

            lblError2.Left = 60;
            lblError2.Top = 230;
            lblError2.Width = 360;
            lblError2.Height = 30;
            lblError2.ForeColor = Color.FromArgb(255, 140, 140);
            lblError2.TextAlign = ContentAlignment.MiddleCenter;

            Button btnFinish = new Button
            {
                Text = "Tamamla",
                Left = 140,
                Top = 280,
                Width = 200,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnFinish.FlatAppearance.BorderSize = 0;
            btnFinish.Click += BtnFinish_Click;

            pnlStep2.Controls.Add(lblIcon2);
            pnlStep2.Controls.Add(lblPrompt2);
            pnlStep2.Controls.Add(txtInitialSavings);
            pnlStep2.Controls.Add(lblError2);
            pnlStep2.Controls.Add(btnFinish);

            pnlCard.Controls.Add(pnlStep1);
            pnlCard.Controls.Add(pnlStep2);
            this.Controls.Add(pnlCard);

            this.Resize += (s, e) => CenterCard();
            CenterCard();
        }

        private void CenterCard()
        {
            pnlCard.Left = (this.ClientSize.Width - pnlCard.Width) / 2;
            pnlCard.Top = (this.ClientSize.Height - pnlCard.Height) / 2;
        }

        private void BtnNext_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtMonthlyIncome.Text, out decimal income) || income < 0)
            {
                lblError1.Text = "Geçerli bir tutar girin.";
                return;
            }

            _monthlyIncomeValue = income;
            pnlStep1.Visible = false;
            pnlStep2.Visible = true;
        }

        private void BtnFinish_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtInitialSavings.Text, out decimal savings) || savings < 0)
            {
                lblError2.Text = "Geçerli bir tutar girin.";
                return;
            }

            _accountService.CompleteOnboarding(_userId, _monthlyIncomeValue, savings);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}