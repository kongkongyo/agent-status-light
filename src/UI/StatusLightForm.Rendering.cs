using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {

        private void DrawWindow(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            g.ScaleTransform(UiScale, UiScale);
            SkinPalette palette = GetSkinPalette();
            bool vertical = IsVerticalLightOrientation();
            RectangleF bodyBounds = vertical ? new RectangleF(19, 9, 58, 144) : new RectangleF(9, 19, 144, 58);

            if (palette.DrawBody)
            {
                using (GraphicsPath bodyPath = RoundPath(bodyBounds, 13))
                using (var bodyBrush = new LinearGradientBrush(bodyBounds, palette.BodyStart, palette.BodyEnd, vertical ? 90f : 0f))
                using (var borderPen = new Pen(palette.Border, 2))
                {
                    g.FillPath(bodyBrush, bodyPath);
                    g.DrawPath(borderPen, bodyPath);
                }
            }

            Color inactiveColor = GetInactiveLightColor(palette);
            if (vertical)
            {
                DrawLight(g, 32, 20, 32, confirmLightColor, currentConfirmCount > 0, currentConfirmCount, palette, inactiveColor);
                DrawLight(g, 32, 65, 32, workingLightColor, currentWorkingCount > 0, currentWorkingCount, palette, inactiveColor);
                DrawLight(g, 32, 110, 32, doneLightColor, currentDoneCount > 0 && doneFlashVisible, currentDoneCount, palette, inactiveColor);
                return;
            }

            DrawLight(g, 20, 32, 32, confirmLightColor, currentConfirmCount > 0, currentConfirmCount, palette, inactiveColor);
            DrawLight(g, 65, 32, 32, workingLightColor, currentWorkingCount > 0, currentWorkingCount, palette, inactiveColor);
            DrawLight(g, 110, 32, 32, doneLightColor, currentDoneCount > 0 && doneFlashVisible, currentDoneCount, palette, inactiveColor);
        }


        private void ApplyTopMost()
        {
            TopMost = true;
            if (!IsHandleCreated) return;

            NativeMethods.SetWindowPos(Handle, NativeMethods.HwndTopMost, Location.X, Location.Y, Width, Height, NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
        }


        private void RenderLayeredWindow()
        {
            if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            using (var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppPArgb))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    DrawWindow(g);
                }

                IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
                IntPtr memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
                IntPtr bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
                IntPtr oldBitmap = NativeMethods.SelectObject(memoryDc, bitmapHandle);

                try
                {
                    var size = new NativeSize(ClientSize.Width, ClientSize.Height);
                    var source = new NativePoint(0, 0);
                    var destination = new NativePoint(Left, Top);
                    var blend = new BlendFunction
                    {
                        BlendOp = NativeMethods.AcSrcOver,
                        BlendFlags = 0,
                        SourceConstantAlpha = 255,
                        AlphaFormat = NativeMethods.AcSrcAlpha
                    };

                    NativeMethods.UpdateLayeredWindow(Handle, screenDc, ref destination, ref size, memoryDc, ref source, 0, ref blend, NativeMethods.UlwAlpha);
                }
                finally
                {
                    NativeMethods.SelectObject(memoryDc, oldBitmap);
                    NativeMethods.DeleteObject(bitmapHandle);
                    NativeMethods.DeleteDC(memoryDc);
                    NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
                }
            }
        }


        private static Point CenterInPrimaryScreen(Size size)
        {
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int x = screen.Left + Math.Max(0, (screen.Width - size.Width) / 2);
            int y = screen.Top + Math.Max(0, (screen.Height - size.Height) / 2);
            return new Point(x, y);
        }


        private Point GetStartupLocation(Size size)
        {
            if (hasSavedWindowLocation && IsWindowLocationVisible(savedWindowLocation, size))
            {
                return savedWindowLocation;
            }

            return CenterInPrimaryScreen(size);
        }


        private static bool IsWindowLocationVisible(Point location, Size size)
        {
            Rectangle window = new Rectangle(location, size);
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(window))
                {
                    return true;
                }
            }

            return false;
        }


        private static int ScaleDimension(int value)
        {
            return (int)Math.Ceiling(value * UiScale);
        }


        private static GraphicsPath RoundPath(RectangleF rect, float radius)
        {
            float diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }


        private Color GetInactiveLightColor(SkinPalette palette)
        {
            return waitingLightColor.HasValue ? waitingLightColor.Value : palette.InactiveLens;
        }


        private static void DrawLight(Graphics g, int x, int y, int size, Color color, bool active, int count, SkinPalette palette, Color inactiveColor)
        {
            Rectangle outer = new Rectangle(x, y, size, size);
            using (var rimBrush = new SolidBrush(palette.Rim))
            {
                g.FillEllipse(rimBrush, outer);
            }

            if (active)
            {
                for (int i = 3; i >= 1; i--)
                {
                    int grow = 3 * i;
                    int alpha = 28 + (18 * (4 - i));
                    Rectangle glowRect = new Rectangle(x - grow, y - grow, size + grow * 2, size + grow * 2);
                    using (var glowBrush = new SolidBrush(Color.FromArgb(alpha, color)))
                    {
                        g.FillEllipse(glowBrush, glowRect);
                    }
                }
            }

            Rectangle inner = new Rectangle(x + 6, y + 6, size - 12, size - 12);
            Color baseColor = active ? color : inactiveColor;
            using (var lensBrush = new LinearGradientBrush(inner, baseColor, palette.LensShadow, 65))
            {
                g.FillEllipse(lensBrush, inner);
            }

            Rectangle shine = new Rectangle(x + 13, y + 11, 11, 7);
            using (var shineBrush = new SolidBrush(Color.FromArgb(active ? 155 : palette.InactiveShineAlpha, palette.Shine)))
            {
                g.FillEllipse(shineBrush, shine);
            }

            if (active && count > 1)
            {
                DrawLightCount(g, inner, count);
            }
        }


        private static void DrawLightCount(Graphics g, Rectangle bounds, int count)
        {
            string text = count > 99 ? "99+" : count.ToString(CultureInfo.InvariantCulture);
            float fontSize = text.Length == 1 ? 14f : (text.Length == 2 ? 12f : 9f);

            using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
            using (var textBrush = new SolidBrush(Color.Black))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                RectangleF textRect = new RectangleF(bounds.X, bounds.Y - 1, bounds.Width, bounds.Height);
                RectangleF shadowRect = new RectangleF(textRect.X, textRect.Y + 1, textRect.Width, textRect.Height);
                g.DrawString(text, font, shadowBrush, shadowRect, format);
                g.DrawString(text, font, textBrush, textRect, format);
            }
        }
    }
}
