using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {
        private void ShowColorSettingsDialog()
        {
            using (var dialog = new Form())
            using (var previewBox = new LightColorPreviewPanel())
            using (var colorDialog = new ColorDialog())
            using (var confirmLabel = new Label())
            using (var workingLabel = new Label())
            using (var doneLabel = new Label())
            using (var waitingLabel = new Label())
            using (var confirmSwatch = new Panel())
            using (var workingSwatch = new Panel())
            using (var doneSwatch = new Panel())
            using (var waitingSwatch = new Panel())
            using (var confirmButton = new Button())
            using (var workingButton = new Button())
            using (var doneButton = new Button())
            using (var waitingButton = new Button())
            using (var resetButton = new Button())
            using (var okButton = new Button())
            using (var cancelButton = new Button())
            {
                Color localConfirm = confirmLightColor;
                Color localWorking = workingLightColor;
                Color localDone = doneLightColor;
                Color? localWaiting = waitingLightColor;
                colorDialog.AnyColor = true;
                colorDialog.SolidColorOnly = true;
                colorDialog.FullOpen = false;

                dialog.Text = T.ColorSettingsTitle;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.ClientSize = new Size(456, 282);
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;

                previewBox.Location = new Point(16, 14);
                previewBox.Size = new Size(184, 104);

                ConfigureColorRow(confirmLabel, confirmSwatch, confirmButton, T.NeedsConfirmationLabel, 224, 18);
                ConfigureColorRow(workingLabel, workingSwatch, workingButton, T.Working, 224, 58);
                ConfigureColorRow(doneLabel, doneSwatch, doneButton, T.Done, 224, 98);
                ConfigureColorRow(waitingLabel, waitingSwatch, waitingButton, T.IdleDim, 224, 138);

                resetButton.Text = T.ResetDefault;
                resetButton.Location = new Point(16, 236);
                resetButton.Size = new Size(96, 26);

                okButton.Text = T.Save;
                okButton.Location = new Point(278, 236);
                okButton.Size = new Size(75, 26);

                cancelButton.Text = T.Cancel;
                cancelButton.Location = new Point(361, 236);
                cancelButton.Size = new Size(75, 26);
                cancelButton.DialogResult = DialogResult.Cancel;

                Action updatePreview = delegate
                {
                    SkinPalette palette = GetSkinPalette();
                    Color inactiveColor = localWaiting.HasValue ? localWaiting.Value : palette.InactiveLens;
                    confirmSwatch.BackColor = localConfirm;
                    workingSwatch.BackColor = localWorking;
                    doneSwatch.BackColor = localDone;
                    waitingSwatch.BackColor = inactiveColor;

                    previewBox.SetColors(localConfirm, localWorking, localDone, inactiveColor, palette);
                };

                confirmButton.Click += delegate
                {
                    Color selected;
                    if (TryChooseColor(colorDialog, dialog, localConfirm, out selected))
                    {
                        localConfirm = selected;
                        updatePreview();
                    }
                };
                workingButton.Click += delegate
                {
                    Color selected;
                    if (TryChooseColor(colorDialog, dialog, localWorking, out selected))
                    {
                        localWorking = selected;
                        updatePreview();
                    }
                };
                doneButton.Click += delegate
                {
                    Color selected;
                    if (TryChooseColor(colorDialog, dialog, localDone, out selected))
                    {
                        localDone = selected;
                        updatePreview();
                    }
                };
                waitingButton.Click += delegate
                {
                    Color selected;
                    Color currentWaiting = localWaiting.HasValue ? localWaiting.Value : GetSkinPalette().InactiveLens;
                    if (TryChooseColor(colorDialog, dialog, currentWaiting, out selected))
                    {
                        localWaiting = selected;
                        updatePreview();
                    }
                };
                resetButton.Click += delegate
                {
                    localConfirm = DefaultConfirmLightColor;
                    localWorking = DefaultWorkingLightColor;
                    localDone = DefaultDoneLightColor;
                    localWaiting = null;
                    updatePreview();
                };
                okButton.Click += delegate
                {
                    if (HasLightColorChanges(localConfirm, localWorking, localDone, localWaiting))
                    {
                        confirmLightColor = localConfirm;
                        workingLightColor = localWorking;
                        doneLightColor = localDone;
                        waitingLightColor = localWaiting;
                        WriteSettings();
                        RenderLayeredWindow();
                    }
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };

                dialog.Controls.Add(previewBox);
                dialog.Controls.Add(confirmLabel);
                dialog.Controls.Add(confirmSwatch);
                dialog.Controls.Add(confirmButton);
                dialog.Controls.Add(workingLabel);
                dialog.Controls.Add(workingSwatch);
                dialog.Controls.Add(workingButton);
                dialog.Controls.Add(doneLabel);
                dialog.Controls.Add(doneSwatch);
                dialog.Controls.Add(doneButton);
                dialog.Controls.Add(waitingLabel);
                dialog.Controls.Add(waitingSwatch);
                dialog.Controls.Add(waitingButton);
                dialog.Controls.Add(resetButton);
                dialog.Controls.Add(okButton);
                dialog.Controls.Add(cancelButton);

                updatePreview();
                dialog.ShowDialog(this);
            }
        }

        private static void ConfigureColorRow(Label label, Panel swatch, Button button, string text, int x, int y)
        {
            label.Text = text;
            label.Location = new Point(x, y + 4);
            label.Size = new Size(76, 22);

            swatch.Location = new Point(x + 82, y);
            swatch.Size = new Size(34, 24);
            swatch.BorderStyle = BorderStyle.FixedSingle;

            button.Text = T.ChooseColor;
            button.Location = new Point(x + 126, y - 1);
            button.Size = new Size(92, 26);
        }

        private static bool TryChooseColor(ColorDialog dialog, IWin32Window owner, Color currentColor, out Color selectedColor)
        {
            dialog.Color = Color.FromArgb(255, currentColor.R, currentColor.G, currentColor.B);
            if (dialog.ShowDialog(owner) == DialogResult.OK)
            {
                selectedColor = dialog.Color;
                return true;
            }

            selectedColor = currentColor;
            return false;
        }

        private bool HasLightColorChanges(Color confirmColor, Color workingColor, Color doneColor, Color? waitingColor)
        {
            bool waitingChanged = waitingColor.HasValue != waitingLightColor.HasValue ||
                (waitingColor.HasValue && !waitingColor.Value.Equals(waitingLightColor.Value));
            return !confirmColor.Equals(confirmLightColor) ||
                !workingColor.Equals(workingLightColor) ||
                !doneColor.Equals(doneLightColor) ||
                waitingChanged;
        }

        private void ResetLightColorsToDefaults()
        {
            confirmLightColor = DefaultConfirmLightColor;
            workingLightColor = DefaultWorkingLightColor;
            doneLightColor = DefaultDoneLightColor;
            waitingLightColor = null;
        }

        private static Color ReadColor(string value, Color fallback)
        {
            Color color;
            return TryParseColor(value, out color) ? color : fallback;
        }

        private static Color? ReadOptionalColor(string value)
        {
            Color color;
            return TryParseColor(value, out color) ? (Color?)color : null;
        }

        private static bool TryParseColor(string value, out Color color)
        {
            color = Color.Empty;
            value = (value ?? String.Empty).Trim();
            if (value.StartsWith("#", StringComparison.Ordinal))
            {
                value = value.Substring(1);
            }

            if (value.Length != 6 && value.Length != 8)
            {
                return false;
            }

            uint parsed;
            if (!UInt32.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed))
            {
                return false;
            }

            if (value.Length == 6)
            {
                color = Color.FromArgb(255, (int)((parsed >> 16) & 0xFF), (int)((parsed >> 8) & 0xFF), (int)(parsed & 0xFF));
                return true;
            }

            color = Color.FromArgb((int)((parsed >> 24) & 0xFF), (int)((parsed >> 16) & 0xFF), (int)((parsed >> 8) & 0xFF), (int)(parsed & 0xFF));
            return true;
        }

        private static string ColorToHex(Color color)
        {
            if (color.A < 255)
            {
                return String.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
            }

            return String.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        private sealed class LightColorPreviewPanel : Control
        {
            private Color confirmColor;
            private Color workingColor;
            private Color doneColor;
            private Color inactiveColor;
            private SkinPalette palette = SkinPalette.Dark();

            public LightColorPreviewPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            }

            public void SetColors(Color confirm, Color working, Color done, Color inactive, SkinPalette skinPalette)
            {
                confirmColor = confirm;
                workingColor = working;
                doneColor = done;
                inactiveColor = inactive;
                palette = skinPalette;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(BackColor);

                using (GraphicsPath bodyPath = RoundPath(new RectangleF(13, 10, 158, 42), 11))
                using (var bodyBrush = new LinearGradientBrush(new RectangleF(13, 10, 158, 42), palette.BodyStart, palette.BodyEnd, 0f))
                using (var borderPen = new Pen(palette.Border, 1))
                {
                    if (palette.DrawBody)
                    {
                        e.Graphics.FillPath(bodyBrush, bodyPath);
                        e.Graphics.DrawPath(borderPen, bodyPath);
                    }
                }

                DrawLight(e.Graphics, 28, 18, 28, confirmColor, true, 0, palette, inactiveColor);
                DrawLight(e.Graphics, 78, 18, 28, workingColor, true, 0, palette, inactiveColor);
                DrawLight(e.Graphics, 128, 18, 28, doneColor, true, 0, palette, inactiveColor);

                DrawLight(e.Graphics, 28, 64, 20, confirmColor, false, 0, palette, inactiveColor);
                DrawLight(e.Graphics, 78, 64, 20, workingColor, false, 0, palette, inactiveColor);
                DrawLight(e.Graphics, 128, 64, 20, doneColor, false, 0, palette, inactiveColor);

                ControlPaint.DrawBorder(e.Graphics, ClientRectangle, SystemColors.ControlDark, ButtonBorderStyle.Solid);
            }
        }
    }
}
