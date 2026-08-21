using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public class SavingsGoalDialog : Form
    {
        private readonly User _user;
        private readonly int _goalId;
        private readonly SavingsGoalService _goalService = new SavingsGoalService();

        private TextBox txtGoalName = new TextBox();
        private TextBox txtTargetAmount = new TextBox();
        private TextBox txtInvestAmount = new TextBox();
        private Label lblStatus = new Label();
        private Label lblProgress = new Label();
        private DataGridView dgvHistory = new DataGridView();

        private CheckBox chkHasDeadline = new CheckBox();
        private DateTimePicker dtpDeadline = new DateTimePicker();
        private Button btnFreqNone = new Button();
        private Button btnFreqDaily = new Button();
        private Button btnFreqWeekly = new Button();
        private Button btnFreqMonthly = new Button();
        private TextBox txtRecurringAmount = new TextBox();
        private Label lblAutoStatus = new Label();
        private string _selectedFrequency = "none"; // "none"|"daily"|"weekly"|"monthly"

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color SuccessColor => AppTheme.SuccessColor;

        public SavingsGoalDialog(User user, int goalId)
        {
            _user = user;
            _goalId = goalId;
            SetupUI();
            LoadGoalData();

            // Başlık çubuğunu koyu temaya geçir
            this.Load += (s, e) => Helpers.DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Hedef Detayı ve Yatırım";
            this.Size = new Size(420, 970);
            this.BackColor = AppBackColor;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // --- HEDEF BİLGİLERİ ---
            Label lblTitle = new Label { Text = "Hedef Bilgileri", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 20, AutoSize = true };

            Label lblName = new Label { Text = "Hedef Adı:", ForeColor = TextMuted, Left = 20, Top = 52, AutoSize = true };
            Panel pnlName = new Panel { Left = 20, Top = 80, Width = 360, Height = 36 };
            SetupSmoothContainer(pnlName, 8, CardBackColor);
            txtGoalName.BorderStyle = BorderStyle.None; txtGoalName.BackColor = CardBackColor; txtGoalName.ForeColor = TextLight; txtGoalName.Font = new Font("Segoe UI", 10F); txtGoalName.Location = new Point(10, 8); txtGoalName.Width = 340;
            pnlName.Controls.Add(txtGoalName);

            Label lblTarget = new Label { Text = "Hedef Tutar:", ForeColor = TextMuted, Left = 20, Top = 122, AutoSize = true };
            Panel pnlTarget = new Panel { Left = 20, Top = 150, Width = 170, Height = 36 };
            SetupSmoothContainer(pnlTarget, 8, CardBackColor);
            txtTargetAmount.BorderStyle = BorderStyle.None; txtTargetAmount.BackColor = CardBackColor; txtTargetAmount.ForeColor = TextLight; txtTargetAmount.Font = new Font("Segoe UI", 10F); txtTargetAmount.Location = new Point(10, 8); txtTargetAmount.Width = 150;
            txtTargetAmount.TextChanged += SmartFormatTextBox;
            pnlTarget.Controls.Add(txtTargetAmount);

            Button btnUpdate = new Button { Text = "💾 Güncelle", Left = 210, Top = 150, Width = 170, Height = 36, Cursor = Cursors.Hand };
            SetupRoundedButton(btnUpdate, Color.FromArgb(80, 85, 105), Color.White);
            btnUpdate.Click += BtnUpdate_Click;

            // --- YATIRIM (ÖDEME) ALANI ---
            Panel divider = new Panel { Left = 20, Top = 210, Width = 360, Height = 1, BackColor = AppTheme.HoverBackColor };

            Label lblInvestTitle = new Label { Text = "Yatırım Yap (Kasadan Düşer)", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 230, AutoSize = true };

            lblProgress.ForeColor = Color.FromArgb(230, 180, 100); lblProgress.Left = 20; lblProgress.Top = 265; lblProgress.Width = 360; lblProgress.Height = 20;

            Panel pnlInvest = new Panel { Left = 20, Top = 295, Width = 170, Height = 36 };
            SetupSmoothContainer(pnlInvest, 8, CardBackColor);
            txtInvestAmount.BorderStyle = BorderStyle.None; txtInvestAmount.BackColor = CardBackColor; txtInvestAmount.ForeColor = TextLight; txtInvestAmount.Font = new Font("Segoe UI", 10F); txtInvestAmount.Location = new Point(10, 8); txtInvestAmount.Width = 150;
            txtInvestAmount.TextChanged += SmartFormatTextBox;
            pnlInvest.Controls.Add(txtInvestAmount);

            Button btnInvest = new Button { Text = "💸 Yatırım Yap", Left = 210, Top = 295, Width = 170, Height = 36, Cursor = Cursors.Hand };
            SetupRoundedButton(btnInvest, SuccessColor, Color.White);
            btnInvest.Click += BtnInvest_Click;

            lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Left = 20; lblStatus.Top = 350; lblStatus.Width = 360; lblStatus.Height = 30; lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            // --- YATIRIM GEÇMİŞİ ---
            Panel divider2 = new Panel { Left = 20, Top = 390, Width = 360, Height = 1, BackColor = AppTheme.HoverBackColor };
            Label lblHistoryTitle = new Label { Text = "Yatırım Geçmişi", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 405, AutoSize = true };

            Panel pnlHistoryWrapper = new Panel { Left = 20, Top = 440, Width = 360, Height = 180, Padding = new Padding(2, 4, 2, 4) };
            SetupSmoothContainer(pnlHistoryWrapper, 10, CardBackColor);

            dgvHistory.Dock = DockStyle.Fill;
            dgvHistory.ReadOnly = true;
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.AllowUserToDeleteRows = false;
            dgvHistory.AllowUserToResizeColumns = false;
            dgvHistory.AllowUserToResizeRows = false;
            dgvHistory.ColumnHeadersVisible = false;
            dgvHistory.RowHeadersVisible = false;
            dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistory.MultiSelect = false;
            dgvHistory.Font = new Font("Segoe UI", 9.5F);
            dgvHistory.RowTemplate.Height = 32;
            dgvHistory.BorderStyle = BorderStyle.None;
            dgvHistory.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvHistory.GridColor = AppTheme.HoverBackColor;
            dgvHistory.BackgroundColor = CardBackColor;
            dgvHistory.DefaultCellStyle.BackColor = CardBackColor;
            dgvHistory.DefaultCellStyle.ForeColor = TextLight;
            dgvHistory.DefaultCellStyle.SelectionBackColor = CardBackColor;
            dgvHistory.DefaultCellStyle.SelectionForeColor = TextLight;
            dgvHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            pnlHistoryWrapper.Controls.Add(dgvHistory);

            // --- HEDEF TARİHİ VE OTOMATİK KATKI ---
            Panel divider3 = new Panel { Left = 20, Top = 635, Width = 360, Height = 1, BackColor = AppTheme.HoverBackColor };
            Label lblAutoTitle = new Label { Text = "Hedef Tarihi ve Otomatik Katkı", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 650, AutoSize = true };

            chkHasDeadline.Text = "Hedef tarihi belirle"; chkHasDeadline.ForeColor = TextMuted; chkHasDeadline.Left = 20; chkHasDeadline.Top = 685; chkHasDeadline.AutoSize = true;
            chkHasDeadline.CheckedChanged += (s, e) => dtpDeadline.Enabled = chkHasDeadline.Checked;

            dtpDeadline.Left = 20; dtpDeadline.Top = 712; dtpDeadline.Width = 200; dtpDeadline.Format = DateTimePickerFormat.Short;
            dtpDeadline.MinDate = DateTime.Today;
            dtpDeadline.Enabled = false;
            dtpDeadline.CalendarForeColor = TextLight;
            dtpDeadline.CalendarMonthBackground = CardBackColor;
            dtpDeadline.CalendarTitleBackColor = AppTheme.HeaderBackColor;
            dtpDeadline.CalendarTitleForeColor = TextLight;
            dtpDeadline.CalendarTrailingForeColor = TextMuted;
            dtpDeadline.HandleCreated += (s, e) =>
            {
                DarkTitleBarHelper.DisableVisualStyle(dtpDeadline);
                dtpDeadline.BackColor = CardBackColor;
                dtpDeadline.ForeColor = TextLight;
            };
            DarkTitleBarHelper.EnableDarkCalendarPopup(dtpDeadline);

            Label lblRecurring = new Label { Text = "Otomatik Katkı Sıklığı:", ForeColor = TextMuted, Left = 20, Top = 752, AutoSize = true };

            const int freqBtnW = 82, freqGap = 4;
            SetupFreqButton(btnFreqNone, "none", "Yok", 20 + 0 * (freqBtnW + freqGap));
            SetupFreqButton(btnFreqDaily, "daily", "Günlük", 20 + 1 * (freqBtnW + freqGap));
            SetupFreqButton(btnFreqWeekly, "weekly", "Haftalık", 20 + 2 * (freqBtnW + freqGap));
            SetupFreqButton(btnFreqMonthly, "monthly", "Aylık", 20 + 3 * (freqBtnW + freqGap));
            int freqTop = 778;
            btnFreqNone.Top = btnFreqDaily.Top = btnFreqWeekly.Top = btnFreqMonthly.Top = freqTop;
            btnFreqNone.Width = btnFreqDaily.Width = btnFreqWeekly.Width = btnFreqMonthly.Width = freqBtnW;
            btnFreqNone.Height = btnFreqDaily.Height = btnFreqWeekly.Height = btnFreqMonthly.Height = 32;

            Label lblRecurringAmount = new Label { Text = "Katkı Tutarı:", ForeColor = TextMuted, Left = 20, Top = 822, AutoSize = true };
            Panel pnlRecurringAmount = new Panel { Left = 20, Top = 848, Width = 200, Height = 36 };
            SetupSmoothContainer(pnlRecurringAmount, 8, CardBackColor);
            txtRecurringAmount.BorderStyle = BorderStyle.None; txtRecurringAmount.BackColor = CardBackColor; txtRecurringAmount.ForeColor = TextLight; txtRecurringAmount.Font = new Font("Segoe UI", 10F); txtRecurringAmount.Location = new Point(10, 8); txtRecurringAmount.Width = 180;
            txtRecurringAmount.TextChanged += SmartFormatTextBox;
            pnlRecurringAmount.Controls.Add(txtRecurringAmount);

            Button btnSaveAuto = new Button { Text = "💾 Kaydet", Left = 230, Top = 848, Width = 150, Height = 36, Cursor = Cursors.Hand };
            SetupRoundedButton(btnSaveAuto, Color.FromArgb(80, 85, 105), Color.White);
            btnSaveAuto.Click += BtnSaveAuto_Click;

            lblAutoStatus.Left = 20; lblAutoStatus.Top = 895; lblAutoStatus.Width = 360; lblAutoStatus.Height = 30; lblAutoStatus.TextAlign = ContentAlignment.MiddleCenter;

            this.Controls.Add(lblTitle); this.Controls.Add(lblName); this.Controls.Add(pnlName); this.Controls.Add(lblTarget); this.Controls.Add(pnlTarget); this.Controls.Add(btnUpdate);
            this.Controls.Add(divider); this.Controls.Add(lblInvestTitle); this.Controls.Add(lblProgress); this.Controls.Add(pnlInvest); this.Controls.Add(btnInvest); this.Controls.Add(lblStatus);
            this.Controls.Add(divider2); this.Controls.Add(lblHistoryTitle); this.Controls.Add(pnlHistoryWrapper);
            this.Controls.Add(divider3); this.Controls.Add(lblAutoTitle);
            this.Controls.Add(chkHasDeadline); this.Controls.Add(dtpDeadline);
            this.Controls.Add(lblRecurring); this.Controls.Add(btnFreqNone); this.Controls.Add(btnFreqDaily); this.Controls.Add(btnFreqWeekly); this.Controls.Add(btnFreqMonthly);
            this.Controls.Add(lblRecurringAmount); this.Controls.Add(pnlRecurringAmount); this.Controls.Add(btnSaveAuto);
            this.Controls.Add(lblAutoStatus);
        }

        private void SetupFreqButton(Button btn, string freq, string text, int left)
        {
            btn.Text = text;
            btn.Left = left;
            btn.Cursor = Cursors.Hand;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(this.BackColor);
                bool active = _selectedFrequency == freq;
                using var path = Helpers.UIStyleHelper.GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                if (active)
                {
                    using var brush = new SolidBrush(AccentColor);
                    e.Graphics.FillPath(brush, path);
                }
                else
                {
                    using var pen = new Pen(TextMuted, 1.2f);
                    e.Graphics.DrawPath(pen, path);
                }
                TextRenderer.DrawText(e.Graphics, text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), active ? Color.White : TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btn.Click += (s, e) =>
            {
                _selectedFrequency = freq;
                btnFreqNone.Invalidate(); btnFreqDaily.Invalidate(); btnFreqWeekly.Invalidate(); btnFreqMonthly.Invalidate();
            };
        }

        private void BtnSaveAuto_Click(object? sender, EventArgs e)
        {
            DateTime? dueDate = chkHasDeadline.Checked ? dtpDeadline.Value.Date : null;
            string? frequency = _selectedFrequency == "none" ? null : _selectedFrequency;

            decimal? recurringAmount = null;
            if (frequency != null)
            {
                string raw = new string(txtRecurringAmount.Text.Where(char.IsDigit).ToArray());
                if (!decimal.TryParse(raw, out decimal parsed) || parsed <= 0)
                {
                    lblAutoStatus.ForeColor = Color.Salmon;
                    lblAutoStatus.Text = "Otomatik katkı için geçerli bir tutar girin.";
                    return;
                }
                recurringAmount = parsed;
            }

            if (_goalService.SetRecurringSettings(_goalId, _user.Id, dueDate, frequency, recurringAmount, out string error))
            {
                lblAutoStatus.ForeColor = Color.LightGreen;
                lblAutoStatus.Text = "Ayarlar kaydedildi.";
            }
            else
            {
                lblAutoStatus.ForeColor = Color.Salmon;
                lblAutoStatus.Text = error;
            }
        }

        private void LoadGoalData()
        {
            var goal = _goalService.GetUserGoals(_user.Id).FirstOrDefault(g => g.Id == _goalId);
            if (goal != null)
            {
                txtGoalName.Text = goal.GoalName;
                txtTargetAmount.Text = goal.TargetAmount.ToString("#,##0");
                txtGoalName.SelectionStart = txtGoalName.Text.Length;
                txtGoalName.SelectionLength = 0;
                
                decimal progressPercent = goal.TargetAmount > 0 ? (goal.CurrentAmount / goal.TargetAmount) * 100 : 0;
                lblProgress.Text = $"Biriken: {goal.CurrentAmount:N0} ₺   /   Kalan: {Math.Max(0, goal.TargetAmount - goal.CurrentAmount):N0} ₺  (%{progressPercent:N1})";

                chkHasDeadline.Checked = goal.DueDate.HasValue;
                if (goal.DueDate.HasValue) dtpDeadline.Value = goal.DueDate.Value < DateTime.Today ? DateTime.Today : goal.DueDate.Value;
                dtpDeadline.Enabled = chkHasDeadline.Checked;

                _selectedFrequency = goal.RecurringFrequency ?? "none";
                btnFreqNone.Invalidate(); btnFreqDaily.Invalidate(); btnFreqWeekly.Invalidate(); btnFreqMonthly.Invalidate();
                txtRecurringAmount.TextChanged -= SmartFormatTextBox;
                txtRecurringAmount.Text = goal.RecurringAmount.HasValue ? goal.RecurringAmount.Value.ToString("#,##0") : "";
                txtRecurringAmount.TextChanged += SmartFormatTextBox;
            }

            LoadInvestmentHistory();
        }

        private void LoadInvestmentHistory()
        {
            var history = _goalService.GetInvestmentHistory(_goalId, _user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            var displayList = history.Select(h => new
            {
                Tarih = h.InvestedAt.ToString("dd.MM.yyyy HH:mm"),
                Tutar = h.Amount.ToString("#,##0", tr) + " ₺"
            }).ToList();

            dgvHistory.DataSource = displayList;

            if (dgvHistory.Columns["Tutar"] != null)
            {
                dgvHistory.Columns["Tarih"]!.FillWeight = 60;
                dgvHistory.Columns["Tutar"]!.FillWeight = 40;
                dgvHistory.Columns["Tutar"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvHistory.Columns["Tutar"]!.DefaultCellStyle.ForeColor = SuccessColor;
            }

            if (history.Count == 0)
            {
                dgvHistory.DataSource = null;
            }
        }

        private void SmartFormatTextBox(object? sender, EventArgs e)
        {
            if (sender is TextBox txt && !string.IsNullOrWhiteSpace(txt.Text))
            {
                string value = new string(txt.Text.Where(char.IsDigit).ToArray());
                if (decimal.TryParse(value, out decimal amount))
                {
                    txt.TextChanged -= SmartFormatTextBox;
                    txt.Text = amount.ToString("#,##0");
                    txt.SelectionStart = txt.Text.Length;
                    txt.TextChanged += SmartFormatTextBox;
                }
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            string rawAmount = new string(txtTargetAmount.Text.Where(char.IsDigit).ToArray());
            if (decimal.TryParse(rawAmount, out decimal targetAmount))
            {
                if (_goalService.UpdateGoal(_goalId, _user.Id, txtGoalName.Text.Trim(), targetAmount, out string error))
                {
                    lblStatus.ForeColor = Color.LightGreen; lblStatus.Text = "Hedef güncellendi.";
                    LoadGoalData();
                }
                else { lblStatus.ForeColor = Color.Salmon; lblStatus.Text = error; }
            }
        }

        private void BtnInvest_Click(object? sender, EventArgs e)
        {
            string rawAmount = new string(txtInvestAmount.Text.Where(char.IsDigit).ToArray());
            if (decimal.TryParse(rawAmount, out decimal investAmount))
            {
                if (_goalService.InvestInGoal(_goalId, _user.Id, investAmount, out string error))
                {
                    lblStatus.ForeColor = Color.LightGreen; lblStatus.Text = "Yatırım başarıyla eklendi! Kasadan düşüldü.";
                    txtInvestAmount.Clear();
                    LoadGoalData();
                }
                else { lblStatus.ForeColor = Color.Salmon; lblStatus.Text = error; }
            }
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(AppBackColor); using (var path = Helpers.UIStyleHelper.GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } }; }
        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor); using (var path = Helpers.UIStyleHelper.GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
    }
}