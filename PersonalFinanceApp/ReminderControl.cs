using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class ReminderControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly ReminderService _reminderService = new ReminderService();

        private static readonly Color AppBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color CardBackColor = Color.FromArgb(40, 44, 60);
        private static readonly Color MonthCardColor = Color.FromArgb(45, 50, 68);
        private static readonly Color CardBorderColor = Color.FromArgb(60, 65, 85);
        private static readonly Color TodayColor = Color.FromArgb(60, 64, 90);
        private static readonly Color TextLight = Color.White;

        // Aktif hatırlatıcılar için kapsül rengi (Hafif saydam kırmızı)
        private static readonly Color ActiveReminderColor = Color.FromArgb(160, 220, 90, 90);
        // Normal kapsül rengi (Daha şeffaf)
        private static readonly Color DefaultCapsuleColor = Color.FromArgb(18, 255, 255, 255);

        private Panel pnlTop = new Panel();
        private Panel pnlCalendar = new Panel();
        private TableLayoutPanel tblMonths = new TableLayoutPanel();
        private Label lblYear = new Label();

        private int _currentYear;
        private List<Reminder> _cachedReminders = new List<Reminder>();

        private static readonly string[] MonthNames =
        {
            "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
            "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
        };

        public ReminderControl(User user)
        {
            _user = user;
            _currentYear = DateTime.Today.Year;
            InitializeComponent();
            SetupUI();
            LoadRemindersAndBuildCalendar();
        }

        public void RefreshData()
        {
            LoadRemindersAndBuildCalendar();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            EnableDoubleBuffering(this);

            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 110;
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

            Panel pnlYearCapsule = new Panel
            {
                Left = 20,
                Top = 65,
                Width = 150,
                Height = 34,
                BackColor = AppBackColor
            };

            pnlYearCapsule.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(AppBackColor);

                int radius = pnlYearCapsule.Height / 2;
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnlYearCapsule.Width - 1, pnlYearCapsule.Height - 1), radius))
                {
                    using (var brush = new SolidBrush(CardBackColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (var pen = new Pen(CardBorderColor, 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            pnlYearCapsule.SizeChanged += (s, e) => pnlYearCapsule.Invalidate();

            Button btnPrev = new Button
            {
                Text = "◀",
                Left = 2,
                Top = 2,
                Width = 30,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextLight,
                Cursor = Cursors.Hand
            };
            btnPrev.FlatAppearance.BorderSize = 0;
            btnPrev.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnPrev.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 255, 255);
            btnPrev.Click += (s, e) => ChangeYear(-1);

            lblYear.Left = 32;
            lblYear.Top = 0;
            lblYear.Width = 86;
            lblYear.Height = 34;
            lblYear.AutoSize = false;
            lblYear.ForeColor = TextLight;
            lblYear.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblYear.TextAlign = ContentAlignment.MiddleCenter;
            lblYear.BackColor = Color.Transparent;

            Button btnNext = new Button
            {
                Text = "▶",
                Left = 118,
                Top = 2,
                Width = 30,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextLight,
                Cursor = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 255, 255);
            btnNext.Click += (s, e) => ChangeYear(1);

            pnlYearCapsule.Controls.Add(btnPrev);
            pnlYearCapsule.Controls.Add(lblYear);
            pnlYearCapsule.Controls.Add(btnNext);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(pnlYearCapsule);

            pnlCalendar.Dock = DockStyle.Fill;
            pnlCalendar.Padding = new Padding(16);
            pnlCalendar.BackColor = AppBackColor;

            tblMonths.Dock = DockStyle.Fill;
            tblMonths.ColumnCount = 4;
            tblMonths.RowCount = 3;
            tblMonths.BackColor = AppBackColor;
            for (int i = 0; i < 4; i++) tblMonths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            for (int i = 0; i < 3; i++) tblMonths.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3f));
            EnableDoubleBuffering(tblMonths);

            pnlCalendar.Controls.Add(tblMonths);

            this.Controls.Add(pnlCalendar);
            this.Controls.Add(pnlTop);
        }

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                control,
                new object[] { true });
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
                    using (var pen = new Pen(CardBorderColor, 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        private void ChangeYear(int delta)
        {
            _currentYear += delta;
            LoadRemindersAndBuildCalendar();
        }

        private void LoadRemindersAndBuildCalendar()
        {
            _cachedReminders = _reminderService.GetUserReminders(_user.Id);
            BuildAllMonths();
        }

        private void BuildAllMonths()
        {
            lblYear.Text = _currentYear.ToString();

            tblMonths.SuspendLayout();
            this.SuspendLayout();

            tblMonths.Controls.Clear();

            for (int month = 1; month <= 12; month++)
            {
                var card = BuildMiniMonthCard(_currentYear, month);
                int col = (month - 1) % 4;
                int row = (month - 1) / 4;
                tblMonths.Controls.Add(card, col, row);
            }

            this.ResumeLayout(true);
            tblMonths.ResumeLayout(true);
        }

        private Panel BuildMiniMonthCard(int year, int month)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6)
            };

            SetupSmoothContainer(card, 12, MonthCardColor);
            EnableDoubleBuffering(card);

            var dayGrid = BuildDayGrid(year, month);

            Panel pnlMonthTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };

            string monthText = MonthNames[month - 1];
            pnlMonthTitle.Paint += (s, e) =>
            {
                DrawSoftChip(e.Graphics, pnlMonthTitle.Width, pnlMonthTitle.Height - 4, monthText,
                    new Font("Segoe UI", 9.5F, FontStyle.Bold), 0, 24, true, DefaultCapsuleColor, 85, 12);

                using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
                {
                    e.Graphics.DrawLine(pen, 15, pnlMonthTitle.Height - 1, pnlMonthTitle.Width - 15, pnlMonthTitle.Height - 1);
                }
            };

            card.Controls.Add(dayGrid);
            card.Controls.Add(pnlMonthTitle);

            return card;
        }

        private TableLayoutPanel BuildDayGrid(int year, int month)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            for (int i = 0; i < 7; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7f));
            }

            DateTime firstOfMonth = new DateTime(year, month, 1);
            int daysInMonth = DateTime.DaysInMonth(year, month);
            int startOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7;

            int totalCells = startOffset + daysInMonth;
            int rowCount = (int)Math.Ceiling(totalCells / 7.0);

            grid.RowCount = rowCount;
            for (int i = 0; i < rowCount; i++)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));
            }

            int dayCounter = 1;
            for (int cell = 0; cell < rowCount * 7; cell++)
            {
                int row = cell / 7;
                int col = cell % 7;

                if (cell < startOffset || dayCounter > daysInMonth)
                {
                    Panel empty = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
                    grid.Controls.Add(empty, col, row);
                    continue;
                }

                int thisDay = dayCounter;
                DateTime cellDate = new DateTime(year, month, thisDay);
                bool isToday = cellDate.Date == DateTime.Today;

                // Tamamlanmamış aktif hatırlatıcı var mı kontrolü
                bool hasActiveReminders = _cachedReminders.Any(r => r.ReminderDate.Date == cellDate.Date && !r.IsCompleted);

                Panel dayCell = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = isToday ? TodayColor : Color.Transparent,
                    Cursor = Cursors.Hand
                };

                string dayText = thisDay.ToString();
                dayCell.Paint += (s, e) =>
                {
                    int chipWidth = 28;
                    int chipHeight = 26;
                    int cornerRadius = 6;

                    Font dayFont = new Font("Segoe UI Semibold", 8.5F, isToday ? FontStyle.Bold : FontStyle.Regular);

                    // Eğer aktif hatırlatıcı varsa arka planı kırmızı kapsül yap, yoksa normal şeffaf rengini ver
                    Color capsuleColor = hasActiveReminders ? ActiveReminderColor : DefaultCapsuleColor;

                    DrawSoftChip(e.Graphics, dayCell.Width, dayCell.Height, dayText,
                        dayFont, 0, chipHeight, true, capsuleColor, chipWidth, cornerRadius);
                };

                dayCell.Click += (s, e) => OpenDayDialog(cellDate);

                grid.Controls.Add(dayCell, col, row);
                dayCounter++;
            }

            return grid;
        }

        private void DrawSoftChip(Graphics g, int areaWidth, int areaHeight, string text, Font font,
            int horizontalPadding, int chipHeight, bool centered, Color bgColor, int fixedWidth = 0, int cornerRadius = -1)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int chipWidth;
            if (fixedWidth > 0)
            {
                chipWidth = fixedWidth;
            }
            else
            {
                SizeF textSize = g.MeasureString(text, font);
                chipWidth = (int)textSize.Width + horizontalPadding;
            }

            int chipX, chipY;
            if (centered)
            {
                chipX = (areaWidth - chipWidth) / 2;
                chipY = (areaHeight - chipHeight) / 2;
            }
            else
            {
                chipX = 2;
                chipY = 1;
            }

            Rectangle chipRect = new Rectangle(chipX, chipY, chipWidth, chipHeight);

            int radius = cornerRadius >= 0 ? cornerRadius : chipHeight / 2;

            using (var path = GetRoundedRectPath(chipRect, radius))
            using (var brush = new SolidBrush(bgColor)) // Arka plan rengini dinamik olarak alıyoruz
            {
                g.FillPath(brush, path);
            }

            using (var textBrush = new SolidBrush(TextLight))
            {
                var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(text, font, textBrush, chipRect, format);
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = Math.Max(radius * 2, 1);
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
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