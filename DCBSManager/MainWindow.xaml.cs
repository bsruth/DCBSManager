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

namespace DCBSManager
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> codes = new List<string>();
        List<DCBSItem> mSelectedItems = new System.Collections.Generic.List<DCBSItem>();

        const int CODE = 1;
        const int TITLE = 3;
        const int COST = 4;
        const int DISC = 5;
        const int DCBS = 6;

        string[] pids = null;
        int pidIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
            this.webBrowser.Navigated += webBrowser_Navigated;

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
            codes = LoadCodes("codes.txt");


            this.Cursor = Cursors.Wait;
            //var dcbsList = LoadXLS();
            var results = Task.Run(() => { return LoadXLS(); })
                .ContinueWith(taskResult => {
                    App.Current.Dispatcher.BeginInvoke( (Action)(() =>
                    {
                        this.DCBSList.ItemsSource = taskResult.Result;
                        this.Cursor = Cursors.Arrow;
                    }));
                });
            
            
            //pids = GetPIDS(codes.ToArray());

            

           // AddToCart();
            
        }

        List<string> LoadCodes( string codeFile)
        {
            List<string> myCodes = new List<string>();
            string line;
            // Read the file and display it line by line.
            System.IO.StreamReader file =
                new System.IO.StreamReader(codeFile);
            while ((line = file.ReadLine()) != null)
            {
                myCodes.Add(line);

            }

            return myCodes;
        }

        List<DCBSItem> LoadXLS()
        {
            string path = "C:\\users\\brian_000\\desktop\\DCBS\\August2013.xls";
            List<DCBSItem> mDCBSItems = new List<DCBSItem>();
            string currentCategory = "Previews";
            bool headerProcessed = false;
            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var hssfworkbook = new HSSFWorkbook(file);
                ISheet sheet = hssfworkbook.GetSheetAt(0);
                System.Collections.IEnumerator rows = sheet.GetRowEnumerator();

                while (rows.MoveNext())
                {
                    IRow row = (HSSFRow)rows.Current;
                    if (DISC <= row.LastCellNum)
                    {
                        DCBSItem newItem = new DCBSItem();
                        var cell = row.GetCell(CODE);
                        if (cell != null)
                        {
                            string codeString = cell.ToString();
                            if (String.Compare(codeString, "Previews", true) == 0)
                            {
                                headerProcessed = true;
                            }
                            else
                            {

                                string codePattern = @"[A-Z]{3}[0-9]{6}[A-Z]*";
                                var matchResult = Regex.Match(codeString, codePattern);
                                if (matchResult.Success)
                                {
                                    //if (currentCategory == "Image Comics")
                                    if(codes.Contains(codeString))
                                    {
                                        newItem.CODE = codeString;
                                        newItem.Title = row.GetCell(TITLE).ToString();
                                        newItem.cost = row.GetCell(COST).ToString();
                                        newItem.discount = row.GetCell(DISC).ToString();
                                        newItem.dcbsprice = row.GetCell(DCBS).ToString();
                                        newItem.category = currentCategory;
                                        newItem.LoadInfo();
                                        mDCBSItems.Add(newItem);
                                    }
                                }
                                else
                                {
                                    if (headerProcessed && (codeString != ""))
                                    {
                                        currentCategory = codeString;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return mDCBSItems;
        }

        void webBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            AddToCart();
        }

        public void AddToCart()
        {

            if (pidIndex < pids.Length)
            {
                string addToCartURI = "http://www.dcbservice.com/_cart.aspx?id=" + pids[pidIndex];
                this.webBrowser.Navigate(addToCartURI);
                pidIndex++;
            }
        }

        public string[] GetPIDS(string[] codesToGet)
        {
            string[] pids = new string[codesToGet.Length];
            try
            {
                for (int codeIndex = 0; codeIndex < codesToGet.Length; ++codeIndex)
                {
                    string uri = "http://www.dcbservice.com/search.aspx?search=" + codesToGet[codeIndex];
                    System.Net.WebRequest req = System.Net.WebRequest.Create(uri);
                    req.Proxy = null;
                    System.Net.WebResponse resp = req.GetResponse();
                    System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());

                    var pageText = sr.ReadToEnd().Trim();


                    string pattern = @"category\.aspx\?id=\d+\&pid=(\d+)\'>";
                    MatchCollection matches;

                    Regex defaultRegex = new Regex(pattern);
                    // Get matches of pattern in text
                    matches = defaultRegex.Matches(pageText);

                    if (matches.Count >= 2 && matches[1].Groups.Count >= 2)
                    {
                        pids[codeIndex] = matches[1].Groups[1].ToString();
                    }
                }

            }
            catch (Exception ex)
            {
                var e = ex.ToString();
            }

            return pids;

        }

        private void DCBSList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var send = sender as ListView;

            var selectedItems = send.SelectedItems;
            mSelectedItems = selectedItems.Cast<DCBSItem>().ToList();
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
