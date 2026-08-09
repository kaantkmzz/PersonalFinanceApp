using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color SuccessColor = Color.FromArgb(70, 180, 120);

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
            this.Text = "Hedef Detayı ve Yatırım";
            this.Size = new Size(420, 500);
            this.BackColor = AppBackColor;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // --- HEDEF BİLGİLERİ ---
            Label lblTitle = new Label { Text = "Hedef Bilgileri", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 20, AutoSize = true };

            Label lblName = new Label { Text = "Hedef Adı:", ForeColor = Color.LightGray, Left = 20, Top = 60, AutoSize = true };
            Panel pnlName = new Panel { Left = 20, Top = 80, Width = 360, Height = 36 };
            SetupSmoothContainer(pnlName, 8, CardBackColor);
            txtGoalName.BorderStyle = BorderStyle.None; txtGoalName.BackColor = CardBackColor; txtGoalName.ForeColor = TextLight; txtGoalName.Font = new Font("Segoe UI", 10F); txtGoalName.Location = new Point(10, 8); txtGoalName.Width = 340;
            pnlName.Controls.Add(txtGoalName);

            Label lblTarget = new Label { Text = "Hedef Tutar:", ForeColor = Color.LightGray, Left = 20, Top = 130, AutoSize = true };
            Panel pnlTarget = new Panel { Left = 20, Top = 150, Width = 170, Height = 36 };
            SetupSmoothContainer(pnlTarget, 8, CardBackColor);
            txtTargetAmount.BorderStyle = BorderStyle.None; txtTargetAmount.BackColor = CardBackColor; txtTargetAmount.ForeColor = TextLight; txtTargetAmount.Font = new Font("Segoe UI", 10F); txtTargetAmount.Location = new Point(10, 8); txtTargetAmount.Width = 150;
            txtTargetAmount.TextChanged += SmartFormatTextBox;
            pnlTarget.Controls.Add(txtTargetAmount);

            Button btnUpdate = new Button { Text = "💾 Güncelle", Left = 210, Top = 150, Width = 170, Height = 36, Cursor = Cursors.Hand };
            SetupRoundedButton(btnUpdate, Color.FromArgb(80, 85, 105), Color.White);
            btnUpdate.Click += BtnUpdate_Click;

            // --- YATIRIM (ÖDEME) ALANI ---
            Panel divider = new Panel { Left = 20, Top = 210, Width = 360, Height = 1, BackColor = Color.FromArgb(60, 65, 85) };

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

            lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Left = 20; lblStatus.Top = 350; lblStatus.Width = 360; lblStatus.Height = 40; lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            this.Controls.Add(lblTitle); this.Controls.Add(lblName); this.Controls.Add(pnlName); this.Controls.Add(lblTarget); this.Controls.Add(pnlTarget); this.Controls.Add(btnUpdate);
            this.Controls.Add(divider); this.Controls.Add(lblInvestTitle); this.Controls.Add(lblProgress); this.Controls.Add(pnlInvest); this.Controls.Add(btnInvest); this.Controls.Add(lblStatus);
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