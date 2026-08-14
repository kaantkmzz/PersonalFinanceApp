using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public class ReportHistoryDialog : Form
    {
        private readonly User _user;
        private readonly ReportHistoryService _historyService = new ReportHistoryService();
        private List<ReportHistoryEntry> _entries = new List<ReportHistoryEntry>();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;

        private DataGridView dgvHistory = new DataGridView();
        private Panel pnlGridWrapper = new Panel();
        private Label lblEmpty = new Label();

        public ReportHistoryDialog(User user)
        {
            _user = user;
            SetupUI();
            LoadHistory();
            this.Load += (s, e) => DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new Size(680, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppBackColor;
            this.Text = "Geçmiş Raporlar";
            this.Font = new Font("Segoe UI", 9F);

            Label lblTitle = new Label { Text = "Geçmiş Raporlar", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true };
            this.Controls.Add(lblTitle);

            pnlGridWrapper.Left = 20; pnlGridWrapper.Top = 55; pnlGridWrapper.Width = 640; pnlGridWrapper.Height = 385;
            pnlGridWrapper.Padding = new Padding(2, 6, 2, 6);
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);

            dgvHistory.Dock = DockStyle.Fill;
            dgvHistory.ReadOnly = true; dgvHistory.AllowUserToAddRows = false; dgvHistory.AllowUserToDeleteRows = false;
            dgvHistory.AllowUserToResizeColumns = false; dgvHistory.AllowUserToResizeRows = false;
            dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgvHistory.MultiSelect = false;
            dgvHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; dgvHistory.RowHeadersVisible = false;
            dgvHistory.Font = new Font("Segoe UI", 9.5F); dgvHistory.RowTemplate.Height = 40;

            dgvHistory.BorderStyle = BorderStyle.None; dgvHistory.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvHistory.GridColor = AppTheme.GridLineColor; dgvHistory.BackgroundColor = CardBackColor;

            dgvHistory.DefaultCellStyle.BackColor = CardBackColor; dgvHistory.DefaultCellStyle.ForeColor = TextLight;
            dgvHistory.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvHistory.DefaultCellStyle.SelectionBackColor = AccentColor;
            dgvHistory.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.HeaderBackColor; dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.HeaderBackColor; dgvHistory.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); dgvHistory.EnableHeadersVisualStyles = false; dgvHistory.ColumnHeadersHeight = 38;

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Periyot", HeaderText = "Periyot", FillWeight = 65 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tarih", HeaderText = "Tarih Aralığı", FillWeight = 130 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Gelir", HeaderText = "Gelir", FillWeight = 70 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Gider", HeaderText = "Gider", FillWeight = 70 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Net", HeaderText = "Net", FillWeight = 70 });

            var btnColumn = new DataGridViewButtonColumn
            {
                Name = "Goruntule",
                HeaderText = "",
                Text = "Görüntüle",
                UseColumnTextForButtonValue = true,
                FillWeight = 70,
                FlatStyle = FlatStyle.Flat
            };
            btnColumn.DefaultCellStyle.BackColor = AccentColor;
            btnColumn.DefaultCellStyle.ForeColor = Color.White;
            btnColumn.DefaultCellStyle.SelectionBackColor = AccentColor;
            btnColumn.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvHistory.Columns.Add(btnColumn);

            dgvHistory.CellContentClick += DgvHistory_CellContentClick;

            pnlGridWrapper.Controls.Add(dgvHistory);
            this.Controls.Add(pnlGridWrapper);

            lblEmpty.Text = "Henüz kaydedilmiş bir rapor yok.\nSeçili periyot (günlük/haftalık/aylık) tamamlandığında burada görünecek.";
            lblEmpty.Left = 20; lblEmpty.Top = 55; lblEmpty.Width = 640; lblEmpty.Height = 60;
            lblEmpty.ForeColor = TextMuted;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.Visible = false;
            this.Controls.Add(lblEmpty);

            Button btnClose = new Button { Text = "Kapat", Left = 480, Top = 450, Width = 180, Height = 36, Cursor = Cursors.Hand };
            SetupRoundedButton(btnClose, Color.FromArgb(80, 85, 105), Color.White);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void LoadHistory()
        {
            _entries = _historyService.GetHistory(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            dgvHistory.Rows.Clear();
            foreach (var entry in _entries)
            {
                dgvHistory.Rows.Add(
                    ReportPeriodHelper.GetLabel(entry.PeriodType),
                    $"{entry.PeriodStart:dd.MM.yyyy HH:mm} - {entry.PeriodEnd:dd.MM.yyyy HH:mm}",
                    entry.TotalIncome.ToString("#,##0", tr) + " ₺",
                    entry.TotalExpense.ToString("#,##0", tr) + " ₺",
                    entry.NetBalance.ToString("#,##0", tr) + " ₺",
                    "Görüntüle"
                );
            }

            bool hasEntries = _entries.Count > 0;
            pnlGridWrapper.Visible = hasEntries;
            lblEmpty.Visible = !hasEntries;
        }

        private void DgvHistory_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _entries.Count) return;
            if (dgvHistory.Columns[e.ColumnIndex].Name != "Goruntule") return;

            using (var dialog = new SavedReportViewDialog(_entries[e.RowIndex]))
            {
                dialog.ShowDialog();
            }
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor)
        {
            pnl.BackColor = AppBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor);
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Font = new Font("Segoe UI", 10F);
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8))
                using (var brush = new SolidBrush(bgColor))
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
