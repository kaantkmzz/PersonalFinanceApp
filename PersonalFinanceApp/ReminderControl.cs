using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class ReminderControl : UserControl
    {
        private readonly User _user;
        private readonly ReminderService _reminderService = new ReminderService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color TodayColor = Color.FromArgb(60, 64, 90);
        private static readonly Color TextLight = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(170, 173, 190);
        private static readonly Color MarkerColor = Color.FromArgb(230, 100, 100);

        private Panel pnlTop = new Panel();
        private Panel pnlCalendar = new Panel();
        private TableLayoutPanel tblCalendar = new TableLayoutPanel();
        private Label lblMonthYear = new Label();

        private int _currentYear;
        private int _currentMonth;
        private List<Reminder> _cachedReminders = new List<Reminder>();

        public ReminderControl(User user)
        {
            _user = user;
            _currentYear = DateTime.Today.Year;
            _currentMonth = DateTime.Today.Month;
            InitializeComponent();
            SetupUI();
            LoadRemindersAndBuildCalendar();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 100;
            pnlTop.BackColor = AppBackColor;

            Label lblTitle = new Label
            {
                Text = "Hatırlatıcılar",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 8,
                AutoSize = true
            };

            Button btnPrev = new Button
            {
                Text = "◀",
                Left = 20,
                Top = 62,
                Width = 40,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = CardBackColor,
                ForeColor = TextLight,
                Cursor = Cursors.Hand
            };
            btnPrev.FlatAppearance.BorderSize = 0;
            btnPrev.Click += (s, e) => ChangeMonth(-1);

            lblMonthYear.Left = 70;
            lblMonthYear.Top = 67;
            lblMonthYear.AutoSize = true;
            lblMonthYear.ForeColor = TextLight;
            lblMonthYear.Font = new Font("Segoe UI", 11F, FontStyle.Bold);

            Button btnNext = new Button
            {
                Text = "▶",
                Left = 230,
                Top = 62,
                Width = 40,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = CardBackColor,
                ForeColor = TextLight,
                Cursor = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Click += (s, e) => ChangeMonth(1);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(btnPrev);
            pnlTop.Controls.Add(lblMonthYear);
            pnlTop.Controls.Add(btnNext);

            pnlCalendar.Dock = DockStyle.Fill;
            pnlCalendar.Padding = new Padding(20);
            pnlCalendar.BackColor = AppBackColor;

            tblCalendar.Dock = DockStyle.Fill;
            tblCalendar.ColumnCount = 7;
            tblCalendar.BackColor = AppBackColor;
            tblCalendar.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            for (int i = 0; i < 7; i++)
            {
                tblCalendar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7f));
            }
            
            EnableDoubleBuffering(tblCalendar);
            EnableDoubleBuffering(pnlCalendar);

            pnlCalendar.Controls.Add(tblCalendar);

            this.Controls.Add(pnlCalendar);
            this.Controls.Add(pnlTop);
        }

        private void ChangeMonth(int delta)
        {
            _currentMonth += delta;
            if (_currentMonth < 1) { _currentMonth = 12; _currentYear--; }
            if (_currentMonth > 12) { _currentMonth = 1; _currentYear++; }

            LoadRemindersAndBuildCalendar();
        }

        // Yansıma (reflection) kullanarak, normalde gizli olan çift tamponlama özelliğini açıyoruz
        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                control,
                new object[] { true });
        }

        private void LoadRemindersAndBuildCalendar()
        {
            _cachedReminders = _reminderService.GetUserReminders(_user.Id);
            BuildCalendar();
        }

        private void BuildCalendar()
        {
            tblCalendar.SuspendLayout();
            this.SuspendLayout();
            string[] monthNames = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
            lblMonthYear.Text = $"{monthNames[_currentMonth - 1]} {_currentYear}";

            tblCalendar.Controls.Clear();
            tblCalendar.RowStyles.Clear();
            tblCalendar.RowCount = 1;
            tblCalendar.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            string[] dayNames = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
            for (int i = 0; i < 7; i++)
            {
                Label lblDayName = new Label
                {
                    Text = dayNames[i],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = TextMuted,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                tblCalendar.Controls.Add(lblDayName, i, 0);
            }

            DateTime firstOfMonth = new DateTime(_currentYear, _currentMonth, 1);
            int daysInMonth = DateTime.DaysInMonth(_currentYear, _currentMonth);
            int startOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7; // Pazartesi = 0 olacak şekilde hizalıyoruz

            int totalCells = startOffset + daysInMonth;
            int rowCount = (int)Math.Ceiling(totalCells / 7.0);

            for (int r = 0; r < rowCount; r++)
            {
                tblCalendar.RowCount++;
                tblCalendar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));
            }

            int dayCounter = 1;
            for (int cell = 0; cell < rowCount * 7; cell++)
            {
                int row = cell / 7 + 1;
                int col = cell % 7;

                if (cell < startOffset || dayCounter > daysInMonth)
                {
                    Panel emptyCell = new Panel { Dock = DockStyle.Fill, BackColor = AppBackColor };
                    tblCalendar.Controls.Add(emptyCell, col, row);
                    continue;
                }

                int thisDay = dayCounter;
                DateTime cellDate = new DateTime(_currentYear, _currentMonth, thisDay);
                bool isToday = cellDate.Date == DateTime.Today;
                bool hasReminders = _cachedReminders.Any(r => r.ReminderDate.Date == cellDate.Date);

                Panel dayPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = isToday ? TodayColor : CardBackColor,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(2)
                };

                Label lblDayNumber = new Label
                {
                    Text = thisDay.ToString(),
                    Left = 8,
                    Top = 6,
                    AutoSize = true,
                    ForeColor = TextLight,
                    Font = new Font("Segoe UI", 10F, isToday ? FontStyle.Bold : FontStyle.Regular),
                    Cursor = Cursors.Hand
                };
                dayPanel.Controls.Add(lblDayNumber);

                if (hasReminders)
                {
                    Label lblMarker = new Label
                    {
                        Text = "●",
                        Left = lblDayNumber.Right + 4,
                        Top = 4,
                        AutoSize = true,
                        ForeColor = MarkerColor,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        Cursor = Cursors.Hand
                    };
                    dayPanel.Controls.Add(lblMarker);
                    lblMarker.Click += (s, e) => OpenDayDialog(cellDate);
                }

                EventHandler openDialog = (s, e) => OpenDayDialog(cellDate);
                dayPanel.Click += openDialog;
                lblDayNumber.Click += openDialog;

                tblCalendar.Controls.Add(dayPanel, col, row);
                dayCounter++;
            }
            this.ResumeLayout(true);
            tblCalendar.ResumeLayout(true);
        }

        private void OpenDayDialog(DateTime date)
        {
            using (var dialog = new ReminderDayDialog(_user, date))
            {
                dialog.ShowDialog();
            }

            LoadRemindersAndBuildCalendar();
        }
    }
}