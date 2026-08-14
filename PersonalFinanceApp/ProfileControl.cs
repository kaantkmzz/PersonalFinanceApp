using System;
using System.Drawing;
using System.Windows.Forms;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class ProfileControl : UserControl
    {
        private readonly User _user;
        private readonly AccountService _accountService = new AccountService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color FieldBackColor => AppTheme.HoverBackColor;

        private Panel pnlCard = new Panel();
        private TextBox txtFullName = new TextBox();
        private Button btnWeekly = new Button();
        private Button btnMonthly = new Button();
        private Button btnNever = new Button();
        private Label lblExplanation = new Label();
        private Panel pnlToggle = new Panel();
        private Label lblStatus = new Label();

        public ProfileControl(User user)
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

            pnlCard.Width = 580;
            pnlCard.Height = 500;
            SetupSmoothContainer(pnlCard, 16, CardBackColor);

            Label lblTitle = new Label { Text = "Profil", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 40, Top = 30, AutoSize = true };

            // --- Ad Soyad ---
            Label lblFullName = new Label { Text = "Ad Soyad:", Left = 40, Top = 90, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlFullName = new Panel { Left = 40, Top = 120, Width = 380, Height = 42 };
            SetupSmoothContainer(pnlFullName, 8, FieldBackColor, CardBackColor);
            txtFullName.Left = 10; txtFullName.Top = 11; txtFullName.Width = 360;
            txtFullName.Font = new Font("Segoe UI", 10.5F); txtFullName.BorderStyle = BorderStyle.None;
            txtFullName.BackColor = FieldBackColor; txtFullName.ForeColor = TextLight;
            txtFullName.Text = _user.FullName;
            pnlFullName.Controls.Add(txtFullName);

            Button btnSaveName = new Button { Text = "Kaydet", Left = 440, Top = 120, Width = 100, Height = 42, Cursor = Cursors.Hand };
            SetupRoundedButton(btnSaveName, AccentColor, Color.White);
            btnSaveName.Click += BtnSaveName_Click;

            // --- Ayırıcı ---
            Panel divider = new Panel { Left = 40, Top = 185, Width = 500, Height = 1, BackColor = AppTheme.SidebarDividerColor };

            // --- Temizleme Sıklığı ---
            Label lblCleanupTitle = new Label { Text = "Temizleme Sıklığı", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 40, Top = 202, AutoSize = true };

            SetupFrequencyButton(btnWeekly, DataCleanupService.Weekly, "Haftalık");
            btnWeekly.Left = 40; btnWeekly.Top = 235; btnWeekly.Width = 155; btnWeekly.Height = 36;

            SetupFrequencyButton(btnMonthly, DataCleanupService.Monthly, "Aylık");
            btnMonthly.Left = 203; btnMonthly.Top = 235; btnMonthly.Width = 155; btnMonthly.Height = 36;

            SetupFrequencyButton(btnNever, DataCleanupService.Never, "Hiçbir zaman");
            btnNever.Left = 366; btnNever.Top = 235; btnNever.Width = 155; btnNever.Height = 36;

            lblExplanation.Left = 40; lblExplanation.Top = 280; lblExplanation.Width = 500; lblExplanation.Height = 56;
            lblExplanation.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic); lblExplanation.ForeColor = TextMuted; lblExplanation.BackColor = Color.Transparent;

            // --- Temizlemeden önce CSV'ye aktar ---
            Label lblExportToggle = new Label
            {
                Text = "Temizlenmeden önce her zaman CSV'ye aktarsın mı?",
                Left = 40,
                Top = 348,
                Width = 400,
                Height = 40,
                ForeColor = TextMuted,
                BackColor = Color.Transparent
            };

            SetupToggle();
            pnlToggle.Left = 470; pnlToggle.Top = 354; pnlToggle.Width = 52; pnlToggle.Height = 28;

            lblStatus.Left = 40; lblStatus.Top = 412; lblStatus.Width = 500; lblStatus.Height = 25; lblStatus.Font = new Font("Segoe UI", 9F); lblStatus.BackColor = Color.Transparent;

            pnlCard.Controls.Add(lblTitle);
            pnlCard.Controls.Add(lblFullName); pnlCard.Controls.Add(pnlFullName); pnlCard.Controls.Add(btnSaveName);
            pnlCard.Controls.Add(divider);
            pnlCard.Controls.Add(lblCleanupTitle);
            pnlCard.Controls.Add(btnWeekly); pnlCard.Controls.Add(btnMonthly); pnlCard.Controls.Add(btnNever);
            pnlCard.Controls.Add(lblExplanation);
            pnlCard.Controls.Add(lblExportToggle); pnlCard.Controls.Add(pnlToggle);
            pnlCard.Controls.Add(lblStatus);

            this.Controls.Add(pnlCard);
            this.Resize += (s, e) => CenterCard();
            this.Load += (s, e) => CenterCard();
            CenterCard();

            UpdateExplanationText();
        }

        private void CenterCard()
        {
            pnlCard.Left = (this.ClientSize.Width - pnlCard.Width) / 2;
            pnlCard.Top = (this.ClientSize.Height - pnlCard.Height) / 2;
        }

        private void BtnSaveName_Click(object? sender, EventArgs e)
        {
            string name = txtFullName.Text.Trim();
            _user.FullName = name;
            _accountService.SetFullName(_user.Id, name);
            lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
            lblStatus.Text = "Ad Soyad güncellendi.";
        }

        // --- Temizleme sıklığı butonları (Rapor ekranındaki periyot butonlarıyla aynı stil) ---
        private void SetupFrequencyButton(Button btn, string frequency, string text)
        {
            btn.Text = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            bool isHovered = false;
            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                bool active = _user.CleanupFrequency == frequency;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? CardBackColor);

                using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                if (active)
                {
                    using var brush = new SolidBrush(isHovered ? ControlPaint.Light(AccentColor) : AccentColor);
                    e.Graphics.FillPath(brush, path);
                }
                else
                {
                    using var pen = new Pen(isHovered ? TextLight : TextMuted, 1.2f);
                    e.Graphics.DrawPath(pen, path);
                }

                Color textColor = active ? Color.White : TextMuted;
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            btn.Click += (s, e) => SelectFrequency(frequency);
        }

        private void SelectFrequency(string frequency)
        {
            if (_user.CleanupFrequency != frequency)
            {
                _user.CleanupFrequency = frequency;
                _user.CleanupPeriodStart = DateTime.Now;
                _accountService.SetCleanupFrequency(_user.Id, frequency, _user.CleanupPeriodStart);
            }

            btnWeekly.Invalidate();
            btnMonthly.Invalidate();
            btnNever.Invalidate();
            pnlToggle.Invalidate();
            UpdateExplanationText();
        }

        private void UpdateExplanationText()
        {
            lblExplanation.Text = _user.CleanupFrequency switch
            {
                DataCleanupService.Weekly => "(Haftada bir kez, işlemleriniz ve kategorileriniz otomatik olarak temizlenir. Tekrarlanan işlemleriniz etkilenmez.)",
                DataCleanupService.Monthly => "(Ayda bir kez, işlemleriniz ve kategorileriniz otomatik olarak temizlenir. Tekrarlanan işlemleriniz etkilenmez.)",
                _ => "(İşlemleriniz ve kategorileriniz asla otomatik olarak temizlenmez.)"
            };
        }

        // --- Açma/kapama anahtarı (toggle switch) ---
        private void SetupToggle()
        {
            pnlToggle.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnlToggle.Parent?.BackColor ?? CardBackColor);

                bool interactive = _user.CleanupFrequency != DataCleanupService.Never;
                bool on = _user.CleanupExportBeforeClear;

                Color trackColor = !interactive
                    ? Color.FromArgb(60, 64, 84)
                    : (on ? AccentColor : Color.FromArgb(90, 94, 115));

                using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnlToggle.Width - 1, pnlToggle.Height - 1), pnlToggle.Height / 2))
                using (var brush = new SolidBrush(trackColor))
                    e.Graphics.FillPath(brush, path);

                int knobSize = pnlToggle.Height - 6;
                int knobX = on ? pnlToggle.Width - knobSize - 3 : 3;
                Color knobColor = interactive ? Color.White : Color.FromArgb(140, 144, 160);
                using (var knobBrush = new SolidBrush(knobColor))
                    e.Graphics.FillEllipse(knobBrush, knobX, 3, knobSize, knobSize);
            };

            pnlToggle.Cursor = Cursors.Hand;
            pnlToggle.Click += (s, e) =>
            {
                if (_user.CleanupFrequency == DataCleanupService.Never) return;
                _user.CleanupExportBeforeClear = !_user.CleanupExportBeforeClear;
                _accountService.SetCleanupExportBeforeClear(_user.Id, _user.CleanupExportBeforeClear);
                pnlToggle.Invalidate();
            };
        }

        protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } }

        // --- GÖRSEL YARDIMCI METOTLAR ---
        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) => SetupSmoothContainer(pnl, radius, bgColor, AppBackColor);
        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor, Color clearColor)
        {
            pnl.BackColor = clearColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(clearColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius);
                using var brush = new SolidBrush(bgColor);
                e.Graphics.FillPath(brush, path);
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                using var brush = new SolidBrush(bgColor);
                e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
