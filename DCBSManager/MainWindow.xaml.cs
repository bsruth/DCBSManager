using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Collections.Generic;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Data.SQLite;

namespace DCBSManager
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       

        public MainWindow()
        {
            InitializeComponent();
            this.webBrowser.Navigated += webBrowser_Navigated;

            ListLoader mLL = new ListLoader("August2013");
            //ClearDB("August2013");

            //SetupDatabase("August2013");
            /*
            string line;
            // Read the file and display it line by line.
            System.IO.StreamReader file =
                new System.IO.StreamReader(@"codes.txt");
            while ((line = file.ReadLine()) != null)
            {
                codes.Add(line);
                
            }

            file.Close();
            */


            this.DCBSList.ItemsSource = mLL.mLoadedItems;
            /*
            this.Cursor = Cursors.Wait;
            
            var results = Task.Run(() => { return LoadXLS(); })
                .ContinueWith(taskResult => {
                    App.Current.Dispatcher.BeginInvoke( (Action)(() =>
                    {
                        this.DCBSList.ItemsSource = taskResult.Result;
                        this.Cursor = Cursors.Arrow;
                    }));
                });
            */
            
            //pids = GetPIDS(codes.ToArray());

            

           // AddToCart();
            
        }

        void webBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            AddToCart();
        }

        public void AddToCart()
        {

            /*if (pidIndex < pids.Length)
            {
                string addToCartURI = "http://www.dcbservice.com/_cart.aspx?id=" + pids[pidIndex];
                this.webBrowser.Navigate(addToCartURI);
                pidIndex++;
            }*/
        }

        private void DCBSList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var send = sender as ListView;

            var selectedItems = send.SelectedItems;
           // mSelectedItems = selectedItems.Cast<DCBSItem>().ToList();
            e.Handled = true;

        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           /* if (e.AddedItems[0] as TabItem != null)
            {
                TabItem selectedTab = (TabItem)e.AddedItems[0];
                if (string.Compare(selectedTab.Header.ToString(),"Web Browser", true) == 0)
                {
                    codes.Clear();
                    foreach (var item in mSelectedItems)
                    {
                        codes.Add(item.CODE);
                    }

                    pids = GetPIDS(codes.ToArray());

                    AddToCart();
                }
            }*/
        }
    }
}
