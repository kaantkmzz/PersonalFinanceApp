using System.Runtime.InteropServices;
using PersonalFinanceApp.Helpers;
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
        private Panel pnlStep = new Panel();

        private TextBox txtInitialSavings = new TextBox();
        private Label lblError = new Label();

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
            this.Load += (s, e) => DarkTitleBarHelper.SetTitleBarDarkMode(this, AppTheme.IsDark);

            pnlCard.Width = 480;
            pnlCard.Height = 360;
            pnlCard.BackColor = AppBackColor;
            pnlCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, pnlCard.Width - 1, pnlCard.Height - 1), 18);
                using var brush = new SolidBrush(CardBackColor);
                e.Graphics.FillPath(brush, path);
            };

            // --- Kasa / Toplam Birikim (tek adım; cüzdan artık burada sorulmuyor, 0 ile başlıyor) ---
            pnlStep.Dock = DockStyle.Fill;
            PaintCardShape(pnlStep);

            Panel pnlIconBadge = CreateIconBadge("🏦");
            // Height=30 "p"/"y" gibi alt çıkıntılı harflerin kuyruğunu kesiyordu (kutu, fontun
            // descender'ı için yeterli yer bırakmıyordu); 40'a çıkarıldı.
            Label lblPrompt = new Label { Text = "Toplam birikiminizi yazınız", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, AutoSize = false, Left = 40, Width = 400, Height = 40, TextAlign = ContentAlignment.MiddleCenter };

            // Alt açıklama kaldırıldı — tek başlık yeterli. Kaldırılan boşluk yukarı çekildi,
            // metin kutusu geniş olsun diye kart genişletildi (bkz. pnlCard.Width).
            Panel pnlField = new Panel { Left = 40, Top = 180, Width = 400, Height = 52 };
            SetupSmoothContainer(pnlField, 10, FieldBackColor);
            txtInitialSavings.BorderStyle = BorderStyle.None;
            txtInitialSavings.BackColor = FieldBackColor;
            txtInitialSavings.ForeColor = TextLight;
            txtInitialSavings.Font = new Font("Segoe UI", 13F);
            txtInitialSavings.TextAlign = HorizontalAlignment.Center;
            txtInitialSavings.Location = new Point(0, 15);
            txtInitialSavings.Width = 400;
            txtInitialSavings.TextChanged += (s, e) => SmartFormatAmount(txtInitialSavings);
            // "p", "y" gibi alt çıkıntılı (descender) harflerin native metin kutusunun alt sınırına
            // çok yakın yazılıp kırpılmasını önlemek için metni birkaç piksel yukarı kaydırıyoruz
            // (bkz. TransactionControl.ShiftEditTextUp — aynı teknik, burada doğrudan TextBox'a uygulanıyor).
            txtInitialSavings.HandleCreated += (s, e) => ShiftTextBoxTextUp(txtInitialSavings, 3);
            if (txtInitialSavings.IsHandleCreated) ShiftTextBoxTextUp(txtInitialSavings, 3);
            pnlField.Controls.Add(txtInitialSavings);

            lblError.Left = 40;
            lblError.Top = 238;
            lblError.Width = 400;
            lblError.Height = 24;
            lblError.ForeColor = DangerColor;
            lblError.BackColor = Color.Transparent;
            lblError.Font = new Font("Segoe UI", 9F);
            lblError.TextAlign = ContentAlignment.MiddleCenter;

            Button btnFinish = new Button { Text = "Tamamla", Left = 40, Top = 274, Width = 400, Height = 46, Cursor = Cursors.Hand };
            SetupRoundedButton(btnFinish, AccentColor, Color.White);
            btnFinish.Click += BtnFinish_Click;

            CenterHorizontally(pnlIconBadge, 0, 36);
            CenterHorizontally(lblPrompt, 0, 113);

            pnlStep.Controls.Add(pnlIconBadge);
            pnlStep.Controls.Add(lblPrompt);
            pnlStep.Controls.Add(pnlField);
            pnlStep.Controls.Add(lblError);
            pnlStep.Controls.Add(btnFinish);

            pnlCard.Controls.Add(pnlStep);
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
                // Emoji glifinin kendi iç boşluğu (side bearing) simetrik değildir, bu yüzden
                // HorizontalCenter bayrağıyla hesaplanan kutu ortası, gözle görülen glif ortasıyla
                // çakışmaz (ikon dairenin ortasında değil, kaymış görünür). Gerçek glif boyutunu
                // ölçüp dikdörtgeni buna göre elle ortalıyoruz; dikey eksende de aynı sebeple
                // birkaç piksel yukarı kaydırma uyguluyoruz.
                var font = new Font("Segoe UI Emoji", 22F);
                Size glyphSize = TextRenderer.MeasureText(e.Graphics, emoji, font, badge.Size, TextFormatFlags.NoPadding);
                var iconRect = new Rectangle((badge.Width - glyphSize.Width) / 2, (badge.Height - glyphSize.Height) / 2 - 4, glyphSize.Width, glyphSize.Height);
                TextRenderer.DrawText(e.Graphics, emoji, font, iconRect, TextLight, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            };
            return badge;
        }

        // TextBox'ın metin biçimlendirme dikdörtgenini (EM_SETRECT) birkaç piksel yukarı taşıyarak
        // "p", "y" gibi alt çıkıntılı harflerin alt sınıra çok yakın yazılıp kırpılmasını önler.
        private const int EM_SETRECT = 0xB3;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private static void ShiftTextBoxTextUp(TextBox txt, int pixels)
        {
            var rect = new RECT { Left = 0, Top = -pixels, Right = txt.ClientSize.Width, Bottom = txt.ClientSize.Height };
            SendMessage(txt.Handle, EM_SETRECT, IntPtr.Zero, ref rect);
        }

        private void CenterHorizontally(Control control, int unusedLeft, int top)
        {
            control.Top = top;
            control.Left = (pnlCard.Width - control.Width) / 2;
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

        private void BtnFinish_Click(object? sender, EventArgs e)
        {
            string raw = new string(txtInitialSavings.Text.Where(char.IsDigit).ToArray());
            if (!decimal.TryParse(raw, out decimal savings) || savings < 0)
            {
                lblError.Text = "Geçerli bir tutar girin.";
                return;
            }

            // Cüzdan artık onboarding'de sorulmuyor; 0 bakiyeyle başlıyor.
            _accountService.CompleteOnboarding(_userId, 0, savings);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // pnlCard'ın yuvarlatılmış köşeleriyle aynı şekli, üstünü tamamen kaplayan adım paneli için de çizer
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
