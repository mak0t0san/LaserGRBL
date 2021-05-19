//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify  it under the terms of the GPLv3 General Public License as published by  the Free Software Foundation; either version 3 of the License, or (at  your option) any later version.
// This program is distributed in the hope that it will be useful, but  WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GPLv3  General Public License for more details.
// You should have received a copy of the GPLv3 General Public License  along with this program; if not, write to the Free Software  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307,  USA. using System;

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Threading;

namespace LaserGRBL.RasterConverter
{
    public partial class RasterToLaserForm : Form
    {
        private readonly GrblCore _core;
        private ImageProcessor _imageProcessor;
        private bool _preventClose;
        private readonly bool _supportPwm = Settings.GetObject("Support Hardware PWM", true);

        private RasterToLaserForm(GrblCore core, string filename, bool append)
        {
            InitializeComponent();
            _core = core;

            UDQuality.Maximum = UDFillingQuality.Maximum = GetMaxQuality();

            BackColor = ColorScheme.FormBackColor;
            GbCenterlineOptions.ForeColor = GbConversionTool.ForeColor = GbLineToLineOptions.ForeColor =
                GbParameters.ForeColor = GbVectorizeOptions.ForeColor = ForeColor = ColorScheme.FormForeColor;
            BtnCancel.BackColor = BtnCreate.BackColor = ColorScheme.FormButtonsColor;

            _imageProcessor = new ImageProcessor(core, filename, GetImageSize(), append);
            //PbOriginal.Image = IP.Original;
            ImageProcessor.PreviewReady += OnPreviewReady;
            ImageProcessor.PreviewBegin += OnPreviewBegin;
            ImageProcessor.GenerationComplete += OnGenerationComplete;

            LblGrayscale.Visible = CbMode.Visible = !_imageProcessor.IsGrayScale;

            CbResize.SuspendLayout();
            CbResize.AddItem(InterpolationMode.HighQualityBicubic);
            CbResize.AddItem(InterpolationMode.NearestNeighbor);
            CbResize.ResumeLayout();

            CbDither.SuspendLayout();
            foreach (ImageTransform.DitheringMode formula in Enum.GetValues(typeof(ImageTransform.DitheringMode)))
            {
                CbDither.Items.Add(formula);
            }

            CbDither.SelectedIndex = 0;
            CbDither.ResumeLayout();
            CbDither.SuspendLayout();

            CbMode.SuspendLayout();
            foreach (ImageTransform.Formula formula in Enum.GetValues(typeof(ImageTransform.Formula)))
            {
                CbMode.AddItem(formula);
            }

            CbMode.SelectedIndex = 0;
            CbMode.ResumeLayout();

            CbDirections.SuspendLayout();
            foreach (ImageProcessor.Direction direction in Enum.GetValues(typeof(ImageProcessor.Direction)))
                if (GrblFile.RasterFilling(direction))
                    CbDirections.AddItem(direction, true);
            CbDirections.SelectedIndex = 0;
            CbDirections.ResumeLayout();

            CbFillingDirection.SuspendLayout();
            CbFillingDirection.AddItem(ImageProcessor.Direction.None);
            foreach (ImageProcessor.Direction direction in Enum.GetValues(typeof(ImageProcessor.Direction)))
            {
                if (GrblFile.VectorFilling(direction))
                    CbFillingDirection.AddItem(direction);
            }

            foreach (ImageProcessor.Direction direction in Enum.GetValues(typeof(ImageProcessor.Direction)))
            {
                if (GrblFile.RasterFilling(direction))
                    CbFillingDirection.AddItem(direction);
            }

            CbFillingDirection.SelectedIndex = 0;
            CbFillingDirection.ResumeLayout();

            RbLineToLineTracing.Visible = _supportPwm;

            LoadSettings();
            RefreshVE();
        }

        private decimal GetMaxQuality()
        {
            return Settings.GetObject("Raster Hi-Res", false) ? 50 : 20;
        }

        private Size GetImageSize()
        {
            return new Size(PbConverted.Size.Width - 20, PbConverted.Size.Height - 20);
        }

        private void OnPreviewBegin()
        {
            _preventClose = true;

            if (InvokeRequired)
            {
                Invoke(new ImageProcessor.PreviewBeginDlg(OnPreviewBegin));
            }
            else
            {
                WT.Enabled = true;
                BtnCreate.Enabled = false;
            }
        }

        private void OnPreviewReady(Image img)
        {
            if (InvokeRequired)
            {
                Invoke(new ImageProcessor.PreviewReadyDlg(OnPreviewReady), img);
            }
            else
            {
                Image old_orig = PbOriginal.Image;
                Image old_conv = PbConverted.Image;
                PbOriginal.Image = CreatePaper(_imageProcessor.Original);
                PbConverted.Image = CreatePaper(img);


                old_conv?.Dispose();

                old_orig?.Dispose();

                WT.Enabled = false;
                WB.Visible = false;
                WB.Running = false;
                BtnCreate.Enabled = true;
                _preventClose = false;
            }
        }

        private static Image CreatePaper(Image img)
        {
            Image newimage = new Bitmap(img.Width + 6, img.Height + 6);
            using (Graphics g = Graphics.FromImage(newimage))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(Brushes.Gray, 6, 6, img.Width + 2, img.Height + 2); //ombra
                g.FillRectangle(Brushes.White, 0, 0, img.Width + 2, img.Height + 2); //pagina
                g.DrawRectangle(Pens.LightGray, 0, 0, img.Width + 1, img.Height + 1); //bordo
                g.DrawImage(img, 1, 1); //disegno
            }

            return newimage;
        }

        private void WTTick(object sender, EventArgs e)
        {
            WT.Enabled = false;
            WB.Visible = true;
            WB.Running = true;
        }

        internal static void CreateAndShowDialog(GrblCore core, string filename, Form parent, bool append)
        {
            using (RasterToLaserForm f = new RasterToLaserForm(core, filename, append))
                f.ShowDialog(parent);
        }

        private void GoodInput(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void BtnCreateClick(object sender, EventArgs e)
        {
            if (_imageProcessor.SelectedTool == ImageProcessor.Tool.Vectorize &&
                GrblFile.TimeConsumingFilling(_imageProcessor.FillingDirection) && _imageProcessor.FillingQuality > 2
                && System.Windows.Forms.MessageBox.Show(this,
                    $"Using {GrblCore.TranslateEnum(_imageProcessor.FillingDirection)} with quality > 2 line/mm could be very time consuming with big image. Continue?",
                    "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) !=
                DialogResult.OK)
                return;

            using (ConvertSizeAndOptionForm f = new ConvertSizeAndOptionForm(_core))
            {
                f.ShowDialog(this, _imageProcessor);
                if (f.DialogResult == DialogResult.OK)
                {
                    _preventClose = true;
                    Cursor = Cursors.WaitCursor;
                    SuspendLayout();
                    TCOriginalPreview.SelectedIndex = 0;
                    FlipControl.Enabled = false;
                    BtnCreate.Enabled = false;
                    WB.Visible = true;
                    WB.Running = true;
                    FormBorderStyle = FormBorderStyle.FixedSingle;
                    TlpLeft.Enabled = false;
                    MaximizeBox = false;
                    ResumeLayout();

                    StoreSettings();

                    _imageProcessor.GenerateGCode(); //processo asincrono che ritorna con l'evento "OnGenerationComplete"
                }
            }
        }


        private void OnGenerationComplete(Exception ex)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ImageProcessor.GenerationCompleteDlg(OnGenerationComplete), ex);
            }
            else
            {
                try
                {
                    if (_imageProcessor != null)
                    {
                        if (_imageProcessor.SelectedTool == ImageProcessor.Tool.Dithering)
                            _core.UsageCounters.Dithering++;
                        else if (_imageProcessor.SelectedTool == ImageProcessor.Tool.Line2Line)
                            _core.UsageCounters.Line2Line++;
                        else if (_imageProcessor.SelectedTool == ImageProcessor.Tool.Vectorize)
                            _core.UsageCounters.Vectorization++;
                        else if (_imageProcessor.SelectedTool == ImageProcessor.Tool.Centerline)
                            _core.UsageCounters.Centerline++;
                        else if (_imageProcessor.SelectedTool == ImageProcessor.Tool.NoProcessing)
                            _core.UsageCounters.Passthrough++;

                        Cursor = Cursors.Default;

                        if (ex != null && !(ex is ThreadAbortException))
                            MessageBox.Show(ex.Message);

                        _preventClose = false;
                        WT.Enabled = false;

                        ImageProcessor P = _imageProcessor;
                        _imageProcessor = null;
                        P?.Dispose();
                    }
                }
                finally
                {
                    Close();
                }
            }
        }


        private void StoreSettings()
        {
            Settings.SetObject("GrayScaleConversion.RasterConversionTool",
                RbLineToLineTracing.Checked ? ImageProcessor.Tool.Line2Line :
                RbDithering.Checked ? ImageProcessor.Tool.Dithering :
                RbCenterline.Checked ? ImageProcessor.Tool.Centerline : ImageProcessor.Tool.Vectorize);

            Settings.SetObject("GrayScaleConversion.Line2LineOptions.Direction",
                (ImageProcessor.Direction) CbDirections.SelectedItem);
            Settings.SetObject("GrayScaleConversion.Line2LineOptions.Quality", UDQuality.Value);
            Settings.SetObject("GrayScaleConversion.Line2LineOptions.Preview", CbLinePreview.Checked);

            Settings.SetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Enabled", CbSpotRemoval.Checked);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Value", UDSpotRemoval.Value);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.Smooting.Enabled", CbSmoothing.Checked);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.Smooting.Value", UDSmoothing.Value);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.Optimize.Enabled", CbOptimize.Checked);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.UseAdaptiveQuality.Enabled",
                CbAdaptiveQuality.Checked);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.Optimize.Value", UDOptimize.Value);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.DownSample.Enabled", CbDownSample.Checked);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.DownSample.Value", UDDownSample.Value);
            //			Settings.SetObject("GrayScaleConversion.VectorizeOptions.ShowDots.Enabled", CbShowDots.Checked);
            //			Settings.SetObject("GrayScaleConversion.VectorizeOptions.ShowImage.Enabled", CbShowImage.Checked);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.FillingDirection",
                (ImageProcessor.Direction) CbFillingDirection.SelectedItem);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.FillingQuality", UDFillingQuality.Value);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.OptimizeFast.Enabled", CbOptimizeFast.Checked);

            Settings.SetObject("GrayScaleConversion.DitheringOptions.DitheringMode",
                (ImageTransform.DitheringMode) CbDither.SelectedItem);

            Settings.SetObject("GrayScaleConversion.Parameters.Interpolation",
                (InterpolationMode) CbResize.SelectedItem);
            Settings.SetObject("GrayScaleConversion.Parameters.Mode", (ImageTransform.Formula) CbMode.SelectedItem);
            Settings.SetObject("GrayScaleConversion.Parameters.R", TBRed.Value);
            Settings.SetObject("GrayScaleConversion.Parameters.G", TBGreen.Value);
            Settings.SetObject("GrayScaleConversion.Parameters.B", TBBlue.Value);
            Settings.SetObject("GrayScaleConversion.Parameters.Brightness", TbBright.Value);
            Settings.SetObject("GrayScaleConversion.Parameters.Contrast", TbContrast.Value);
            Settings.SetObject("GrayScaleConversion.Parameters.Threshold.Enabled", CbThreshold.Checked);
            Settings.SetObject("GrayScaleConversion.Parameters.Threshold.Value", TbThreshold.Value);
            Settings.SetObject("GrayScaleConversion.Parameters.WhiteClip", TBWhiteClip.Value);

            Settings.SetObject("GrayScaleConversion.VectorizeOptions.BorderSpeed", _imageProcessor.BorderSpeed);
            Settings.SetObject("GrayScaleConversion.Gcode.Speed.Mark", _imageProcessor.MarkSpeed);

            Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.LaserOn", _imageProcessor.LaserOn);
            Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.LaserOff", _imageProcessor.LaserOff);
            Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.PowerMin", _imageProcessor.MinPower);
            Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.PowerMax", _imageProcessor.MaxPower);

            Settings.SetObject("GrayScaleConversion.Gcode.Offset.X", _imageProcessor.TargetOffset.X);
            Settings.SetObject("GrayScaleConversion.Gcode.Offset.Y", _imageProcessor.TargetOffset.Y);
            Settings.SetObject("GrayScaleConversion.Gcode.BiggestDimension",
                Math.Max(_imageProcessor.TargetSize.Width, _imageProcessor.TargetSize.Height));

            Settings.SetObject("GrayScaleConversion.VectorizeOptions.LineThreshold.Enabled", _imageProcessor.UseLineThreshold);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.LineThreshold.Value", _imageProcessor.LineThreshold);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.CornerThreshold.Enabled", _imageProcessor.UseCornerThreshold);
            Settings.SetObject("GrayScaleConversion.VectorizeOptions.CornerThreshold.Value", _imageProcessor.CornerThreshold);


            Settings.Save(); // Saves settings in application configuration file
        }

        private void LoadSettings()
        {
            if ((_imageProcessor.SelectedTool =
                    Settings.GetObject("GrayScaleConversion.RasterConversionTool", ImageProcessor.Tool.Line2Line)) ==
                ImageProcessor.Tool.Line2Line)
                RbLineToLineTracing.Checked = true;
            else if ((_imageProcessor.SelectedTool =
                         Settings.GetObject("GrayScaleConversion.RasterConversionTool",
                             ImageProcessor.Tool.Line2Line)) ==
                     ImageProcessor.Tool.Dithering)
                RbDithering.Checked = true;
            else if ((_imageProcessor.SelectedTool =
                         Settings.GetObject("GrayScaleConversion.RasterConversionTool",
                             ImageProcessor.Tool.Line2Line)) ==
                     ImageProcessor.Tool.Centerline)
                RbCenterline.Checked = true;
            else
                RbVectorize.Checked = true;

            CbDirections.SelectedItem = _imageProcessor.LineDirection =
                Settings.GetObject("GrayScaleConversion.Line2LineOptions.Direction",
                    ImageProcessor.Direction.Horizontal);
            UDQuality.Value = _imageProcessor.Quality = Math.Min(UDQuality.Maximum,
                Settings.GetObject("GrayScaleConversion.Line2LineOptions.Quality", 3.0m));
            CbLinePreview.Checked =
                _imageProcessor.LinePreview = Settings.GetObject("GrayScaleConversion.Line2LineOptions.Preview", false);

            CbSpotRemoval.Checked = _imageProcessor.UseSpotRemoval =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Enabled", false);
            UDSpotRemoval.Value = _imageProcessor.SpotRemoval =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Value", 2.0m);
            CbSmoothing.Checked = _imageProcessor.UseSmoothing =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.Smooting.Enabled", false);
            UDSmoothing.Value = _imageProcessor.Smoothing =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.Smooting.Value", 1.0m);
            CbOptimize.Checked = _imageProcessor.UseOptimize =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.Optimize.Enabled", false);
            CbAdaptiveQuality.Checked = _imageProcessor.UseAdaptiveQuality =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.UseAdaptiveQuality.Enabled", false);
            UDOptimize.Value = _imageProcessor.Optimize =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.Optimize.Value", 0.2m);
            CbDownSample.Checked = _imageProcessor.UseDownSampling =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.DownSample.Enabled", false);
            UDDownSample.Value = _imageProcessor.DownSampling =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.DownSample.Value", 2.0m);
            CbOptimizeFast.Checked = _imageProcessor.OptimizeFast =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.OptimizeFast.Enabled", false);

            //CbShowDots.Checked = IP.ShowDots = Settings.GetObject("GrayScaleConversion.VectorizeOptions.ShowDots.Enabled", false);
            //CbShowImage.Checked = IP.ShowImage = Settings.GetObject("GrayScaleConversion.VectorizeOptions.ShowImage.Enabled", true);
            CbFillingDirection.SelectedItem = _imageProcessor.FillingDirection =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.FillingDirection",
                    ImageProcessor.Direction.None);
            UDFillingQuality.Value = _imageProcessor.FillingQuality = Math.Min(UDFillingQuality.Maximum,
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.FillingQuality", 3.0m));

            CbResize.SelectedItem = _imageProcessor.Interpolation =
                Settings.GetObject("GrayScaleConversion.Parameters.Interpolation",
                    InterpolationMode.HighQualityBicubic);
            CbMode.SelectedItem = _imageProcessor.Formula = Settings.GetObject("GrayScaleConversion.Parameters.Mode",
                ImageTransform.Formula.SimpleAverage);
            TBRed.Value = _imageProcessor.Red = Settings.GetObject("GrayScaleConversion.Parameters.R", 100);
            TBGreen.Value = _imageProcessor.Green = Settings.GetObject("GrayScaleConversion.Parameters.G", 100);
            TBBlue.Value = _imageProcessor.Blue = Settings.GetObject("GrayScaleConversion.Parameters.B", 100);
            TbBright.Value = _imageProcessor.Brightness = Settings.GetObject("GrayScaleConversion.Parameters.Brightness", 100);
            TbContrast.Value = _imageProcessor.Contrast = Settings.GetObject("GrayScaleConversion.Parameters.Contrast", 100);
            CbThreshold.Checked = _imageProcessor.UseThreshold =
                Settings.GetObject("GrayScaleConversion.Parameters.Threshold.Enabled", false);
            TbThreshold.Value = _imageProcessor.Threshold = Settings.GetObject("GrayScaleConversion.Parameters.Threshold.Value", 50);
            TBWhiteClip.Value = _imageProcessor.WhiteClip = Settings.GetObject("GrayScaleConversion.Parameters.WhiteClip", 5);

            CbDither.SelectedItem = Settings.GetObject("GrayScaleConversion.DitheringOptions.DitheringMode",
                ImageTransform.DitheringMode.FloydSteinberg);

            CbLineThreshold.Checked = _imageProcessor.UseLineThreshold =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.LineThreshold.Enabled", true);
            TBLineThreshold.Value = _imageProcessor.LineThreshold =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.LineThreshold.Value", 10);

            CbCornerThreshold.Checked = _imageProcessor.UseCornerThreshold =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.CornerThreshold.Enabled", true);
            TBCornerThreshold.Value = _imageProcessor.CornerThreshold =
                Settings.GetObject("GrayScaleConversion.VectorizeOptions.CornerThreshold.Value", 110);

            if (RbLineToLineTracing.Checked && !_supportPwm)
                RbDithering.Checked = true;
        }

        private void OnRGBCBDoubleClick(object sender, EventArgs e)
        {
            ((UserControls.ColorSlider) sender).Value = 100;
        }

        private void OnThresholdDoubleClick(object sender, EventArgs e)
        {
            ((UserControls.ColorSlider) sender).Value = 50;
        }

        private void CbMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                _imageProcessor.Formula = (ImageTransform.Formula) CbMode.SelectedItem;

                SuspendLayout();
                TBRed.Visible = TBGreen.Visible =
                    TBBlue.Visible = (_imageProcessor.Formula == ImageTransform.Formula.Custom && !_imageProcessor.IsGrayScale);
                LblRed.Visible = LblGreen.Visible =
                    LblBlue.Visible = (_imageProcessor.Formula == ImageTransform.Formula.Custom && !_imageProcessor.IsGrayScale);
                ResumeLayout();
            }
        }

        private void TBRed_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.Red = TBRed.Value;
        }

        private void TBGreen_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.Green = TBGreen.Value;
        }

        private void TBBlue_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.Blue = TBBlue.Value;
        }

        private void TbBright_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                _imageProcessor.Brightness = TbBright.Value;
            }
        }

        private void TbContrast_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.Contrast = TbContrast.Value;
        }

        private void CbThreshold_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                _imageProcessor.UseThreshold = CbThreshold.Checked;
                RefreshVE();
            }
        }

        private void RefreshVE()
        {
            GbParameters.Enabled = !RbNoProcessing.Checked;
            GbVectorizeOptions.Visible = RbVectorize.Checked;
            GbCenterlineOptions.Visible = RbCenterline.Checked;
            GbLineToLineOptions.Visible = RbLineToLineTracing.Checked || RbDithering.Checked;
            GbPassthrough.Visible = RbNoProcessing.Checked;
            GbLineToLineOptions.Text =
                RbLineToLineTracing.Checked ? Strings.Line2LineOptions : Strings.DitheringOptions;

            CbThreshold.Visible = !RbDithering.Checked;
            TbThreshold.Visible = !RbDithering.Checked && CbThreshold.Checked;

            LblDitherMode.Visible = CbDither.Visible = RbDithering.Checked;
        }

        private void TbThreshold_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.Threshold = TbThreshold.Value;
        }

        private void RbLineToLineTracing_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                if (RbLineToLineTracing.Checked)
                    _imageProcessor.SelectedTool = ImageProcessor.Tool.Line2Line;
                RefreshVE();
            }
        }

        private void RbNoProcessing_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                if (RbNoProcessing.Checked)
                    _imageProcessor.SelectedTool = ImageProcessor.Tool.NoProcessing;
                RefreshVE();
            }
        }

        private void RbCenterline_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                if (RbCenterline.Checked)
                    _imageProcessor.SelectedTool = ImageProcessor.Tool.Centerline;
                RefreshVE();
            }
        }

        private void RbVectorize_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                if (RbVectorize.Checked)
                    _imageProcessor.SelectedTool = ImageProcessor.Tool.Vectorize;
                RefreshVE();
            }
        }

        private void UDQuality_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.Quality = UDQuality.Value;
        }

        private void CbLinePreview_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.LinePreview = CbLinePreview.Checked;
        }

        private void UDSpotRemoval_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.SpotRemoval = (int) UDSpotRemoval.Value;
        }

        private void CbSpotRemoval_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
                _imageProcessor.UseSpotRemoval = CbSpotRemoval.Checked;
            UDSpotRemoval.Enabled = CbSpotRemoval.Checked;
        }

        private void UDSmoothing_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.Smoothing = UDSmoothing.Value;
        }

        private void CbSmoothing_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.UseSmoothing = CbSmoothing.Checked;
            UDSmoothing.Enabled = CbSmoothing.Checked;
        }

        private void UDOptimize_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.Optimize = UDOptimize.Value;
        }

        private void CbOptimize_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.UseOptimize = CbOptimize.Checked;
            UDOptimize.Enabled = CbOptimize.Checked;
        }

        private void RasterToLaserForm_Load(object sender, EventArgs e)
        {
            _imageProcessor?.Resume();
        }

        private void RasterToLaserFormFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_preventClose)
            {
                e.Cancel = true;
            }
            else
            {
                ImageProcessor.PreviewReady -= OnPreviewReady;
                ImageProcessor.PreviewBegin -= OnPreviewBegin;
                ImageProcessor.GenerationComplete -= OnGenerationComplete;
                _imageProcessor?.Dispose();
            }
        }

        private void CbDirectionsSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.LineDirection = (ImageProcessor.Direction) CbDirections.SelectedItem;
        }

        private void CbResizeSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                _imageProcessor.Interpolation = (InterpolationMode) CbResize.SelectedItem;
                //PbOriginal.Image = IP.Original;
            }
        }

        private void BtRotateCWClick(object sender, EventArgs e)
        {
            _imageProcessor?.RotateCW();
            //PbOriginal.Image = IP.Original;
        }

        private void BtRotateCCWClick(object sender, EventArgs e)
        {
            _imageProcessor?.RotateCCW();
            //PbOriginal.Image = IP.Original;
        }

        private void BtFlipHClick(object sender, EventArgs e)
        {
            _imageProcessor?.FlipH();
            //PbOriginal.Image = IP.Original;
        }

        private void BtFlipVClick(object sender, EventArgs e)
        {
            _imageProcessor?.FlipV();
            //PbOriginal.Image = IP.Original;
        }

        private void BtnRevertClick(object sender, EventArgs e)
        {
            _imageProcessor?.Revert();
            //PbOriginal.Image = IP.Original;
        }

        private void CbFillingDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                _imageProcessor.FillingDirection = (ImageProcessor.Direction) CbFillingDirection.SelectedItem;
                BtnFillingQualityInfo.Visible = LblFillingLineLbl.Visible = LblFillingQuality.Visible =
                    UDFillingQuality.Visible = ((ImageProcessor.Direction) CbFillingDirection.SelectedItem !=
                                                ImageProcessor.Direction.None);
            }
        }

        private void UDFillingQuality_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
                _imageProcessor.FillingQuality = UDFillingQuality.Value;
        }


        private bool _isDrag = false;
        private Rectangle imageRectangle;
        private Rectangle theRectangle = new Rectangle(new Point(0, 0), new Size(0, 0));
        private Point sP;
        private Point eP;

        private void PbConvertedMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _cropping)
            {
                int left = (PbConverted.Width - PbConverted.Image.Width) / 2;
                int top = (PbConverted.Height - PbConverted.Image.Height) / 2;
                int right = PbConverted.Width - left;
                int bottom = PbConverted.Height - top;

                imageRectangle = new Rectangle(left, top, PbConverted.Image.Width, PbConverted.Image.Height);

                if ((e.X >= left && e.Y >= top) && (e.X <= right && e.Y <= bottom))
                {
                    _isDrag = true;
                    sP = e.Location;
                    eP = e.Location;
                }
            }
        }

        private void PbConvertedMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrag)
            {
                //erase old rectangle
                ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);

                eP = e.Location;

                //limit eP to image rectangle
                int left = (PbConverted.Width - PbConverted.Image.Width) / 2;
                int top = (PbConverted.Height - PbConverted.Image.Height) / 2;
                int right = PbConverted.Width - left;
                int bottom = PbConverted.Height - top;
                eP.X = Math.Min(Math.Max(eP.X, left), right);
                eP.Y = Math.Min(Math.Max(eP.Y, top), bottom);

                theRectangle = new Rectangle(PbConverted.PointToScreen(sP), new Size(eP.X - sP.X, eP.Y - sP.Y));

                // Draw the new rectangle by calling DrawReversibleFrame
                ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);
            }
        }

        private void PbConvertedMouseUp(object sender, MouseEventArgs e)
        {
            // If the MouseUp event occurs, the user is not dragging.
            if (_isDrag)
            {
                _isDrag = false;

                //erase old rectangle
                ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);


                int left = (PbConverted.Width - PbConverted.Image.Width) / 2;
                int top = (PbConverted.Height - PbConverted.Image.Height) / 2;

                Rectangle CropRect = new Rectangle(Math.Min(sP.X, eP.X) - left,
                    Math.Min(sP.Y, eP.Y) - top,
                    Math.Abs(eP.X - sP.X),
                    Math.Abs(eP.Y - sP.Y));

                //Rectangle CropRect = new Rectangle(p.X-left, p.Y-top, orientedRect.Width, orientedRect.Height);

                _imageProcessor.CropImage(CropRect, PbConverted.Image.Size);

                //PbOriginal.Image = IP.Original;

                // Reset the rectangle.
                theRectangle = new Rectangle(0, 0, 0, 0);
                _cropping = false;
                Cursor.Clip = new Rectangle();
                UpdateCropping();
            }
        }

        private bool _cropping;

        private void BtnCropClick(object sender, EventArgs e)
        {
            _cropping = !_cropping;
            UpdateCropping();
        }

        private void UpdateCropping()
        {
            BtnCrop.BackColor = _cropping ? Color.Orange : DefaultBackColor;
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            try
            {
                ImageProcessor imageProcessor = _imageProcessor;
                _imageProcessor = null;
                imageProcessor?.Dispose();
            }
            finally
            {
                Close();
            }
        }

        private void RbDithering_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                if (RbDithering.Checked)
                    _imageProcessor.SelectedTool = ImageProcessor.Tool.Dithering;
                RefreshVE();
            }
        }

        private void CbDownSample_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                _imageProcessor.UseDownSampling = CbDownSample.Checked;
                UDDownSample.Enabled = CbDownSample.Checked;
            }
        }

        private void UDDownSample_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
                _imageProcessor.DownSampling = UDDownSample.Value;
        }

        private void CbOptimizeFast_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null)
            {
                _imageProcessor.OptimizeFast = CbOptimizeFast.Checked;
            }
        }

        private void PbConverted_Resize(object sender, EventArgs e)
        {
            try
            {
                _imageProcessor?.FormResize(GetImageSize());
            }
            catch (System.ArgumentException ex)
            {
                //Catching this exception https://github.com/arkypita/LaserGRBL/issues/1288
            }
        }

        private void CbDither_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.DitheringMode = (ImageTransform.DitheringMode) CbDither.SelectedItem;
        }

        private void BtnQualityInfo_Click(object sender, EventArgs e)
        {
            UDQuality.Value = Math.Min(UDQuality.Maximum,
                (decimal) ResolutionHelperForm.CreateAndShowDialog(this, _core, (double) UDQuality.Value));
            //Tools.Utils.OpenLink(@"https://lasergrbl.com/usage/raster-image-import/setting-reliable-resolution/");
        }

        private void BtnFillingQualityInfo_Click(object sender, EventArgs e)
        {
            UDFillingQuality.Value = Math.Min(UDFillingQuality.Maximum,
                (decimal) ResolutionHelperForm.CreateAndShowDialog(this, _core, (double) UDFillingQuality.Value));
            //Tools.Utils.OpenLink(@"https://lasergrbl.com/usage/raster-image-import/setting-reliable-resolution/");
        }

        private void TBWhiteClip_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.WhiteClip = TBWhiteClip.Value;
        }

        private void TBWhiteClip_MouseDown(object sender, MouseEventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.Demo = true;
        }

        private void TBWhiteClip_MouseUp(object sender, MouseEventArgs e)
        {
            if (_imageProcessor != null)
            {
                _imageProcessor.Demo = false;
            }
        }

        private void BtnReverse_Click(object sender, EventArgs e)
        {
            _imageProcessor?.Invert();
            //PbOriginal.Image = IP.Original;
        }

        private void CbUseLineThreshold_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.UseLineThreshold = CbLineThreshold.Checked;
            TBLineThreshold.Enabled = CbLineThreshold.Checked;
        }

        private void CbCornerThreshold_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.UseCornerThreshold = CbCornerThreshold.Checked;
            TBCornerThreshold.Enabled = CbCornerThreshold.Checked;
        }

        private void TBLineThreshold_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.LineThreshold = (int) TBLineThreshold.Value;
        }

        private void TBCornerThreshold_ValueChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.CornerThreshold = (int) TBCornerThreshold.Value;
        }

        private void TBCornerThreshold_DoubleClick(object sender, EventArgs e)
        {
            TBCornerThreshold.Value = 110;
        }

        private void TBLineThreshold_DoubleClick(object sender, EventArgs e)
        {
            TBLineThreshold.Value = 10;
        }

        private void CbAdaptiveQuality_CheckedChanged(object sender, EventArgs e)
        {
            if (_imageProcessor != null) _imageProcessor.UseAdaptiveQuality = CbAdaptiveQuality.Checked;
        }

        private void BtnAdaptiveQualityInfo_Click(object sender, EventArgs e)
        {
            Tools.Utils.OpenLink(
                @"https://lasergrbl.com/usage/raster-image-import/vectorization-tool/#adaptive-quality");
        }

        private void BtnAutoTrim_Click(object sender, EventArgs e)
        {
            _imageProcessor?.AutoTrim();
        }

        private void RbCenterline_Click(object sender, EventArgs e)
        {
            if (!Tools.OSHelper.Is64BitProcess)
            {
                MessageBox.Show(Strings.WarnCenterline64bit, Strings.WarnMessageBoxHeader, MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                //RbVectorize.Checked = true;
            }
        }

        private void RbLineToLineTracing_Click(object sender, EventArgs e)
        {
            if (!_supportPwm)
            {
                MessageBox.Show(Strings.WarnLine2LinePWM, Strings.WarnMessageBoxHeader, MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                //RbDithering.Checked = true;
            }
        }
    }
}