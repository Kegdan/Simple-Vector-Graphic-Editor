using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;

namespace WpfApplication1
{
    // Класс главного окна 
    public partial class MainWindow : Window
    {
        Dictionary<string, Color> colorDictionary = new Dictionary<string, Color>();  
        List<Button> Buttons = new List<Button>(3); 
        private CWorkSpace _cWorkSpace;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Initialized(object sender, EventArgs e)
        {
            Gmain.Width = Width;
            Gmain.Height = Height;

            var colosProp = (typeof (Colors)).GetProperties();
            for (int i = 0; i < colosProp.Count(); i++)
            {
                var name = colosProp[i].Name;
                colorDictionary.Add(name, (Color)colosProp[i].GetValue(null,null));
                CLineColor.Items.Add(name);
                CFirstColor.Items.Add(name);
                CSecondColor.Items.Add(name);
                if (name == "Blue")
                {
                    CLineColor.SelectedIndex = i;
                    CFirstColor.SelectedIndex = i;
                    CSecondColor.SelectedIndex = i;
                }
            }
            Buttons.Add(Add_Rectangle);
            Buttons.Add(Add_polyline);
            Buttons.Add(Remove);
           
            _cWorkSpace = new CWorkSpace(Gmain);
            _cWorkSpace.Initialization();
            _cWorkSpace.AddRemoveManager.PostAction = ButtonActivator;
            _cWorkSpace.AddRemoveManager.OffButton = ButtonDeActivator;
        }

        private void ButtonDeActivator()
        {
            foreach (var button in Buttons)
            {
                button.IsEnabled = false;
            }
        }

        private void SaveAsBitmap(object sender, RoutedEventArgs e)
        {
            SaveManager.Instance().SaveAsBitmap(_cWorkSpace);
        }

        private void SaveAsSVG(object sender, RoutedEventArgs e)
        {
            SaveManager.Instance().SaveAsSvg(_cWorkSpace.GiveDataForSave());
        }

        private void Load(object sender, RoutedEventArgs e)
        {
            var data = SaveManager.Instance().LoadFromSvg();
            if (data != null)
           _cWorkSpace.RecoverState(data);
        }

        private string _lastValue;
        private void Thickness_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (Regex.Matches(Thickness.Text, @"^\d*$").Count ==0)
            {
                Thickness.Text = _lastValue;
                return;
            }
            _lastValue = Thickness.Text;
        }

       

        private void Add_Rectangle_Click(object sender, RoutedEventArgs e)
        {
            if ((bool) GradientFill.IsChecked)
            {
                _cWorkSpace.AddRemoveManager.LoadRectGradData(colorDictionary[CFirstColor.Text],
                    colorDictionary[CSecondColor.Text]);
            }
            else
            {
                _cWorkSpace.AddRemoveManager.LoadRectData(colorDictionary[CFirstColor.Text]);
            }
            ButtonActivControl((Button)sender);
        }

        private void ButtonActivControl(Button button)
        {
            foreach (var but in Buttons)
                    but.IsEnabled = true;

            button.IsEnabled = false;
        }
        private void ButtonActivator()
        {
            foreach (var but in Buttons)
                but.IsEnabled = true;

           
        }

        private void Add_polyline_Click(object sender, RoutedEventArgs e)
        {
            _cWorkSpace.AddRemoveManager.LoadLineData(colorDictionary[CLineColor.Text], int.Parse(Thickness.Text));
            ButtonActivControl((Button)sender);
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            _cWorkSpace.AddRemoveManager.LoadRemove();
            ButtonActivControl((Button)sender);
        }

        private void GradientFill_Checked(object sender, RoutedEventArgs e)
        {
            CSecondColor.IsEnabled = true;
        }

        private void GradientFill_Unchecked(object sender, RoutedEventArgs e)
        {
            CSecondColor.IsEnabled = false;
        }

      

        
        

      

    }
}
