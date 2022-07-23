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
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace Founds_Center
{

    enum Transaction
    {
        none = 0,
        add = 1,
        delete = 2,
        view = 3
    }

    public partial class MainWindow : Window
    {
        //I will not upload my connection string
        private const string connectionString = "";
        private bool isConnected = false;
        private bool isHidden = false;
        private bool maximized = false;
        private double saveWidth = 0, saveHeight = 0;
        private double saveTop = 0, saveLeft = 0;
        private List<Items> items;
        private CenterData[] dataForTransactions;
        private Transaction currentTr = Transaction.none;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Handeles the connection logic outside the UI thread
            Thread thread = new Thread(() =>
            {
                bool wait = true;

                while (wait)
                { 
                    wait = ConnectToSql();
                }

                Dispatcher.Invoke(() =>
                {
                    transactions.ItemsSource = dataForTransactions;
                });
            });

            thread.Start();

            items = new();

            //Create empty grid colums
            for (int i = 0; i < 19; i++)
            {
                items.Add(new Items());
            }

            dataCTran.ItemsSource = items;
        }

        //The first connection to retrive data from database
        private bool ConnectToSql()
        {
            string sqlCommand = "Select Center,Text from DataCenter";

            SqlConnection conn = new(connectionString);

            //Thread safety
            lock (conn)
            {
                try
                {
                    conn.Open();
                }
                catch (Exception)
                {
                    //return true to wait
                    Thread.Sleep(3000);
                    return true;
                }

                SqlCommand cmd = new(sqlCommand, conn);
                SqlDataReader dataReader = cmd.ExecuteReader();
                LinkedList<CenterData> tempData = new();

                while (dataReader.Read())
                {
                    string center = dataReader.GetString(0);
                    string text = dataReader.GetString(1);

                    tempData.AddLast(new CenterData(Convert.ToInt32(center), text));
                }

                conn.Close();

                //Stores our possible centers
                //The properties of the array and the centers allow us to use binery search when needed and optimize run time
                dataForTransactions = tempData.ToArray();
                isConnected = true;
            }

            //return false to not wait
            return false;
        }

        private void XImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Application.Current.Shutdown();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                int width = (int)dragBar.ActualWidth;
                int height = (int)dragBar.ActualHeight;

                if (e.GetPosition(window).X >= 34 && e.GetPosition(window).X <= width)
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

        //Handles the logic of showing the menu
        private void MenuBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Checks if the menu is hidden
            if (isHidden == false)
            {
                isHidden = true;

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

        private void MImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => OnMaximize();

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

        //Close the add transaction
        private void Close_Transation_Add(object sender, RoutedEventArgs e)
        {
            addTransaction.Visibility = Visibility.Hidden;
            currentTr = Transaction.none;

            //Return back to original state
            addTrSaveBtn.IsEnabled = false;
            discardBtnAddTr.IsEnabled = false;
            dataCTran.ItemsSource = null;
            items.ForEach(x => x.Clean());
            dataCTran.ItemsSource = items;
        }

        private void AddBtnClicked(object sender, MouseButtonEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please check your internet connection or contact the adminastrator to check the server state", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            addTransaction.Visibility = Visibility.Visible;
            currentTr = Transaction.add;
        }

        private void DiscardBtnAdd(object sender, RoutedEventArgs e)
        {
            //Clean the table and disable the button
            dataCTran.ItemsSource = null;
            items.ForEach(x => x.Clean());
            dataCTran.ItemsSource = items;
            discardBtnAddTr.IsEnabled = false;
        }

        private void SaveBtnAddTr(object sender, RoutedEventArgs e)
        {
            List<Items> tempItem = items.Where(x => x.IsEmpty() == false).ToList();

            //Makes sure the list isn't empty
            if (tempItem.Count == 0)
            {
                return;
            }

            //Checks the list for errors
            for (int i = 0; i < tempItem.Count; i++)
            {
                bool f = tempItem[i].CurrectCenter(dataForTransactions); //Check if the found is right
                bool fc = tempItem[i].IsFcenterCurrect(); //Check if the found center is right
                bool cSum = tempItem[i].sum > 0; //Check if the sum is now lower than or 0 

                //Return errors based on the type of error
                if (!f || !fc || !cSum)
                {
                    if (!f)
                    {
                        MessageBox.Show("One of the founds are not currect", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (!fc)
                    {
                        MessageBox.Show("One of the found centers are not currect", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (!cSum)
                    {
                        MessageBox.Show("One of the founds are not currect", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return;
                }
            }

            Thread thread = new(() => 
            {
                SqlConnection conn = new(connectionString);
                string sqlCommand = "INSERT INTO Data(Found_Center, Center, Sum, Text) VALUES(@FC, @C, @S, @T)";

                //Thread safety
                lock (conn)
                {
                    try
                    {
                        conn.Open();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Please check your internet connection or contact the adminastrator to check the server state", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    SqlCommand cmd = new(sqlCommand, conn);

                    for (int i = 0; i < tempItem.Count; i++)
                    {
                        cmd.Parameters.AddWithValue("@FC", tempItem[i].fcenter);
                        cmd.Parameters.AddWithValue("@C", tempItem[i].center);
                        cmd.Parameters.AddWithValue("@S", tempItem[i].sum);
                        cmd.Parameters.AddWithValue("@T", tempItem[i].text);
                        cmd.ExecuteNonQuery();
                    }

                    conn.Close();
                }

                Dispatcher.Invoke(() =>
                {
                    addTrSaveBtn.IsEnabled = false;

                    //Clean the table and disable the button
                    dataCTran.ItemsSource = null;
                    items.ForEach(x => x.Clean());
                    dataCTran.ItemsSource = items;
                    discardBtnAddTr.IsEnabled = false;
                });
            });

            thread.Start();
        }

        private void dataCTran_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            addTrSaveBtn.IsEnabled = true;
            discardBtnAddTr.IsEnabled = true;
        }

        /*
         * Window state doesn't position the window currectly,
         * we use here our own trick to resize the window without entering fullscreen mode.
         */
        private void OnMaximize()
        {
            if (maximized)
            {
                maximized = false;

                //Return the window to the original size before the resize
                window.Width = saveWidth;
                window.Height = saveHeight;

                //Positions our window back
                this.Top = saveTop;
                this.Left = saveLeft;

                //Set the window in resize mode
                rRect.Visibility = Visibility.Visible;
                window.ResizeMode = ResizeMode.CanResizeWithGrip;
            }
            else
            {
                maximized = true;
                saveLeft = window.Left;
                saveTop = window.Top;

                //Fix the window border issue that happened by fullscreen mode
                this.Top = 1;
                this.Left = 1;

                //Stores the width of the window
                saveWidth = window.ActualWidth;
                saveHeight = window.ActualHeight;
                //Sizes the window currectly
                window.Width = SystemParameters.MaximizedPrimaryScreenWidth - 17;
                window.Height = SystemParameters.MaximizedPrimaryScreenHeight - 17;

                //Set the window to fullscreen (doesn't change the state of the window)
                rRect.Visibility = Visibility.Hidden;
                window.ResizeMode = ResizeMode.NoResize;
            }
        }

        private void ZImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => window.WindowState = WindowState.Minimized;

    }
}
