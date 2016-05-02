
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApplication1
{
    class AddRemoveManager
    {
        public delegate void ActionDeligate();
        public  enum Action
        {
            AddPolyline, AddRectangle, Remove, None
        }

        public Action CurrentAction = Action.None;

        public ActionDeligate PostAction;
        public ActionDeligate OffButton; 
        private  int _thickness =3;
        private Color _lineColor = Colors.Blue;
        private Color _firstColor= Colors.Blue;
        private Color _secondColor= Colors.Blue;
        private bool _isGradient;

        public Rectangle GetRectangle()
        {
            var outRect = new Rectangle();
            outRect.RenderTransform = new RotateTransform(0);
            if (_isGradient)
            {
                outRect.Fill = new LinearGradientBrush(_firstColor, _secondColor, 0);
            }
            else
            {
                outRect.Fill = new SolidColorBrush(_firstColor);
            }
            outRect.Height = 100;
            outRect.Width = 100;

            return outRect;
        }

        public Polyline GetPolyline()
        {
            var outLine = new Polyline();

            outLine.Stroke = new SolidColorBrush(_lineColor);
            outLine.StrokeThickness = _thickness;
            return outLine;

        }

        public void LoadLineData(Color lineColor,int thickness = 3)
        {
            _thickness = thickness;
            _lineColor = lineColor;
            CurrentAction = Action.AddPolyline;
        }
        public void LoadRectData(Color firstColor)
        {
           
            _firstColor = firstColor;
            _isGradient = false;
            CurrentAction = Action.AddRectangle;
        }
        public void LoadRectGradData(Color firstColor, Color secondColor)
        {
            
            _firstColor = firstColor;
            _secondColor = secondColor;
            _isGradient = true;
            CurrentAction = Action.AddRectangle;
        }
        public void LoadRemove()
        {

            CurrentAction = Action.Remove;
        }
        public void Clear()
        {

            CurrentAction = Action.None;
            PostAction();
        }

        public bool IsAddRect()
        {
            return CurrentAction == Action.AddRectangle;
        }

        public bool IsAddLine()
        {
            return CurrentAction == Action.AddPolyline;
        }

        public void RemoveButton()
        {
            CurrentAction = Action.None;
            OffButton();
        }

        public bool IsRemove()
        {
            return CurrentAction == Action.Remove;
        }
    }
}
