using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApplication1
{// Класс, управляющий сохранением 
    class SaveManager
    {
        // синглтон классический
        private static SaveManager _saveManager;

        private SaveManager(){}

        public static SaveManager Instance()
        {
            return _saveManager ?? (_saveManager = new SaveManager());
        }
        // сохраняем картинку как битмап
        public void SaveAsBitmap(CWorkSpace cWorkSpace)
        {
           
            var sFD = new SaveFileDialog();
            sFD.Filter = "Bitmap Image|*.bmp";
            sFD.DefaultExt = "picture.bmp";
            sFD.Title = "Select the folder";
            sFD.ShowDialog();
            if (sFD.FileName == "") return;
            
            var fS = (FileStream) sFD.OpenFile();
            
           
            var margin = cWorkSpace.Margin;
            cWorkSpace.Margin = new Thickness(0, 0,
                 margin.Left - margin.Right, margin.Bottom - margin.Top);
            cWorkSpace.RemoveSelection();
            var size = new Size(cWorkSpace.Width, cWorkSpace.Height);
            cWorkSpace.Measure(size);
            cWorkSpace.Arrange(new Rect(size));
            var renderBitmap =
                new RenderTargetBitmap(
                    (int)size.Width,
                    (int)size.Height,
                    96d,
                    96d,
                    PixelFormats.Pbgra32);
            
            renderBitmap.Render(cWorkSpace);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            encoder.Save(fS);
            cWorkSpace.RecoverSelection();
            cWorkSpace.Margin = margin;
            fS.Close();
        }
        // сохроняем картинку как Simple vector grapics
        public void SaveAsSvg(List<Shape> data )
        {
            var sFD = new SaveFileDialog();
            sFD.Filter = "Simple vector grapics|*.svg";
            sFD.DefaultExt = "picture.svg";
            sFD.Title = "Select the folder";
            sFD.ShowDialog();
            if (sFD.FileName == "") return;


            var FS = new FileStream(sFD.FileName,
               FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            var bf = new BinaryFormatter();
            var serealizeData = (from Shape child in data select TransformToSerealizible(child)).ToList();
            bf.Serialize(FS, serealizeData);
            FS.Close();

        }
        // загружаем из файла
        public List<Shape> LoadFromSvg()
        {
            var sFD = new OpenFileDialog();
            sFD.Filter = "Simple vector grapics|*.svg";
            sFD.DefaultExt = "picture.svg";
            sFD.Title = "Select the folder";
            sFD.ShowDialog();
            if (sFD.FileName == "") return null;

            
            var FS = new FileStream(sFD.FileName,
                FileMode.Open, FileAccess.Read, FileShare.Read);
            var bf1 = new BinaryFormatter();
            var data  = (List<SerealizibleShape>)bf1.Deserialize(FS);
            
            FS.Close();
            return (from SerealizibleShape sh in data select sh.ToShape()).ToList();


        }

        // метод возвращаюший  SerealizibleShape из обычного
         private static SerealizibleShape TransformToSerealizible(Shape shape)
        {
            if (shape is Polyline) return new SerealiziblePolyline((Polyline)shape);
            return new SerealizibleRectangle((Rectangle)shape);
        }



         // сереализуемая оболочка для Shape
        [Serializable()]
        private abstract class SerealizibleShape
        {
            

            public abstract Shape ToShape();

            protected byte[] ColorToBytes(Color color)
            {
                var outArray = new byte[4];
                outArray[0] = color.A;
                outArray[1] = color.R;
                outArray[2] = color.G;
                outArray[3] = color.B;
                return outArray;
            }
            protected Color BytesToColor(byte[] bytes)
            {
                return Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
            }
        }
        // сереализуемая оболочка для Polyline
        [Serializable()]
        class SerealiziblePolyline : SerealizibleShape
        {
            private readonly List<double[]> _points = new List<double[]>();
            private readonly byte[] _polylineColor = new byte[4];
            private readonly double _polylineThickness;
            public SerealiziblePolyline(Polyline polyline)
            {
                foreach (var point in polyline.Points)
                {
                    _points.Add(new []{point.X, point.Y});
                }
                var color = ((SolidColorBrush)polyline.Stroke).Color;
                _polylineColor = ColorToBytes(color);
                _polylineThickness = polyline.StrokeThickness;
            }

            public override Shape ToShape()
            {
                var outpolyline = new Polyline();
                foreach (var point in _points)
                    outpolyline.Points.Add(new Point(point[0], point[1]));
                outpolyline.StrokeThickness = _polylineThickness;
                outpolyline.Stroke = new SolidColorBrush(BytesToColor(_polylineColor));
                return outpolyline;
            }
        }
        // сереализуемая оболочка для Rectangle
        [Serializable()]
        class SerealizibleRectangle : SerealizibleShape
        {
            private readonly double _width;
            private readonly double _height;
            private readonly double _left;
            private readonly double _top;
            private readonly double _angle;
            private readonly List<byte[]> _colors = new List<byte[]>();

            public SerealizibleRectangle(Rectangle rectangle)
            {
              _width =  rectangle.Width;
              _height = rectangle.Height;
               _left = Canvas.GetLeft(rectangle);
               _top= Canvas.GetTop(rectangle);
              _angle = ((RotateTransform) rectangle.RenderTransform).Angle;

                if (rectangle.Fill is LinearGradientBrush)
                {
                    var GradientStops = ((LinearGradientBrush) rectangle.Fill).GradientStops;
                    var color = GradientStops[0].Color;
                    _colors.Add(ColorToBytes(color));
                    color = GradientStops[1].Color;
                    _colors.Add(ColorToBytes(color));
                }
                else
                {
                    var color = ((SolidColorBrush) rectangle.Fill).Color;
                    _colors.Add(ColorToBytes(color));
                }
            }


            public override Shape ToShape()
            {
                var outRect = new Rectangle();
                Brush brush;
                if (_colors.Count>1)
                {
                    brush = new LinearGradientBrush(BytesToColor(_colors[0]), BytesToColor(_colors[1]), _angle);
                }
                else
                {
                    brush = new SolidColorBrush(BytesToColor(_colors[0]));
                }
                outRect.Fill = brush;
                outRect.Width = _width;
                outRect.Height = _height;
                Canvas.SetLeft(outRect, _left);
                Canvas.SetTop(outRect, _top);
                outRect.RenderTransform = new RotateTransform(_angle);
                return outRect;
            }
        }

      
    }
}
