using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class TransferHistoryDialog : Form
    {
        private readonly int _userId;
        private readonly AccountService _accountService = new AccountService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;

        private DataGridView dgvHistory = new DataGridView();

        public TransferHistoryDialog(int userId)
        {
            _userId = userId;
            InitializeComponent();
            SetupUI();
            LoadHistory();
            this.Load += (s, e) => DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Transfer Geçmişi";
            this.Width = 560;
            this.Height = 460;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9.5F);

            // Panel + Padding ile kesin, simetrik ortalama sağlıyoruz
            Panel pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = AppBackColor
            };

            dgvHistory.Dock = DockStyle.Fill;
            dgvHistory.ReadOnly = true;
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.AllowUserToDeleteRows = false;
            dgvHistory.AllowUserToResizeColumns = false;
            dgvHistory.AllowUserToResizeRows = false;
            dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistory.MultiSelect = false;
            dgvHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvHistory.BackgroundColor = CardBackColor;
            dgvHistory.BorderStyle = BorderStyle.None;
            dgvHistory.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvHistory.GridColor = AppTheme.GridLineColor;
            dgvHistory.RowHeadersVisible = false;
            dgvHistory.Font = new Font("Segoe UI", 9.5F);
            dgvHistory.DefaultCellStyle.BackColor = CardBackColor;
            dgvHistory.DefaultCellStyle.ForeColor = TextLight;
            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.HeaderBackColor;
            dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.HeaderBackColor;
            dgvHistory.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvHistory.EnableHeadersVisualStyles = false;
            dgvHistory.ColumnHeadersHeight = 36;
            dgvHistory.RowTemplate.Height = 34;
            dgvHistory.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;

            dgvHistory.DefaultCellStyle.SelectionBackColor = AccentColor;
            dgvHistory.DefaultCellStyle.SelectionForeColor = Color.White;

            Panel pnlGridWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2, 6, 2, 6) };
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);
            pnlGridWrapper.Controls.Add(dgvHistory);

            pnlContainer.Controls.Add(pnlGridWrapper);
            this.Controls.Add(pnlContainer);
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

        private void LoadHistory()
        {
            var history = _accountService.GetTransferHistory(_userId);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            var displayList = history.Select(h => new
            {
                Tarih = h.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                Yön = h.Direction == "wallet_to_safe" ? "Cüzdan → Kasa" : "Kasa → Cüzdan",
                Tutar = h.Amount.ToString("#,##0", tr) + " ₺"
            }).ToList();

            dgvHistory.DataSource = displayList;

            if (dgvHistory.Columns["Tarih"] != null)
            {
                dgvHistory.Columns["Tarih"]!.FillWeight = 45;
                dgvHistory.Columns["Yön"]!.FillWeight = 35;
                dgvHistory.Columns["Tutar"]!.FillWeight = 20;
            }
        }
    }
}