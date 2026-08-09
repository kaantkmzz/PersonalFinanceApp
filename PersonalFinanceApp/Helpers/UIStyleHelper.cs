using System.Drawing.Drawing2D;

namespace PersonalFinanceApp.Helpers
{
    public static class UIStyleHelper
    {
        public static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
        public static void ApplyRoundedCorners(Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;

            var path = new GraphicsPath();
            var rect = new Rectangle(0, 0, control.Width, control.Height);
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            control.Region = new Region(path);
        }

        // İşlemler ekranındaki tablo standardını (çizgiler, satır yüksekliği, seçim rengi, başlık vurgusu) uyguluyor
        public static void StyleGridStandard(DataGridView grid)
        {
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(240, 240, 244);
            grid.RowTemplate.Height = 42;
            grid.ColumnHeadersHeight = 42;
            grid.BackgroundColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 227, 235);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = grid.ColumnHeadersDefaultCellStyle.ForeColor;
        }

        // Metin kutusu / açılır kutu için gri zemin + yuvarlak köşe
        public static void StyleInput(Control control, int radius = 8)
        {
            control.BackColor = Color.FromArgb(238, 239, 246);
            if (control is TextBox tb) tb.BorderStyle = BorderStyle.FixedSingle;
            ApplyRoundedCorners(control, radius);
        }
    }
}