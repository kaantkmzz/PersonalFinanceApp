using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class RecurringTransactionDialog : Form
    {
        private readonly User _user;
        private readonly RecurringTransactionService _recurringService = new RecurringTransactionService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color DangerColor => AppTheme.DangerColor;

        private ComboBox cmbType = new ComboBox();
        private TextBox txtCategory = new TextBox();
        private TextBox txtAmount = new TextBox();
        private TextBox txtDescription = new TextBox();
        private Button btnAdd = new Button();
        private Label lblStatus = new Label();

        private DataGridView dgvRecurring = new DataGridView();
        private List<RecurringTransaction> _cachedRecurring = new List<RecurringTransaction>();
        private Button btnDelete = new Button();
        private bool _isUpdatingProgrammatically = false;

        private string _selectedFrequency = "monthly";
        private Button btnFreqDaily = new Button();
        private Button btnFreqWeekly = new Button();
        private Button btnFreqMonthly = new Button();

        private class RecurringRow
        {
            public int ID { get; set; }
            public string Tip { get; set; } = string.Empty;
            public string Kategori { get; set; } = string.Empty;
            public string Tutar { get; set; } = string.Empty;
            public string Sıklık { get; set; } = string.Empty;
            public bool Aktif { get; set; }
        }

        public RecurringTransactionDialog(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadRecurring();
            this.Load += (s, e) => DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Tekrarlanan İşlemler";
            this.ClientSize = new Size(560, 660);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9.5F);

            // --- SIKLIK SEÇİMİ ---
            Label lblFrequency = new Label { Text = "Sıklık:", Left = 20, Top = 15, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };

            const int freqTop = 37, freqHeight = 36, freqGap = 10;
            int freqWidth = (500 - freqGap * 2) / 3;

            btnFreqDaily.Text = "Günlük";
            btnFreqDaily.Left = 20; btnFreqDaily.Top = freqTop; btnFreqDaily.Width = freqWidth; btnFreqDaily.Height = freqHeight; btnFreqDaily.Cursor = Cursors.Hand;
            btnFreqDaily.Click += (s, e) => SelectFrequency("daily");

            btnFreqWeekly.Text = "Haftalık";
            btnFreqWeekly.Left = btnFreqDaily.Right + freqGap; btnFreqWeekly.Top = freqTop; btnFreqWeekly.Width = freqWidth; btnFreqWeekly.Height = freqHeight; btnFreqWeekly.Cursor = Cursors.Hand;
            btnFreqWeekly.Click += (s, e) => SelectFrequency("weekly");

            btnFreqMonthly.Text = "Aylık";
            btnFreqMonthly.Left = btnFreqWeekly.Right + freqGap; btnFreqMonthly.Top = freqTop; btnFreqMonthly.Width = freqWidth; btnFreqMonthly.Height = freqHeight; btnFreqMonthly.Cursor = Cursors.Hand;
            btnFreqMonthly.Click += (s, e) => SelectFrequency("monthly");

            SetupRoundedButton(btnFreqDaily);
            SetupRoundedButton(btnFreqWeekly);
            SetupRoundedButton(btnFreqMonthly);
            RefreshFrequencyButtons();

            // --- GİRDİ ALANLARI ---
            const int fieldLabelTop = 88, fieldTop = 116, fieldHeight = 36;

            Label lblTypeField = new Label { Text = "Tip:", Left = 20, Top = fieldLabelTop, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlType = new Panel { Left = 20, Top = fieldTop, Width = 110, Height = fieldHeight };
            cmbType.Left = 5; cmbType.Top = 7; cmbType.Width = 105;
            cmbType.Font = new Font("Segoe UI", 9.5F); cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("Gelir"); cmbType.Items.Add("Gider"); cmbType.SelectedIndex = 1;
            pnlType.Controls.Add(cmbType);
            SetupCustomComboBox(pnlType, cmbType);

            Label lblCategoryField = new Label { Text = "Kategori:", Left = 145, Top = fieldLabelTop, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlCategory = new Panel { Left = 145, Top = fieldTop, Width = 140, Height = fieldHeight };
            SetupSmoothContainer(pnlCategory, 8, CardBackColor);
            txtCategory.Left = 10; txtCategory.Top = 8; txtCategory.Width = 120;
            txtCategory.Font = new Font("Segoe UI", 9.5F); txtCategory.BorderStyle = BorderStyle.None;
            txtCategory.BackColor = CardBackColor; txtCategory.ForeColor = TextLight;
            pnlCategory.Controls.Add(txtCategory);

            Label lblAmountField = new Label { Text = "Tutar:", Left = 300, Top = fieldLabelTop, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlAmount = new Panel { Left = 300, Top = fieldTop, Width = 100, Height = fieldHeight };
            SetupSmoothContainer(pnlAmount, 8, CardBackColor);
            txtAmount.Left = 10; txtAmount.Top = 8; txtAmount.Width = 80;
            txtAmount.Font = new Font("Segoe UI", 9.5F); txtAmount.BorderStyle = BorderStyle.None;
            txtAmount.BackColor = CardBackColor; txtAmount.ForeColor = TextLight;
            txtAmount.TextChanged += (s, e) => SmartFormatAmount(txtAmount);
            pnlAmount.Controls.Add(txtAmount);

            Label lblDescField = new Label { Text = "Açıklama:", Left = 415, Top = fieldLabelTop, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };
            Panel pnlDesc = new Panel { Left = 415, Top = fieldTop, Width = 125, Height = fieldHeight };
            SetupSmoothContainer(pnlDesc, 8, CardBackColor);
            txtDescription.Left = 10; txtDescription.Top = 8; txtDescription.Width = 105;
            txtDescription.Font = new Font("Segoe UI", 9.5F); txtDescription.BorderStyle = BorderStyle.None;
            txtDescription.BackColor = CardBackColor; txtDescription.ForeColor = TextLight;
            pnlDesc.Controls.Add(txtDescription);

            btnAdd.Text = "Ekle";
            btnAdd.Left = 20; btnAdd.Top = 164; btnAdd.Width = 520; btnAdd.Height = 36; btnAdd.Cursor = Cursors.Hand;
            SetupRoundedButton(btnAdd, AccentColor, Color.White);
            btnAdd.Click += BtnAdd_Click;

            lblStatus.Left = 20; lblStatus.Top = 208; lblStatus.Width = 520; lblStatus.Height = 22;
            lblStatus.BackColor = Color.Transparent;
            lblStatus.Font = new Font("Segoe UI", 9F);

            // --- LİSTE ---
            Panel pnlGridWrapper = new Panel { Left = 20, Top = 234, Width = 520, Height = 320, Padding = new Padding(2, 6, 2, 6) };
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);

            dgvRecurring.Dock = DockStyle.Fill;
            dgvRecurring.AllowUserToAddRows = false;
            dgvRecurring.AllowUserToDeleteRows = false;
            dgvRecurring.AllowUserToResizeColumns = false;
            dgvRecurring.AllowUserToResizeRows = false;
            dgvRecurring.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRecurring.MultiSelect = false;
            dgvRecurring.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRecurring.RowHeadersVisible = false;
            dgvRecurring.Font = new Font("Segoe UI", 9.5F);
            dgvRecurring.RowTemplate.Height = 34;

            dgvRecurring.BorderStyle = BorderStyle.None; dgvRecurring.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvRecurring.GridColor = AppTheme.GridLineColor; dgvRecurring.BackgroundColor = CardBackColor;
            dgvRecurring.DefaultCellStyle.BackColor = CardBackColor; dgvRecurring.DefaultCellStyle.ForeColor = TextLight;
            dgvRecurring.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvRecurring.DefaultCellStyle.SelectionBackColor = AccentColor;
            dgvRecurring.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvRecurring.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.HeaderBackColor; dgvRecurring.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvRecurring.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.HeaderBackColor; dgvRecurring.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvRecurring.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold); dgvRecurring.EnableHeadersVisualStyles = false; dgvRecurring.ColumnHeadersHeight = 34;

            dgvRecurring.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvRecurring.IsCurrentCellDirty)
                {
                    dgvRecurring.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            dgvRecurring.CellValueChanged += DgvRecurring_CellValueChanged;

            pnlGridWrapper.Controls.Add(dgvRecurring);

            btnDelete.Text = "Seçili İşlemi Sil";
            btnDelete.Left = 20; btnDelete.Top = 576; btnDelete.Width = 520; btnDelete.Height = 40; btnDelete.Cursor = Cursors.Hand;
            SetupRoundedButton(btnDelete, DangerColor, Color.White);
            btnDelete.Click += BtnDelete_Click;

            this.Controls.Add(lblFrequency);
            this.Controls.Add(btnFreqDaily); this.Controls.Add(btnFreqWeekly); this.Controls.Add(btnFreqMonthly);
            this.Controls.Add(lblTypeField); this.Controls.Add(pnlType);
            this.Controls.Add(lblCategoryField); this.Controls.Add(pnlCategory);
            this.Controls.Add(lblAmountField); this.Controls.Add(pnlAmount);
            this.Controls.Add(lblDescField); this.Controls.Add(pnlDesc);
            this.Controls.Add(btnAdd);
            this.Controls.Add(lblStatus);
            this.Controls.Add(pnlGridWrapper);
            this.Controls.Add(btnDelete);
        }

        private void SelectFrequency(string frequency)
        {
            _selectedFrequency = frequency;
            RefreshFrequencyButtons();
        }

        private void RefreshFrequencyButtons()
        {
            btnFreqDaily.BackColor = _selectedFrequency == "daily" ? AccentColor : CardBackColor;
            btnFreqDaily.ForeColor = _selectedFrequency == "daily" ? Color.White : TextLight;
            btnFreqWeekly.BackColor = _selectedFrequency == "weekly" ? AccentColor : CardBackColor;
            btnFreqWeekly.ForeColor = _selectedFrequency == "weekly" ? Color.White : TextLight;
            btnFreqMonthly.BackColor = _selectedFrequency == "monthly" ? AccentColor : CardBackColor;
            btnFreqMonthly.ForeColor = _selectedFrequency == "monthly" ? Color.White : TextLight;
            btnFreqDaily.Invalidate(); btnFreqWeekly.Invalidate(); btnFreqMonthly.Invalidate();
        }

        private static string FrequencyLabel(string frequency) => frequency switch
        {
            "daily" => "Günlük",
            "weekly" => "Haftalık",
            _ => "Aylık"
        };

        private void LoadRecurring()
        {
            _cachedRecurring = _recurringService.GetUserRecurring(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            _isUpdatingProgrammatically = true;

            var displayList = _cachedRecurring.Select(r => new RecurringRow
            {
                ID = r.Id,
                Tip = r.Type == "income" ? "Gelir" : "Gider",
                Kategori = r.CategoryName,
                Tutar = r.Amount.ToString("#,##0", tr) + " ₺",
                Sıklık = FrequencyLabel(r.Frequency),
                Aktif = r.IsActive
            }).ToList();

            dgvRecurring.DataSource = displayList;

            if (dgvRecurring.Columns["ID"] != null)
            {
                dgvRecurring.Columns["ID"]!.Visible = false;
                dgvRecurring.Columns["Tip"]!.ReadOnly = true;
                dgvRecurring.Columns["Kategori"]!.ReadOnly = true;
                dgvRecurring.Columns["Tutar"]!.ReadOnly = true;
                dgvRecurring.Columns["Sıklık"]!.ReadOnly = true;
                dgvRecurring.Columns["Tip"]!.FillWeight = 55;
                dgvRecurring.Columns["Kategori"]!.FillWeight = 75;
                dgvRecurring.Columns["Tutar"]!.FillWeight = 65;
                dgvRecurring.Columns["Sıklık"]!.FillWeight = 60;
                dgvRecurring.Columns["Aktif"]!.FillWeight = 40;
            }

            _isUpdatingProgrammatically = false;
        }

        private void DgvRecurring_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_isUpdatingProgrammatically || e.RowIndex < 0) return;
            if (dgvRecurring.Columns[e.ColumnIndex].Name != "Aktif") return;

            var row = dgvRecurring.Rows[e.RowIndex];
            int recurringId = Convert.ToInt32(row.Cells["ID"].Value);
            bool newValue = (bool)row.Cells["Aktif"].Value!;

            _recurringService.SetActive(recurringId, _user.Id, newValue);
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

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string type = cmbType.SelectedItem?.ToString() == "Gelir" ? "income" : "expense";
            string category = txtCategory.Text.Trim();
            string description = txtDescription.Text;

            string rawAmount = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
            if (!decimal.TryParse(rawAmount, out decimal amount))
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Geçersiz tutar.";
                return;
            }

            bool success = _recurringService.AddRecurring(_user.Id, category, type, amount, description, _selectedFrequency, out string errorMessage);

            if (success)
            {
                // Yeni eklenen tekrarlayan işlemi kullanıcı uygulamayı kapatıp açana kadar beklemeden hemen
                // işliyoruz (LastProcessedDate boş olduğu için "due" sayılır, giriş anındaki akışla aynı yolu
                // kullanır) — böylece ilk işlem oluşturmak için yeniden giriş yapmaya gerek kalmıyor.
                var (added, failed) = _recurringService.ProcessDueRecurring(_user.Id);

                if (added.Count > 0)
                {
                    lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                    lblStatus.Text = "Tekrarlanan işlem eklendi ve ilk işlem oluşturuldu.";
                }
                else if (failed.Count > 0)
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 200, 120);
                    lblStatus.Text = $"Tekrarlanan işlem eklendi. İlk işlem oluşturulamadı: {failed[0]}";
                }
                else
                {
                    lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                    lblStatus.Text = "Tekrarlanan işlem eklendi.";
                }
                txtCategory.Clear();
                txtAmount.Clear();
                txtDescription.Clear();
                LoadRecurring();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvRecurring.CurrentRow == null)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen silmek için bir işlem seçin.";
                return;
            }

            var confirm = MessageBox.Show("Bu tekrarlanan işlemi silmek istediğinize emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                int recurringId = Convert.ToInt32(dgvRecurring.CurrentRow.Cells["ID"].Value);
                _recurringService.DeleteRecurring(recurringId, _user.Id);
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "İşlem silindi.";
                LoadRecurring();
            }
        }

        // --- GÖRSEL YARDIMCI METOTLAR ---
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

            cmb.Region = new Region(new Rectangle(1, 1, cmb.Width - 2, cmb.Height - 2));

            Panel pnlArrow = new Panel();
            pnlArrow.Width = 28;
            pnlArrow.Height = cmb.Height - 2;
            pnlArrow.Left = cmb.Right - pnlArrow.Width - 1;
            pnlArrow.Top = cmb.Top + 1;
            pnlArrow.BackColor = CardBackColor;
            pnlArrow.Cursor = Cursors.Hand;

            pnlArrow.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int ax = pnlArrow.Width / 2 - 5;
                int ay = pnlArrow.Height / 2 - 2;
                using (var brush = new SolidBrush(TextMuted))
                    e.Graphics.FillPolygon(brush, new Point[] { new Point(ax, ay), new Point(ax + 10, ay), new Point(ax + 5, ay + 6) });
            };

            pnlArrow.MouseClick += (s, e) => { cmb.DroppedDown = true; };
            pnl.MouseClick += (s, e) => { cmb.DroppedDown = true; };

            pnl.Controls.Add(pnlArrow);
            pnlArrow.BringToFront();
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

        // Sıklık düğmeleri için: BackColor/ForeColor değişimiyle aktif/pasif durumunu gösterebilen buton
        private void SetupRoundedButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8))
                using (var brush = new SolidBrush(btn.BackColor))
                    e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor)
        {
            btn.BackColor = bgColor;
            btn.ForeColor = textColor;
            SetupRoundedButton(btn);
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
