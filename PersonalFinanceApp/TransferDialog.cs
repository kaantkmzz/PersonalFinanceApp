using PersonalFinanceApp.Helpers;

namespace PersonalFinanceApp
{
    public enum TransferAccount { Wallet, Safe, Invest }

    // Cüzdan / Kasa / Varlıklarım arasında herhangi bir yönde transfer. Altı ayrı radio button yerine
    // "Nereden" / "Nereye" iki açılır kutu — yön sayısı artsa da (bkz. AccountService) arayüz sade kalır.
    public partial class TransferDialog : Form
    {
        public decimal Amount { get; private set; }
        public TransferAccount From { get; private set; }
        public TransferAccount To { get; private set; }

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;

        private static readonly string[] AccountLabels = { "Cüzdan", "Kasa", "Varlıklarım" };

        private ComboBox cmbFrom = new ComboBox();
        private ComboBox cmbTo = new ComboBox();
        private TextBox txtAmount = new TextBox();
        private Label lblError = new Label();

        public TransferDialog()
        {
            InitializeComponent();
            SetupUI();
            this.Load += (s, e) => DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Transfer";
            this.Width = 380;
            this.Height = 370;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9.5F);

            Label lblFrom = new Label { Text = "Nereden:", Left = 30, Top = 20, ForeColor = TextMuted, AutoSize = true };
            Panel pnlFrom = new Panel { Left = 30, Top = 44, Width = 300, Height = 38 };
            SetupComboBox(pnlFrom, cmbFrom);
            cmbFrom.Items.AddRange(AccountLabels);
            cmbFrom.SelectedIndex = 0;
            cmbFrom.SelectedIndexChanged += (s, e) => EnsureDifferentAccounts(cmbFrom, cmbTo);

            Label lblTo = new Label { Text = "Nereye:", Left = 30, Top = 95, ForeColor = TextMuted, AutoSize = true };
            Panel pnlTo = new Panel { Left = 30, Top = 119, Width = 300, Height = 38 };
            SetupComboBox(pnlTo, cmbTo);
            cmbTo.Items.AddRange(AccountLabels);
            cmbTo.SelectedIndex = 1;
            cmbTo.SelectedIndexChanged += (s, e) => EnsureDifferentAccounts(cmbTo, cmbFrom);

            Label lblAmount = new Label { Text = "Tutar:", Left = 30, Top = 170, ForeColor = TextLight, AutoSize = true };
            txtAmount.Left = 30;
            txtAmount.Top = 196;
            txtAmount.Width = 300;
            txtAmount.TextChanged += (s, e) => SmartFormatAmount(txtAmount);

            lblError.Left = 30;
            lblError.Top = 228;
            lblError.Width = 300;
            lblError.Height = 30;
            lblError.ForeColor = Color.FromArgb(255, 140, 140);

            Button btnOk = new Button
            {
                Text = "Transfer Et",
                Left = 30,
                Top = 264,
                Width = 300,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;

            this.Controls.Add(lblFrom);
            this.Controls.Add(pnlFrom);
            this.Controls.Add(lblTo);
            this.Controls.Add(pnlTo);
            this.Controls.Add(lblAmount);
            this.Controls.Add(txtAmount);
            this.Controls.Add(lblError);
            this.Controls.Add(btnOk);
        }

        // "Nereden" ile "Nereye" aynı hesaba ayarlanamaz; biri değişince diğeri çakışıyorsa otomatik kaydırılır.
        private bool _suppressAccountSync = false;
        private void EnsureDifferentAccounts(ComboBox changed, ComboBox other)
        {
            if (_suppressAccountSync) return;
            if (other.SelectedIndex != changed.SelectedIndex) return;

            _suppressAccountSync = true;
            other.SelectedIndex = (changed.SelectedIndex + 1) % AccountLabels.Length;
            _suppressAccountSync = false;
        }

        private void SetupComboBox(Panel pnl, ComboBox cmb)
        {
            pnl.BackColor = AppBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(AppBackColor);
                using var path = Helpers.UIStyleHelper.GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 8);
                using var brush = new SolidBrush(CardBackColor);
                e.Graphics.FillPath(brush, path);
            };

            cmb.Left = 10; cmb.Top = 9; cmb.Width = 280;
            cmb.Font = new Font("Segoe UI", 9F);
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
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
                TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index]?.ToString() ?? string.Empty, cmb.Font, e.Bounds, TextLight, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };

            pnl.Controls.Add(cmb);
        }

        private bool _suppressAmountFormatting = false;

        // Tutar kutusuna yazılan rakamları "10.000" gibi binlik ayraçlarla biçimlendirir.
        private void SmartFormatAmount(TextBox txt)
        {
            if (_suppressAmountFormatting || string.IsNullOrWhiteSpace(txt.Text)) return;
            string value = new string(txt.Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(value)) return;
            if (decimal.TryParse(value, out decimal amount))
            {
                string formatted = amount.ToString("#,##0", new System.Globalization.CultureInfo("tr-TR"));
                if (txt.Text == formatted) return;
                _suppressAmountFormatting = true;
                txt.Text = formatted;
                txt.SelectionStart = txt.Text.Length;
                _suppressAmountFormatting = false;
            }
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string rawAmount = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
            if (!decimal.TryParse(rawAmount, out decimal amount) || amount <= 0)
            {
                lblError.Text = "Geçerli bir tutar girin.";
                return;
            }

            if (cmbFrom.SelectedIndex == cmbTo.SelectedIndex)
            {
                lblError.Text = "Aynı hesaptan aynı hesaba transfer olmaz.";
                return;
            }

            Amount = amount;
            From = (TransferAccount)cmbFrom.SelectedIndex;
            To = (TransferAccount)cmbTo.SelectedIndex;
            this.DialogResult = DialogResult.OK;
        }
    }
}
