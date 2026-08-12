using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class OnboardingForm : Form
    {
        private readonly int _userId;
        private readonly AccountService _accountService = new AccountService();

        private static readonly Color AppBackColor = Color.FromArgb(24, 27, 38);
        private static readonly Color CardBackColor = Color.FromArgb(37, 41, 59);
        private static readonly Color FieldBackColor = Color.FromArgb(46, 50, 70);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color DangerColor = Color.FromArgb(230, 120, 120);

        private Panel pnlCard = new Panel();
        private Panel pnlStep1 = new Panel();
        private Panel pnlStep2 = new Panel();
        private Panel pnlStepDot1 = new Panel();
        private Panel pnlStepDot2 = new Panel();

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
            pnlCard.Height = 420;
            pnlCard.BackColor = AppBackColor;
            pnlCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, pnlCard.Width - 1, pnlCard.Height - 1), 18);
                using var brush = new SolidBrush(CardBackColor);
                e.Graphics.FillPath(brush, path);
            };

            // --- Adım göstergesi (üstte iki nokta) ---
            pnlStepDot1.Size = new Size(28, 6);
            pnlStepDot1.Top = 26;
            pnlStepDot1.Paint += (s, e) => PaintStepDot(e.Graphics, pnlStepDot1, true);

            pnlStepDot2.Size = new Size(28, 6);
            pnlStepDot2.Top = 26;
            pnlStepDot2.Paint += (s, e) => PaintStepDot(e.Graphics, pnlStepDot2, false);

            pnlStepDot1.Left = pnlCard.Width / 2 - 34;
            pnlStepDot2.Left = pnlCard.Width / 2 + 6;

            // --- Adım 1: Cüzdan / Aylık Gelir ---
            pnlStep1.Dock = DockStyle.Fill;
            PaintCardShape(pnlStep1);

            Panel pnlIconBadge1 = CreateIconBadge("👛");
            Label lblPrompt1 = new Label { Text = "Aylık toplam gelirinizi yazınız", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, AutoSize = true };
            Label lblHint1 = new Label { Text = "Bu tutar, aylık bütçenizi takip etmemize yardımcı olur", Font = new Font("Segoe UI", 9F), ForeColor = TextMuted, AutoSize = true };

            Panel pnlField1 = new Panel { Left = 60, Top = 195, Width = 360, Height = 48 };
            SetupSmoothContainer(pnlField1, 10, FieldBackColor);
            txtMonthlyIncome.BorderStyle = BorderStyle.None;
            txtMonthlyIncome.BackColor = FieldBackColor;
            txtMonthlyIncome.ForeColor = TextLight;
            txtMonthlyIncome.Font = new Font("Segoe UI", 13F);
            txtMonthlyIncome.TextAlign = HorizontalAlignment.Center;
            txtMonthlyIncome.Location = new Point(0, 13);
            txtMonthlyIncome.Width = 360;
            txtMonthlyIncome.TextChanged += (s, e) => SmartFormatAmount(txtMonthlyIncome);
            pnlField1.Controls.Add(txtMonthlyIncome);

            lblError1.Left = 60;
            lblError1.Top = 250;
            lblError1.Width = 360;
            lblError1.Height = 24;
            lblError1.ForeColor = DangerColor;
            lblError1.Font = new Font("Segoe UI", 9F);
            lblError1.TextAlign = ContentAlignment.MiddleCenter;

            Button btnNext = new Button { Text = "Devam Et", Left = 60, Top = 285, Width = 360, Height = 46, Cursor = Cursors.Hand };
            SetupRoundedButton(btnNext, AccentColor, Color.White);
            btnNext.Click += BtnNext_Click;

            CenterHorizontally(pnlIconBadge1, 0, 40);
            CenterHorizontally(lblPrompt1, 0, 118);
            CenterHorizontally(lblHint1, 0, 150);

            pnlStep1.Controls.Add(pnlIconBadge1);
            pnlStep1.Controls.Add(lblPrompt1);
            pnlStep1.Controls.Add(lblHint1);
            pnlStep1.Controls.Add(pnlField1);
            pnlStep1.Controls.Add(lblError1);
            pnlStep1.Controls.Add(btnNext);

            // --- Adım 2: Kasa / Toplam Birikim ---
            pnlStep2.Dock = DockStyle.Fill;
            pnlStep2.Visible = false;
            PaintCardShape(pnlStep2);

            Panel pnlIconBadge2 = CreateIconBadge("🏦");
            Label lblPrompt2 = new Label { Text = "Toplam birikiminizi yazınız", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, AutoSize = true };
            Label lblHint2 = new Label { Text = "Kasanızda şu an bulunan toplam tasarruf tutarı", Font = new Font("Segoe UI", 9F), ForeColor = TextMuted, AutoSize = true };

            Panel pnlField2 = new Panel { Left = 60, Top = 195, Width = 360, Height = 48 };
            SetupSmoothContainer(pnlField2, 10, FieldBackColor);
            txtInitialSavings.BorderStyle = BorderStyle.None;
            txtInitialSavings.BackColor = FieldBackColor;
            txtInitialSavings.ForeColor = TextLight;
            txtInitialSavings.Font = new Font("Segoe UI", 13F);
            txtInitialSavings.TextAlign = HorizontalAlignment.Center;
            txtInitialSavings.Location = new Point(0, 13);
            txtInitialSavings.Width = 360;
            txtInitialSavings.TextChanged += (s, e) => SmartFormatAmount(txtInitialSavings);
            pnlField2.Controls.Add(txtInitialSavings);

            lblError2.Left = 60;
            lblError2.Top = 250;
            lblError2.Width = 360;
            lblError2.Height = 24;
            lblError2.ForeColor = DangerColor;
            lblError2.Font = new Font("Segoe UI", 9F);
            lblError2.TextAlign = ContentAlignment.MiddleCenter;

            Button btnFinish = new Button { Text = "Tamamla", Left = 60, Top = 285, Width = 360, Height = 46, Cursor = Cursors.Hand };
            SetupRoundedButton(btnFinish, AccentColor, Color.White);
            btnFinish.Click += BtnFinish_Click;

            CenterHorizontally(pnlIconBadge2, 0, 40);
            CenterHorizontally(lblPrompt2, 0, 118);
            CenterHorizontally(lblHint2, 0, 150);

            pnlStep2.Controls.Add(pnlIconBadge2);
            pnlStep2.Controls.Add(lblPrompt2);
            pnlStep2.Controls.Add(lblHint2);
            pnlStep2.Controls.Add(pnlField2);
            pnlStep2.Controls.Add(lblError2);
            pnlStep2.Controls.Add(btnFinish);

            pnlCard.Controls.Add(pnlStep1);
            pnlCard.Controls.Add(pnlStep2);
            pnlCard.Controls.Add(pnlStepDot1);
            pnlCard.Controls.Add(pnlStepDot2);
            pnlStepDot1.BringToFront();
            pnlStepDot2.BringToFront();
            this.Controls.Add(pnlCard);

            this.Resize += (s, e) => CenterCard();
            CenterCard();
        }

        private Panel CreateIconBadge(string emoji)
        {
            Panel badge = new Panel { Width = 76, Height = 76 };
            badge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(CardBackColor);
                using var brush = new SolidBrush(Color.FromArgb(40, AccentColor));
                e.Graphics.FillEllipse(brush, 0, 0, badge.Width - 1, badge.Height - 1);
                TextRenderer.DrawText(e.Graphics, emoji, new Font("Segoe UI Emoji", 28F), badge.ClientRectangle, TextLight, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            return badge;
        }

        private void CenterHorizontally(Control control, int unusedLeft, int top)
        {
            control.Top = top;
            control.Left = (pnlCard.Width - control.Width) / 2;
        }

        private void PaintStepDot(Graphics g, Panel dot, bool active)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(CardBackColor);
            using var path = GetRoundedRectPath(new Rectangle(0, 0, dot.Width - 1, dot.Height - 1), dot.Height / 2);
            using var brush = new SolidBrush(active ? AccentColor : Color.FromArgb(70, 74, 96));
            g.FillPath(brush, path);
        }

        private bool _suppressFormatting = false;

        private void SmartFormatAmount(TextBox txt)
        {
            if (_suppressFormatting || string.IsNullOrWhiteSpace(txt.Text)) return;
            string value = new string(txt.Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(value)) return;
            if (decimal.TryParse(value, out decimal amount))
            {
                string formatted = amount.ToString("#,##0");
                if (txt.Text == formatted) return;
                _suppressFormatting = true;
                txt.Text = formatted;
                txt.SelectionStart = txt.Text.Length;
                _suppressFormatting = false;
            }
        }

        private void CenterCard()
        {
            pnlCard.Left = (this.ClientSize.Width - pnlCard.Width) / 2;
            pnlCard.Top = (this.ClientSize.Height - pnlCard.Height) / 2;
        }

        private void BtnNext_Click(object? sender, EventArgs e)
        {
            string raw = new string(txtMonthlyIncome.Text.Where(char.IsDigit).ToArray());
            if (!decimal.TryParse(raw, out decimal income) || income < 0)
            {
                lblError1.Text = "Geçerli bir tutar girin.";
                return;
            }

            _monthlyIncomeValue = income;
            pnlStep1.Visible = false;
            pnlStep2.Visible = true;
            pnlStepDot1.Invalidate();
        }

        private void BtnFinish_Click(object? sender, EventArgs e)
        {
            string raw = new string(txtInitialSavings.Text.Where(char.IsDigit).ToArray());
            if (!decimal.TryParse(raw, out decimal savings) || savings < 0)
            {
                lblError2.Text = "Geçerli bir tutar girin.";
                return;
            }

            _accountService.CompleteOnboarding(_userId, _monthlyIncomeValue, savings);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // pnlCard'ın yuvarlatılmış köşeleriyle aynı şekli, üstünü tamamen kaplayan adım panelleri için de çizer
        // (Dock=Fill bir çocuk, ebeveynin köşe yuvarlamasını düz bir dikdörtgenle kapatmasın diye).
        private void PaintCardShape(Panel panel)
        {
            panel.BackColor = AppBackColor;
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 18);
                using var brush = new SolidBrush(CardBackColor);
                e.Graphics.FillPath(brush, path);
            };
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor)
        {
            pnl.BackColor = CardBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Ebeveyn (adım paneli) kendi BackColor özelliği değil, özel Paint ile CardBackColor görünür
                // olduğu için burada da doğrudan CardBackColor kullanıyoruz (parent.BackColor yanlış olurdu).
                e.Graphics.Clear(CardBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius);
                using var brush = new SolidBrush(bgColor);
                e.Graphics.FillPath(brush, path);
            };
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;

            bool isHovered = false;
            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(CardBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 10);
                using var brush = new SolidBrush(isHovered ? ControlPaint.Light(bgColor) : bgColor);
                e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, btn.Text, new Font("Segoe UI", 10.5F, FontStyle.Bold), new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = Math.Max(radius * 2, 1);
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
