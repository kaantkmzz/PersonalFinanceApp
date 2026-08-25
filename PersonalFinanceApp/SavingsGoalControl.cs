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

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color DangerColor => AppTheme.DangerColor;

        private Panel pnlTop = new Panel();
        private Panel pnlGrid = new Panel();
        private Panel pnlBottom = new Panel();

        private TextBox txtGoalName = new TextBox();
        private TextBox txtTargetAmount = new TextBox();
        private Button btnAddGoal = new Button();
        private Label lblStatus = new Label();

        // Alt bilgi şeridi: her istatistik kendi yuvarlak köşeli kutucuğunda
        private FlowLayoutPanel pnlStatsFlow = new FlowLayoutPanel();

        private DataGridView dgvGoals = new DataGridView();
        private List<SavingsGoal> _cachedGoals = new List<SavingsGoal>();
        private Button btnDelete = new Button();

        private class GoalRow
        {
            public int ID { get; set; }
            public string Hedef { get; set; } = string.Empty;
            public string Biriken { get; set; } = string.Empty;
            public string HedefTutar { get; set; } = string.Empty;
            public string KalanGun { get; set; } = string.Empty;
            public string Ilerleme { get; set; } = string.Empty;
            public double IlerlemeRaw { get; set; }
            public bool Tamamlandı { get; set; }
        }

        // İlerleme çubuklarının, ekran her açıldığında/yenilendiğinde 0'dan gerçek değerine dolan animasyonu
        private float _goalsAnimProgress = 1f;
        private readonly System.Windows.Forms.Timer _goalsAnimTimer = new System.Windows.Forms.Timer { Interval = 16 };
        private readonly System.Diagnostics.Stopwatch _goalsAnimStopwatch = new System.Diagnostics.Stopwatch();
        private const int GoalsAnimDurationMs = 650;
        private static readonly Font ProgressPercentFont = new Font("Segoe UI", 8F);

        public SavingsGoalControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            _goalsAnimTimer.Tick += GoalsAnimTimer_Tick;
            LoadGoals();
            this.Disposed += (s, e) => _goalsAnimTimer.Dispose();
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
            txtGoalName.Name = "GoalName";
            txtGoalName.BorderStyle = BorderStyle.None; txtGoalName.Font = new Font("Segoe UI", 10.5F); txtGoalName.BackColor = CardBackColor; txtGoalName.ForeColor = TextLight; txtGoalName.Width = 230; txtGoalName.Location = new Point(10, 8);
            pnlGoalName.Controls.Add(txtGoalName);

            Label lblTargetAmount = new Label { Text = "Hedef Tutar:", Left = 290, Top = 70, ForeColor = TextMuted, AutoSize = true };
            Panel pnlTargetAmount = new Panel { Left = 290, Top = 95, Width = 150, Height = 38 };
            SetupSmoothContainer(pnlTargetAmount, 8, CardBackColor);
            txtTargetAmount.Name = "GoalTargetAmount";
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

            pnlTop.Controls.Add(lblTitle); pnlTop.Controls.Add(lblGoalName); pnlTop.Controls.Add(pnlGoalName); pnlTop.Controls.Add(lblTargetAmount); pnlTop.Controls.Add(pnlTargetAmount); pnlTop.Controls.Add(btnAddGoal); pnlTop.Controls.Add(lblStatus);

            // --- ORTA PANEL (Tablo) ---
            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.Padding = new Padding(20, 0, 40, 0);
            pnlGrid.BackColor = AppBackColor;

            Panel pnlGridWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2, 6, 2, 6) };
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);

            dgvGoals.Dock = DockStyle.Fill;
            dgvGoals.ReadOnly = true;
            dgvGoals.AllowUserToAddRows = false; dgvGoals.AllowUserToDeleteRows = false; dgvGoals.AllowUserToResizeColumns = false; dgvGoals.AllowUserToResizeRows = false;
            dgvGoals.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgvGoals.MultiSelect = false; dgvGoals.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvGoals.RowHeadersVisible = false; dgvGoals.Font = new Font("Segoe UI", 9.5F); dgvGoals.RowTemplate.Height = 50;

            dgvGoals.BorderStyle = BorderStyle.None; dgvGoals.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvGoals.GridColor = AppTheme.GridLineColor; dgvGoals.BackgroundColor = CardBackColor;
            dgvGoals.DefaultCellStyle.BackColor = CardBackColor; dgvGoals.DefaultCellStyle.ForeColor = TextLight;
            dgvGoals.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvGoals.DefaultCellStyle.SelectionBackColor = AccentColor; dgvGoals.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvGoals.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.HeaderBackColor; dgvGoals.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvGoals.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.HeaderBackColor; dgvGoals.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvGoals.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); dgvGoals.EnableHeadersVisualStyles = false; dgvGoals.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None; dgvGoals.ColumnHeadersHeight = 40;

            dgvGoals.CellPainting += DgvGoals_CellPainting;
            dgvGoals.CellDoubleClick += DgvGoals_CellDoubleClick;
            dgvGoals.DataBindingComplete += (s, e) => ApplyAchievedStyling();
            EnableDoubleBuffering(dgvGoals);

            pnlGridWrapper.Controls.Add(dgvGoals);
            pnlGrid.Controls.Add(pnlGridWrapper);

            // --- ALT PANEL (İstatistik kutucukları ve Silme) ---
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 96;
            pnlBottom.Padding = new Padding(20, 15, 30, 15);
            pnlBottom.BackColor = AppBackColor;

            btnDelete.Text = "🗑️ Hedefi Sil";
            btnDelete.Left = 20;
            btnDelete.Top = 29;
            btnDelete.Width = 140;
            btnDelete.Height = 38;
            btnDelete.Cursor = Cursors.Hand;
            SetupRoundedButton(btnDelete, DangerColor, Color.White, false);
            btnDelete.Click += BtnDelete_Click;

            // --- SAĞ ALT: İSTATİSTİK KUTUCUKLARI ---
            pnlStatsFlow.FlowDirection = FlowDirection.LeftToRight;
            pnlStatsFlow.AutoSize = true;
            pnlStatsFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlStatsFlow.WrapContents = false;
            pnlStatsFlow.BackColor = AppBackColor;
            pnlStatsFlow.Margin = new Padding(0);

            pnlBottom.Controls.Add(btnDelete);
            pnlBottom.Controls.Add(pnlStatsFlow);
            pnlBottom.Resize += (s, e) => PositionStatsFlow();

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
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.ColumnIndex >= dgvGoals.ColumnCount) return;

            if (dgvGoals.Columns[e.ColumnIndex].Name == "Ilerleme")
            {
                PaintProgressCell(e);
                return;
            }

            if (e.ColumnIndex > 0)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);
                using (Pen p = new Pen(AppTheme.RowSeparatorColor, 1)) { e.Graphics!.DrawLine(p, e.CellBounds.Left, e.CellBounds.Top + 10, e.CellBounds.Left, e.CellBounds.Bottom - 10); }
                e.Handled = true;
            }
        }

        // "İlerleme" hücresini düz yazı yerine dolan bir ilerleme çubuğu olarak çizer.
        private void PaintProgressCell(DataGridViewCellPaintingEventArgs e)
        {
            e.PaintBackground(e.CellBounds, true);
            var g = e.Graphics!;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            double rawPercent = 0;
            if (dgvGoals.Rows[e.RowIndex].Cells["IlerlemeRaw"].Value is double d) rawPercent = d;
            rawPercent = Math.Min(100, Math.Max(0, rawPercent));
            double animated = rawPercent * _goalsAnimProgress;

            string text = $"% {animated:N1}";
            var textRect = new Rectangle(e.CellBounds.Left, e.CellBounds.Top + 7, e.CellBounds.Width, 18);
            TextRenderer.DrawText(g, text, ProgressPercentFont, textRect, TextLight, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            const int barHeight = 8;
            const int pad = 14;
            var barRect = new Rectangle(e.CellBounds.Left + pad, e.CellBounds.Bottom - barHeight - 8, Math.Max(0, e.CellBounds.Width - pad * 2), barHeight);

            using (var trackPath = GetRoundedRectPath(barRect, barHeight / 2))
            using (var trackBrush = new SolidBrush(AppTheme.GridLineColor))
                g.FillPath(trackBrush, trackPath);

            int fillWidth = (int)(barRect.Width * (animated / 100.0));
            if (fillWidth > barHeight)
            {
                var fillRect = new Rectangle(barRect.Left, barRect.Top, fillWidth, barRect.Height);
                Color fillColor = rawPercent >= 99.9 ? AppTheme.SuccessColor : AccentColor;
                using var fillPath = GetRoundedRectPath(fillRect, barHeight / 2);
                using var fillBrush = new SolidBrush(fillColor);
                g.FillPath(fillBrush, fillPath);
            }

            e.Handled = true;
        }

        private void StartGoalsProgressAnimation()
        {
            _goalsAnimTimer.Stop();
            _goalsAnimProgress = 0f;
            _goalsAnimStopwatch.Restart();
            _goalsAnimTimer.Start();
        }

        private void GoalsAnimTimer_Tick(object? sender, EventArgs e)
        {
            double t = _goalsAnimStopwatch.Elapsed.TotalMilliseconds / GoalsAnimDurationMs;
            bool finished = t >= 1.0;
            if (finished) t = 1.0;

            _goalsAnimProgress = (float)(1 - Math.Pow(1 - t, 3));
            dgvGoals.Invalidate();

            if (finished) _goalsAnimTimer.Stop();
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

            int totalCount = _cachedGoals.Count;
            int achievedCount = _cachedGoals.Count(g => g.IsAchieved);
            int pendingCount = totalCount - achievedCount;
            decimal totalAmount = _cachedGoals.Sum(g => g.TargetAmount);
            decimal totalInvested = _cachedGoals.Sum(g => g.CurrentAmount);

            var displayList = _cachedGoals.Select(g =>
            {
                decimal percent = g.TargetAmount > 0 ? (g.CurrentAmount / g.TargetAmount) * 100 : 0;
                if (g.IsAchieved) percent = 100;

                string kalanGun = "";
                if (g.DueDate.HasValue && !g.IsAchieved)
                {
                    int days = (g.DueDate.Value.Date - DateTime.Today).Days;
                    kalanGun = days < 0 ? "Süresi geçti" : days == 0 ? "Bugün" : $"{days} gün";
                }

                return new GoalRow
                {
                    ID = g.Id,
                    Hedef = g.GoalName,
                    Biriken = _user.HideAmountsEnabled ? "••••••" : g.CurrentAmount.ToString("#,##0", tr) + " ₺",
                    HedefTutar = _user.HideAmountsEnabled ? "••••••" : g.TargetAmount.ToString("#,##0", tr) + " ₺",
                    KalanGun = kalanGun,
                    Ilerleme = $"% {percent:N1}",
                    IlerlemeRaw = (double)percent,
                    Tamamlandı = g.IsAchieved
                };
            }).ToList();

            dgvGoals.DataSource = displayList;

            if (dgvGoals.Columns["ID"] != null)
            {
                dgvGoals.Columns["ID"]!.Visible = false;
                dgvGoals.Columns["IlerlemeRaw"]!.Visible = false;
                dgvGoals.Columns["Hedef"]!.FillWeight = 110;

                dgvGoals.Columns["Biriken"]!.FillWeight = 60;
                dgvGoals.Columns["Biriken"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                dgvGoals.Columns["HedefTutar"]!.HeaderText = "Hedef Tutar";
                dgvGoals.Columns["HedefTutar"]!.FillWeight = 60;
                dgvGoals.Columns["HedefTutar"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                dgvGoals.Columns["KalanGun"]!.HeaderText = "Kalan Süre";
                dgvGoals.Columns["KalanGun"]!.FillWeight = 55;
                dgvGoals.Columns["KalanGun"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                dgvGoals.Columns["Ilerleme"]!.HeaderText = "İlerleme";
                dgvGoals.Columns["Ilerleme"]!.FillWeight = 55;
                dgvGoals.Columns["Ilerleme"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                dgvGoals.Columns["Tamamlandı"]!.FillWeight = 40;
                dgvGoals.Columns["Tamamlandı"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvGoals.Columns["Tamamlandı"]!.ReadOnly = true;
            }

            string harcananText = _user.HideAmountsEnabled ? "••••••" : totalInvested.ToString("#,##0", tr) + " ₺";
            string toplamText = _user.HideAmountsEnabled ? "••••••" : totalAmount.ToString("#,##0", tr) + " ₺";

            pnlStatsFlow.Controls.Clear();
            pnlStatsFlow.Controls.Add(CreateStatChip(totalCount.ToString(), "Toplam Hedef"));
            pnlStatsFlow.Controls.Add(CreateStatChip(achievedCount.ToString(), "Gerçekleşen"));
            pnlStatsFlow.Controls.Add(CreateStatChip(pendingCount.ToString(), "Bekleyen"));
            pnlStatsFlow.Controls.Add(CreateStatChip(harcananText, "Harcanan"));
            pnlStatsFlow.Controls.Add(CreateStatChip(toplamText, "Toplam Tutar"));
            PositionStatsFlow();

            StartGoalsProgressAnimation();
        }

        // Alt bilgi şeridindeki tek bir istatistik kutucuğu: etiket üstte, değer altta (ikonsuz, tablo ile aynı renkler).
        private Panel CreateStatChip(string value, string label)
        {
            Font valueFont = new Font("Segoe UI", 12F, FontStyle.Bold);
            Font labelFont = new Font("Segoe UI", 9F);

            Size valueSize = TextRenderer.MeasureText(value, valueFont, Size.Empty, TextFormatFlags.NoPadding);
            Size labelSize = TextRenderer.MeasureText(label, labelFont, Size.Empty, TextFormatFlags.NoPadding);
            int chipWidth = Math.Max(valueSize.Width, labelSize.Width) + 32;
            const int chipHeight = 70;

            Panel chip = new Panel { Width = chipWidth, Height = chipHeight, Margin = new Padding(6, 0, 6, 0) };
            chip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(chip.Parent?.BackColor ?? AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, chip.Width - 1, chip.Height - 1), 10);
                using var brush = new SolidBrush(CardBackColor);
                e.Graphics.FillPath(brush, path);
            };

            Label lblLabel = new Label { Text = label, Font = labelFont, ForeColor = TextMuted, AutoSize = true, BackColor = Color.Transparent };
            lblLabel.Left = (chipWidth - labelSize.Width) / 2;
            lblLabel.Top = 12;

            Label lblValue = new Label { Text = value, Font = valueFont, ForeColor = TextLight, AutoSize = true, BackColor = Color.Transparent };
            lblValue.Left = (chipWidth - valueSize.Width) / 2;
            lblValue.Top = 36;

            chip.Controls.Add(lblLabel);
            chip.Controls.Add(lblValue);
            return chip;
        }

        // Şeridin sağ kenarı, tablonun sağ kenarıyla (pnlGrid'in 40px iç boşluğu + kart sarmalayıcının 2px'i) aynı hizada bitsin.
        private const int GridRightInset = 42;

        private void PositionStatsFlow()
        {
            pnlStatsFlow.Left = Math.Max(180, pnlBottom.ClientSize.Width - pnlStatsFlow.PreferredSize.Width - GridRightInset);
            pnlStatsFlow.Top = (pnlBottom.ClientSize.Height - pnlStatsFlow.PreferredSize.Height) / 2;
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

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor, bool isOutlined) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor); using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius) { var path = new System.Drawing.Drawing2D.GraphicsPath(); int d = Math.Max(radius * 2, 1); path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path; }

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                control,
                new object[] { true });
        }
    }
}