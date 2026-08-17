using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public class AssetDetailDialog : Form
    {
        private readonly User _user;
        private readonly string _symbol;
        private readonly AssetService _assetService = new AssetService();

        public bool ChangesMade { get; private set; }

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color SuccessColor => AppTheme.SuccessColor;
        private static Color DangerColor => AppTheme.DangerColor;
        private static Color IncomeColor => AppTheme.IncomeColor;
        private static Color ExpenseColor => AppTheme.ExpenseColor;

        private Label lblTitle = new Label();
        private Label lblSummary = new Label();
        private TextBox txtBuyQuantity = new TextBox();
        private TextBox txtSellQuantity = new TextBox();
        private Label lblStatus = new Label();
        private DataGridView dgvHistory = new DataGridView();

        public AssetDetailDialog(User user, string symbol)
        {
            _user = user;
            _symbol = symbol;
            SetupUI();
            this.Load += (s, e) => Helpers.DarkTitleBarHelper.EnableDarkTitleBar(this);
            this.Shown += async (s, e) => await LoadDataAsync();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Varlık Detayı";
            this.Size = new Size(420, 660);
            this.BackColor = AppBackColor;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = TextLight;
            lblTitle.Left = 20; lblTitle.Top = 20; lblTitle.AutoSize = true;
            lblTitle.Text = _symbol;

            lblSummary.ForeColor = TextMuted;
            lblSummary.Left = 20; lblSummary.Top = 55; lblSummary.Width = 360; lblSummary.Height = 90;
            lblSummary.Text = "Yükleniyor...";

            Panel divider1 = new Panel { Left = 20, Top = 155, Width = 360, Height = 1, BackColor = AppTheme.HoverBackColor };

            Label lblBuyTitle = new Label { Text = "Satın Al", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 170, AutoSize = true };
            Label lblBuyHint = new Label { Text = "Miktar:", ForeColor = TextMuted, Left = 20, Top = 202, AutoSize = true };
            Panel pnlBuy = new Panel { Left = 20, Top = 228, Width = 170, Height = 36 };
            SetupSmoothContainer(pnlBuy, 8, CardBackColor);
            txtBuyQuantity.BorderStyle = BorderStyle.None; txtBuyQuantity.BackColor = CardBackColor; txtBuyQuantity.ForeColor = TextLight; txtBuyQuantity.Font = new Font("Segoe UI", 10F); txtBuyQuantity.Location = new Point(10, 8); txtBuyQuantity.Width = 150;
            pnlBuy.Controls.Add(txtBuyQuantity);

            Button btnBuy = new Button { Text = "➕ Al", Left = 210, Top = 228, Width = 170, Height = 36, Cursor = Cursors.Hand };
            SetupRoundedButton(btnBuy, SuccessColor, Color.White);
            btnBuy.Click += BtnBuy_Click;

            Panel divider2 = new Panel { Left = 20, Top = 280, Width = 360, Height = 1, BackColor = AppTheme.HoverBackColor };

            Label lblSellTitle = new Label { Text = "Sat", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 295, AutoSize = true };
            Label lblSellHint = new Label { Text = "Miktar:", ForeColor = TextMuted, Left = 20, Top = 327, AutoSize = true };
            Panel pnlSell = new Panel { Left = 20, Top = 353, Width = 170, Height = 36 };
            SetupSmoothContainer(pnlSell, 8, CardBackColor);
            txtSellQuantity.BorderStyle = BorderStyle.None; txtSellQuantity.BackColor = CardBackColor; txtSellQuantity.ForeColor = TextLight; txtSellQuantity.Font = new Font("Segoe UI", 10F); txtSellQuantity.Location = new Point(10, 8); txtSellQuantity.Width = 150;
            pnlSell.Controls.Add(txtSellQuantity);

            Button btnSell = new Button { Text = "➖ Sat", Left = 210, Top = 353, Width = 170, Height = 36, Cursor = Cursors.Hand };
            SetupRoundedButton(btnSell, DangerColor, Color.White);
            btnSell.Click += BtnSell_Click;

            lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Left = 20; lblStatus.Top = 400; lblStatus.Width = 360; lblStatus.Height = 30; lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            Panel divider3 = new Panel { Left = 20, Top = 435, Width = 360, Height = 1, BackColor = AppTheme.HoverBackColor };
            Label lblHistoryTitle = new Label { Text = "İşlem Geçmişi", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 450, AutoSize = true };

            Panel pnlHistoryWrapper = new Panel { Left = 20, Top = 485, Width = 360, Height = 135, Padding = new Padding(2, 4, 2, 4) };
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
            dgvHistory.Font = new Font("Segoe UI", 9F);
            dgvHistory.RowTemplate.Height = 30;
            dgvHistory.BorderStyle = BorderStyle.None;
            dgvHistory.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvHistory.GridColor = AppTheme.HoverBackColor;
            dgvHistory.BackgroundColor = CardBackColor;
            dgvHistory.DefaultCellStyle.BackColor = CardBackColor;
            dgvHistory.DefaultCellStyle.ForeColor = TextLight;
            dgvHistory.DefaultCellStyle.SelectionBackColor = CardBackColor;
            dgvHistory.DefaultCellStyle.SelectionForeColor = TextLight;
            dgvHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvHistory.CellFormatting += DgvHistory_CellFormatting;

            pnlHistoryWrapper.Controls.Add(dgvHistory);

            this.Controls.Add(lblTitle); this.Controls.Add(lblSummary); this.Controls.Add(divider1);
            this.Controls.Add(lblBuyTitle); this.Controls.Add(lblBuyHint); this.Controls.Add(pnlBuy); this.Controls.Add(btnBuy);
            this.Controls.Add(divider2);
            this.Controls.Add(lblSellTitle); this.Controls.Add(lblSellHint); this.Controls.Add(pnlSell); this.Controls.Add(btnSell);
            this.Controls.Add(lblStatus);
            this.Controls.Add(divider3); this.Controls.Add(lblHistoryTitle); this.Controls.Add(pnlHistoryWrapper);
        }

        private void DgvHistory_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvHistory.Columns[e.ColumnIndex].Name != "Yön" || e.RowIndex < 0) return;
            var value = dgvHistory.Rows[e.RowIndex].Cells["YönRaw"].Value as string;
            e.CellStyle!.ForeColor = value == "buy" ? IncomeColor : ExpenseColor;
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            var view = await _assetService.GetHoldingWithLivePriceAsync(_user.Id, _symbol);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            if (view == null)
            {
                lblSummary.Text = "Bilgi alınamadı.";
            }
            else
            {
                string priceText = view.CurrentPriceTry.HasValue ? view.CurrentPriceTry.Value.ToString("#,##0.00", tr) + " ₺" : "alınamadı";
                string valueText = view.CurrentValueTry.HasValue ? view.CurrentValueTry.Value.ToString("#,##0.00", tr) + " ₺" : "-";
                string plText = view.ProfitLossTry.HasValue
                    ? $"{view.ProfitLossTry.Value:+#,##0.00;-#,##0.00} ₺ (%{view.ProfitLossPercent:+0.00;-0.00})"
                    : "-";

                lblSummary.Text = $"Miktar: {view.Quantity:0.########}\nOrtalama Maliyet: {view.AvgCostTry.ToString("#,##0.00", tr)} ₺\nGüncel Fiyat: {priceText}\nGüncel Değer: {valueText}\nKâr/Zarar: {plText}";
            }

            LoadHistory();
        }

        private void LoadHistory()
        {
            var history = _assetService.GetTransactionHistory(_user.Id, _symbol);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            var displayList = history.Select(h => new
            {
                Tarih = h.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                Yön = h.Direction == "buy" ? "Alım" : "Satım",
                YönRaw = h.Direction,
                Detay = $"{h.Quantity:0.####} @ {h.PriceTry.ToString("#,##0.00", tr)} ₺"
            }).ToList();

            dgvHistory.DataSource = displayList;

            if (dgvHistory.Columns["Tarih"] != null)
            {
                dgvHistory.Columns["Tarih"]!.FillWeight = 45;
                dgvHistory.Columns["Yön"]!.FillWeight = 20;
                dgvHistory.Columns["YönRaw"]!.Visible = false;
                dgvHistory.Columns["Detay"]!.FillWeight = 35;
            }

            if (history.Count == 0) dgvHistory.DataSource = null;
        }

        private async void BtnBuy_Click(object? sender, EventArgs e)
        {
            string raw = new string(txtBuyQuantity.Text.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray()).Replace(',', '.');
            if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal quantity) || quantity <= 0)
            {
                lblStatus.ForeColor = Color.Salmon; lblStatus.Text = "Geçerli bir miktar girin."; return;
            }

            var (success, error) = await _assetService.BuyAssetAsync(_user.Id, _symbol, quantity);
            if (success)
            {
                lblStatus.ForeColor = Color.LightGreen; lblStatus.Text = "Alım başarılı.";
                txtBuyQuantity.Clear();
                ChangesMade = true;
                await LoadDataAsync();
            }
            else
            {
                lblStatus.ForeColor = Color.Salmon; lblStatus.Text = error;
            }
        }

        private async void BtnSell_Click(object? sender, EventArgs e)
        {
            string raw = new string(txtSellQuantity.Text.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray()).Replace(',', '.');
            if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal quantity) || quantity <= 0)
            {
                lblStatus.ForeColor = Color.Salmon; lblStatus.Text = "Geçerli bir miktar girin."; return;
            }

            var (success, error) = await _assetService.SellAssetAsync(_user.Id, _symbol, quantity);
            if (success)
            {
                lblStatus.ForeColor = Color.LightGreen; lblStatus.Text = "Satış başarılı.";
                txtSellQuantity.Clear();
                ChangesMade = true;
                await LoadDataAsync();
            }
            else
            {
                lblStatus.ForeColor = Color.Salmon; lblStatus.Text = error;
            }
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(AppBackColor); using (var path = Helpers.UIStyleHelper.GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } }; }
        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor); using (var path = Helpers.UIStyleHelper.GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }
    }
}
