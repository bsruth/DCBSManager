using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;

namespace DCBSManager
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ListLoader mLL = null;
        CostCalculator _dcbsCostCalc = null;
        CostCalculator _maybeCostCalc = null;
        CostCalculator _retailCostCalc = null;
        CostCalculator _overallCostCalc = null;

        public MainWindow()
        {
            InitializeComponent();
            mLL = new ListLoader();


            DataContext = mLL;

            _dcbsCostCalc = new CostCalculator(mLL.DefiniteItems, PurchaseCategories.Definite)
            {
                ShippingCost = 7.50,
                IndividualBagBoardCost = 0.12
            };

            _maybeCostCalc = new CostCalculator(mLL.MaybeItems, PurchaseCategories.Maybe)
            {
                IndividualBagBoardCost = 0.12
            };

            _retailCostCalc = new CostCalculator(mLL.RetailItems, PurchaseCategories.Retail)
            {
                RetailTaxPercentage = 0.093
            };

            _overallCostCalc = new CostCalculator(mLL.PurchaseItems, PurchaseCategories.Total)
            {
                RetailTaxPercentage = 0.093,
                ShippingCost = 7.50,
                IndividualBagBoardCost = 0.12
            };

            _dcbsTotal.DataContext = _dcbsCostCalc;
            _maybeTotal.DataContext = _maybeCostCalc;
            _retailTotal.DataContext = _retailCostCalc;
            _overallTotal.DataContext = _overallCostCalc;

           ListSelection.ItemsSource = ListLoader.GetAvailableDatabases();
            ListSelection.SelectedIndex = 0;
        }

        private async void titleSearch_Search(object sender, RoutedEventArgs e)
        {
            var searchTextBox = sender as SearchTextBox;
            if (searchTextBox != null)
            {
                var searchText = searchTextBox.Text;
                var visibleItems = DCBSList.ItemsSource as List<DCBSItem>;
                if ( (visibleItems != null) && (searchText != ""))
                {
                    this.DCBSList.ItemsSource = await ListLoader.FilterList(searchText, visibleItems);
                }
                else
                {
                    this.DCBSList.ItemsSource = await ListLoader.FilterList(searchText, mLL.LoadedItems);
                }
            }
        }

        private async void goToDCBSUpload_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://www.dcbservice.com/cart/orderupload";
            var excelFile = await mLL.PrepareDCBSOrderExcelFileForUpload(mLL.GetSelectedItems());
            Clipboard.SetText(excelFile);
            Process.Start(new ProcessStartInfo(url));
        }

        private void ListSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            this.Cursor = Cursors.Wait;


            var selectedList = e.AddedItems[0] as DCBSList;

            if(selectedList.ListItemKey == DCBSManager.DCBSList.ListItemKeys.NewList)
            {
                var newListName = ListLoader.CheckForUpdatedList();
                if(String.IsNullOrEmpty(newListName))
                {
                    MessageBox.Show("No new list found.");
                    return;
                }

                if(MessageBox.Show("New list available: " + System.IO.Path.GetFileNameWithoutExtension(newListName) + " Download?", 
                    "New List Found", MessageBoxButton.YesNo) == MessageBoxResult.Yes )
                {
                    ListLoader.DownloadList(newListName);
                    selectedList = new DCBSList
                    {
                        ListBaseFileName = System.IO.Path.GetFileNameWithoutExtension(newListName),
                        ListItemKey = DCBSManager.DCBSList.ListItemKeys.Database
                    };
                }
            }

            var results = Task.Run(async () =>
            {

                return await mLL.LoadList(selectedList);
            }).ContinueWith(taskResult =>
            {
                App.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        this.DCBSList.ItemsSource = taskResult.Result;

                        var b = e.AddedItems[0] as DCBSList;

                        if (b != null && b.ListItemKey == DCBSManager.DCBSList.ListItemKeys.NewList)
                        {
                            this.ListSelection.ItemsSource = ListLoader.GetAvailableDatabases();
                            this.ListSelection.SelectedIndex = 0;
                        }
                        this.Cursor = Cursors.Arrow;
                    }
                    catch (Exception ex)
                    {
                        var str = ex.ToString();
                    }
                }));
            });



        }

        private async void maybeFilter_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = mLL.GetSelectedItems();
            var maybeItems = selectedItems
                .Where(item => item.PurchaseCategory == PurchaseCategories.Maybe);
            var searchString = titleSearch.Text;
            DCBSList.ItemsSource = await ListLoader.FilterList(searchString, maybeItems.ToList());
        }

        private async void definiteFilter_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = mLL.GetSelectedItems();
            var definiteItems = selectedItems
                .Where(item => ( (item.PurchaseCategory == PurchaseCategories.Retail) || (item.PurchaseCategory == PurchaseCategories.Definite)));
            var searchString = titleSearch.Text;
            DCBSList.ItemsSource = await ListLoader.FilterList(searchString, definiteItems.ToList());
        }

        private async void _dcbsFilter_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = mLL.GetSelectedItems();
            var dcbsItems = selectedItems
                .Where(item => (item.PurchaseCategory == PurchaseCategories.Definite));
             var searchString = titleSearch.Text;
            DCBSList.ItemsSource = await ListLoader.FilterList(searchString, dcbsItems.ToList());
        }

        private async void _retailFilter_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = mLL.GetSelectedItems();
            var definiteItems = selectedItems
                .Where(item => (item.PurchaseCategory == PurchaseCategories.Retail));
            var searchString = titleSearch.Text;
            DCBSList.ItemsSource = await ListLoader.FilterList(searchString, definiteItems.ToList());
        }

        private async void _showAllItems_Click(object sender, RoutedEventArgs e)
        {
            var searchString = titleSearch.Text;
            DCBSList.ItemsSource = await ListLoader.FilterList(searchString, mLL.LoadedItems);
        }
    }
}
