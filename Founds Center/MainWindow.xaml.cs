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
    public partial class MainWindow : Window
    {
        private enum Transaction
        {
            error = -1,
            none = 0,
            add = 1,
            delete = 2,
            view = 3
        }

        private const string connectionString = @"data source=34.163.26.64;initial catalog=founddata;user id=sqlserver;password=22992299"; //Connection string
        private bool isConnected = false; //Checks connection to the server
        private bool isHidden = false;
        private bool maximized = false;
        private double saveWidth = 0, saveHeight = 0;
        private double saveTop = 0, saveLeft = 0;
        private List<Items> items; //Data source
        private CenterData[] dataForTransactions; //The center data
        private Transaction currentTr = Transaction.none; //Enum for the transaction
        private Items[] data; //Data source
        private int[] deleteIndex; //Stores the indexes to delete
        private bool cantClose = false; //For when saving

        public MainWindow()
        {
            InitializeComponent();
        }

        //Parses the number to the transaction enum
        private static Transaction UsageCurrentTr(int cr)
        {
            Transaction transaction;

            try
            {
                transaction = (Transaction)Enum.Parse(typeof(Transaction), cr.ToString());
            }
            catch (Exception)
            {
                transaction = Transaction.error;
            }

            return transaction;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Handeles the connection logic
            Thread task = new(() => 
            {
                bool wait = true;

                while (wait)
                {
                    wait = ConnectToSql();
                }

                Dispatcher.Invoke(() => 
                {
                    transactions.ItemsSource = dataForTransactions;
                    dataForTransactions.ToList().ForEach(x => dtTrCenter.Items.Add(x.center));

                    if (currentTr == Transaction.add)
                    {
                        addTransaction.Visibility = Visibility.Visible;
                    }
                    else if (currentTr == Transaction.delete)
                    {
                        deleteTransaction.Visibility = Visibility.Visible;
                    }
                });
            });
            task.Start();

            items = new();

            //Create empty grid colums
            for (int i = 0; i < 19; i++)
            {
                items.Add(new Items());
            }

            dataCTran.ItemsSource = items;

            //Check what transaction we were on
            Transaction t = UsageCurrentTr(MySettings.Default.CurrentTransaction);
            currentTr = (t == Transaction.error) ? Transaction.none : t;
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
                    Task.Delay(400);
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

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MySettings.Default.CurrentTransaction = (int)currentTr;
            MySettings.Default.Save();

            //Forcing all the threads to close
            while (cantClose)
                Thread.Sleep(10);

            Environment.Exit(0);
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
            ShowHideMenu();
        }

        /// <summary>
        /// Shows or hides the UI for the menu
        /// </summary>
        private void ShowHideMenu()
        {
            //Checks if the menu is hidden
            if (isHidden == false)
            {
                //Menu is shown
                isHidden = true;

                transactions.Visibility = Visibility.Hidden;
                spl.Visibility = Visibility.Hidden;
                fileView.Visibility = Visibility.Visible;

                if (currentTr == Transaction.add)
                {
                    addTransactionButton.IsEnabled = false;
                    addTransactionText.Foreground = Brushes.Gray;
                }
                else
                {
                    //Return to defualt state if the transaction screen is empty
                    addTransactionButton.IsEnabled = true;
                    addTransactionText.Foreground = Brushes.White;
                }
            }
            else
            {
                //Menu hidden
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

        private void AddBtnClicked(object sender, MouseButtonEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please check your internet connection or contact the adminastrator to check the server state", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            addTransaction.Visibility = Visibility.Visible;
            currentTr = Transaction.add;
            ShowHideMenu();
        }    

        private void SearchTrViewBtn_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new(() => { STr_ConnectToData(); });
            thread.Start();
        }

        private void STr_ConnectToData()
        {
            string sqlCommand = "Select Found_Center, Center, Sum, Text from TransactionData";

            SqlConnection conn = new(connectionString);
            List<Items> tempData = new();

            //Thread safety
            lock (conn)
            {
                try
                {
                    conn.Open();
                }
                catch (Exception)
                {
                    //Return error
                    MessageBox.Show("Error can't connect to the server", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                SqlCommand cmd = new(sqlCommand, conn);
                SqlDataReader dataReader = cmd.ExecuteReader();

                while (dataReader.Read())
                {
                    string fcenter = dataReader.GetString(0);
                    string center = dataReader.GetString(1);
                    double sum = dataReader.GetDouble(2);
                    string text = dataReader.GetString(3);

                    tempData.Add(new Items(fcenter, Convert.ToInt32(center), Convert.ToInt32(sum), text));
                }

                conn.Close();

                Dispatcher.Invoke(() =>
                {
                    //Note: add to the list the data
                    if (!String.IsNullOrEmpty(vTrCenter.Text))
                    {
                        for (int i = 0; i < tempData.Count; i++)
                        {
                            Items items = tempData[i];

                            if (!items.center.ToString().Equals(vTrCenter.Text))
                            {
                                tempData.RemoveAt(i);
                            }
                        }

                        //Slow
                        //tempData = tempData.Where(x => x.center == Convert.ToInt32(dtTrCenter.Text)).ToList();
                    }

                    if (!String.IsNullOrEmpty(vTrCoin.Text))
                    {
                        //found centers must be of type xxxx0/1/2x...x
                        //so the coin will always be in the 4 index

                        for (int i = 0; i < tempData.Count; i++)
                        {
                            Items items = tempData[i];

                            if (!items.fcenter[4].ToString().Equals(vTrCoin.Text))
                            {
                                tempData.RemoveAt(i);
                            }
                        }

                        //Slow
                        //tempData = tempData.Where(x => Convert.ToInt32(x.fcenter[4].ToString()) == Convert.ToInt32(dtTrCoin.Text)).ToList();
                    }

                    if (!String.IsNullOrEmpty(vTrFoundCenter.Text))
                    {
                        //the coin is on the 4 index so after that

                        for (int i = 0; i < tempData.Count; i++)
                        {
                            Items items = tempData[i];

                            if (!items.fcenter.Trim()[5..].ToString().Equals(vTrFoundCenter.Text[^1].ToString()))
                            {
                                tempData.RemoveAt(i);
                            }
                        }

                        //Slow
                        //tempData = tempData.Where(x => Convert.ToInt32((x.fcenter.Trim().Substring(5))) == Convert.ToInt32(((dtTrFoundCenter.Text[dtTrFoundCenter.Text.Length - 1]).ToString()))).ToList();
                    }
                });
            }

            data = tempData.ToArray();

            Dispatcher.Invoke(() =>
            {
                dataVTran.ItemsSource = null;
                dataVTran.ItemsSource = data;
            });
        }

        private void Close_Transation_View(object sender, RoutedEventArgs e)
        {
            //Return to defualt
            dataVTran.ItemsSource = null;
            vTrFoundCenter.Text = "";
            vTrCoin.Text = "";
            vTrCenter.Text = "";
            data = null;
            deleteIndex = null;

            viewTransaction.Visibility = Visibility.Hidden;
            currentTr = Transaction.none;
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

        private void Export_ViewTR(object sender, RoutedEventArgs e)
        {

        }

        #region DeleteTransaction
        private bool HasNull(List<int> items)
        {
            return items.Contains(-1) || deleteIndex is null;
        }

        private static bool IsDifferent(List<int> items1, List<int> items2)
        {
            //The length of the lists will be the same because of the last condition
            int count = items1.Count;

            if (count != items2.Count)
                return true;

            for (int i = 0; i < items1.Count; i++)
            {
                if (items1[i] != items2[i])
                    return true;
            }

            return false;
        }

        private void Close_Transation_Delete(object sender, RoutedEventArgs e)
        {
            //Return to defualt
            dataDTran.ItemsSource = null;
            deleteTrSaveBtn.IsEnabled = false;
            dtTrFoundCenter.Text = "";
            dtTrCoin.Text = "";
            dtTrCenter.Text = "";
            data = null;
            deleteIndex = null;

            deleteTransaction.Visibility = Visibility.Hidden;
            currentTr = Transaction.none;
        }

        private void ConnectToDelete()
        {
            string sqlCommand = "DELETE FROM TransactionData WHERE Found_Center=@fc And Center=@c And Sum=@s And Text=@t";
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
                    //Return error
                    MessageBox.Show("Error can't connect to the server", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                cantClose = true; //Makes the software wait before closing
                for (int i = 0; i < deleteIndex.Length; i++)
                {
                    SqlCommand cmd = new(sqlCommand, conn);
                    List<Items> tempArray;

                    cmd.Parameters.AddWithValue("@fc", data[deleteIndex[i]].fcenter);
                    cmd.Parameters.AddWithValue("@c", data[deleteIndex[i]].center);
                    cmd.Parameters.AddWithValue("@s", data[deleteIndex[i]].sum);
                    cmd.Parameters.AddWithValue("@t", data[deleteIndex[i]].text);
                    cmd.ExecuteNonQuery();

                    //Copys the array so the items that can be deleted will be removed
                    tempArray = data.ToList();
                    tempArray.RemoveAt(deleteIndex[i]);
                    data = new Items[tempArray.Count];

                    for (int a = 0; a < data.Length; a++)
                    {
                        data[a] = tempArray[a];
                    }

                    Dispatcher.Invoke(() =>
                    {
                        dataDTran.ItemsSource = null;
                        dataDTran.ItemsSource = data;
                    });
                }

                conn.Close();
            }

            cantClose = false; //We finished deleting
        }

        private void DeleteBtnClicked(object sender, MouseButtonEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please check your internet connection or contact the adminastrator to check the server state", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            deleteTransaction.Visibility = Visibility.Visible;
            currentTr = Transaction.delete;
            ShowHideMenu();
        }

        private void DeleteTrSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            deleteTrSaveBtn.IsEnabled = false;

            Thread thread = new(() => { ConnectToDelete(); });
            thread.Start();
        }

        private void DataDTran_CurrentCellChanged(object sender, EventArgs e)
        {
            DataGridCellInfo cell = dataDTran.CurrentCell;
            var selectedIndexes = new List<int>();
            //int tempIndex = dataDTran.Items.IndexOf(cell.Item);

            foreach (var selItem in dataDTran.SelectedItems)
            {
                selectedIndexes.Add(dataDTran.Items.IndexOf(selItem));
            }

            if (HasNull(selectedIndexes) || IsDifferent(deleteIndex.ToList(), selectedIndexes))
            {
                deleteIndex = new int[selectedIndexes.Count];

                for (int i = 0; i < deleteIndex.Length; i++)
                {
                    deleteIndex[i] = selectedIndexes[i];
                }
            }

            deleteTrSaveBtn.IsEnabled = true;
        }

        private void DeleteTrClearBtn(object sender, RoutedEventArgs e)
        {
            dataDTran.ItemsSource = null;
            deleteTrSaveBtn.IsEnabled = false;
            dtTrFoundCenter.Text = "";
            dtTrCoin.Text = "";
            dtTrCenter.Text = "";
        }

        private void SearchTrDBtn_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new(() => { DTr_ConnectToData(); });
            thread.Start();
        }

        private void DTr_ConnectToData()
        {
            string sqlCommand = "Select Found_Center, Center, Sum, Text from TransactionData";

            SqlConnection conn = new(connectionString);
            List<Items> tempData = new();

            //Thread safety
            lock (conn)
            {
                try
                {
                    conn.Open();
                }
                catch (Exception)
                {
                    //Return error
                    MessageBox.Show("Error can't connect to the server", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                SqlCommand cmd = new(sqlCommand, conn);
                SqlDataReader dataReader = cmd.ExecuteReader();

                while (dataReader.Read())
                {
                    string fcenter = dataReader.GetString(0);
                    string center = dataReader.GetString(1);
                    double sum = dataReader.GetDouble(2);
                    string text = dataReader.GetString(3);

                    tempData.Add(new Items(fcenter, Convert.ToInt32(center), Convert.ToInt32(sum), text));
                }

                conn.Close();

                Dispatcher.Invoke(() =>
                {
                    //Note: add to the list the data
                    if (!String.IsNullOrEmpty(dtTrCenter.Text))
                    {
                        for (int i = 0; i < tempData.Count; i++)
                        {
                            Items items = tempData[i];

                            if (!items.center.ToString().Equals(dtTrCenter.Text))
                            {
                                tempData.RemoveAt(i);
                            }
                        }

                        //Slow
                        //tempData = tempData.Where(x => x.center == Convert.ToInt32(dtTrCenter.Text)).ToList();
                    }

                    if (!String.IsNullOrEmpty(dtTrCoin.Text))
                    {
                        //found centers must be of type xxxx0/1/2x...x
                        //so the coin will always be in the 4 index

                        for (int i = 0; i < tempData.Count; i++)
                        {
                            Items items = tempData[i];

                            if (!items.fcenter[4].ToString().Equals(dtTrCoin.Text))
                            {
                                tempData.RemoveAt(i);
                            }
                        }

                        //Slow
                        //tempData = tempData.Where(x => Convert.ToInt32(x.fcenter[4].ToString()) == Convert.ToInt32(dtTrCoin.Text)).ToList();
                    }

                    if (!String.IsNullOrEmpty(dtTrFoundCenter.Text))
                    {
                        //the coin is on the 4 index so after that

                        for (int i = 0; i < tempData.Count; i++)
                        {
                            Items items = tempData[i];

                            if (!items.fcenter.Trim()[5..].ToString().Equals(dtTrFoundCenter.Text[^1].ToString()))
                            {
                                tempData.RemoveAt(i);
                            }
                        }

                        //Slow
                        //tempData = tempData.Where(x => Convert.ToInt32((x.fcenter.Trim().Substring(5))) == Convert.ToInt32(((dtTrFoundCenter.Text[dtTrFoundCenter.Text.Length - 1]).ToString()))).ToList();
                    }
                });
            }

            data = tempData.ToArray();

            Dispatcher.Invoke(() =>
            {
                dataDTran.ItemsSource = null;
                dataDTran.ItemsSource = data;
            });
        }
        #endregion

        #region SaveTransaction
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
                        MessageBox.Show("One of the founds are not correct", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (!fc)
                    {
                        MessageBox.Show("One of the found centers are not correct", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (!cSum)
                    {
                        MessageBox.Show("One of the founds are not correct", window.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return;
                }
            }

            cantClose = true; //Makes the software wait before closing
            new Thread(() =>
            {
                SqlConnection conn = new(connectionString);
                string sqlCommand = "INSERT INTO TransactionData(Found_Center, Center, Sum, Text) VALUES(@FC, @C, @S, @T)";

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
                cantClose = false; //We finished saving

                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        addTrSaveBtn.IsEnabled = false;

                        //Clean the table and disable the button
                        dataCTran.ItemsSource = null;
                        items.ForEach(x => x.Clean());
                        dataCTran.ItemsSource = items;
                        discardBtnAddTr.IsEnabled = false;
                    });
                }
                catch (Exception)
                {
                    //Do nothing
                }
            }).Start();
        }

        private void DataCTran_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            addTrSaveBtn.IsEnabled = true;
            discardBtnAddTr.IsEnabled = true;
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
        #endregion

    }
}
