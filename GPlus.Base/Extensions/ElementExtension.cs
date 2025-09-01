using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Drawing.Brushes;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Drawing.Drawing2D.Matrix;


namespace GPlus.Base.Extensions
{
    public static class ElementExtension
    {
        private const float Scale = 50;
        public static Parameter? GetParameter(this Element element, ElementId parameterId)
        {
            try
            {
#if V2023
            var parameterid = parameterId.IntegerValue;
#else
                var parameterid = parameterId.Value;
#endif
                if (parameterid < 0)
                    return element.get_Parameter((BuiltInParameter)parameterid);
                Element? paramElement = element.Document.GetElement(parameterId);
                if (paramElement == null) return null;
                if (paramElement is ParameterElement paramElementAsParamElement)
                    return element.get_Parameter(paramElementAsParamElement.GetDefinition());
                else if (paramElement is SharedParameterElement sharedParamElement)
                    return element.get_Parameter(sharedParamElement.GetDefinition());
                else return null;
            }
            catch
            { return null; }
        }

        public static ImageSource? GetFillPatternImage(this FillPatternElement fillPattern)
        {
            var pattern = fillPattern.GetFillPattern();
            return CreateFillPatternImage(pattern);
        }
        private static ImageSource? CreateFillPatternImage(FillPattern fillPattern)
        {
            if (fillPattern == null) return null;

            int width = 100;
            int height = 30;

            var image= new Bitmap(width, height);

            using (var g = Graphics.FromImage(image))
            {
                if(fillPattern.IsSolidFill)
                    g.FillRectangle(Brushes.Black, new Rectangle(0, 0, width, height));

                else
                    DrawFillPattern(fillPattern, g, width, height);
            }
            return BitmapToImageSource(image);
        }
        private static ImageSource BitmapToImageSource(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memory;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // para usar cross-thread se necessário
                return bitmapImage;
            }
        }
        private static void DrawFillPattern(FillPattern fillPattern,Graphics g, int width, int height)
        {
            float matrixScale;

            if (fillPattern == null)
                return;

            if (fillPattern.Target == FillPatternTarget.Model)
                matrixScale = Scale;
            else
                matrixScale = Scale * 10;

            try
            {
                var rect = new Rectangle(0, 0,width, height);

                var centerX = (rect.Left + rect.Left+ rect.Width) / 2;

                var centerY = (rect.Top + rect.Top+ rect.Height) / 2;

                g.TranslateTransform(centerX, centerY);

                var rectF = new Rectangle(-1, -1, 2, 2);

                g.FillRectangle(Brushes.Blue, rectF);
                g.ResetTransform();

                var fillGrids = fillPattern.GetFillGrids();

                foreach (var fillGrid in fillGrids)
                {
                    var degreeAngle = (float)RadianToGradus(fillGrid.Angle);

                    var pen = new System.Drawing.Pen(System.Drawing.Color.Black)
                    {
                        Width = 1f / matrixScale
                    };

                    float dashLength = 1;

                    var segments = fillGrid.GetSegments();

                    if (segments.Count > 0)
                    {
                        pen.DashPattern = segments
                            .Select(Convert.ToSingle)
                            .ToArray();

                        dashLength = pen.DashPattern.Sum();
                    }

                    g.ResetTransform();

                    var rotateMatrix = new Matrix();
                    rotateMatrix.Rotate(degreeAngle);

                    var matrix = new Matrix(1, 0, 0, -1,centerX, centerY);

                    matrix.Scale(matrixScale, matrixScale);

                    matrix.Translate((float)fillGrid.Origin.U,(float)fillGrid.Origin.V);

                    var backMatrix = matrix.Clone();
                    backMatrix.Multiply(rotateMatrix);
                    matrix.Multiply(rotateMatrix);

                    bool first = true;
                    for (int i = 20; i > 0; i--)
                    {
                        if (!first)
                        {
                            matrix.Translate((float)fillGrid.Shift,(float)fillGrid.Offset);

                            backMatrix.Translate((float)fillGrid.Shift, -(float)fillGrid.Offset);
                        }
                        else
                        {
                            first = false;
                        }

                        var offset = (-10) * dashLength;
                        matrix.Translate(offset, 0);
                        backMatrix.Translate(offset, 0);

                        g.Transform = matrix;

                        g.DrawLine(pen, new PointF(0, 0), new PointF(200, 0));

                        g.Transform = backMatrix;

                        g.DrawLine(pen, new PointF(0, 0),new PointF(200, 0));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }
        //private static ImageSource CreateFillPatternImage(FillPattern pattern)
        //{
        //    double width = 100;
        //    double height = 30;

        //    DrawingVisual visual = new DrawingVisual();
        //    using (var context = visual.RenderOpen())
        //    {
        //        if (pattern.IsSolidFill)
        //        {
        //            context.DrawRectangle(Brushes.Black, null, new Rect(0, 0, width, height));
        //        }
        //        else
        //        {
        //            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
        //            try
        //            {
        //                DrawFillPattern(context, width, height, pattern);
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine(ex);
        //            }
        //        }
        //    }

        //    var rtb = new RenderTargetBitmap(
        //        (int)Math.Ceiling(width),
        //        (int)Math.Ceiling(height),
        //        96, 96, PixelFormats.Pbgra32);

        //    rtb.Render(visual);
        //    return rtb;
        //}
        //private static void DrawFillPattern(DrawingContext context, double width, double height, FillPattern pattern)
        //{
        //    if (pattern == null) return;

        //    double matrixScale = pattern.Target == FillPatternTarget.Model ? 50 : 50 * 10;
        //    var center = new Point(width / 2, height / 2);

        //    var fillGrids = pattern.GetFillGrids();
        //    if (!fillGrids.Any())
        //        return;

        //    foreach (var fillGrid in fillGrids)
        //    {
        //        double angleDeg = RadianToGradus(fillGrid.Angle);
        //        var dashArray = fillGrid.GetSegments().Select(s => Math.Max(0.1, s));

        //        double penThickness = Math.Max(0.5, 1.0 / matrixScale);
        //        Pen pen = new Pen(Brushes.Black, penThickness)
        //        {
        //            DashStyle = new DashStyle(dashArray, 0)
        //        };

        //        TransformGroup transform = new TransformGroup();
        //        transform.Children.Add(new ScaleTransform(matrixScale, -matrixScale)); // mirror Y
        //        transform.Children.Add(new TranslateTransform(fillGrid.Origin.U, fillGrid.Origin.V));
        //        transform.Children.Add(new RotateTransform(angleDeg));
        //        transform.Children.Add(new TranslateTransform(center.X, center.Y));

        //        // Generate lines (simplified)
        //        for (int i = -10; i < 10; ++i)
        //        {
        //            var offsetX = i * fillGrid.Shift;
        //            var offsetY = i * fillGrid.Offset;

        //            Point p1 = transform.Transform(new Point(offsetX, 0));
        //            Point p2 = transform.Transform(new Point(offsetX + 100, 0));
        //            context.DrawLine(pen, p1, p2);
        //        }
        //    }
        //}
        private static double RadianToGradus(double radian) => radian * 180.0 / Math.PI;
    }
}
