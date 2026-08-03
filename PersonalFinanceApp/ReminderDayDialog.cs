using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class ReminderDayDialog : Form
    {
        private readonly User _user;
        private readonly DateTime _date;
        private readonly ReminderService _reminderService = new ReminderService();

        private static readonly Color AppBackColor = Color.FromArgb(37, 41, 59);
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color DangerColor = Color.FromArgb(220, 90, 90);

        private DataGridView dgvReminders = new DataGridView();
        private List<Reminder> _cachedReminders = new List<Reminder>();
        private bool _isUpdatingProgrammatically = false;

        private TextBox txtTitle = new TextBox();
        private DateTimePicker dtpTime = new DateTimePicker();
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

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            string[] monthNames = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
            this.Text = $"{_date.Day} {monthNames[_date.Month - 1]} {_date.Year}";
            this.Width = 560;
            this.Height = 620;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9.5F);

            Label lblTitleField = new Label { Text = "Başlık:", Left = 20, Top = 20, ForeColor = TextMuted, AutoSize = true };
            txtTitle.Left = 20;
            txtTitle.Top = 48;
            txtTitle.Width = 340;

            Label lblTimeField = new Label { Text = "Saat:", Left = 380, Top = 20, ForeColor = TextMuted, AutoSize = true };
            dtpTime.Left = 380;
            dtpTime.Top = 48;
            dtpTime.Width = 100;
            dtpTime.Format = DateTimePickerFormat.Time;
            dtpTime.ShowUpDown = true;

            btnAdd.Text = "Ekle";
            btnAdd.Left = 20;
            btnAdd.Top = 85;
            btnAdd.Width = 500;
            btnAdd.Height = 32;
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.BackColor = AccentColor;
            btnAdd.ForeColor = Color.White;
            btnAdd.Cursor = Cursors.Hand;
            btnAdd.Click += BtnAdd_Click;

            lblStatus.Left = 20;
            lblStatus.Top = 125;
            lblStatus.Width = 500;
            lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F);

            dgvReminders.Left = 20;
            dgvReminders.Top = 160;
            dgvReminders.Width = 500;
            dgvReminders.Height = 340;
            dgvReminders.AllowUserToAddRows = false;
            dgvReminders.AllowUserToDeleteRows = false;
            dgvReminders.AllowUserToResizeColumns = false;
            dgvReminders.AllowUserToResizeRows = false;
            dgvReminders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReminders.MultiSelect = false;
            dgvReminders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReminders.BackgroundColor = Color.White;
            dgvReminders.BorderStyle = BorderStyle.None;
            dgvReminders.RowHeadersVisible = false;
            dgvReminders.Font = new Font("Segoe UI", 9.5F);
            dgvReminders.RowTemplate.Height = 30;
            dgvReminders.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dgvReminders.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 239, 246);
            dgvReminders.DefaultCellStyle.SelectionForeColor = Color.FromArgb(40, 40, 40);

            dgvReminders.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvReminders.IsCurrentCellDirty)
                {
                    dgvReminders.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            dgvReminders.CellValueChanged += DgvReminders_CellValueChanged;

            btnDelete.Text = "Seçili Hatırlatıcıyı Sil";
            btnDelete.Left = 20;
            btnDelete.Top = 520;
            btnDelete.Width = 500;
            btnDelete.Height = 34;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 1;
            btnDelete.FlatAppearance.BorderColor = DangerColor;
            btnDelete.BackColor = AppBackColor;
            btnDelete.ForeColor = DangerColor;
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.Click += BtnDelete_Click;

            this.Controls.Add(lblTitleField);
            this.Controls.Add(txtTitle);
            this.Controls.Add(lblTimeField);
            this.Controls.Add(dtpTime);
            this.Controls.Add(btnAdd);
            this.Controls.Add(lblStatus);
            this.Controls.Add(dgvReminders);
            this.Controls.Add(btnDelete);
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
                dgvReminders.Columns["ID"].Visible = false;
                dgvReminders.Columns["Saat"].ReadOnly = true;
                dgvReminders.Columns["Başlık"].ReadOnly = true;
                dgvReminders.Columns["Saat"].FillWeight = 30;
                dgvReminders.Columns["Başlık"].FillWeight = 100;
                dgvReminders.Columns["Tamamlandı"].FillWeight = 40;
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
            DateTime reminderDateTime = _date.Date + dtpTime.Value.TimeOfDay;

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
                if (idCellDel?.Value == null)
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = "Geçersiz hatırlatıcı seçimi.";
                    return;
                }
                if (!int.TryParse(idCellDel.Value.ToString(), out int reminderId))
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = "Geçersiz hatırlatıcı ID.";
                    return;
                }
                _reminderService.DeleteReminder(reminderId, _user.Id);
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Hatırlatıcı silindi.";
                LoadReminders();
            }
        }
    }
}