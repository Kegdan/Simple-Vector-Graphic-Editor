using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;


namespace WpfApplication1
{  // Собственно главный класс - расширение Canvas
    class CWorkSpace: Canvas
    {
        private readonly CapturedPolylineNode _capturedPolylineNode = new CapturedPolylineNode();
        private readonly CapturedRect _capturedRect = new CapturedRect();
        private Selection _selection;
        private Point _canvasMovePos;
        public AddRemoveManager AddRemoveManager = new AddRemoveManager();
        // костыли
        private bool _isBlock;
        private bool _isSelection;
        private bool _isUpped;

        public CWorkSpace(Grid parent)
        {
            ClipToBounds = true;
            parent.Children.Add(this);
            Margin = new Thickness(140,0,0,0);
            Width = parent.Width-140;
            Height = parent.Height;
            Background = Brushes.Azure;
        }

        public void Initialization()
        {
            _selection = new Selection(this);
            MouseMove += CWSMouseMove;
            MouseLeftButtonDown += CWSMouseLeftButtonDown;
            MouseLeftButtonUp += CWSMouseLeftButtonUp;
            MouseLeave += CWSMouseLeave;
        }

       

        #region MouseOnCanvasEvents
       

        private void CWSMouseMove(object sender, MouseEventArgs e)
        {
            if ((e.LeftButton != MouseButtonState.Pressed) || _isBlock|| _isUpped) return;
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                _isSelection = true;
                _selection.SetSelection(e.GetPosition(this));

            }else if (_selection.Move)
            {
                _selection.SetOffset(e.GetPosition(this));

            }else
            if (_capturedPolylineNode.Polyline != null)
            {
                // пермещение узла полилинии
                var senderCanvas = (Canvas)sender;
                var currentPos = e.GetPosition(senderCanvas);
                _capturedPolylineNode.SetNodePos(currentPos);

            }
            else if (_capturedRect.Rectangle != null)
            {
                // Выбор действия над прямоугольником
                switch (_capturedRect.CurrentAction)
                {
                    case CapturedRect.Action.Move: _capturedRect.SetOffset(e.GetPosition(this)); break;
                    case CapturedRect.Action.ChangeAngle: _capturedRect.ChangeAngel(e.GetPosition(this)); break;
                    case CapturedRect.Action.ResizeXdown: _capturedRect.ResizeXdown(e.GetPosition(this)); break;
                    case CapturedRect.Action.ResizeYDown: _capturedRect.ResizeYdown(e.GetPosition(this)); break;

                    case CapturedRect.Action.ResizeXUp: _capturedRect.ResizeXUp(e.GetPosition(this)); break;
                    case CapturedRect.Action.ResizeYUp: _capturedRect.ResizeYUp(e.GetPosition(this)); break;
                }

            }
            else
            {
                // Перемещение рабочей области
                var currentPos = e.GetPosition(this);
                var xOffset = currentPos.X - _canvasMovePos.X;
                var yOffset = currentPos.Y - _canvasMovePos.Y;

                foreach (var points in from Shape ch in Children where ch is Polyline select ((Polyline)ch).Points)
                    for (var i = 0; i < points.Count; i++)
                        points[i] = new Point(points[i].X + xOffset, points[i].Y + yOffset);


                foreach (var child in from Shape ch in Children where ch is Rectangle select (Rectangle)ch)
                {
                    SetLeft(child, GetLeft(child) + xOffset);
                    SetTop(child, GetTop(child) + yOffset);
                }

                _canvasMovePos = currentPos;
            }
        }

        private void CWSMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isUpped = false;
            // ищем рядом узезл какой либо полилинии
            var newpoint = e.GetPosition(this);
            if (e.ClickCount != 1) return;
            if (AddRemoveManager.IsAddRect())
            {
                _isBlock = true;
                var rect = AddRemoveManager.GetRectangle();
                SetLeft(rect, newpoint.X);
                SetTop(rect, newpoint.Y);
                rect.MouseLeftButtonDown += RectMouseLeftButtonDown;
                Children.Add(rect);
                AddRemoveManager.Clear();
                return;
            }
            if (AddRemoveManager.IsAddLine())
            {
                _isBlock = true;
                var polyline = AddRemoveManager.GetPolyline();
                polyline.Points = new PointCollection() { newpoint, new Point(newpoint.X+100, newpoint.Y) };
                Children.Add(polyline);
                polyline.MouseLeftButtonDown += PolylineMouseLeftButtonDown;
                AddRemoveManager.Clear();
                return;
            }
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                AddRemoveManager.RemoveButton();
                Children.Remove(_selection.SelectionRectangle);
              Children.Add( _selection.SetStartPoint( newpoint));
                return;
            }
            if (!_selection.InSelect(newpoint))
            {
                
                _selection.Remove();
                if (!AddRemoveManager.IsRemove())
                {
                    AddRemoveManager.Clear();
                }
                
            }
            if (_isSelection) return;
            

                var find = false;
                foreach (var ch in Children.OfType<Polyline>())
                {
                    for (var i = 0; i < ch.Points.Count; i++)
                    {
                        if (Math.Pow(newpoint.X - ch.Points[i].X, 2) + Math.Pow(newpoint.Y - ch.Points[i].Y, 2) >
                            100) continue;
                        _capturedPolylineNode.Polyline = ch;
                        _capturedPolylineNode.NodeIndex = i;
                        find = true;
                        break;
                    }
                    if (find) return;
                }

                // не нашли - значит перемещаем рабочую область
                _canvasMovePos = newpoint;
            
        }

        private void CWSMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isUpped = true;
            // сброс костылей
            if (_isSelection)
            {
                _isSelection = false;
                _selection.TakeObjectList(this);
            }
            _isBlock = false;
           
            _capturedPolylineNode.RemoveNodeIfNeed();
            _capturedRect.Rectangle = null;
            _capturedPolylineNode.Polyline = null;
            _capturedRect.CurrentAction = CapturedRect.Action.None;
        }
        private void CWSMouseLeave(object sender, MouseEventArgs e)
        {
            CWSMouseLeftButtonUp(null, null);
        }

#endregion
        
        private void PolylineMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // собитие при нажатии на полилинию, добавление узла 
            if (AddRemoveManager.IsRemove())
            {
                Children.Remove((Polyline)sender);
                AddRemoveManager.Clear();
                return;
            }
            if (e.ClickCount != 2)
            {
                _isBlock = true; 
                return;
            }
            var senderPolyline = (Polyline)sender;
            var newpoint = e.GetPosition(this);
            for (var i = 0; i < senderPolyline.Points.Count - 1; i++)
            {

                if (!IsNewPointOnLine(newpoint, i, senderPolyline)) continue;

                senderPolyline.Points.Insert(i + 1, newpoint);
                _capturedPolylineNode.Polyline = senderPolyline;
                _capturedPolylineNode.NodeIndex = i + 1;
                break;
            }
        }

        private void RectMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            if (AddRemoveManager.IsRemove())
            {
                Children.Remove((Rectangle)sender);
                AddRemoveManager.Clear();
                return;
            }
            var rectOIbject = (Rectangle) sender;
            var RectCoor = e.GetPosition(rectOIbject);
            _capturedRect.Rectangle = rectOIbject;
            _capturedRect.SetMoveGlobalPos(e.GetPosition(this));
            if (CapturedRect.inCorner(RectCoor, rectOIbject) )
            {
                _capturedRect.CurrentAction = CapturedRect.Action.ChangeAngle;
            }
            else if (CapturedRect.OnLeftEdge(RectCoor, rectOIbject))
            {
                _capturedRect.CurrentAction = CapturedRect.Action.ResizeXdown;
            }
            else if (CapturedRect.OnTopEdge(RectCoor, rectOIbject))
            {
                _capturedRect.CurrentAction = CapturedRect.Action.ResizeYDown;
            }
            else if (CapturedRect.OnRightEdge(RectCoor))
            {
                _capturedRect.CurrentAction = CapturedRect.Action.ResizeXUp;
            }
            else if (CapturedRect.OnBottomEdge(RectCoor))
            {
                _capturedRect.CurrentAction = CapturedRect.Action.ResizeYUp;
            }
            else
            {
                
                _capturedRect.CurrentAction = CapturedRect.Action.Move;
            }

        }

        // Проверка для нахождения где лежит свежеиспеценный узел полилинии
        private bool IsNewPointOnLine(Point newpoint, int index, Polyline senderPolyline)
        {
            return (Math.Abs((senderPolyline.Points[index + 1].X - senderPolyline.Points[index].X) *
                             (newpoint.Y - senderPolyline.Points[index].Y) -
                             (newpoint.X - senderPolyline.Points[index].X) *
                             (senderPolyline.Points[index + 1].Y - senderPolyline.Points[index].Y)) <=
                    Math.Abs((senderPolyline.Points[index + 1].X - senderPolyline.Points[index].X) -
                             (senderPolyline.Points[index + 1].Y - senderPolyline.Points[index].Y)) *
                    senderPolyline.StrokeThickness * 2)
                &&
                    ((senderPolyline.Points[index + 1].X - senderPolyline.Points[index].X) * (newpoint.X - senderPolyline.Points[index].X)
                    + (senderPolyline.Points[index + 1].Y - senderPolyline.Points[index].Y) * (newpoint.Y - senderPolyline.Points[index].Y) >= 0 );
        }

        // Востановление данных, загруженных через SaveManager
        public void RecoverState(List<Shape> data)
        {
            _isBlock = true;
            Children.Clear();
            foreach (var shape in data)
            {
                if (shape is Polyline)
                {
                    shape.MouseLeftButtonDown += PolylineMouseLeftButtonDown;
                }
                else
                {
                    shape.MouseLeftButtonDown += RectMouseLeftButtonDown;
                }
                Children.Add(shape);

            }
            _selection = new Selection(this);


        }

        public List<Shape> GiveDataForSave()
        {
            return (from Shape ch in Children where !ch.Equals(_selection.SelectionRectangle) select ch).ToList();
        }

        public void RemoveSelection()
        {
            Children.Remove(_selection.SelectionRectangle);
        }

        public void RecoverSelection()
        {
            _selection.Recover(this);
        }

        class CapturedRect
        {

            private static int ConstLenght = 10;
            public Rectangle Rectangle;
            private Point _movePos;

            public enum Action
            {
                Move = 1, ResizeXdown, ResizeYDown, ChangeAngle, None,

                ResizeXUp,
                ResizeYUp
            }

            public Action CurrentAction;
            
           
            public void SetMoveGlobalPos(Point currentPos)
            {
                _movePos = currentPos;
            }

            public void SetOffset(Point currentPos)
            {
                SetLeft(Rectangle, GetLeft(Rectangle)+currentPos.X - _movePos.X);
                SetTop(Rectangle, GetTop(Rectangle)+currentPos.Y - _movePos.Y);
                _movePos = currentPos;
            }

            public static bool inCorner(Point rectCoor, Rectangle rectOIbject)
            {
                return ((rectCoor.X < ConstLenght) && (rectCoor.Y < ConstLenght)) || ((rectCoor.X < ConstLenght) && (rectCoor.Y > rectOIbject.Height - ConstLenght)) ||
                       ((rectCoor.X > rectOIbject.Width - ConstLenght) && (rectCoor.Y < ConstLenght)) || ((rectCoor.X > rectOIbject.Width - ConstLenght) && (rectCoor.Y > rectOIbject.Height - ConstLenght));
            }


            public static bool OnLeftEdge(Point rectCoor, Rectangle rectOIbject)
            {
                return (rectCoor.X > rectOIbject.Width - ConstLenght);
            }

            public static bool OnTopEdge(Point rectCoor, Rectangle rectOIbject)
            {
                return (rectCoor.Y > rectOIbject.Height - ConstLenght);
            }
            public static bool OnBottomEdge(Point rectCoor)
            {
                return (rectCoor.Y < ConstLenght);
            }

            public static bool OnRightEdge(Point rectCoor)
            {
                return rectCoor.X < ConstLenght;
            }

            public void ChangeAngel(Point currentPos)
            {

                Rectangle.RenderTransform =
                    new RotateTransform(((RotateTransform)Rectangle.RenderTransform).Angle + (_movePos.X - currentPos.X), Rectangle.Width / 2, Rectangle.Height/2);
                SetMoveGlobalPos(currentPos);
            }

            public void ResizeXdown(Point currentPos)
            {
                var currentPosInrRect = ((Canvas)Rectangle.Parent).TranslatePoint(currentPos, Rectangle);
                var movePosInrRect = ((Canvas)Rectangle.Parent).TranslatePoint(_movePos, Rectangle);
                var delta = (currentPosInrRect.X - movePosInrRect.X);
                if (Rectangle.Width + delta > ConstLenght)
                {
                    Rectangle.Width += delta;
                }
                else Rectangle.Width = ConstLenght;
                SetMoveGlobalPos(currentPos);
            }

            public void ResizeYdown(Point currentPos)
            {
                var currentPosInrRect = ((Canvas)Rectangle.Parent).TranslatePoint(currentPos, Rectangle);
                var movePosInrRect = ((Canvas)Rectangle.Parent).TranslatePoint(_movePos, Rectangle);
          
                var delta = (currentPosInrRect.Y - movePosInrRect.Y);
                if (Rectangle.Height + delta > ConstLenght)
                {
                    Rectangle.Height += delta;
                }
                else Rectangle.Height = ConstLenght;

                
                SetMoveGlobalPos(currentPos);
            }


            public void ResizeXUp(Point currentPos)
            {
                var currentPosInrRect = ((Canvas)Rectangle.Parent).TranslatePoint(currentPos, Rectangle);
                var movePosInrRect = ((Canvas)Rectangle.Parent).TranslatePoint(_movePos, Rectangle);
                var delta = (currentPosInrRect.X - movePosInrRect.X);
                if (Rectangle.Width + delta > ConstLenght)
                {
                    var StPoint = ((Canvas) Rectangle.Parent).TranslatePoint(new Point(GetLeft(Rectangle), GetTop(Rectangle)),
                        Rectangle);
                    var newstart = Rectangle.TranslatePoint(new Point(StPoint.X + delta, StPoint.Y), (Canvas)Rectangle.Parent);
                    SetLeft(Rectangle,newstart.X);
                    SetTop(Rectangle, newstart.Y);

                    Rectangle.Width -= delta;
                }
                else Rectangle.Width = ConstLenght;
                SetMoveGlobalPos(currentPos);
            }

            public void ResizeYUp(Point currentPos)
            {
                var currentPosInrRect = ((Canvas)Rectangle.Parent).TranslatePoint(currentPos, Rectangle);
                var movePosInrRect = ((Canvas)Rectangle.Parent).TranslatePoint(_movePos, Rectangle);
                var delta = (currentPosInrRect.Y - movePosInrRect.Y);
                if (Rectangle.Height + delta > ConstLenght)
                {
                    var StPoint = ((Canvas)Rectangle.Parent).TranslatePoint(new Point(GetLeft(Rectangle), GetTop(Rectangle)),
                        Rectangle);
                    var newstart = Rectangle.TranslatePoint(new Point(StPoint.X , StPoint.Y+ delta), (Canvas)Rectangle.Parent);
                    SetLeft(Rectangle, newstart.X);
                    SetTop(Rectangle, newstart.Y);

                    Rectangle.Height -= delta;
                }
                else Rectangle.Height = ConstLenght;


                SetMoveGlobalPos(currentPos);
            }
        }

        class CapturedPolylineNode
        {
            public Polyline Polyline;
            public int NodeIndex=-1;

            public void SetNodePos(Point pos)
            {
                Polyline.Points[NodeIndex] = pos;
            }

            private Point GetNodeWithOffset(int offset=0)
            {
                return Polyline.Points[NodeIndex + offset];
            }

            public void RemoveNodeIfNeed()
            {
                if (Polyline != null && Polyline.Points.Count > 2 &&
                ((NodeIndex + 1 < Polyline.Points.Count &&
                  DistanceToNodeWhisOffset(1) <= _removeDistance)
                 ||
                 (NodeIndex > 0 &&
                  DistanceToNodeWhisOffset(-1) <= _removeDistance))
                )
                    Polyline.Points.RemoveAt(NodeIndex);
            }

            private double DistanceToNodeWhisOffset(int offset)
            {
                return Math.Pow(GetNodeWithOffset().X - GetNodeWithOffset(offset).X, 2)
                       + Math.Pow(GetNodeWithOffset().Y - GetNodeWithOffset(offset).Y, 2);
            }

            private readonly double _removeDistance = Math.Pow(10, 2);
        
        }

        class Selection
        {
            
            private Rectangle _selection ;
            public Rectangle SelectionRectangle {
                get { return _selection; } 
                private set { _selection = value; } }
            private List<Shape> _selectedObjects = new List<Shape>();
            private Point _movePos;
            public bool Move;

            public Selection(CWorkSpace parent)
            {
                Initialize();
            }

            private void SelectionMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            {
                _movePos = e.GetPosition((CWorkSpace)_selection.Parent);
                Move = true;
            }
            

            public void SetSelection(Point currentPos)
            {
                var LT = _selection.TranslatePoint(new Point(0, 0), (Canvas)_selection.Parent);



                if ((currentPos.Y - LT.Y < 0) && (currentPos.X - LT.X < 0))
                {
                    _selection.RenderTransform = new RotateTransform(180);
                    _selection.Width = Math.Abs(currentPos.X - LT.X);
                    _selection.Height = Math.Abs(currentPos.Y - LT.Y);
                }
                else
                    if (currentPos.Y - LT.Y < 0)
                    {
                        _selection.RenderTransform = new RotateTransform(270);
                        _selection.Width = Math.Abs(currentPos.Y - LT.Y);
                        _selection.Height = Math.Abs(currentPos.X - LT.X);
                    }
                    else
                        if (currentPos.X - LT.X < 0)
                        {
                            _selection.RenderTransform = new RotateTransform(90);
                            _selection.Width = Math.Abs(currentPos.Y - LT.Y);
                            _selection.Height = Math.Abs(currentPos.X - LT.X);
                        }
                        else
                        {
                            _selection.RenderTransform = new RotateTransform(0);
                            _selection.Width = Math.Abs(currentPos.X - LT.X);
                            _selection.Height = Math.Abs(currentPos.Y - LT.Y);
                        }
            }

            public Rectangle SetStartPoint(Point currentPos)
            {
                var output =Initialize();
               
                SetLeft(_selection, currentPos.X);
                SetTop(_selection, currentPos.Y);
                _selectedObjects = new List<Shape>();
                return output;
            }

            private Rectangle Initialize()
            {
                _selection = new Rectangle();
                _selection.Fill = new SolidColorBrush(Color.FromArgb(60, 147, 0, 200));
                
                _selection.MouseLeftButtonDown += SelectionMouseLeftButtonDown;
                return _selection;
            }

            public void Recover(CWorkSpace parent)
            {
                parent.Children.Add(_selection);
            }

            public void TakeObjectList(CWorkSpace cWorkSpace)
            {
                

                _selectedObjects.AddRange(cWorkSpace.Children.OfType<Polyline>().Where(polyline => polyline.Points.Any(InSelect)));
                _selectedObjects.AddRange(cWorkSpace.Children.OfType<Rectangle>().Where(InSelect));

            }

            private bool InSelect(Rectangle rectangle)
            {
                return !rectangle.Equals(_selection)&&(InRectSelect(rectangle, _selection) || InRectSelect( _selection,rectangle));
            }

            public bool InSelect(Point point)
            {
                var LT = _selection.TranslatePoint(new Point(0, 0), (Canvas)_selection.Parent);
                var RB = _selection.TranslatePoint(new Point(_selection.Width, _selection.Height), (Canvas)_selection.Parent);

                return (point.X > Math.Min(LT.X, RB.X)) && (point.X < Math.Max(LT.X, RB.X)) && (point.Y > Math.Min(LT.Y, RB.Y)) &&
                       (point.Y < Math.Max(LT.Y, RB.Y));
            }

            private bool InRectSelect(Rectangle rectangle, Rectangle rectangle2)
            {
                var LT = rectangle.TranslatePoint(new Point(0, 0), rectangle2);
                var RT = rectangle.TranslatePoint(new Point(rectangle.Width, 0), rectangle2);
                var LB = rectangle.TranslatePoint(new Point(0, rectangle.Height), rectangle2);
                var RB = rectangle.TranslatePoint(new Point(rectangle.Width, rectangle.Height), rectangle2);

                return (InSelectRect(LT, rectangle2)) || (InSelectRect(RT, rectangle2)) || (InSelectRect(LB, rectangle2)) || (InSelectRect(RB, rectangle2));
            }

            private bool InSelectRect(Point point, Rectangle rect)
            {
                return (point.X > 0) && (point.X < rect.Width) && (point.Y > 0) &&
                     (point.Y < rect.Height);
            }

            public void Remove()
            {
                _selection.Width = 0;
                _selection.Height = 0;
                _selectedObjects = new List<Shape>();
                Move = false;
            }

           

            public void SetOffset(Point currentPos)
            {
                
                var xOffset = currentPos.X - _movePos.X;
                var yOffset = currentPos.Y - _movePos.Y;
               
                SetLeft(_selection, GetLeft(_selection) + xOffset);
                SetTop(_selection, GetTop(_selection) + yOffset);

                foreach (var points in from Shape ch in _selectedObjects.Distinct() where ch is Polyline  select ((Polyline)ch).Points)
                    for (var i = 0; i < points.Count; i++)
                        points[i] = new Point(points[i].X + xOffset, points[i].Y + yOffset);


                foreach (var child in from Shape ch in _selectedObjects.Distinct() where ch is Rectangle select (Rectangle)ch)
                {
                    SetLeft(child, GetLeft(child) + xOffset);
                    SetTop(child, GetTop(child) + yOffset);
                }

                _movePos = currentPos;
            }
        }

       
    }
}
