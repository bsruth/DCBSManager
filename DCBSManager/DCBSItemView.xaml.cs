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
                string url = "http://www.dcbservice.com/category.aspx?pid=" + dcbsItem.PID.ToString();
                Process.Start(new ProcessStartInfo(url));
            }
            e.Handled = true;
        }
    }
}
