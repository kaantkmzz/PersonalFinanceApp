using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class SavingsGoalControl : UserControl
    {
        private readonly User _user;
        private readonly SavingsGoalService _goalService = new SavingsGoalService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color AccentColor = Color.FromArgb(99, 102, 241);
        private static readonly Color DangerColor = Color.FromArgb(220, 90, 90);

        private Panel pnlTop = new Panel();
        private Panel pnlGrid = new Panel();
        private Panel pnlBottom = new Panel();

        private TextBox txtGoalName = new TextBox();
        private TextBox txtTargetAmount = new TextBox();
        private Button btnAddGoal = new Button();
        private Label lblStatus = new Label();

        private DataGridView dgvGoals = new DataGridView();
        private List<SavingsGoal> _cachedGoals = new List<SavingsGoal>();
        private Button btnDelete = new Button();
        private bool _isUpdatingProgrammatically = false;
        private class GoalRow
        {
            public int ID { get; set; }
            public string Hedef { get; set; } = string.Empty;
            public string HedefTutar { get; set; } = string.Empty;
            public bool MevcutDurum { get; set; }
        }

        public SavingsGoalControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            LoadGoals();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 190;
            pnlTop.BackColor = AppBackColor;

            Label lblTitle = new Label
            {
                Text = "Hedefler",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 15,
                AutoSize = true
            };

            Label lblGoalName = new Label { Text = "Hedef Adı:", Left = 20, Top = 70, ForeColor = TextMuted, AutoSize = true };
            txtGoalName.Left = 20;
            txtGoalName.Top = 100;
            txtGoalName.Width = 250;
            txtGoalName.Font = new Font("Segoe UI", 9.5F);

            Label lblTargetAmount = new Label { Text = "Hedef Tutar:", Left = 290, Top = 70, ForeColor = TextMuted, AutoSize = true };
            txtTargetAmount.Left = 290;
            txtTargetAmount.Top = 100;
            txtTargetAmount.Width = 150;
            txtTargetAmount.Font = new Font("Segoe UI", 9.5F);

            btnAddGoal.Text = "Hedef Ekle";
            btnAddGoal.Left = 460;
            btnAddGoal.Top = 98;
            btnAddGoal.Width = 130;
            btnAddGoal.Height = 30;
            btnAddGoal.FlatStyle = FlatStyle.Flat;
            btnAddGoal.FlatAppearance.BorderSize = 0;
            btnAddGoal.BackColor = AccentColor;
            btnAddGoal.ForeColor = Color.White;
            btnAddGoal.Cursor = Cursors.Hand;
            btnAddGoal.Click += BtnAddGoal_Click;

            lblStatus.Left = 20;
            lblStatus.Top = 140;
            lblStatus.Width = 700;
            lblStatus.Height = 25;
            lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
            lblStatus.Font = new Font("Segoe UI", 9F);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblGoalName);
            pnlTop.Controls.Add(txtGoalName);
            pnlTop.Controls.Add(lblTargetAmount);
            pnlTop.Controls.Add(txtTargetAmount);
            pnlTop.Controls.Add(btnAddGoal);
            pnlTop.Controls.Add(lblStatus);

            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.Padding = new Padding(20, 0, 20, 0);
            pnlGrid.BackColor = AppBackColor;

            dgvGoals.Dock = DockStyle.Fill;
            dgvGoals.AllowUserToAddRows = false;
            dgvGoals.AllowUserToDeleteRows = false;
            dgvGoals.AllowUserToResizeColumns = false;
            dgvGoals.AllowUserToResizeRows = false;
            dgvGoals.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvGoals.MultiSelect = false;
            dgvGoals.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvGoals.BackgroundColor = Color.White;
            dgvGoals.BorderStyle = BorderStyle.None;
            dgvGoals.RowHeadersVisible = false;
            dgvGoals.GridColor = Color.FromArgb(230, 230, 235);
            dgvGoals.Font = new Font("Segoe UI", 9.5F);
            dgvGoals.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            dgvGoals.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 60, 70);
            dgvGoals.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvGoals.ColumnHeadersHeight = 36;
            dgvGoals.EnableHeadersVisualStyles = false;
            dgvGoals.RowTemplate.Height = 32;
            dgvGoals.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dgvGoals.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 239, 246);
            dgvGoals.DefaultCellStyle.SelectionForeColor = Color.FromArgb(40, 40, 40);

            dgvGoals.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvGoals.IsCurrentCellDirty)
                {
                    dgvGoals.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            dgvGoals.CellValueChanged += DgvGoals_CellValueChanged;
            dgvGoals.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvGoals.Columns[e.ColumnIndex].Name == "MevcutDurum")
                {
                    dgvGoals.EndEdit();
                }
            };

            pnlGrid.Controls.Add(dgvGoals);

            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 60;
            pnlBottom.BackColor = AppBackColor;

            btnDelete.Text = "Hedefi Sil";
            btnDelete.Left = 20;
            btnDelete.Top = 12;
            btnDelete.Width = 130;
            btnDelete.Height = 35;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 1;
            btnDelete.FlatAppearance.BorderColor = DangerColor;
            btnDelete.BackColor = AppBackColor;
            btnDelete.ForeColor = DangerColor;
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.Click += BtnDelete_Click;

            pnlBottom.Controls.Add(btnDelete);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private void LoadGoals()
        {
            _cachedGoals = _goalService.GetUserGoals(_user.Id);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            _isUpdatingProgrammatically = true;

            var displayList = _cachedGoals.Select(g => new
            {
                ID = g.Id,
                Hedef = g.GoalName,
                HedefTutar = g.TargetAmount.ToString("#,##0", tr) + " ₺",
                MevcutDurum = g.IsAchieved
            }).ToList();

            dgvGoals.DataSource = displayList;

            if (dgvGoals.Columns["ID"] != null)
            {
                dgvGoals.Columns["ID"].FillWeight = 25;
                dgvGoals.Columns["Hedef"].FillWeight = 100;
                dgvGoals.Columns["HedefTutar"].HeaderText = "Hedef Tutar";
                dgvGoals.Columns["MevcutDurum"].HeaderText = "Gerçekleşti mi?";
                dgvGoals.Columns["MevcutDurum"].FillWeight = 40;
                dgvGoals.Columns["ID"].ReadOnly = true;
                dgvGoals.Columns["Hedef"].ReadOnly = true;
                dgvGoals.Columns["HedefTutar"].ReadOnly = true;
            }

            ApplyAchievedStyling();

            _isUpdatingProgrammatically = false;
        }

        // Gerçekleşen hedeflerin satırını üstü çizili gösteriyoruz
        private void ApplyAchievedStyling()
        {
            foreach (DataGridViewRow row in dgvGoals.Rows)
            {
                int goalId = Convert.ToInt32(row.Cells["ID"].Value);
                var goal = _cachedGoals.FirstOrDefault(g => g.Id == goalId);
                if (goal == null) continue;

                if (goal.IsAchieved)
                {
                    row.DefaultCellStyle.Font = new Font(dgvGoals.Font, FontStyle.Strikeout);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 150);
                }
                else
                {
                    row.DefaultCellStyle.Font = dgvGoals.Font;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
            }
        }

        private void DgvGoals_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_isUpdatingProgrammatically || e.RowIndex < 0) return;
            if (dgvGoals.Columns[e.ColumnIndex].Name != "MevcutDurum") return;

            var row = dgvGoals.Rows[e.RowIndex];
            int goalId = Convert.ToInt32(row.Cells["ID"].Value);
            bool newValue = (bool)row.Cells["MevcutDurum"].Value;

            bool success;
            string errorMessage;

            if (newValue)
            {
                success = _goalService.MarkAchieved(goalId, _user.Id, out errorMessage);
            }
            else
            {
                success = _goalService.UnmarkAchieved(goalId, _user.Id, out errorMessage);
            }

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = newValue ? "Hedef gerçekleşti olarak işaretlendi." : "Hedef işareti kaldırıldı.";
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }

            LoadGoals(); // hem başarı hem hata durumunda tabloyu (ve checkbox'ı) doğru duruma tazeliyoruz
        }

        private void BtnAddGoal_Click(object? sender, EventArgs e)
        {
            string name = txtGoalName.Text.Trim();

            if (!decimal.TryParse(txtTargetAmount.Text, out decimal targetAmount))
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Geçersiz hedef tutar.";
                return;
            }

            bool success = _goalService.AddGoal(_user.Id, name, targetAmount, out string errorMessage);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Hedef eklendi.";
                txtGoalName.Clear();
                txtTargetAmount.Clear();
                LoadGoals();
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = errorMessage;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvGoals.CurrentRow == null)
            {
                lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblStatus.Text = "Lütfen silmek için bir hedef seçin.";
                return;
            }

            var confirm = MessageBox.Show("Bu hedefi silmek istediğinize emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                int goalId = Convert.ToInt32(dgvGoals.CurrentRow.Cells["ID"].Value);
                _goalService.DeleteGoal(goalId, _user.Id);
                lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblStatus.Text = "Hedef silindi.";
                LoadGoals();
            }
        }
    }
}