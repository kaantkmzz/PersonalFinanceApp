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
        private NotebookTextBox txtContent = new NotebookTextBox();
        private Button btnNew = new Button();
        private Button btnSave = new Button();
        private Button btnDelete = new Button();
        private Label lblStatus = new Label();
        private Label lblEditingTitle = new Label();

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

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
            btnNew.Cursor = Cursors.Hand;
            SetupRoundedButton(btnNew, AccentColor, Color.White, false);
            btnNew.Click += BtnNew_Click;

            Panel pnlGridWrapper = new Panel
            {
                Left = 20,
                Top = 115,
                Width = 420,
                Height = 600,
                Padding = new Padding(2, 6, 2, 6)
            };
            SetupSmoothContainer(pnlGridWrapper, 12, CardBackColor);

            dgvNotes.Dock = DockStyle.Fill;
            dgvNotes.ReadOnly = true;
            dgvNotes.AllowUserToAddRows = false;
            dgvNotes.AllowUserToDeleteRows = false;
            dgvNotes.AllowUserToResizeColumns = false;
            dgvNotes.AllowUserToResizeRows = false;
            dgvNotes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvNotes.MultiSelect = false;
            dgvNotes.ColumnHeadersVisible = false;
            dgvNotes.RowHeadersVisible = false;
            dgvNotes.Font = new Font("Segoe UI", 9.5F);
            dgvNotes.RowTemplate.Height = 46;
            dgvNotes.SelectionChanged += (s, e) => LoadSelectedNote();

            dgvNotes.BorderStyle = BorderStyle.None;
            dgvNotes.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvNotes.GridColor = Color.FromArgb(60, 65, 85);

            dgvNotes.BackgroundColor = CardBackColor;
            dgvNotes.DefaultCellStyle.BackColor = CardBackColor;
            dgvNotes.DefaultCellStyle.ForeColor = TextLight;
            dgvNotes.AlternatingRowsDefaultCellStyle.BackColor = CardBackColor;
            dgvNotes.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 64, 90);
            dgvNotes.DefaultCellStyle.SelectionForeColor = TextLight;

            dgvNotes.CellPainting += DgvNotes_CellPainting;

            pnlGridWrapper.Controls.Add(dgvNotes);

            pnlLeft.Controls.Add(lblTitle);
            pnlLeft.Controls.Add(btnNew);
            pnlLeft.Controls.Add(pnlGridWrapper);

            // --- Sağ panel: not düzenleme alanı ---
            Panel pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppBackColor
            };

            Panel pnlRightTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 155,
                BackColor = AppBackColor,
                Padding = new Padding(20, 20, 20, 0)
            };

            lblEditingTitle.Text = "Bir not seçin ya da yeni bir not oluşturun";
            lblEditingTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblEditingTitle.ForeColor = TextLight;
            lblEditingTitle.Left = 0;
            lblEditingTitle.Top = 0;
            lblEditingTitle.AutoSize = true;

            Label lblTitleField = new Label { Text = "Başlık:", Left = 0, Top = 45, ForeColor = TextMuted, AutoSize = true };

            Panel pnlTitleWrapper = new Panel
            {
                Left = 0,
                Top = 75,
                Width = 700,
                Height = 42
            };
            SetupSmoothContainer(pnlTitleWrapper, 8, CardBackColor);

            txtTitle.BorderStyle = BorderStyle.None;
            txtTitle.Font = new Font("Segoe UI", 11F);
            txtTitle.BackColor = CardBackColor;
            txtTitle.ForeColor = TextLight;
            txtTitle.Width = 680;
            txtTitle.Location = new Point(10, 8);

            pnlTitleWrapper.Controls.Add(txtTitle);

            // Görsel denge için Top değeri 120'den 135'e indirilip not alanına yaklaştırıldı
            Label lblContentField = new Label { Text = "İçerik:", Left = 0, Top = 135, ForeColor = TextMuted, AutoSize = true };

            pnlRightTop.Controls.Add(lblEditingTitle);
            pnlRightTop.Controls.Add(lblTitleField);
            pnlRightTop.Controls.Add(pnlTitleWrapper);
            pnlRightTop.Controls.Add(lblContentField);

            Panel pnlRightBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = AppBackColor,
                Padding = new Padding(20, 10, 20, 20)
            };

            btnSave.Text = "Kaydet";
            btnSave.Left = 0;
            btnSave.Top = 0;
            btnSave.Width = 140;
            btnSave.Height = 36;
            btnSave.Cursor = Cursors.Hand;
            SetupRoundedButton(btnSave, AccentColor, Color.White, false);
            btnSave.Click += BtnSave_Click;

            btnDelete.Text = "Notu Sil";
            btnDelete.Left = 150;
            btnDelete.Top = 0;
            btnDelete.Width = 140;
            btnDelete.Height = 36;
            btnDelete.Cursor = Cursors.Hand;
            SetupRoundedButton(btnDelete, DangerColor, Color.White, false);
            btnDelete.Click += BtnDelete_Click;

            lblStatus.Left = 0;
            lblStatus.Top = 45;
            lblStatus.Width = 500;
            lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F);

            pnlRightBottom.Controls.Add(btnSave);
            pnlRightBottom.Controls.Add(btnDelete);
            pnlRightBottom.Controls.Add(lblStatus);

            txtContent.Dock = DockStyle.Fill;
            txtContent.BorderStyle = BorderStyle.None;
            txtContent.Multiline = true;
            txtContent.ScrollBars = ScrollBars.None;
            txtContent.WordWrap = true;
            txtContent.Font = new Font("Segoe UI", 11F);
            txtContent.BackColor = CardBackColor;
            txtContent.ForeColor = TextLight;

            Panel pnlContentOuter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 40, 20),
                BackColor = AppBackColor
            };

            Panel pnlContentWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6, 12, 6, 12)
            };
            SetupSmoothContainer(pnlContentWrapper, 12, CardBackColor);

            pnlContentWrapper.Controls.Add(txtContent);
            pnlContentOuter.Controls.Add(pnlContentWrapper);

            pnlRight.Controls.Add(pnlContentOuter);
            pnlRight.Controls.Add(pnlRightBottom);
            pnlRight.Controls.Add(pnlRightTop);

            this.Controls.Add(pnlRight);
            this.Controls.Add(pnlLeft);
        }

        private void DgvNotes_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvNotes.Columns[e.ColumnIndex].Name == "Tarih")
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);

                bool isSelected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
                Color lineColor = isSelected ? Color.FromArgb(110, 115, 140) : Color.FromArgb(60, 65, 85);

                using (Pen p = new Pen(lineColor, 1))
                {
                    e.Graphics.DrawLine(p, e.CellBounds.Left, e.CellBounds.Top + 8, e.CellBounds.Left, e.CellBounds.Bottom - 8);
                }

                e.Handled = true;
            }
        }

        private void LoadNotes()
        {
            _cachedNotes = _noteService.GetUserNotes(_user.Id);

            var displayList = _cachedNotes.Select(n => new
            {
                ID = n.Id,
                Başlık = string.IsNullOrWhiteSpace(n.Title) ? "(Başlıksız)" : n.Title,
                Tarih = n.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy")
            }).ToList();

            dgvNotes.DataSource = displayList;

            if (dgvNotes.Columns["ID"] != null)
                dgvNotes.Columns["ID"].Visible = false;

            if (dgvNotes.Columns["Başlık"] != null && dgvNotes.Columns["Tarih"] != null)
            {
                dgvNotes.Columns["Başlık"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                dgvNotes.Columns["Tarih"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgvNotes.Columns["Tarih"].Width = 110;
                dgvNotes.Columns["Tarih"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvNotes.Columns["Tarih"].DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);
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
            dgvNotes.CurrentCell = null;
            lblEditingTitle.Text = "Yeni Not";
            txtTitle.Clear();
            txtContent.Clear();
            lblStatus.Text = string.Empty;
            _selectedNoteId = null;
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
                e.Graphics.Clear(btn.Parent.BackColor);

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

    public class NotebookTextBox : TextBox
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        private const int EM_SETMARGINS = 0xd3;
        private const int EC_LEFTMARGIN = 0x1;

        private const int WM_PAINT = 0x000F;
        private const int WM_VSCROLL = 0x0115;
        private const int WM_MOUSEWHEEL = 0x020A;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SendMessage(this.Handle, EM_SETMARGINS, (IntPtr)EC_LEFTMARGIN, (IntPtr)40);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    int lineHeight = this.Font.Height;

                    using (Pen p = new Pen(Color.FromArgb(60, 65, 85), 1))
                    {
                        for (int y = lineHeight; y < this.Height; y += lineHeight)
                        {
                            g.DrawLine(p, 0, y + 2, this.Width, y + 2);
                        }
                    }

                    using (Pen pMargin = new Pen(Color.FromArgb(50, 55, 75), 1))
                    {
                        g.DrawLine(pMargin, 32, 0, 32, this.Height);
                    }
                }
            }
            else if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                this.Invalidate();
            }
        }
    }
}