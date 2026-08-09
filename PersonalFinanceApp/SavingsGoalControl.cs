using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;
using PersonalFinanceApp.Helpers;

namespace PersonalFinanceApp
{
    public partial class SavingsGoalControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly SavingsGoalService _goalService = new SavingsGoalService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color DangerColor = Color.FromArgb(220, 90, 90);

        private Panel pnlTop = new Panel();
        private Panel pnlGrid = new Panel();
        private Panel pnlBottom = new Panel();

        // Kasa Kapsülü
        private Panel pnlSafeBalanceCapsule = new Panel();
        private Label lblSafeBalance = new Label();
        private readonly AccountService _accountService = new AccountService();

        private TextBox txtGoalName = new TextBox();
        private TextBox txtTargetAmount = new TextBox();
        private Button btnAddGoal = new Button();
        private Label lblStatus = new Label();

        // Özet Kapsülü
        private Panel pnlSummaryCapsule = new Panel();
        private Label lblTotalSummary = new Label();

        private DataGridView dgvGoals = new DataGridView();
        private List<SavingsGoal> _cachedGoals = new List<SavingsGoal>();
        private Button btnDelete = new Button();

        private class GoalRow
        {
            public int ID { get; set; }
            public string Hedef { get; set; } = string.Empty;
            public string Biriken { get; set; } = string.Empty;
            public string HedefTutar { get; set; } = string.Empty;
            public string Ilerleme { get; set; } = string.Empty;
            public bool Tamamlandı { get; set; }
        }

        public SavingsGoalControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadGoals();
        }

        public void RefreshData()
        {
            LoadGoals();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            // --- ÜST PANEL ---
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 170;
            pnlTop.BackColor = AppBackColor;

            Label lblTitle = new Label { Text = "Hedefler", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true };

            Label lblGoalName = new Label { Text = "Yeni Hedef Adı:", Left = 20, Top = 70, ForeColor = TextMuted, AutoSize = true };
            Panel pnlGoalName = new Panel { Left = 20, Top = 95, Width = 250, Height = 38 };
            SetupSmoothContainer(pnlGoalName, 8, CardBackColor);
            txtGoalName.BorderStyle = BorderStyle.None; txtGoalName.Font = new Font("Segoe UI", 10.5F); txtGoalName.BackColor = CardBackColor; txtGoalName.ForeColor = TextLight; txtGoalName.Width = 230; txtGoalName.Location = new Point(10, 8);
            pnlGoalName.Controls.Add(txtGoalName);

            Label lblTargetAmount = new Label { Text = "Hedef Tutar:", Left = 290, Top = 70, ForeColor = TextMuted, AutoSize = true };
            Panel pnlTargetAmount = new Panel { Left = 290, Top = 95, Width = 150, Height = 38 };
            SetupSmoothContainer(pnlTargetAmount, 8, CardBackColor);
            txtTargetAmount.BorderStyle = BorderStyle.None; txtTargetAmount.Font = new Font("Segoe UI", 10.5F); txtTargetAmount.BackColor = CardBackColor; txtTargetAmount.ForeColor = TextLight; txtTargetAmount.Width = 130; txtTargetAmount.Location = new Point(10, 8);
            txtTargetAmount.TextChanged += TxtTargetAmount_TextChanged;
            pnlTargetAmount.Controls.Add(txtTargetAmount);

            btnAddGoal.Text = "➕ Hedef Ekle";
            btnAddGoal.Left = 460;
            btnAddGoal.Top = 95;
            btnAddGoal.Width = 150;
            btnAddGoal.Height = 38;
            btnAddGoal.Cursor = Cursors.Hand;
            SetupRoundedButton(btnAddGoal, AccentColor, Color.White, false);
            btnAddGoal.Click += BtnAddGoal_Click;

            lblStatus.Left = 20;
            lblStatus.Top = 140;
            lblStatus.Width = 700;
            lblStatus.Height = 25;
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);

            // --- SAĞ ÜST: KASA BAKİYESİ ŞEFFAF KAPSÜLÜ ---
            pnlSafeBalanceCapsule.Height = 38;
            pnlSafeBalanceCapsule.Top = 95; // "Hedef Ekle" butonu ile aynı hizaya getirildi
            // Belirgin kapsül rengi: Hafif yeşilimsi koyu zemin ve canlı yeşil çerçeve
            SetupTranslucentCapsule(pnlSafeBalanceCapsule, Color.FromArgb(35, 45, 40), Color.FromArgb(46, 204, 113));

            lblSafeBalance.Dock = DockStyle.Fill;
            lblSafeBalance.TextAlign = ContentAlignment.MiddleCenter;
            lblSafeBalance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblSafeBalance.ForeColor = Color.FromArgb(210, 255, 220);
            pnlSafeBalanceCapsule.Controls.Add(lblSafeBalance);

            pnlTop.Controls.Add(lblTitle); pnlTop.Controls.Add(lblGoalName); pnlTop.Controls.Add(pnlGoalName); pnlTop.Controls.Add(lblTargetAmount); pnlTop.Controls.Add(pnlTargetAmount); pnlTop.Controls.Add(btnAddGoal); pnlTop.Controls.Add(lblStatus);
            pnlTop.Controls.Add(pnlSafeBalanceCapsule);

            pnlTop.Resize += (s, e) => { pnlSafeBalanceCapsule.Left = pnlTop.Width - pnlSafeBalanceCapsule.Width - 40; };

            // --- ORTA PANEL (Tablo) ---
            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.Padding = new Padding(20, 0, 40, 0);
            pnlGrid.BackColor = AppBackColor;

            Panel pnlGridWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2, 6, 2, 6) };
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);

            dgvGoals.Dock = DockStyle.Fill;
            dgvGoals.AllowUserToAddRows = false; dgvGoals.AllowUserToDeleteRows = false; dgvGoals.AllowUserToResizeColumns = false; dgvGoals.AllowUserToResizeRows = false;
            dgvGoals.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgvGoals.MultiSelect = false; dgvGoals.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvGoals.RowHeadersVisible = false; dgvGoals.Font = new Font("Segoe UI", 9.5F); dgvGoals.RowTemplate.Height = 44;

            dgvGoals.BorderStyle = BorderStyle.None; dgvGoals.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvGoals.GridColor = Color.FromArgb(55, 60, 80); dgvGoals.BackgroundColor = CardBackColor;
            dgvGoals.DefaultCellStyle.BackColor = CardBackColor; dgvGoals.DefaultCellStyle.ForeColor = TextLight;
            dgvGoals.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvGoals.DefaultCellStyle.SelectionBackColor = AccentColor; dgvGoals.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvGoals.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 39, 54); dgvGoals.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvGoals.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 39, 54); dgvGoals.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvGoals.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); dgvGoals.EnableHeadersVisualStyles = false; dgvGoals.ColumnHeadersHeight = 40;

            dgvGoals.CellPainting += DgvGoals_CellPainting;
            dgvGoals.CellDoubleClick += DgvGoals_CellDoubleClick;
            dgvGoals.DataBindingComplete += (s, e) => ApplyAchievedStyling();

            pnlGridWrapper.Controls.Add(dgvGoals);
            pnlGrid.Controls.Add(pnlGridWrapper);

            // --- ALT PANEL (Özet Kapsülü ve Silme) ---
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 80;
            pnlBottom.Padding = new Padding(20, 15, 40, 15);
            pnlBottom.BackColor = AppBackColor;

            btnDelete.Text = "🗑️ Hedefi Sil";
            btnDelete.Left = 20;
            btnDelete.Top = 20;
            btnDelete.Width = 140;
            btnDelete.Height = 38;
            btnDelete.Cursor = Cursors.Hand;
            SetupRoundedButton(btnDelete, DangerColor, Color.White, false);
            btnDelete.Click += BtnDelete_Click;

            // --- SAĞ ALT: ÖZET ŞEFFAF KAPSÜLÜ ---
            pnlSummaryCapsule.Dock = DockStyle.Right;
            // Belirgin kapsül rengi: Hafif morumsu koyu zemin ve parlak mor/mavi çerçeve
            SetupTranslucentCapsule(pnlSummaryCapsule, Color.FromArgb(40, 38, 55), AccentColor);

            lblTotalSummary.Dock = DockStyle.Fill;
            lblTotalSummary.TextAlign = ContentAlignment.MiddleCenter;
            lblTotalSummary.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblTotalSummary.ForeColor = Color.FromArgb(230, 200, 120);

            pnlSummaryCapsule.Controls.Add(lblTotalSummary);

            pnlBottom.Controls.Add(btnDelete);
            pnlBottom.Controls.Add(pnlSummaryCapsule);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private void TxtTargetAmount_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTargetAmount.Text)) return;
            string value = new string(txtTargetAmount.Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(value)) { txtTargetAmount.Text = ""; return; }
            if (decimal.TryParse(value, out decimal amount)) { txtTargetAmount.TextChanged -= TxtTargetAmount_TextChanged; txtTargetAmount.Text = amount.ToString("#,##0"); txtTargetAmount.SelectionStart = txtTargetAmount.Text.Length; txtTargetAmount.TextChanged += TxtTargetAmount_TextChanged; }
        }

        private void DgvGoals_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex > 0 && e.ColumnIndex < dgvGoals.ColumnCount)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);
                using (Pen p = new Pen(Color.FromArgb(50, 55, 75), 1)) { e.Graphics!.DrawLine(p, e.CellBounds.Left, e.CellBounds.Top + 10, e.CellBounds.Left, e.CellBounds.Bottom - 10); }
                e.Handled = true;
            }
        }

        private void DgvGoals_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int goalId = Convert.ToInt32(dgvGoals.Rows[e.RowIndex].Cells["ID"].Value);
                using (var dialog = new SavingsGoalDialog(_user, goalId))
                {
                    dialog.ShowDialog();
                }
                LoadGoals();
            }
        }

        private void LoadGoals()
        {
            _cachedGoals = _goalService.GetUserGoals(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");
            var (_, safeBalance) = _accountService.GetBalances(_user.Id);

            lblSafeBalance.Text = _user.HideAmountsEnabled ? "🏦 Kasa: ••••••" : $"🏦 Kasa: {safeBalance.ToString("#,##0", tr)} ₺";
            Size safeTextSize = TextRenderer.MeasureText(lblSafeBalance.Text, lblSafeBalance.Font);
            pnlSafeBalanceCapsule.Width = safeTextSize.Width + 40;
            pnlSafeBalanceCapsule.Left = pnlTop.Width - pnlSafeBalanceCapsule.Width - 40;

            int totalCount = _cachedGoals.Count;
            int achievedCount = _cachedGoals.Count(g => g.IsAchieved);
            int pendingCount = totalCount - achievedCount;
            decimal totalAmount = _cachedGoals.Sum(g => g.TargetAmount);
            decimal totalInvested = _cachedGoals.Sum(g => g.CurrentAmount);

            var displayList = _cachedGoals.Select(g =>
            {
                decimal percent = g.TargetAmount > 0 ? (g.CurrentAmount / g.TargetAmount) * 100 : 0;
                if (g.IsAchieved) percent = 100;

                return new GoalRow
                {
                    ID = g.Id,
                    Hedef = g.GoalName,
                    Biriken = _user.HideAmountsEnabled ? "••••••" : g.CurrentAmount.ToString("#,##0", tr) + " ₺",
                    HedefTutar = _user.HideAmountsEnabled ? "••••••" : g.TargetAmount.ToString("#,##0", tr) + " ₺",
                    Ilerleme = $"% {percent:N1}",
                    Tamamlandı = g.IsAchieved
                };
            }).ToList();

            dgvGoals.DataSource = displayList;

            if (dgvGoals.Columns["ID"] != null)
            {
                dgvGoals.Columns["ID"]!.Visible = false;
                dgvGoals.Columns["Hedef"]!.FillWeight = 110;

                dgvGoals.Columns["Biriken"]!.FillWeight = 60;
                dgvGoals.Columns["Biriken"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                dgvGoals.Columns["HedefTutar"]!.HeaderText = "Hedef Tutar";
                dgvGoals.Columns["HedefTutar"]!.FillWeight = 60;
                dgvGoals.Columns["HedefTutar"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                dgvGoals.Columns["Ilerleme"]!.HeaderText = "İlerleme";
                dgvGoals.Columns["Ilerleme"]!.FillWeight = 40;
                dgvGoals.Columns["Ilerleme"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                dgvGoals.Columns["Tamamlandı"]!.FillWeight = 40;
                dgvGoals.Columns["Tamamlandı"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvGoals.Columns["Tamamlandı"]!.ReadOnly = true;
            }

            if (_user.HideAmountsEnabled)
            {
                lblTotalSummary.Text = $"🎯 Toplam Hedef: {totalCount}   |   ✅ Gerçekleşen: {achievedCount}   |   ⏳ Bekleyen: {pendingCount}   |   💸 Harcanan: ••••••   |   💰 Toplam Tutar: ••••••";
            }
            else
            {
                lblTotalSummary.Text = $"🎯 Toplam Hedef: {totalCount}   |   ✅ Gerçekleşen: {achievedCount}   |   ⏳ Bekleyen: {pendingCount}   |   💸 Harcanan: {totalInvested.ToString("#,##0", tr)} ₺   |   💰 Toplam Tutar: {totalAmount.ToString("#,##0", tr)} ₺";
            }

            Size textSize = TextRenderer.MeasureText(lblTotalSummary.Text, lblTotalSummary.Font);
            pnlSummaryCapsule.Width = textSize.Width + 50;
        }

        private void ApplyAchievedStyling()
        {
            foreach (DataGridViewRow row in dgvGoals.Rows)
            {
                int goalId = Convert.ToInt32(row.Cells["ID"].Value);
                var goal = _cachedGoals.FirstOrDefault(g => g.Id == goalId);
                if (goal == null) continue;

                if (goal.IsAchieved)
                {
                    row.DefaultCellStyle.Font = new Font(dgvGoals.Font, FontStyle.Strikeout);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 150);
                }
                else
                {
                    row.DefaultCellStyle.Font = dgvGoals.Font;
                    row.DefaultCellStyle.ForeColor = TextLight;
                }
            }
        }

        private void BtnAddGoal_Click(object? sender, EventArgs e)
        {
            string name = txtGoalName.Text.Trim();
            string rawAmount = new string(txtTargetAmount.Text.Where(char.IsDigit).ToArray());

            if (!decimal.TryParse(rawAmount, out decimal targetAmount) || targetAmount <= 0)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = "Lütfen geçerli bir hedef tutarı girin."; return;
            }

            if (_goalService.AddGoal(_user.Id, name, targetAmount, out string errorMessage))
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "Hedef başarıyla eklendi.";
                txtGoalName.Clear(); txtTargetAmount.Clear();
                LoadGoals();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = errorMessage;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvGoals.CurrentRow == null) return;
            if (MessageBox.Show("Bu hedefi silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int goalId = Convert.ToInt32(dgvGoals.CurrentRow.Cells["ID"].Value);
                _goalService.DeleteGoal(goalId, _user.Id);
                LoadGoals();
            }
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } }; pnl.SizeChanged += (s, e) => pnl.Invalidate(); }

        // Görünürlüğü artırılmış kapsül tasarımı (Daha tok zemin, net çizgiler)
        private void SetupTranslucentCapsule(Panel pnl, Color fillColor, Color borderColor)
        {
            pnl.BackColor = AppBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor);

                var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);

                using (var path = GetRoundedRectPath(rect, pnl.Height / 2))
                {
                    // İç dolgu (Artık daha belirgin)
                    using (var brush = new SolidBrush(fillColor))
                        e.Graphics.FillPath(brush, path);

                    // Dış çerçeve (Artık 2 piksel kalınlığında, dikkat çekici)
                    using (var pen = new Pen(borderColor, 2f))
                        e.Graphics.DrawPath(pen, path);
                }
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor, bool isOutlined) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius) { var path = new System.Drawing.Drawing2D.GraphicsPath(); int d = Math.Max(radius * 2, 1); path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path; }
    }
}