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
    public partial class ReminderDayDialog : Form
    {
        private readonly User _user;
        private readonly DateTime _date;
        private readonly ReminderService _reminderService = new ReminderService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color DangerColor => AppTheme.DangerColor;

        private DataGridView dgvReminders = new DataGridView();
        private List<Reminder> _cachedReminders = new List<Reminder>();
        private bool _isUpdatingProgrammatically = false;

        private TextBox txtTitle = new TextBox();
        private MaskedTextBox mskTime = new MaskedTextBox();
        private Button btnAdd = new Button();
        private Button btnDelete = new Button();
        private Label lblStatus = new Label();

        private class ReminderRow
        {
            public int ID { get; set; }
            public string Saat { get; set; } = string.Empty;
            public string Başlık { get; set; } = string.Empty;
            public bool Tamamlandı { get; set; }
        }

        public ReminderDayDialog(User user, DateTime date)
        {
            _user = user;
            _date = date;
            InitializeComponent();
            SetupUI();
            LoadReminders();
            this.Load += (s, e) => DarkTitleBarHelper.EnableDarkTitleBar(this);
        }

        // Yavaş açılma sorununu çözmek için CreateParams bloğu tamamen kaldırıldı!

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            string[] monthNames = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
            this.Text = $"{_date.Day} {monthNames[_date.Month - 1]} {_date.Year}";
            this.Width = 560;
            this.Height = 615;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9.5F);

            // Başlık Alanı
            Label lblTitleField = new Label { Text = "Başlık:", Left = 20, Top = 18, ForeColor = TextMuted, AutoSize = true };

            Panel pnlTitleWrapper = new Panel
            {
                Left = 20,
                Top = 42,
                Width = 320,
                Height = 38
            };
            SetupSmoothContainer(pnlTitleWrapper, 8, CardBackColor);

            txtTitle.BorderStyle = BorderStyle.None;
            txtTitle.Font = new Font("Segoe UI", 11F);
            txtTitle.BackColor = CardBackColor;
            txtTitle.ForeColor = TextLight;
            txtTitle.Width = 300;
            txtTitle.Location = new Point(10, 8);
            pnlTitleWrapper.Controls.Add(txtTitle);

            // Saat Alanı
            Label lblTimeField = new Label { Text = "Saat:", Left = 360, Top = 18, ForeColor = TextMuted, AutoSize = true };

            Panel pnlTimeWrapper = new Panel
            {
                Left = 360,
                Top = 42,
                Width = 160,
                Height = 38
            };
            SetupSmoothContainer(pnlTimeWrapper, 8, CardBackColor);

            mskTime.Mask = "00:00";
            mskTime.ValidatingType = typeof(DateTime);
            mskTime.Text = DateTime.Now.ToString("HH:mm");
            mskTime.BorderStyle = BorderStyle.None;
            mskTime.Font = new Font("Segoe UI", 11F);
            mskTime.BackColor = CardBackColor;
            mskTime.ForeColor = TextLight;
            mskTime.Width = 140;
            // DİKKAT: Metni tam ortalamak için Y koordinatı 8'den 6'ya çekildi.
            mskTime.Location = new Point(10, 6);
            mskTime.TextAlign = HorizontalAlignment.Center;
            pnlTimeWrapper.Controls.Add(mskTime);

            // Ekle Butonu
            btnAdd.Text = "Ekle";
            btnAdd.Left = 20;
            btnAdd.Top = 90;
            btnAdd.Width = 500;
            btnAdd.Height = 36;
            btnAdd.Cursor = Cursors.Hand;
            SetupRoundedButton(btnAdd, AccentColor, Color.White, false);
            btnAdd.Click += BtnAdd_Click;

            // Tabloyu saran panel
            Panel pnlGridWrapper = new Panel
            {
                Left = 20,
                Top = 140,
                Width = 500,
                Height = 340,
                Padding = new Padding(2, 6, 2, 6)
            };
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);

            dgvReminders.Dock = DockStyle.Fill;
            // DİKKAT: Checkbox'ın tıklanabilmesi için tablonun genel ReadOnly özelliği false yapıldı!
            dgvReminders.ReadOnly = false;
            dgvReminders.AllowUserToAddRows = false;
            dgvReminders.AllowUserToDeleteRows = false;
            dgvReminders.AllowUserToResizeColumns = false;
            dgvReminders.AllowUserToResizeRows = false;
            dgvReminders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReminders.MultiSelect = false;
            dgvReminders.ColumnHeadersVisible = true;
            dgvReminders.RowHeadersVisible = false;
            dgvReminders.Font = new Font("Segoe UI", 9.5F);
            dgvReminders.RowTemplate.Height = 40;

            dgvReminders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReminders.BorderStyle = BorderStyle.None;
            dgvReminders.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvReminders.GridColor = AppTheme.HoverBackColor;
            dgvReminders.BackgroundColor = CardBackColor;

            dgvReminders.DefaultCellStyle.BackColor = CardBackColor;
            dgvReminders.DefaultCellStyle.ForeColor = TextLight;
            dgvReminders.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvReminders.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 64, 90);
            dgvReminders.DefaultCellStyle.SelectionForeColor = TextLight;

            dgvReminders.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.HeaderBackColor;
            dgvReminders.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            dgvReminders.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.HeaderBackColor;
            dgvReminders.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            dgvReminders.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvReminders.EnableHeadersVisualStyles = false;
            dgvReminders.ColumnHeadersHeight = 38;

            dgvReminders.CellPainting += DgvReminders_CellPainting;

            dgvReminders.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvReminders.IsCurrentCellDirty)
                {
                    dgvReminders.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            dgvReminders.CellValueChanged += DgvReminders_CellValueChanged;

            pnlGridWrapper.Controls.Add(dgvReminders);

            lblStatus.Left = 20;
            lblStatus.Top = 485;
            lblStatus.Width = 500;
            lblStatus.Height = 22;
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            // Sil Butonu
            btnDelete.Text = "Seçili Hatırlatıcıyı Sil";
            btnDelete.Left = 20;
            btnDelete.Top = 515;
            btnDelete.Width = 500;
            btnDelete.Height = 38;
            btnDelete.Cursor = Cursors.Hand;
            SetupRoundedButton(btnDelete, DangerColor, Color.White, false);
            btnDelete.Click += BtnDelete_Click;

            this.Controls.Add(lblTitleField);
            this.Controls.Add(pnlTitleWrapper);
            this.Controls.Add(lblTimeField);
            this.Controls.Add(pnlTimeWrapper);
            this.Controls.Add(btnAdd);
            this.Controls.Add(pnlGridWrapper);
            this.Controls.Add(lblStatus);
            this.Controls.Add(btnDelete);
        }

        private void DgvReminders_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex > 0)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);

                bool isSelected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
                Color lineColor = isSelected ? AppTheme.SelectedRowLineColor : AppTheme.HoverBackColor;

                using (Pen p = new Pen(lineColor, 1))
                {
                    e.Graphics!.DrawLine(p, e.CellBounds.Left, e.CellBounds.Top + 6, e.CellBounds.Left, e.CellBounds.Bottom - 6);
                }

                e.Handled = true;
            }
        }

        private void LoadReminders()
        {
            var allReminders = _reminderService.GetUserReminders(_user.Id);
            _cachedReminders = allReminders.Where(r => r.ReminderDate.Date == _date.Date).OrderBy(r => r.ReminderDate).ToList();

            _isUpdatingProgrammatically = true;

            var displayList = _cachedReminders.Select(r => new ReminderRow
            {
                ID = r.Id,
                Saat = r.ReminderDate.ToString("HH:mm"),
                Başlık = r.Title,
                Tamamlandı = r.IsCompleted
            }).ToList();

            dgvReminders.DataSource = displayList;

            if (dgvReminders.Columns["ID"] != null)
            {
                dgvReminders.Columns["ID"]!.Visible = false;

                // Sadece Saat ve Başlık kilitli kalacak, Tamamlandı kilitsiz!
                dgvReminders.Columns["Saat"]!.ReadOnly = true;
                dgvReminders.Columns["Başlık"]!.ReadOnly = true;
                dgvReminders.Columns["Tamamlandı"]!.ReadOnly = false;

                dgvReminders.Columns["Saat"]!.FillWeight = 50;
                dgvReminders.Columns["Başlık"]!.FillWeight = 160;
                dgvReminders.Columns["Tamamlandı"]!.FillWeight = 70;

                dgvReminders.Columns["Saat"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvReminders.Columns["Tamamlandı"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            foreach (DataGridViewRow row in dgvReminders.Rows)
            {
                var idCell = row.Cells["ID"];
                if (idCell?.Value == null) continue;
                if (!int.TryParse(idCell.Value.ToString(), out int reminderId)) continue;
                var reminder = _cachedReminders.FirstOrDefault(r => r.Id == reminderId);
                if (reminder == null) continue;

                if (reminder.IsCompleted)
                {
                    row.DefaultCellStyle.Font = new Font(dgvReminders.Font, FontStyle.Strikeout);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 150);
                }
            }

            _isUpdatingProgrammatically = false;
        }

        private void DgvReminders_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_isUpdatingProgrammatically || e.RowIndex < 0) return;
            if (dgvReminders.Columns[e.ColumnIndex].Name != "Tamamlandı") return;

            var row = dgvReminders.Rows[e.RowIndex];
            var idCell = row.Cells["ID"];
            if (idCell?.Value == null) return;
            if (!int.TryParse(idCell.Value.ToString(), out int reminderId)) return;
            var completedCell = row.Cells["Tamamlandı"];
            if (completedCell?.Value == null || !bool.TryParse(completedCell.Value.ToString(), out bool newValue)) return;

            _reminderService.SetCompleted(reminderId, _user.Id, newValue);
            LoadReminders();
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string title = txtTitle.Text.Trim();

            if (!TimeSpan.TryParse(mskTime.Text, out TimeSpan timeOfDay))
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Geçersiz saat formatı. Lütfen kontrol edin.";
                return;
            }

            DateTime reminderDateTime = _date.Date + timeOfDay;

            bool success = _reminderService.AddReminder(_user.Id, title, reminderDateTime, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Hatırlatıcı eklendi.";
                txtTitle.Clear();
                LoadReminders();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvReminders.CurrentRow == null)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen silmek için bir hatırlatıcı seçin.";
                return;
            }

            var confirm = MessageBox.Show("Bu hatırlatıcıyı silmek istediğinize emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                var idCellDel = dgvReminders.CurrentRow.Cells["ID"];
                if (idCellDel?.Value == null) return;
                if (!int.TryParse(idCellDel.Value.ToString(), out int reminderId)) return;

                _reminderService.DeleteReminder(reminderId, _user.Id);
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Hatırlatıcı silindi.";
                LoadReminders();
            }
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor)
        {
            pnl.BackColor = AppBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(AppBackColor);

                using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius))
                {
                    using (var brush = new SolidBrush(bgColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (var pen = new Pen(bgColor, 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor, bool isOutlined)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.BackColor = Color.Transparent;

            bool isHovered = false;
            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);

                Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                using (var path = GetRoundedRectPath(rect, 8))
                {
                    if (isOutlined)
                    {
                        using (var pen = new Pen(isHovered ? ControlPaint.Light(textColor) : textColor, 1.5f))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }
                    else
                    {
                        using (var brush = new SolidBrush(isHovered ? ControlPaint.Light(bgColor) : bgColor))
                        {
                            e.Graphics.FillPath(brush, path);
                        }
                    }
                }
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, rect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = Math.Max(radius * 2, 1);
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}