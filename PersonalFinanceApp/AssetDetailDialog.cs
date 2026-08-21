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
        private readonly string _assetType;
        private readonly AssetService _assetService = new AssetService();
        private readonly AssetPriceAlertService _alertService = new AssetPriceAlertService();

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
        private ComboBox cmbAlertDirection = new ComboBox();
        private TextBox txtAlertThreshold = new TextBox();
        private Label lblAlertStatus = new Label();
        private Panel pnlAlertList = new Panel();

        public AssetDetailDialog(User user, string symbol)
        {
            _user = user;
            _symbol = symbol;
            _assetType = AssetCatalog.FindBySymbol(symbol)?.AssetType ?? "crypto";
            SetupUI();
            this.Load += (s, e) => Helpers.DarkTitleBarHelper.EnableDarkTitleBar(this);
            this.Shown += async (s, e) => await LoadDataAsync();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Varlık Detayı";
            this.Size = new Size(480, 940);
            this.BackColor = AppBackColor;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = TextLight;
            lblTitle.Left = 20; lblTitle.Top = 20; lblTitle.AutoSize = true;
            lblTitle.Text = AssetCatalog.FindBySymbol(_symbol)?.Name ?? _symbol;

            lblSummary.ForeColor = TextMuted;
            lblSummary.Font = new Font("Segoe UI", 10F);
            lblSummary.Left = 20; lblSummary.Top = 55; lblSummary.Width = 420; lblSummary.Height = 140;
            lblSummary.Text = "Yükleniyor...";

            Panel divider1 = new Panel { Left = 20, Top = 205, Width = 420, Height = 1, BackColor = AppTheme.HoverBackColor };

            Label lblBuyTitle = new Label { Text = "Satın Al", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 220, AutoSize = true };
            Label lblBuyHint = new Label { Text = "Miktar:", ForeColor = TextMuted, Left = 20, Top = 252, AutoSize = true };
            Panel pnlBuy = new Panel { Left = 20, Top = 278, Width = 200, Height = 38 };
            SetupSmoothContainer(pnlBuy, 8, CardBackColor);
            txtBuyQuantity.BorderStyle = BorderStyle.None; txtBuyQuantity.BackColor = CardBackColor; txtBuyQuantity.ForeColor = TextLight; txtBuyQuantity.Font = new Font("Segoe UI", 10F); txtBuyQuantity.Location = new Point(10, 9); txtBuyQuantity.Width = 180;
            pnlBuy.Controls.Add(txtBuyQuantity);

            Button btnBuy = new Button { Text = "➕ Al", Left = 240, Top = 278, Width = 200, Height = 38, Cursor = Cursors.Hand };
            SetupRoundedButton(btnBuy, SuccessColor, Color.White);
            btnBuy.Click += BtnBuy_Click;

            Panel divider2 = new Panel { Left = 20, Top = 335, Width = 420, Height = 1, BackColor = AppTheme.HoverBackColor };

            Label lblSellTitle = new Label { Text = "Sat", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 350, AutoSize = true };
            Label lblSellHint = new Label { Text = "Miktar:", ForeColor = TextMuted, Left = 20, Top = 382, AutoSize = true };
            Panel pnlSell = new Panel { Left = 20, Top = 408, Width = 200, Height = 38 };
            SetupSmoothContainer(pnlSell, 8, CardBackColor);
            txtSellQuantity.BorderStyle = BorderStyle.None; txtSellQuantity.BackColor = CardBackColor; txtSellQuantity.ForeColor = TextLight; txtSellQuantity.Font = new Font("Segoe UI", 10F); txtSellQuantity.Location = new Point(10, 9); txtSellQuantity.Width = 180;
            pnlSell.Controls.Add(txtSellQuantity);

            Button btnSell = new Button { Text = "➖ Sat", Left = 240, Top = 408, Width = 200, Height = 38, Cursor = Cursors.Hand };
            SetupRoundedButton(btnSell, DangerColor, Color.White);
            btnSell.Click += BtnSell_Click;

            lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Left = 20; lblStatus.Top = 458; lblStatus.Width = 420; lblStatus.Height = 30; lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            Panel divider3 = new Panel { Left = 20, Top = 495, Width = 420, Height = 1, BackColor = AppTheme.HoverBackColor };
            Label lblHistoryTitle = new Label { Text = "İşlem Geçmişi", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 510, AutoSize = true };

            Panel pnlHistoryWrapper = new Panel { Left = 20, Top = 545, Width = 420, Height = 190, Padding = new Padding(2, 4, 2, 4) };
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

            // --- Fiyat Alarmı ---
            Panel divider4 = new Panel { Left = 20, Top = 750, Width = 420, Height = 1, BackColor = AppTheme.HoverBackColor };
            Label lblAlertTitle = new Label { Text = "Fiyat Alarmı", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 765, AutoSize = true };

            cmbAlertDirection.Left = 20; cmbAlertDirection.Top = 800; cmbAlertDirection.Width = 170; cmbAlertDirection.Height = 32;
            cmbAlertDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAlertDirection.Font = new Font("Segoe UI", 9.5F);
            cmbAlertDirection.Items.Add("Üstüne çıkarsa");
            cmbAlertDirection.Items.Add("Altına inerse");
            cmbAlertDirection.SelectedIndex = 0;

            Panel pnlThreshold = new Panel { Left = 200, Top = 800, Width = 140, Height = 32 };
            SetupSmoothContainer(pnlThreshold, 8, CardBackColor);
            txtAlertThreshold.BorderStyle = BorderStyle.None; txtAlertThreshold.BackColor = CardBackColor; txtAlertThreshold.ForeColor = TextLight;
            txtAlertThreshold.Font = new Font("Segoe UI", 10F); txtAlertThreshold.Location = new Point(10, 6); txtAlertThreshold.Width = 120;
            txtAlertThreshold.PlaceholderText = "Eşik ₺";
            pnlThreshold.Controls.Add(txtAlertThreshold);

            Button btnAddAlert = new Button { Text = "Kur", Left = 350, Top = 800, Width = 90, Height = 32, Cursor = Cursors.Hand };
            SetupRoundedButton(btnAddAlert, AppTheme.AccentColor, Color.White);
            btnAddAlert.Click += BtnAddAlert_Click;

            lblAlertStatus.Left = 20; lblAlertStatus.Top = 838; lblAlertStatus.Width = 420; lblAlertStatus.Height = 20;
            lblAlertStatus.Font = new Font("Segoe UI", 8.5F);

            pnlAlertList.Left = 20; pnlAlertList.Top = 862; pnlAlertList.Width = 420; pnlAlertList.Height = 60;
            pnlAlertList.AutoScroll = true;
            pnlAlertList.BackColor = AppBackColor;

            this.Controls.Add(lblTitle); this.Controls.Add(lblSummary); this.Controls.Add(divider1);
            this.Controls.Add(lblBuyTitle); this.Controls.Add(lblBuyHint); this.Controls.Add(pnlBuy); this.Controls.Add(btnBuy);
            this.Controls.Add(divider2);
            this.Controls.Add(lblSellTitle); this.Controls.Add(lblSellHint); this.Controls.Add(pnlSell); this.Controls.Add(btnSell);
            this.Controls.Add(lblStatus);
            this.Controls.Add(divider3); this.Controls.Add(lblHistoryTitle); this.Controls.Add(pnlHistoryWrapper);
            this.Controls.Add(divider4); this.Controls.Add(lblAlertTitle);
            this.Controls.Add(cmbAlertDirection); this.Controls.Add(pnlThreshold); this.Controls.Add(btnAddAlert);
            this.Controls.Add(lblAlertStatus); this.Controls.Add(pnlAlertList);
        }

        private void BtnAddAlert_Click(object? sender, EventArgs e)
        {
            string raw = new string(txtAlertThreshold.Text.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray()).Replace(',', '.');
            if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal threshold) || threshold <= 0)
            {
                lblAlertStatus.ForeColor = Color.Salmon;
                lblAlertStatus.Text = "Geçerli bir eşik fiyat girin.";
                return;
            }

            string direction = cmbAlertDirection.SelectedIndex == 0 ? "above" : "below";
            if (_alertService.AddAlert(_user.Id, _symbol, _assetType, direction, threshold, out string error))
            {
                lblAlertStatus.ForeColor = Color.LightGreen;
                lblAlertStatus.Text = "Alarm kuruldu.";
                txtAlertThreshold.Clear();
                LoadAlerts();
            }
            else
            {
                lblAlertStatus.ForeColor = Color.Salmon;
                lblAlertStatus.Text = error;
            }
        }

        private void LoadAlerts()
        {
            pnlAlertList.Controls.Clear();
            var alerts = _alertService.GetActiveBySymbol(_user.Id, _symbol);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            int top = 0;
            foreach (var alert in alerts)
            {
                string arrow = alert.Direction == "above" ? "↑" : "↓";
                Label lbl = new Label
                {
                    Text = $"{arrow} {alert.ThresholdPrice.ToString("#,##0.00", tr)} ₺",
                    ForeColor = TextLight,
                    Left = 0,
                    Top = top,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9.5F)
                };
                Button btnRemove = new Button { Text = "✕", Left = 380, Top = top - 4, Width = 26, Height = 24, Cursor = Cursors.Hand };
                SetupRoundedButton(btnRemove, Color.FromArgb(80, 85, 105), Color.White);
                int alertId = alert.Id;
                btnRemove.Click += (s, e) =>
                {
                    _alertService.DeleteAlert(alertId, _user.Id);
                    LoadAlerts();
                };
                pnlAlertList.Controls.Add(lbl);
                pnlAlertList.Controls.Add(btnRemove);
                top += 30;
            }

            if (alerts.Count == 0)
            {
                pnlAlertList.Controls.Add(new Label { Text = "Aktif alarm yok.", ForeColor = TextMuted, Font = new Font("Segoe UI", 9F, FontStyle.Italic), AutoSize = true, Top = 0, Left = 0 });
            }
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
            LoadAlerts();
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
                Detay = $"{h.Quantity:0.####} adet · {h.PriceTry.ToString("#,##0.00", tr)} ₺/adet"
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
