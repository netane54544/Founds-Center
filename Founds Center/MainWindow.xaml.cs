using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Founds_Center
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isHidden = false;
        private bool maximized = false;
        private double saveWidth = 0, saveHeight = 0;
        private double saveTop = 0, saveLeft = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void xImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                int width = (int)dragBar.ActualWidth;
                int height = (int)dragBar.ActualHeight;

                if (e.GetPosition(window).X >= 0 && e.GetPosition(window).X <= width)
                {
                    if (e.GetPosition(window).Y >= 0 && e.GetPosition(window).Y <= height)
                    {
                        if (e.ClickCount >= 2)
                        {
                            OnMaximize();
                        }

                        if (!maximized)
                        {
                            this.DragMove();
                        }
                    }
                }
            }
        }

        private void menuBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UIElementCollection controlList = grid.Children;

            if (isHidden == false)
            {
                isHidden = true;

                //grid.Children.OfType<ListView>().ToList().ForEach(x => x.Visibility = Visibility.Hidden);
                transactions.Visibility = Visibility.Hidden;
                spl.Visibility = Visibility.Hidden;
                fileView.Visibility = Visibility.Visible;
            }
            else
            {
                isHidden = false;
                transactions.Visibility = Visibility.Visible;
                spl.Visibility = Visibility.Visible;
                fileView.Visibility = Visibility.Hidden;
            }
        }

        private void mImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => OnMaximize();

        //Only for stackpanels
        //Creates hover effect when entering
        private void MouseIn(object sender, MouseEventArgs e)
        {
            StackPanel st = (StackPanel)sender;
            st.Background = Brushes.Gray;
        }

        //Only for stackpanels
        //Disables the over effect when exiting
        private void MouseOut(object sender, MouseEventArgs e)
        {
            StackPanel st = (StackPanel)sender;
            st.Background = Brushes.Transparent;
        }

        private void OnMaximize()
        {
            if (maximized)
            {
                maximized = false;
                //window.WindowState = WindowState.Normal;
                window.Width = saveWidth;
                window.Height = saveHeight;
                this.Top = saveTop;
                this.Left = saveLeft;
                rRect.Visibility = Visibility.Visible;
                window.ResizeMode = ResizeMode.CanResizeWithGrip;
            }
            else
            {
                maximized = true;
                saveLeft = window.Left;
                saveTop = window.Top;
                this.Top = 1;
                this.Left = 1;
                saveWidth = window.ActualWidth;
                saveHeight = window.ActualHeight;
                window.Width = SystemParameters.MaximizedPrimaryScreenWidth - 17;
                window.Height = SystemParameters.MaximizedPrimaryScreenHeight - 17;
                //this.WindowState = WindowState.Maximized;
                rRect.Visibility = Visibility.Hidden;
                window.ResizeMode = ResizeMode.NoResize;
            }
        }

        private void zImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => window.WindowState = WindowState.Minimized;

    }
}
