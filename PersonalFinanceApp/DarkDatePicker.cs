using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using PersonalFinanceApp.Helpers;

namespace PersonalFinanceApp
{
    // Native DateTimePicker'ın kapalı kutusu WinForms/.NET 10'da hiçbir yöntemle koyulaştırılamadı
    // (DisableVisualStyle+BackColor, ShowUpDown modu, Application.SetColorMode(Dark) — üçü de denendi,
    // bkz. TransactionControl.SetupDarkDateTimePicker'daki not) — bu yüzden tarih-only seçim gereken
    // yerlerde (tarih+saat değil) bu tamamen özel çizilen denetim kullanılıyor. Kapalı kutu da, açılan
    // tek-aylık takvim de uygulamanın kendi renkleriyle çiziliyor.
    public class DarkDatePicker : Panel
    {
        public event EventHandler? ValueChanged;

        private DateTime _value = DateTime.Today;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime Value
        {
            get => _value;
            set
            {
                DateTime v = ShowTime ? value : value.Date;
                DateTime clamped = MinDate.HasValue && v.Date < MinDate.Value.Date
                    ? (ShowTime ? MinDate.Value.Date.Add(v.TimeOfDay) : MinDate.Value.Date)
                    : v;
                if (_value == clamped) return;
                _value = clamped;
                UpdateText();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime? MinDate { get; set; }

        // true iken kapalı kutu ve açılır takvim "dd.MM.yyyy HH:mm" saat seçimini de gösterir
        // (bkz. TransactionEditDialog — işlem tarihi saat bileşeni de taşır).
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowTime { get; set; }

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;

        private readonly Label _lblValue;
        private CalendarPopup? _openPopup;

        public DarkDatePicker()
        {
            this.Height = 36;
            this.Cursor = Cursors.Hand;
            this.DoubleBuffered = true;

            this.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(this.Parent?.BackColor ?? AppBackColor);
                using var path = Helpers.UIStyleHelper.GetRoundedRectPath(new Rectangle(0, 0, this.Width - 1, this.Height - 1), 8);
                using (var brush = new SolidBrush(CardBackColor)) e.Graphics.FillPath(brush, path);

                int ax = this.Width - 20, ay = this.Height / 2 - 2;
                using var arrowBrush = new SolidBrush(this.Enabled ? TextMuted : Color.FromArgb(90, TextMuted));
                e.Graphics.FillPolygon(arrowBrush, new Point[] { new Point(ax, ay), new Point(ax + 8, ay), new Point(ax + 4, ay + 5) });
            };
            this.SizeChanged += (s, e) => { PositionLabel(); this.Invalidate(); };

            _lblValue = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextLight,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F),
                Left = 10,
                Top = 0,
                Cursor = Cursors.Hand
            };
            this.Controls.Add(_lblValue);
            PositionLabel();
            UpdateText();

            this.Click += (s, e) => TogglePopup();
            _lblValue.Click += (s, e) => TogglePopup();
            this.EnabledChanged += (s, e) =>
            {
                _lblValue.ForeColor = this.Enabled ? TextLight : TextMuted;
                this.Invalidate();
            };
        }

        private void PositionLabel()
        {
            _lblValue.Width = Math.Max(0, this.Width - 34);
            _lblValue.Height = this.Height;
        }

        private void UpdateText() => _lblValue.Text = _value.ToString(ShowTime ? "dd.MM.yyyy HH:mm" : "dd.MM.yyyy");

        private void TogglePopup()
        {
            if (!this.Enabled) return;
            if (_openPopup != null)
            {
                _openPopup.Close();
                return;
            }

            Point screenPoint = this.PointToScreen(new Point(0, this.Height + 4));
            var popup = new CalendarPopup(_value, MinDate, ShowTime);
            popup.DateSelected += d => { Value = d; popup.Close(); };
            popup.FormClosed += (s, e) => _openPopup = null;
            popup.Location = screenPoint;
            _openPopup = popup;
            popup.Show(this.FindForm() ?? (IWin32Window)this);
        }
    }

    // Tek aylık, gezinilebilir küçük takvim penceresi — odak kaybedince (dışarı tıklama) kendini kapatır.
    internal class CalendarPopup : Form
    {
        public event Action<DateTime>? DateSelected;

        private DateTime _displayMonth;
        private DateTime _selected;
        private readonly DateTime? _minDate;
        private readonly bool _showTime;
        private readonly Label _lblMonthYear = new Label();
        private readonly TableLayoutPanel _grid = new TableLayoutPanel();
        private NumericUpDown? _numHour;
        private NumericUpDown? _numMinute;

        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color HoverBackColor => AppTheme.HoverBackColor;

        private static readonly string[] MonthNames =
        {
            "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
            "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
        };
        private static readonly string[] WeekdayInitials = { "Pt", "Sa", "Ça", "Pe", "Cu", "Ct", "Pa" };

        public CalendarPopup(DateTime selected, DateTime? minDate, bool showTime = false)
        {
            _showTime = showTime;
            _selected = showTime ? selected : selected.Date;
            _minDate = minDate?.Date;
            _displayMonth = new DateTime(selected.Year, selected.Month, 1);

            // Bu uygulamadaki tüm Form'larda olduğu gibi — eksik kalırsa WinForms'un kendi
            // per-form DPI otomatik ölçeklemesi, zaten sabit piksel olarak tasarlanmış boyutların
            // üzerine BİR KEZ DAHA ölçekleme uyguluyor, gün hücrelerindeki 2 haneli sayıların
            // (10, 11, ... 31) ikinci rakamı kırpılarak görünüyordu.
            this.AutoScaleMode = AutoScaleMode.None;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            // ~150% DPI'de dar hücreler (7 sütun × küçük Size) iki haneli gün sayılarının (10-31)
            // ikinci rakamını kırpıyordu — daha geniş bir boyut her sütuna yeterli nefes alanı bırakıyor.
            this.Size = new Size(340, showTime ? 340 + 52 : 340);
            this.BackColor = CardBackColor;
            this.Deactivate += (s, e) => this.Close();

            this.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.GridLineColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            };

            SetupUI();
            BuildGrid();
        }

        private void SetupUI()
        {
            Button btnPrev = new Button { Text = "◄", Left = 8, Top = 8, Width = 32, Height = 28, Cursor = Cursors.Hand };
            StyleNavButton(btnPrev);
            btnPrev.Click += (s, e) => { _displayMonth = _displayMonth.AddMonths(-1); BuildGrid(); };

            Button btnNext = new Button { Text = "►", Left = this.Width - 40, Top = 8, Width = 32, Height = 28, Cursor = Cursors.Hand };
            StyleNavButton(btnNext);
            btnNext.Click += (s, e) => { _displayMonth = _displayMonth.AddMonths(1); BuildGrid(); };

            _lblMonthYear.Left = 44;
            _lblMonthYear.Top = 8;
            _lblMonthYear.Width = this.Width - 88;
            _lblMonthYear.Height = 28;
            _lblMonthYear.TextAlign = ContentAlignment.MiddleCenter;
            _lblMonthYear.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            _lblMonthYear.ForeColor = TextLight;

            _grid.Left = 8;
            _grid.Top = 44;
            _grid.Width = this.Width - 16;
            _grid.Height = 340 - 52;
            _grid.ColumnCount = 7;
            _grid.RowCount = 7;
            _grid.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            _grid.BackColor = CardBackColor;
            for (int i = 0; i < 7; i++) _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7f));
            for (int i = 0; i < 7; i++) _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 7f));

            this.Controls.Add(btnPrev);
            this.Controls.Add(btnNext);
            this.Controls.Add(_lblMonthYear);
            this.Controls.Add(_grid);

            if (_showTime)
            {
                int rowTop = _grid.Top + _grid.Height + 6;

                Label lblTime = new Label { Text = "Saat:", Left = 8, Top = rowTop + 6, AutoSize = true, ForeColor = TextMuted, Font = new Font("Segoe UI", 9F) };

                _numHour = new NumericUpDown { Left = 60, Top = rowTop, Width = 50, Height = 26, Minimum = 0, Maximum = 23, Value = _selected.Hour, TextAlign = HorizontalAlignment.Center };
                StyleNumericUpDown(_numHour);
                _numHour.Text = _selected.Hour.ToString("00");
                _numHour.ValueChanged += (s, e) =>
                {
                    _selected = _selected.Date.Add(new TimeSpan((int)_numHour.Value, (int)(_numMinute?.Value ?? 0), 0));
                    _numHour.Text = ((int)_numHour.Value).ToString("00");
                };

                Label lblColon = new Label { Text = ":", Left = 113, Top = rowTop + 4, AutoSize = true, ForeColor = TextLight };

                _numMinute = new NumericUpDown { Left = 128, Top = rowTop, Width = 50, Height = 26, Minimum = 0, Maximum = 59, Value = _selected.Minute, TextAlign = HorizontalAlignment.Center };
                StyleNumericUpDown(_numMinute);
                _numMinute.Text = _selected.Minute.ToString("00");
                _numMinute.ValueChanged += (s, e) =>
                {
                    _selected = _selected.Date.Add(new TimeSpan((int)(_numHour?.Value ?? 0), (int)_numMinute.Value, 0));
                    _numMinute.Text = ((int)_numMinute.Value).ToString("00");
                };

                Button btnApply = new Button { Text = "Uygula", Left = this.Width - 96, Top = rowTop - 2, Width = 80, Height = 30, Cursor = Cursors.Hand };
                btnApply.FlatStyle = FlatStyle.Flat;
                btnApply.FlatAppearance.BorderSize = 0;
                btnApply.BackColor = AccentColor;
                btnApply.ForeColor = Color.White;
                btnApply.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                btnApply.Click += (s, e) => DateSelected?.Invoke(_selected);

                this.Controls.Add(lblTime);
                this.Controls.Add(_numHour);
                this.Controls.Add(lblColon);
                this.Controls.Add(_numMinute);
                this.Controls.Add(btnApply);
            }
        }

        private void StyleNumericUpDown(NumericUpDown num)
        {
            num.BackColor = CardBackColor;
            num.ForeColor = TextLight;
            num.Font = new Font("Segoe UI", 9.5F);
            num.BorderStyle = BorderStyle.FixedSingle;
            // Yukarı/aşağı ok düğmeleri, NumericUpDown'ın kendi penceresi değil; içinde gizli
            // tuttuğu ayrı bir "UpDownButtons" alt denetimidir (DataGridView'in VScrollBar/HScrollBar
            // alt denetimleriyle aynı durum, bkz. SetDataGridViewScrollBarDarkMode) — bu yüzden temayı
            // NumericUpDown'ın kendi Handle'ına uygulamanın görsel etkisi yok, alt denetimi bulup
            // onu boyamak gerekiyor.
            num.HandleCreated += (s, e) =>
            {
                DarkTitleBarHelper.SetScrollBarDarkMode(num, true);
                foreach (Control child in num.Controls)
                {
                    child.HandleCreated += (cs, ce) => DarkTitleBarHelper.SetScrollBarDarkMode(child, true);
                    if (child.IsHandleCreated) DarkTitleBarHelper.SetScrollBarDarkMode(child, true);
                }
            };
        }

        private void StyleNavButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = TextMuted;
            btn.Font = new Font("Segoe UI", 9F);
        }

        private void BuildGrid()
        {
            _lblMonthYear.Text = $"{MonthNames[_displayMonth.Month - 1]} {_displayMonth.Year}";

            _grid.SuspendLayout();
            _grid.Controls.Clear();

            for (int i = 0; i < 7; i++)
            {
                Label lblHeader = new Label
                {
                    Text = WeekdayInitials[i],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    ForeColor = TextMuted
                };
                _grid.Controls.Add(lblHeader, i, 0);
            }

            int daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
            int startOffset = ((int)_displayMonth.DayOfWeek + 6) % 7;
            int dayCounter = 1;

            for (int cell = 0; cell < 6 * 7; cell++)
            {
                int row = 1 + cell / 7;
                int col = cell % 7;

                if (cell < startOffset || dayCounter > daysInMonth)
                {
                    _grid.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, col, row);
                    continue;
                }

                int thisDay = dayCounter;
                DateTime cellDate = new DateTime(_displayMonth.Year, _displayMonth.Month, thisDay);
                bool isSelected = cellDate == _selected.Date;
                bool isToday = cellDate == DateTime.Today;
                bool isDisabled = _minDate.HasValue && cellDate < _minDate.Value;

                Label dayLabel = new Label
                {
                    Text = thisDay.ToString(),
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9.5F, isToday ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = isDisabled ? Color.FromArgb(70, TextMuted) : (isSelected ? Color.White : TextLight),
                    BackColor = isSelected ? AccentColor : Color.Transparent,
                    Cursor = isDisabled ? Cursors.No : Cursors.Hand
                };

                if (!isDisabled)
                {
                    dayLabel.MouseEnter += (s, e) => { if (!isSelected) dayLabel.BackColor = HoverBackColor; };
                    dayLabel.MouseLeave += (s, e) => { if (!isSelected) dayLabel.BackColor = Color.Transparent; };
                    dayLabel.Click += (s, e) =>
                    {
                        if (_showTime)
                        {
                            _selected = cellDate.Add(_selected.TimeOfDay);
                            BuildGrid();
                        }
                        else
                        {
                            DateSelected?.Invoke(cellDate);
                        }
                    };
                }

                _grid.Controls.Add(dayLabel, col, row);
                dayCounter++;
            }

            _grid.ResumeLayout(true);
        }
    }
}
