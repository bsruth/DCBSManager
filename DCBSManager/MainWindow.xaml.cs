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

        ListLoader mLL = null;

        public MainWindow()
        {
            InitializeComponent();
            mLL = new ListLoader();
            


            //load available databases
            var fileList = mLL.GetAvailableDatabases();

            this.ListSelection.ItemsSource = mLL.GetAvailableDatabases();
            this.ListSelection.SelectedIndex = 0;
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
            

        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] as TabItem != null)
                {
                    TabItem selectedTab = (TabItem)e.AddedItems[0];
                    if (string.Compare(selectedTab.Header.ToString(), "Cart", true) == 0)
                    {
                        mLL.AddToCart(webBrowser);
                    }
                    else if (string.Compare(selectedTab.Header.ToString(), "Selected Items", true) == 0)
                    {
                        SelectedList.ItemsSource = mLL.GetSelectedItems();
                    }
                }
            }
        }

        /// <summary>
        /// Rotates through the different purchase categories for an item whenever it is clicked
        /// TODO: This should be a command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DCBSItemView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var dcbsItemView = sender as DCBSItemView;
            if(dcbsItemView != null) {
                var listItem = dcbsItemView.DataContext as DCBSItem;
                if(listItem != null) {
                    listItem.PurchaseCategory = (PurchaseCategories)(((Int64)(listItem.PurchaseCategory) + 1) % 4);
                }
            }

            e.Handled = true;

        }

        private async void titleSearch_Search(object sender, RoutedEventArgs e)
        {
            var searchTextBox = sender as SearchTextBox;
            if (searchTextBox != null)
            {
                var searchText = searchTextBox.Text;
                this.DCBSList.ItemsSource = await mLL.FilterList(searchText);
            }
        }

        private async void dumpSelectedItemsToExcel_Click(object sender, RoutedEventArgs e)
        {
            await mLL.DumpTabSeparatedValues("dumpFile.txt", mLL.GetSelectedItems());
        }

        private void ListSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            this.Cursor = Cursors.Wait;

            

            var results = Task.Run(async () =>
            {

                return await mLL.LoadList(e.AddedItems[0].ToString());
            }).ContinueWith(taskResult =>
            {
                App.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        this.DCBSList.ItemsSource = taskResult.Result;
                        this.Cursor = Cursors.Arrow;
                    }
                    catch (Exception ex)
                    {
                        var str = ex.ToString();
                    }
                }));
            });            

        }
    }
}
