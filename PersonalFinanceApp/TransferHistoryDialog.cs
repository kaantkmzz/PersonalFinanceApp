using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class TransferHistoryDialog : Form
    {
        private readonly int _userId;
        private readonly AccountService _accountService = new AccountService();

        private static readonly Color AppBackColor = Color.FromArgb(37, 41, 59);
        private static readonly Color TextLight = Color.White;

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
            dgvHistory.BackgroundColor = Color.White;
            dgvHistory.BorderStyle = BorderStyle.None;
            dgvHistory.RowHeadersVisible = false;
            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvHistory.EnableHeadersVisualStyles = false;
            dgvHistory.RowTemplate.Height = 28;
            dgvHistory.AlternatingRowsDefaultCellStyle.BackColor = Color.White;

            // Standart hafif seçim rengi
            dgvHistory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 239, 246);
            dgvHistory.DefaultCellStyle.SelectionForeColor = Color.FromArgb(40, 40, 40);

            pnlContainer.Controls.Add(dgvHistory);
            this.Controls.Add(pnlContainer);
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