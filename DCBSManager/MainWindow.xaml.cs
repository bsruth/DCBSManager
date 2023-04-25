using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;

namespace DCBSManager
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ListLoader mLL = new ListLoader();
        CostCalculator _dcbsCostCalc = null;
        CostCalculator _maybeCostCalc = null;
        CostCalculator _retailCostCalc = null;
        CostCalculator _overallCostCalc = null;
        CostCalculator _notReceivedCostCalc = null;
        DCBSItem _selectedItem = null;

        public MainWindow()
        {
            InitializeComponent();

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
            _notReceivedCostCalc = new CostCalculator(mLL.NotReceivedItems, PurchaseCategories.NotReceived)
            {
                RetailTaxPercentage = 0.093,
                ShippingCost = 7.50,
                IndividualBagBoardCost = 0.12
            };

            _dcbsTotal.DataContext = _dcbsCostCalc;
            _maybeTotal.DataContext = _maybeCostCalc;
            _retailTotal.DataContext = _retailCostCalc;
            _overallTotal.DataContext = _overallCostCalc;
            _notReceivedTotal.DataContext = _notReceivedCostCalc;

           ListSelection.ItemsSource = ListLoader.GetAvailableDatabases();
           ListSelection.SelectedIndex = 0;
        }

        private async void titleSearch_Search(object sender, RoutedEventArgs e)
        {
            var searchTextBox = sender as SearchTextBox;
            if (searchTextBox != null)
            {
                DCBSList.ItemsSource = await ListLoader.FilterList(searchTextBox.Text, mLL.LoadedItems);
                DCBSList.ScrollIntoView(_selectedItem);
                DCBSList.SelectedItem = _selectedItem;
                if (String.IsNullOrEmpty(searchTextBox.Text))
                {
                    DCBSList.Focus();
                }
            }
        }

        private async void goToDCBSUpload_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://www.dcbservice.com/cart/orderupload";
            var excelFile = await mLL.PrepareDCBSOrderExcelFileForUpload(mLL.GetSelectedItems(), ListLoader.OrderStore.DCBS);
            if(excelFile == string.Empty)
            {
                MessageBox.Show("Could not find DCBS excel file");
                return;
            }
            Clipboard.SetText(excelFile);
            Process.Start(new ProcessStartInfo(url));
        }
        private async void deepDiscount_Click(object sender, RoutedEventArgs e)
        {
            var excelFile = await mLL.PrepareDCBSOrderExcelFileForUpload(mLL.GetSelectedItems(), ListLoader.OrderStore.DeepDiscount);
            if(excelFile == string.Empty)
            {
                MessageBox.Show("Could not find Deep Discount excel file");
                return;
            }
            Clipboard.SetText(excelFile);
        }

        private void ListSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            var newSelectedList = e.AddedItems[0] as DCBSList;
            if(newSelectedList == mLL.CurrentList)
            {
                return;
            }

            var results = Task.Run(async () =>
            {
                return await mLL.LoadList(newSelectedList);
            }).ContinueWith(taskResult =>
            {
                App.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        DCBSList.ItemsSource = taskResult.Result;
                    }
                    catch (Exception ex)
                    {
                        var str = ex.ToString();
                    }
                }));
            });



        }

        private void maybeFilter_Click(object sender, RoutedEventArgs e)
        {
            DCBSList.ItemsSource = mLL.FilterToPurchaseCategory(PurchaseCategories.Maybe);
        }

        private void definiteFilter_Click(object sender, RoutedEventArgs e)
        {
            DCBSList.ItemsSource = mLL.FilterToPurchaseCategory(PurchaseCategories.Retail, PurchaseCategories.Definite, PurchaseCategories.Received, PurchaseCategories.Matt );
        }
        private void notReceivedFilter_Click(object sender, RoutedEventArgs e)
        {
            DCBSList.ItemsSource = mLL.FilterToPurchaseCategory(PurchaseCategories.Definite, PurchaseCategories.Retail);
        }

        private void _dcbsFilter_Click(object sender, RoutedEventArgs e)
        {
            DCBSList.ItemsSource = mLL.FilterToPurchaseCategory(PurchaseCategories.Definite, PurchaseCategories.Matt);
        }
        
        private void _retailFilter_Click(object sender, RoutedEventArgs e)
        {
            DCBSList.ItemsSource = mLL.FilterToPurchaseCategory(PurchaseCategories.Retail);
        }

        private async void _showAllItems_Click(object sender, RoutedEventArgs e)
        {
            var searchString = titleSearch.Text;
            DCBSList.ItemsSource = await ListLoader.FilterList(searchString, mLL.LoadedItems);
        }

        private void DCBSList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                _selectedItem = e.AddedItems[0] as DCBSItem;
            }
        }

        private void _updateCheckButton_Click(object sender, RoutedEventArgs e)
        {
          
                var newListName = ListLoader.CheckForUpdatedList();
                if (String.IsNullOrEmpty(newListName))
                {
                    MessageBox.Show("No new list found.");
                    return;
                }

                if (MessageBox.Show("New list available: " + System.IO.Path.GetFileNameWithoutExtension(newListName) + " Download?",
                    "New List Found", MessageBoxButton.YesNo) == MessageBoxResult.No)
                { 
                    return;
                }
            DCBSList newSelectedList;
            if (File.Exists(newListName))
            {
                newSelectedList = new DCBSList(Path.GetFileNameWithoutExtension(newListName));
            }
            else
            {
                newSelectedList = ListLoader.DownloadList(newListName);
            }
            var results = Task.Run(async () =>
            {
                return await mLL.LoadList(newSelectedList);
            }).ContinueWith(taskResult =>
            {
                App.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        DCBSList.ItemsSource = taskResult.Result;
                        ListSelection.ItemsSource = ListLoader.GetAvailableDatabases();
                        ListSelection.SelectedIndex = 0;
                    }
                    catch (Exception ex)
                    {
                        var str = ex.ToString();
                    }
                }));
            });

        }

        private void DCBSList_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var item = DCBSList.SelectedItem as DCBSItem;
            
            if(item != null && e.Key == System.Windows.Input.Key.Space)
            {
                item.MoveToNextPurchaseCategory();
            }
        }
    }
}
