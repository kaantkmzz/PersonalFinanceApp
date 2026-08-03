using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class NoteControl : UserControl
    {
        private readonly User _user;
        private readonly NoteService _noteService = new NoteService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color DangerColor = Color.FromArgb(220, 90, 90);

        private DataGridView dgvNotes = new DataGridView();
        private List<Note> _cachedNotes = new List<Note>();
        private int? _selectedNoteId = null;

        private TextBox txtTitle = new TextBox();
        private TextBox txtContent = new TextBox();
        private Button btnNew = new Button();
        private Button btnSave = new Button();
        private Button btnDelete = new Button();
        private Label lblStatus = new Label();
        private Label lblEditingTitle = new Label();

        public NoteControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadNotes();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            // --- Sol panel: not listesi ---
            Panel pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 480,
                BackColor = AppBackColor,
                Padding = new Padding(20, 20, 10, 20)
            };

            Label lblTitle = new Label
            {
                Text = "Notlar",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 0,
                AutoSize = true
            };

            btnNew.Text = "+ Yeni Not";
            btnNew.Left = 20;
            btnNew.Top = 65;
            btnNew.Width = 420;
            btnNew.Height = 34;
            btnNew.FlatStyle = FlatStyle.Flat;
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.BackColor = AccentColor;
            btnNew.ForeColor = Color.White;
            btnNew.Cursor = Cursors.Hand;
            btnNew.Click += BtnNew_Click;

            Panel pnlGridWrapper = new Panel
            {
                Left = 20,
                Top = 115,
                Width = 420,
                Height = 600,
                BackColor = AppBackColor
            };

            dgvNotes.Dock = DockStyle.Fill;
            dgvNotes.ReadOnly = true;
            dgvNotes.AllowUserToAddRows = false;
            dgvNotes.AllowUserToDeleteRows = false;
            dgvNotes.AllowUserToResizeColumns = false;
            dgvNotes.AllowUserToResizeRows = false;
            dgvNotes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvNotes.MultiSelect = false;
            dgvNotes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvNotes.ColumnHeadersVisible = false;
            dgvNotes.BackgroundColor = Color.White;
            dgvNotes.BorderStyle = BorderStyle.None;
            dgvNotes.RowHeadersVisible = false;
            dgvNotes.Font = new Font("Segoe UI", 9.5F);
            dgvNotes.RowTemplate.Height = 46;
            dgvNotes.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dgvNotes.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 239, 246);
            dgvNotes.DefaultCellStyle.SelectionForeColor = Color.FromArgb(40, 40, 40);
            dgvNotes.SelectionChanged += (s, e) => LoadSelectedNote();

            pnlGridWrapper.Controls.Add(dgvNotes);

            pnlLeft.Controls.Add(lblTitle);
            pnlLeft.Controls.Add(btnNew);
            pnlLeft.Controls.Add(pnlGridWrapper);

            // --- Sağ panel: not düzenleme alanı ---
            Panel pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppBackColor,
                Padding = new Padding(20)
            };

            lblEditingTitle.Text = "Bir not seçin ya da yeni bir not oluşturun";
            lblEditingTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblEditingTitle.ForeColor = TextLight;
            lblEditingTitle.Left = 20;
            lblEditingTitle.Top = 20;
            lblEditingTitle.AutoSize = true;

            Label lblTitleField = new Label { Text = "Başlık:", Left = 20, Top = 55, ForeColor = TextMuted, AutoSize = true };
            txtTitle.Left = 20;
            txtTitle.Top = 80;
            txtTitle.Width = 700;
            txtTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtTitle.Font = new Font("Segoe UI", 10.5F);

            Label lblContentField = new Label { Text = "İçerik:", Left = 20, Top = 120, ForeColor = TextMuted, AutoSize = true };

            Panel pnlContentWrapper = new Panel
            {
                Left = 20,
                Top = 145,
                Width = 700,
                Height = 380,
                BackColor = AppBackColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            txtContent.Dock = DockStyle.Fill;
            txtContent.Multiline = true;
            txtContent.ScrollBars = ScrollBars.Vertical;
            txtContent.Font = new Font("Segoe UI", 10.5F);
            pnlContentWrapper.Controls.Add(txtContent);

            btnSave.Text = "Kaydet";
            btnSave.Left = 20;
            btnSave.Top = 540;
            btnSave.Width = 140;
            btnSave.Height = 36;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.BackColor = AccentColor;
            btnSave.ForeColor = Color.White;
            btnSave.Cursor = Cursors.Hand;
            btnSave.Click += BtnSave_Click;

            btnDelete.Text = "Notu Sil";
            btnDelete.Left = 170;
            btnDelete.Top = 540;
            btnDelete.Width = 140;
            btnDelete.Height = 36;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 1;
            btnDelete.FlatAppearance.BorderColor = DangerColor;
            btnDelete.BackColor = AppBackColor;
            btnDelete.ForeColor = DangerColor;
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.Click += BtnDelete_Click;

            lblStatus.Left = 20;
            lblStatus.Top = 585;
            lblStatus.Width = 500;
            lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F);

            pnlRight.Controls.Add(lblEditingTitle);
            pnlRight.Controls.Add(lblTitleField);
            pnlRight.Controls.Add(txtTitle);
            pnlRight.Controls.Add(lblContentField);
            pnlRight.Controls.Add(pnlContentWrapper);
            pnlRight.Controls.Add(btnSave);
            pnlRight.Controls.Add(btnDelete);
            pnlRight.Controls.Add(lblStatus);

            this.Controls.Add(pnlRight);
            this.Controls.Add(pnlLeft);
        }

        private void LoadNotes()
        {
            _cachedNotes = _noteService.GetUserNotes(_user.Id);

            var displayList = _cachedNotes.Select(n => new
            {
                ID = n.Id,
                Başlık = string.IsNullOrWhiteSpace(n.Title) ? "(Başlıksız)" : n.Title,
                Tarih = n.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            }).ToList();

            dgvNotes.DataSource = displayList;

            if (dgvNotes.Columns["ID"] != null)
            {
                dgvNotes.Columns["ID"].Visible = false;
            }
        }

        private void LoadSelectedNote()
        {
            if (dgvNotes.CurrentRow == null) return;

            int noteId = Convert.ToInt32(dgvNotes.CurrentRow.Cells["ID"].Value);
            var note = _cachedNotes.FirstOrDefault(n => n.Id == noteId);
            if (note == null) return;

            _selectedNoteId = note.Id;
            lblEditingTitle.Text = "Not Düzenle";
            txtTitle.Text = note.Title;
            txtContent.Text = note.Content;
            lblStatus.Text = string.Empty;
        }

        private void BtnNew_Click(object? sender, EventArgs e)
        {
            dgvNotes.ClearSelection();
            dgvNotes.CurrentCell = null; // "aktif satır" bilgisini de gerçekten sıfırlıyoruz
            lblEditingTitle.Text = "Yeni Not";
            txtTitle.Clear();
            txtContent.Clear();
            lblStatus.Text = string.Empty;
            _selectedNoteId = null; // en sona aldık — SelectionChanged tetiklense bile artık üzerine yazamaz
            txtTitle.Focus();
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string title = txtTitle.Text.Trim();
            string content = txtContent.Text;

            bool success;
            string errorMessage;

            if (_selectedNoteId.HasValue)
            {
                success = _noteService.UpdateNote(_selectedNoteId.Value, _user.Id, title, content, out errorMessage);
            }
            else
            {
                success = _noteService.AddNote(_user.Id, title, content, out errorMessage);
            }

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Not kaydedildi.";
                LoadNotes();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (!_selectedNoteId.HasValue)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen silmek için bir not seçin.";
                return;
            }

            var confirm = MessageBox.Show("Bu notu silmek istediğinize emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _noteService.DeleteNote(_selectedNoteId.Value, _user.Id);
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Not silindi.";
                BtnNew_Click(null, EventArgs.Empty);
                LoadNotes();
            }
        }
    }
}