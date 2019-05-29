using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace DCBSManager
{
    /// <summary>
    /// Interaction logic for DCBSItem.xaml
    /// </summary>
    public partial class DCBSItemView : UserControl
    {
        public DCBSItemView()
        {
            InitializeComponent();
        }

        private void ViewItemInBrowser(object sender, RoutedEventArgs e)
        {
            var dcbsItem = this.DataContext as DCBSItem;
            if (dcbsItem != null)
            {
                string url = "https://www.dcbservice.com/search/" + dcbsItem.DCBSOrderCode;
                Process.Start(new ProcessStartInfo(url));
            }
            e.Handled = true;
        }

        private void _categoryChangeBtn_Click(object sender, RoutedEventArgs e)
        {
           
                var item = DataContext as DCBSItem;
                if (item != null)
                {
                    int numberOfSelectableCategories = Enum.GetNames(typeof(PurchaseCategories)).Length - 1;
                    item.PurchaseCategory = (PurchaseCategories)(((Int64)(item.PurchaseCategory) + 1) % numberOfSelectableCategories);
                }
        
        }
    }
}
