using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class AssetControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly AssetService _assetService = new AssetService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color IncomeColor => AppTheme.IncomeColor;
        private static Color ExpenseColor => AppTheme.ExpenseColor;

        private Panel pnlTop = new Panel();
        private Panel pnlGrid = new Panel();
        private Panel pnlBottom = new Panel();

        private ComboBox cmbSearch = new ComboBox();
        private Label lblSelectedPrice = new Label();
        private TextBox txtBuyQuantity = new TextBox();
        private Button btnBuy = new Button();
        private Label lblStatus = new Label();

        // Miktar yerine ₺ tutarı girip alım yapabilmek için (bkz. BtnBuy_Click): "3 milyon adet" gibi
        // ondalıklı miktar hesaplamak yerine kullanıcı doğrudan "10.000 ₺'lik al" diyebiliyor.
        private bool _buyByAmount = true;
        private decimal? _selectedPriceTry;
        private Button btnModeQuantity = new Button();
        private Button btnModeAmount = new Button();
        private Label lblConversionHint = new Label();

        private FlowLayoutPanel pnlStatsFlow = new FlowLayoutPanel();
        private DataGridView dgvHoldings = new DataGridView();

        private readonly System.Windows.Forms.Timer _refreshTimer = new System.Windows.Forms.Timer { Interval = 30000 };

        private class HoldingRow
        {
            public string Sembol { get; set; } = string.Empty;
            public string Miktar { get; set; } = string.Empty;
            public string OrtMaliyet { get; set; } = string.Empty;
            public string GuncelFiyat { get; set; } = string.Empty;
            public string GuncelDeğer { get; set; } = string.Empty;
            public string KarZarar { get; set; } = string.Empty;
            public double KarZararRaw { get; set; }
            public bool FiyatVar { get; set; }
        }

        public AssetControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            _refreshTimer.Tick += async (s, e) => await LoadHoldingsAsync();
            _refreshTimer.Start();
            this.Disposed += (s, e) => _refreshTimer.Dispose();
            _ = LoadHoldingsAsync();
        }

        public void RefreshData()
        {
            _ = LoadHoldingsAsync();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            // --- ÜST PANEL ---
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 190;
            pnlTop.BackColor = AppBackColor;

            Label lblTitle = new Label { Text = "Varlıklarım", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true };

            // Varlıklarım'ın artık kendi bakiyesi yok — alım/satım doğrudan Kasa'dan yapılıyor (bkz.
            // AssetService.BuyAssetAsync/SellAssetAsync), bu yüzden burada ayrı bir bakiye etiketi yok.

            // İkinci satır: arama/alım araç çubuğu
            const int row2Top = 95;

            // Etiket ile kutunun arasında yeterli boşluk olsun diye (dar aralık bazı DPI ayarlarında
            // "Varlık Ara:" yazısının kutunun üstüne/içine binmesine yol açıyordu) satır aralığı 24'ten 28'e çıkarıldı.
            const int rowGap = 28;
            Label lblSearch = new Label { Text = "Varlık Ara:", Left = 20, Top = row2Top, ForeColor = TextMuted, AutoSize = true };
            Panel pnlSearch = new Panel { Left = 20, Top = row2Top + rowGap, Width = 220, Height = 34 };
            cmbSearch.Font = new Font("Segoe UI", 10F);
            cmbSearch.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var item in AssetCatalog.Items)
            {
                cmbSearch.Items.Add($"{item.Symbol} — {item.Name}");
            }
            cmbSearch.SelectedIndexChanged += CmbSearch_SelectedIndexChanged;
            pnlSearch.Controls.Add(cmbSearch);
            SetupCustomComboBox(pnlSearch, cmbSearch);

            lblSelectedPrice.Left = 20; lblSelectedPrice.Top = row2Top + rowGap + 32; lblSelectedPrice.Width = 220; lblSelectedPrice.Height = 26;
            lblSelectedPrice.ForeColor = TextMuted;
            lblSelectedPrice.TextAlign = ContentAlignment.MiddleLeft;

            // "Miktar" (adet) yerine "₺ Tutar" girerek de alım yapılabiliyor — küçük dilim
            // (segmented) bir geçiş, hangi modun aktif olduğunu vurgulu renkle gösteriyor.
            btnModeAmount.Text = "₺ Tutar"; btnModeAmount.Left = 260; btnModeAmount.Top = row2Top - 1; btnModeAmount.Width = 62; btnModeAmount.Height = 20; btnModeAmount.Cursor = Cursors.Hand; btnModeAmount.Font = new Font("Segoe UI", 7.5F);
            btnModeQuantity.Text = "Adet"; btnModeQuantity.Left = 326; btnModeQuantity.Top = row2Top - 1; btnModeQuantity.Width = 62; btnModeQuantity.Height = 20; btnModeQuantity.Cursor = Cursors.Hand; btnModeQuantity.Font = new Font("Segoe UI", 7.5F);
            btnModeAmount.Click += (s, e) => SetBuyMode(true);
            btnModeQuantity.Click += (s, e) => SetBuyMode(false);

            Panel pnlQty = new Panel { Left = 260, Top = row2Top + rowGap, Width = 130, Height = 34 };
            SetupSmoothContainer(pnlQty, 8, CardBackColor);
            txtBuyQuantity.BorderStyle = BorderStyle.None; txtBuyQuantity.Font = new Font("Segoe UI", 10F); txtBuyQuantity.BackColor = CardBackColor; txtBuyQuantity.ForeColor = TextLight; txtBuyQuantity.Width = 110; txtBuyQuantity.Location = new Point(10, 6);
            txtBuyQuantity.TextChanged += (s, e) => UpdateConversionHint();
            pnlQty.Controls.Add(txtBuyQuantity);

            lblConversionHint.Left = 260; lblConversionHint.Top = row2Top + rowGap + 36; lblConversionHint.Width = 130; lblConversionHint.Height = 18;
            lblConversionHint.ForeColor = TextMuted; lblConversionHint.Font = new Font("Segoe UI", 8F); lblConversionHint.AutoEllipsis = true;

            btnBuy.Text = "➕ Satın Al";
            btnBuy.Left = 410; btnBuy.Top = row2Top + rowGap; btnBuy.Width = 150; btnBuy.Height = 34; btnBuy.Cursor = Cursors.Hand;
            SetupRoundedButton(btnBuy, AccentColor, Color.White);
            btnBuy.Click += BtnBuy_Click;

            // Satın al/sat sonucu artık butonun hemen yanında görünür (eskiden sağda uzakta, ayrı bir
            // sütundaymış gibi duruyordu).
            lblStatus.Left = btnBuy.Right + 16; lblStatus.Top = row2Top + rowGap; lblStatus.Width = 320; lblStatus.Height = 34;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblSearch);
            pnlTop.Controls.Add(pnlSearch);
            pnlTop.Controls.Add(lblSelectedPrice);
            pnlTop.Controls.Add(btnModeAmount);
            pnlTop.Controls.Add(btnModeQuantity);
            pnlTop.Controls.Add(pnlQty);
            pnlTop.Controls.Add(lblConversionHint);
            pnlTop.Controls.Add(btnBuy);
            pnlTop.Controls.Add(lblStatus);

            SetBuyMode(_buyByAmount);

            // --- ORTA PANEL (Tablo) ---
            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.Padding = new Padding(20, 0, 40, 0);
            pnlGrid.BackColor = AppBackColor;

            Panel pnlGridWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2, 6, 2, 6) };
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);

            dgvHoldings.Dock = DockStyle.Fill;
            dgvHoldings.AllowUserToAddRows = false; dgvHoldings.AllowUserToDeleteRows = false; dgvHoldings.AllowUserToResizeColumns = false; dgvHoldings.AllowUserToResizeRows = false;
            dgvHoldings.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgvHoldings.MultiSelect = false; dgvHoldings.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvHoldings.RowHeadersVisible = false; dgvHoldings.Font = new Font("Segoe UI", 9.5F); dgvHoldings.RowTemplate.Height = 44;

            dgvHoldings.BorderStyle = BorderStyle.None; dgvHoldings.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvHoldings.GridColor = AppTheme.GridLineColor; dgvHoldings.BackgroundColor = CardBackColor;
            dgvHoldings.DefaultCellStyle.BackColor = CardBackColor; dgvHoldings.DefaultCellStyle.ForeColor = TextLight;
            dgvHoldings.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvHoldings.DefaultCellStyle.SelectionBackColor = AccentColor; dgvHoldings.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvHoldings.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.HeaderBackColor; dgvHoldings.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvHoldings.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.HeaderBackColor; dgvHoldings.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvHoldings.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); dgvHoldings.EnableHeadersVisualStyles = false; dgvHoldings.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None; dgvHoldings.ColumnHeadersHeight = 40;

            dgvHoldings.CellFormatting += DgvHoldings_CellFormatting;
            dgvHoldings.CellDoubleClick += DgvHoldings_CellDoubleClick;
            EnableDoubleBuffering(dgvHoldings);

            pnlGridWrapper.Controls.Add(dgvHoldings);
            pnlGrid.Controls.Add(pnlGridWrapper);

            // --- ALT PANEL (İstatistik kutucukları) ---
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 96;
            pnlBottom.Padding = new Padding(20, 15, 30, 15);
            pnlBottom.BackColor = AppBackColor;

            pnlStatsFlow.FlowDirection = FlowDirection.LeftToRight;
            pnlStatsFlow.AutoSize = true;
            pnlStatsFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlStatsFlow.WrapContents = false;
            pnlStatsFlow.BackColor = AppBackColor;
            pnlStatsFlow.Margin = new Padding(0);

            pnlBottom.Controls.Add(pnlStatsFlow);
            pnlBottom.Resize += (s, e) => PositionStatsFlow();

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private int _priceRequestToken;

        private async void CmbSearch_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbSearch.SelectedIndex < 0) return;
            var item = AssetCatalog.Items[cmbSearch.SelectedIndex];
            lblSelectedPrice.Text = "Fiyat alınıyor...";
            _selectedPriceTry = null;
            UpdateConversionHint();

            int myToken = ++_priceRequestToken;
            var priceService = new AssetPriceService();
            decimal? price = await priceService.GetPriceTryAsync(item.Symbol, item.AssetType);

            // Kullanıcı bekleme sırasında seçimi değiştirdiyse (ör. hızlıca başka bir varlığa tıkladıysa),
            // bu artık bayatlamış cevabı ekrana yazıp güncel seçimin fiyatını ezmesin.
            if (myToken != _priceRequestToken || this.IsDisposed) return;

            var tr = new System.Globalization.CultureInfo("tr-TR");
            lblSelectedPrice.Text = price.HasValue ? $"Güncel Fiyat: {price.Value.ToString("#,##0.00", tr)} ₺" : "Fiyat alınamadı";
            _selectedPriceTry = price;
            UpdateConversionHint();
        }

        // "₺ Tutar" / "Adet" küçük dilim geçişi: seçili mod vurgulu (accent) renkte, diğeri soluk
        // görünür; ayrıca metin kutusunun içeriğini bozmadan yalnızca yorumlanış biçimini değiştirir.
        private void SetBuyMode(bool byAmount)
        {
            _buyByAmount = byAmount;
            SetupRoundedButton(btnModeAmount, byAmount ? AccentColor : CardBackColor, byAmount ? Color.White : TextMuted);
            SetupRoundedButton(btnModeQuantity, !byAmount ? AccentColor : CardBackColor, !byAmount ? Color.White : TextMuted);
            btnModeAmount.Invalidate();
            btnModeQuantity.Invalidate();
            UpdateConversionHint();
        }

        // Kullanıcı yazarken (ya da fiyat/mod değiştiğinde) girilen değerin karşılığını canlı gösterir:
        // ₺ tutar modunda "≈ 0,0021 BTC", adet modunda "≈ 4.250 ₺" gibi.
        private void UpdateConversionHint()
        {
            var tr = new System.Globalization.CultureInfo("tr-TR");
            string raw = new string(txtBuyQuantity.Text.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray()).Replace(',', '.');

            if (!_selectedPriceTry.HasValue || !decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal entered) || entered <= 0)
            {
                lblConversionHint.Text = string.Empty;
                return;
            }

            lblConversionHint.Text = _buyByAmount
                ? $"≈ {(entered / _selectedPriceTry.Value).ToString("0.########", tr)} adet"
                : $"≈ {(entered * _selectedPriceTry.Value).ToString("#,##0.00", tr)} ₺";
        }

        private async void BtnBuy_Click(object? sender, EventArgs e)
        {
            if (cmbSearch.SelectedIndex < 0)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = "Önce bir varlık seçin."; return;
            }

            string raw = new string(txtBuyQuantity.Text.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray()).Replace(',', '.');
            if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal entered) || entered <= 0)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = _buyByAmount ? "Geçerli bir tutar girin." : "Geçerli bir miktar girin.";
                return;
            }

            decimal quantity = entered;
            if (_buyByAmount)
            {
                if (!_selectedPriceTry.HasValue)
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = "Fiyat henüz alınamadı, birazdan tekrar deneyin."; return;
                }
                quantity = entered / _selectedPriceTry.Value;
            }

            var item = AssetCatalog.Items[cmbSearch.SelectedIndex];
            var (success, error) = await _assetService.BuyAssetAsync(_user.Id, item.Symbol, quantity);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150); lblStatus.Text = "Alım başarılı.";
                txtBuyQuantity.Clear();
                await LoadHoldingsAsync();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140); lblStatus.Text = error;
            }
        }

        private async System.Threading.Tasks.Task LoadHoldingsAsync()
        {
            var views = await _assetService.GetHoldingsWithLivePricesAsync(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            var rows = views.Select(v => new HoldingRow
            {
                Sembol = v.Symbol,
                Miktar = v.Quantity.ToString("0.########", tr),
                OrtMaliyet = v.AvgCostTry.ToString("#,##0.00", tr) + " ₺",
                GuncelFiyat = v.CurrentPriceTry.HasValue ? v.CurrentPriceTry.Value.ToString("#,##0.00", tr) + " ₺" : "N/A",
                GuncelDeğer = v.CurrentValueTry.HasValue ? v.CurrentValueTry.Value.ToString("#,##0.00", tr) + " ₺" : "N/A",
                KarZarar = v.ProfitLossTry.HasValue
                    ? $"{v.ProfitLossTry.Value.ToString("+#,##0.00;-#,##0.00", tr)} ₺ (%{v.ProfitLossPercent!.Value.ToString("+0.00;-0.00", tr)})"
                    : "N/A",
                KarZararRaw = (double)(v.ProfitLossTry ?? 0),
                FiyatVar = v.CurrentPriceTry.HasValue
            }).ToList();

            dgvHoldings.DataSource = rows;

            if (dgvHoldings.Columns["Sembol"] != null)
            {
                dgvHoldings.Columns["KarZararRaw"]!.Visible = false;
                dgvHoldings.Columns["FiyatVar"]!.Visible = false;
                dgvHoldings.Columns["Sembol"]!.FillWeight = 60;
                dgvHoldings.Columns["Miktar"]!.FillWeight = 75;
                dgvHoldings.Columns["OrtMaliyet"]!.FillWeight = 80;
                dgvHoldings.Columns["OrtMaliyet"]!.HeaderText = "Ort. Maliyet";
                dgvHoldings.Columns["GuncelFiyat"]!.FillWeight = 80;
                dgvHoldings.Columns["GuncelFiyat"]!.HeaderText = "Güncel Fiyat";
                dgvHoldings.Columns["GuncelDeğer"]!.FillWeight = 80;
                dgvHoldings.Columns["GuncelDeğer"]!.HeaderText = "Güncel Değer";
                dgvHoldings.Columns["KarZarar"]!.FillWeight = 110;
                dgvHoldings.Columns["KarZarar"]!.HeaderText = "Kâr / Zarar";

                foreach (DataGridViewColumn col in dgvHoldings.Columns)
                {
                    if (col.Name != "Sembol" && col.Visible) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            decimal totalValue = views.Sum(v => v.CurrentValueTry ?? 0);
            decimal totalCost = views.Sum(v => v.AvgCostTry * v.Quantity);
            decimal totalPl = totalValue - totalCost;

            string totalValueText = _user.HideAmountsEnabled ? "••••••" : totalValue.ToString("#,##0", tr) + " ₺";
            string totalCostText = _user.HideAmountsEnabled ? "••••••" : totalCost.ToString("#,##0", tr) + " ₺";
            string totalPlText = _user.HideAmountsEnabled ? "••••••" : totalPl.ToString("+#,##0;-#,##0", tr) + " ₺";

            pnlStatsFlow.Controls.Clear();
            pnlStatsFlow.Controls.Add(CreateStatChip(views.Count.ToString(), "Pozisyon"));
            pnlStatsFlow.Controls.Add(CreateStatChip(totalCostText, "Toplam Maliyet"));
            pnlStatsFlow.Controls.Add(CreateStatChip(totalValueText, "Güncel Değer"));
            pnlStatsFlow.Controls.Add(CreateStatChip(totalPlText, "Kâr / Zarar"));
            PositionStatsFlow();
        }

        private void DgvHoldings_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvHoldings.Columns[e.ColumnIndex].Name != "KarZarar") return;

            bool fiyatVar = dgvHoldings.Rows[e.RowIndex].Cells["FiyatVar"].Value is bool b && b;
            if (!fiyatVar) { e.CellStyle!.ForeColor = TextMuted; return; }

            double raw = dgvHoldings.Rows[e.RowIndex].Cells["KarZararRaw"].Value is double d ? d : 0;
            e.CellStyle!.ForeColor = raw >= 0 ? IncomeColor : ExpenseColor;
        }

        private void DgvHoldings_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string symbol = dgvHoldings.Rows[e.RowIndex].Cells["Sembol"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(symbol)) return;

            using (var dialog = new AssetDetailDialog(_user, symbol))
            {
                dialog.ShowDialog();
                if (dialog.ChangesMade)
                {
                    _ = LoadHoldingsAsync();
                }
            }
        }

        // Alt bilgi şeridindeki tek bir istatistik kutucuğu: etiket üstte, değer altta.
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
                using var path = Helpers.UIStyleHelper.GetRoundedRectPath(new Rectangle(0, 0, chip.Width - 1, chip.Height - 1), 10);
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

        private const int GridRightInset = 42;

        private void PositionStatsFlow()
        {
            pnlStatsFlow.Left = Math.Max(20, pnlBottom.ClientSize.Width - pnlStatsFlow.PreferredSize.Width - GridRightInset);
            pnlStatsFlow.Top = (pnlBottom.ClientSize.Height - pnlStatsFlow.PreferredSize.Height) / 2;
        }

        // ComboBox'ın varsayılan beyaz sistem çizimini (kutu + açılır liste) tema renklerine göre
        // kendi çizdiğimiz sürümle değiştirir; bkz. TransactionControl.SetupCustomComboBox.
        private void SetupCustomComboBox(Panel pnl, ComboBox cmb)
        {
            SetupSmoothContainer(pnl, 8, CardBackColor);
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = CardBackColor;
            cmb.ForeColor = TextLight;

            cmb.DrawMode = DrawMode.OwnerDrawFixed;
            cmb.ItemHeight = 22;
            cmb.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bgColor = isSelected ? AppTheme.HoverBackColor : CardBackColor;
                e.Graphics.FillRectangle(new SolidBrush(bgColor), e.Bounds);
                TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index]?.ToString() ?? string.Empty, cmb.Font, e.Bounds, TextLight, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            };
            // Bu liste (AssetCatalog.Items, 20+ satır) uzun olduğundan açılır listede yerleşik bir
            // kaydırma çubuğu çıkıyor — Windows görsel stili açıkken bu çubuğun kendisi ve listenin
            // native kenarlığı DrawItem'ın boyadığı koyu satırların üzerine beyaz sızabiliyor.
            // DisableVisualStyle bunu (DateTimePicker'da da kullanılan aynı teknikle) engelliyor.
            cmb.HandleCreated += (s, e) => Helpers.DarkTitleBarHelper.DisableVisualStyle(cmb);

            cmb.Left = 10; cmb.Top = 7; cmb.Width = pnl.Width - 20;
            cmb.Region = new Region(new Rectangle(1, 1, cmb.Width - 2, cmb.Height - 2));

            Panel pnlArrow = new Panel();
            pnlArrow.Width = 28;
            pnlArrow.Height = cmb.Height - 2;
            pnlArrow.Left = cmb.Right - pnlArrow.Width;
            pnlArrow.Top = cmb.Top + 1;
            pnlArrow.BackColor = CardBackColor;
            pnlArrow.Cursor = Cursors.Hand;
            pnlArrow.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int ax = pnlArrow.Width / 2 - 5;
                int ay = pnlArrow.Height / 2 - 2;
                using var brush = new SolidBrush(TextMuted);
                e.Graphics.FillPolygon(brush, new Point[] { new Point(ax, ay), new Point(ax + 10, ay), new Point(ax + 5, ay + 6) });
            };
            pnlArrow.MouseClick += (s, e) => { cmb.DroppedDown = true; };
            pnl.MouseClick += (s, e) => { cmb.DroppedDown = true; };

            pnl.Controls.Add(pnlArrow);
            pnlArrow.BringToFront();
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) { pnl.BackColor = AppBackColor; pnl.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor); using (var path = Helpers.UIStyleHelper.GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } }; pnl.SizeChanged += (s, e) => pnl.Invalidate(); }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor) { btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.BackColor = Color.Transparent; btn.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor); using (var path = Helpers.UIStyleHelper.GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8)) { using (var brush = new SolidBrush(bgColor)) { e.Graphics.FillPath(brush, path); } } TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }; }

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
