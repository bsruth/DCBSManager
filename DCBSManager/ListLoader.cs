using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Web;

namespace DCBSManager
{
    public class DCBSList
    {
        public enum ListItemKeys
        {
            Database,
            NewList
        }

        public ListItemKeys ListItemKey;
        public String ListBaseFileName;
        public String ListItemString;

        public override String ToString()
        {
            return ListItemString;
        }
    }

    public class ListLoader : INotifyPropertyChanged
    {

        List<string> codes = new List<string>();
        List<DCBSItem> mSelectedItems = new System.Collections.Generic.List<DCBSItem>();
        Queue<DCBSItem> mItemsLeftToAddToCart = new Queue<DCBSItem>();
        WebBrowser mWebBrowser = new WebBrowser();


        ObservableCollection<DCBSItem> _definiteItems = new ObservableCollection<DCBSItem>();
        ObservableCollection<DCBSItem> _maybeItems = new ObservableCollection<DCBSItem>();
        ObservableCollection<DCBSItem> _retailItems = new ObservableCollection<DCBSItem>();
        ObservableCollection<DCBSItem> _purchaseItems = new ObservableCollection<DCBSItem>();

        int _retailItemsCount = 0;
        int _maybeItemsCount = 0;
        bool _loadingNewList = false;
        string _listLoadingText = "Loading";

        const string all_items_table_name = "all_items";
        const string col_code = "code";
        const string col_title = "title";
        const string col_retail = "retail";
        const string col_discount = "discount";
        const string col_category = "category";
        const string col_dcbsprice = "dcbs_price";
        const string col_pid = "pid";
        const string col_description = "description";
        const string col_thumbnail = "thumbnail";
        const string col_purchase_category = "purchase_category";


        public string mDatabaseName;

        const int CODE = 1;
        const int TITLE = 3;
        const int COST = 4;
        const int DISC = 5;
        const int DCBS = 6;


        #region Properties

        public List<DCBSItem> LoadedItems { get; set; }

        public bool NewListLoading
        {
            get
            {
                return _loadingNewList;
            }

            set
            {
                if (value != _loadingNewList)
                {
                    _loadingNewList = value;
                    OnPropertyChanged("NewListLoading");
                }
            }
        }

        public string ListLoadingText
        {
            get
            {
                return _listLoadingText;
            }

            set
            {
                if (value != _listLoadingText)
                {
                    _listLoadingText = value;
                    OnPropertyChanged("ListLoadingText");
                }
            }
        }

        public ObservableCollection<DCBSItem> DefiniteItems
        {
            get
            {
                return _definiteItems;
            }

            private set
            {
                if (value != null)
                {
                    _definiteItems = value;
                    OnPropertyChanged("DefiniteItems");
                }
            }
        }

        public ObservableCollection<DCBSItem> MaybeItems
        {
            get
            {
                return _maybeItems;
            }

            private set
            {
                if (value != null)
                {
                    _maybeItems = value;
                    OnPropertyChanged("MaybeItems");
                }
            }
        }

        public ObservableCollection<DCBSItem> PurchaseItems
        {
            get
            {
                return _purchaseItems;
            }

            private set
            {
                if (value != null)
                {
                    _purchaseItems = value;
                    OnPropertyChanged("PurchaseItems");
                }
            }
        }

        public ObservableCollection<DCBSItem> RetailItems
        {
            get
            {
                return _retailItems;
            }

            private set
            {
                if (value != null)
                {
                    _retailItems = value;
                    OnPropertyChanged("RetailItems");
                }
            }
        }

        public int MaybeItemsCount
        {
            get
            {
                return _maybeItemsCount;
            }
            private set
            {
                if (_maybeItemsCount != value)
                {
                    _maybeItemsCount = value;
                    OnPropertyChanged("MaybeItemsCount");
                }
            }
        }
        public int RetailItemsCount
        {
            get
            {
                return _retailItemsCount;
            }
            private set
            {
                if (_retailItemsCount != value)
                {
                    _retailItemsCount = value;
                    OnPropertyChanged("RetailItemsCount");
                }
            }
        }
        #endregion


        public ListLoader()
        {
        }


        public async Task<List<DCBSItem>> LoadList(DCBSList listName)
        {
            NewListLoading = true;

            string databaseFileName = listName.ListBaseFileName + ".sqlite";
            if (File.Exists(databaseFileName))
            {
                LoadedItems = await LoadFromDatabase(listName.ListBaseFileName);
                mDatabaseName = listName.ListBaseFileName;
            }
            else
            {
                SetupDatabase(listName.ListBaseFileName);
                //codes = LoadCodes("codes.txt");
                mDatabaseName = listName.ListBaseFileName;
                LoadedItems = await LoadXLS(listName.ListBaseFileName, "");
            }


            UpdateCategoryLists(ref _definiteItems, PurchaseCategories.Definite);
            UpdateCategoryLists(ref _maybeItems, PurchaseCategories.Maybe);
            UpdateCategoryLists(ref _retailItems, PurchaseCategories.Retail);
            UpdateCategoryLists(ref _purchaseItems, PurchaseCategories.Retail, PurchaseCategories.Definite);

            NewListLoading = false;
            return LoadedItems;
        }


        private void UpdateCategoryLists(ref ObservableCollection<DCBSItem> collectionToUpdate, params PurchaseCategories[] categoryToUpdate) {
            collectionToUpdate.Clear();
            var itemsToAdd = from item in LoadedItems where categoryToUpdate.Contains(item.PurchaseCategory) select item;
            foreach (var item in itemsToAdd)
            {
                collectionToUpdate.Add(item);
            }

        }

        public static void DownloadList( string fileName)
        {
            //download excel file
            string fileURL = "http://media.dcbservice.com/downloads/" + fileName;
            var downloadClient = new WebClient();
            downloadClient.DownloadFile(fileURL, ".\\" + fileName);
        }
        public static String CheckForUpdatedList()
        {      

            try
            {
                string updatedListURL = "http://www.dcbservice.com/downloads.aspx";

                System.Net.WebRequest req = System.Net.WebRequest.Create(updatedListURL);
                req.Proxy = null;
                System.Net.WebResponse resp = req.GetResponse();
                System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());

                var pageText = sr.ReadToEnd().Trim();
                //<a href="http://media.dcbservice.com/downloads/August2013.xls">
                string dynamicContentPattern = @"(\w+\.xls)";

                MatchCollection dynamicContentMatches;

                Regex dynamicContentRegex = new Regex(dynamicContentPattern);
                // Get matches of pattern in text
                dynamicContentMatches = dynamicContentRegex.Matches(pageText);

                foreach(Match xlsfile in dynamicContentMatches)
                {
                   
                    string fileName = xlsfile.Groups[1].ToString();
                    var availableDatabases = GetAvailableDatabases();
                    
                        bool found = false;
                       foreach (var db in availableDatabases) {
                            if (db.ListBaseFileName.Equals(Path.GetFileNameWithoutExtension(fileName)) == true)
                            {
                                found = true;
                                break;
                            }
                             
                        }

                       if (found == false)
                       {
                        return fileName;
                       }


                    
                }
            }
            catch (Exception ex)
            {
                var blah = ex.ToString();
            }

            return "";

        }
        public static  List<DCBSList> GetAvailableDatabases()
        {
            var workingDir = new DirectoryInfo(@".");
            var files = from file in workingDir.EnumerateFiles(@"*.sqlite")
                        select Path.GetFileNameWithoutExtension(file.Name);

            var filesList = new List<DCBSList>();
            foreach(var file in files) {
                var dcbsList = new DCBSList();
                dcbsList.ListBaseFileName = file;
                dcbsList.ListItemKey = DCBSList.ListItemKeys.Database;

                             string fileNamePattern = @"([a-zA-Z]+)(\d{4})";
                MatchCollection fileNameMatches;
                Regex filenameRegex = new Regex(fileNamePattern);
                fileNameMatches = filenameRegex.Matches(file);
                if(fileNameMatches.Count == 1) {
                    dcbsList.ListItemString = fileNameMatches[0].Groups[2].ToString();
                    dcbsList.ListItemString += "/";
                    dcbsList.ListItemString += DateTime.ParseExact(fileNameMatches[0].Groups[1].ToString(), "MMMM", CultureInfo.CurrentCulture).Month.ToString("00");
                    dcbsList.ListItemString += " " + fileNameMatches[0].Groups[1].ToString();
                }
                else
                {
                    dcbsList.ListItemString = dcbsList.ListBaseFileName;
                }
                filesList.Add(dcbsList);
            }
            var sortedFiles = filesList.OrderByDescending(file => file.ListItemString).ToList();
            sortedFiles.Add(new DCBSList { ListBaseFileName = "Check for Updates...", ListItemKey = DCBSList.ListItemKeys.NewList, ListItemString = "Check for Updates..." });
            return sortedFiles;
        }

        public List<DCBSItem> GetSelectedItems()
        {

            var selectedList = this.LoadedItems
                .Where(item => item.PurchaseCategory != PurchaseCategories.None);

            return selectedList.ToList();
        }

        public List<DCBSItem> GetDCBSCartItems()
        {
            var cartItems = this.LoadedItems
                .Where(item => item.PurchaseCategory == PurchaseCategories.Definite);

            return cartItems.ToList();
        }

        public void AddToCart(WebBrowser webBrowser)
        {
            var cartItems = GetDCBSCartItems();
            mItemsLeftToAddToCart = new Queue<DCBSItem>(cartItems);
            
            mWebBrowser = webBrowser;
            mWebBrowser.Navigated += webBrowser_Navigated;
            if(mItemsLeftToAddToCart.Count > 0) {
                AddToWebBrowser(mWebBrowser, mItemsLeftToAddToCart.Dequeue());
            }
           
        }

        private void AddToWebBrowser(WebBrowser webBrowser, DCBSItem item)
        {
            string addToCartURI = "http://www.dcbservice.com/_cart.aspx?id=" + item.PID;
            webBrowser.Navigate(addToCartURI);
        }

        void webBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (mItemsLeftToAddToCart.Count > 0)
            {
                AddToWebBrowser(mWebBrowser, mItemsLeftToAddToCart.Dequeue());
            }
            else
            {
                mWebBrowser.Navigated -= webBrowser_Navigated;
            }
        }

        void ClearDB(string databaseFileName)
        {
            using (var conn = new SQLiteConnection(@"Data Source=" + databaseFileName + ".sqlite;Version=3;"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DROP TABLE all_items;";
                    var ret = cmd.ExecuteNonQuery();
                }

            }
        }

        public void SetupDatabase(string databaseFileName)
        {
            if (File.Exists(databaseFileName + ".sqlite") == false)
            {
                SQLiteConnection.CreateFile(databaseFileName + ".sqlite");
            }

            using (var conn = new SQLiteConnection(@"Data Source=" + databaseFileName + ".sqlite;Version=3;"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    StringBuilder cmdBuilder = new StringBuilder(); 
                    cmdBuilder.Append(@"CREATE TABLE IF NOT EXISTS ");
                    cmdBuilder.Append(all_items_table_name);
                    cmdBuilder.Append(@" (code TEXT UNIQUE, title TEXT, retail_price REAL, discount TEXT,
                                category TEXT, dcbs_price REAL, pid INTEGER UNIQUE, description TEXT,
                                thumbnail BLOB, purchase_category INTEGER);");
                    cmd.CommandText = cmdBuilder.ToString();
                    
                    var ret = cmd.ExecuteNonQuery();
                }

            }
        }

        public bool AddItemToDatabase(DCBSItem item)
        {
            using (var conn = new SQLiteConnection(@"Data Source=" + mDatabaseName + ".sqlite;Version=3;"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    //cmd.CommandText = "INSERT INTO all_items (code, title) VALUES (" + item.CODE + "," + item.Title + ");";
                    cmd.CommandText = @"INSERT INTO all_items (code, title, retail_price, discount,
                                    category, dcbs_price, pid, description, thumbnail, purchase_category) VALUES
                                    (@CODE,@TITLE,@RETAIL,@DISCOUNT,@CATEGORY,@DCBSPRICE,@PID,
                                    @DESCRIPTION,@THUMBNAIL,@PURCHASECATEGORY);";
                    cmd.Parameters.AddWithValue("@CODE", item.DCBSOrderCode);
                    cmd.Parameters.AddWithValue("@TITLE", item.Title);
                    cmd.Parameters.AddWithValue("@RETAIL", item.RetailPrice);
                    cmd.Parameters.AddWithValue("@DISCOUNT", item.DCBSDiscount);
                    cmd.Parameters.AddWithValue("@CATEGORY", item.Category);
                    cmd.Parameters.AddWithValue("@DCBSPRICE", item.DCBSPrice);
                    cmd.Parameters.AddWithValue("@PID", item.PID);
                    cmd.Parameters.AddWithValue("@DESCRIPTION", item.Description);
                    cmd.Parameters.AddWithValue("@THUMBNAIL", item.ThumbnailRawBytes);
                    cmd.Parameters.AddWithValue("@PURCHASECATEGORY", (Int64)item.PurchaseCategory);
                    
                    try
                    {
                        cmd.ExecuteNonQuery().ToString();
                    }
                    catch (SQLiteException ex)
                    {
                        var blah = ex.ToString();
                        return false;
                    }

                }

            }

            return true;
        }

        public async Task<List<DCBSItem>> LoadFromDatabase(string databaseName)
        {
            ListLoadingText = "Loading from database";
            return await Task.Run(async () =>
            {
                List<DCBSItem> itemsList = new List<DCBSItem>();

                using (var conn = new SQLiteConnection(@"Data Source=" + databaseName + ".sqlite;Version=3;"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        //get the number of items so we can display it
                        cmd.CommandText = "SELECT COUNT(*) FROM all_items";
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        int itemNumber = 1;
                        cmd.CommandText = "SELECT * FROM all_items;";
                        var ret = cmd.ExecuteReader();
                        while (ret.Read())
                        {
                            if(count > 0)
                            {
                                ListLoadingText = "Loading from database (" + itemNumber.ToString() + " of " + count.ToString() + ")";
                            }
                            try
                            {
                                var item = new DCBSItem();
                                item.DCBSOrderCode = ret.GetFieldValue<string>(0);
                                item.Title = HttpUtility.HtmlDecode(ret.GetFieldValue<string>(1));
                                item.RetailPrice = ret.GetFieldValue<double>(2);
                                item.DCBSDiscount = ret.GetFieldValue<string>(3);
                                item.Category = HttpUtility.HtmlDecode(ret.GetFieldValue<string>(4));
                                item.DCBSPrice = ret.GetFieldValue<double>(5);
                                item.PID = ret.GetFieldValue<Int64>(6);
                                item.Description = HttpUtility.HtmlDecode(ret.GetFieldValue<string>(7));
                                if (ret.IsDBNull(8) == true)
                                {
                                    item.ThumbnailRawBytes = null;
                                    item.Thumbnail = await LoadDefaultBitmapImage();
                                }
                                else
                                {
                                    item.ThumbnailRawBytes = ret.GetFieldValue<byte[]>(8);
                                    item.Thumbnail = await BitmapImageFromBytes(item.ThumbnailRawBytes);

                                }

                                item.PurchaseCategory = (PurchaseCategories)ret.GetFieldValue<Int64>(9);
                                item.PurchaseCategoryChanged += ItemPurchaseCategoryChanged;
                                itemsList.Add(item);
                            }
                            catch (Exception ex)
                            {
                                var blah = ex.ToString();
                            }

                            ++itemNumber;
                        }

                    }

                }

                return itemsList;
            });
        }

        private async void ItemPurchaseCategoryChanged(object sender, PurchaseCategoryChangedRoutedEventArgs e)
        {
            var result = await UpdateItemPurchaseCategory(sender as DCBSItem);
        }

        private async Task<bool> UpdateItemPurchaseCategory(DCBSItem itemToUpdate)
        {
            if(itemToUpdate == null)
            {
                return false;
            }

            using (var conn = new SQLiteConnection(@"Data Source=" + mDatabaseName + ".sqlite;Version=3;"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE all_items set purchase_category = " + (Int64)(itemToUpdate.PurchaseCategory) + " where pid = " + itemToUpdate.PID + ";";
                    try
                    {
                        var result = await cmd.ExecuteNonQueryAsync();
                        if(result < 1) {
                            return false;
                        }
                    }
                    catch (Exception)
                    {
                        return false;
                    }               
                }

            }


            UpdateCategoryLists(ref _definiteItems, PurchaseCategories.Definite);
            UpdateCategoryLists(ref _maybeItems, PurchaseCategories.Maybe);
            UpdateCategoryLists(ref _retailItems, PurchaseCategories.Retail);
            UpdateCategoryLists(ref _purchaseItems, PurchaseCategories.Retail, PurchaseCategories.Definite);
            return true;
        }

        /// <summary>
        /// Filters by either title or category.
        /// </summary>
        /// <param name="filterText"></param>
        /// <returns></returns>
        public async Task<List<DCBSItem>> FilterList(string filterText, List<DCBSItem> listToFilter)
        {
            
            var filterTask = Task<List<DCBSItem>>.Factory.StartNew(() =>  {

                return listToFilter.Where(item =>
                {

                    return (item.Title.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                           (item.Category.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0);
                 }).ToList();
            });

            List<DCBSItem> filteredList = await filterTask;

            return filteredList;
        }

        public static async Task<BitmapImage> BitmapImageFromBytes(byte[] bytes)
        {
            BitmapImage image = null;
            MemoryStream stream = null;


           await App.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        stream = new MemoryStream(bytes);
                        stream.Seek(0, SeekOrigin.Begin);
                        System.Drawing.Image img = System.Drawing.Image.FromStream(stream);
                        image = new BitmapImage();
                        image.BeginInit();
                        MemoryStream ms = new MemoryStream();
                        img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                        ms.Seek(0, SeekOrigin.Begin);
                        image.StreamSource = ms;
                        image.StreamSource.Seek(0, SeekOrigin.Begin);
                        image.EndInit();
                    }
                    catch (Exception)
                    {

                    }
                    finally
                    {
                        stream.Close();
                        stream.Dispose();
                    }
                }));


            

            return image;

        }

        public static async Task<BitmapImage> LoadDefaultBitmapImage()
        {
            BitmapImage defaultImage = null;
            await App.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                defaultImage  = new BitmapImage();
                defaultImage .BeginInit();
                defaultImage .UriSource = new Uri("pack://application:,,,/DCBSManager;component/Images/no_image.jpg", UriKind.Absolute);
                defaultImage .EndInit();

            }));

            return defaultImage;
        }

        public void ListItemsInDatabase()
        {
            using (var conn = new SQLiteConnection(@"Data Source=" + mDatabaseName + ".sqlite;Version=3;"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM all_items;";
                    var ret = cmd.ExecuteReader();
                    while (ret.Read())
                    {
                        System.Diagnostics.Debug.Print(ret.GetFieldValue<string>(0).ToString());
                        System.Diagnostics.Debug.Print(ret.GetFieldValue<string>(1).ToString());
                    }
                    var i = 0;
                }

            }
        }
        public List<string> LoadCodes(string codeFile)
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
        
        /// <summary>
        /// Parses the passed DCBS Order Sheet Excel file and builds a list of DCBSItems
        /// in the specified category
        /// </summary>
        /// <param name="filePath">Path to Excel file</param>
        /// <param name="category">Category to extract, or empty string to extract all</param>
        /// <returns>List of DCBSItems in the Excel file matching the category</returns>
        public async Task<List<DCBSItem>> ParseXLSFile(string filePath, string category)
        {
            return await Task.Run(() =>
            {
                List<DCBSItem> mDCBSItems = new List<DCBSItem>();

                string currentCategory = "Previews";
                bool headerProcessed = false;
                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var hssfworkbook = new HSSFWorkbook(file);
                    ISheet sheet = hssfworkbook.GetSheetAt(0);
                    System.Collections.IEnumerator rows = sheet.GetRowEnumerator();

                    while (rows.MoveNext())
                    {
                        IRow row = (HSSFRow)rows.Current;
                        if (DISC <= row.LastCellNum)
                        {
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

                                    const string codePattern = @"[A-Z]{3}[0-9]{6}[A-Z]*";
                                    var matchResult = Regex.Match(codeString, codePattern);
                                    if (matchResult.Success)
                                    {
                                        if (String.IsNullOrEmpty(category) || currentCategory == category)
                                        {
                                            DCBSItem newItem = new DCBSItem();
                                            newItem.DCBSOrderCode = codeString;
                                            newItem.Title = row.GetCell(TITLE).ToString();
                                            newItem.RetailPrice = double.Parse(row.GetCell(COST).ToString(), NumberStyles.Currency);
                                            newItem.DCBSDiscount = row.GetCell(DISC).ToString();
                                            newItem.DCBSPrice = double.Parse(row.GetCell(DCBS).ToString(), NumberStyles.Currency);
                                            newItem.Category = currentCategory;
                                            newItem.PurchaseCategoryChanged += ItemPurchaseCategoryChanged;
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
            });
        }
        /// <summary>
        /// Overload that gets all categories
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task<List<DCBSItem>> ParseXLSFile(string filePath)
        {
            return await ParseXLSFile(filePath, "");
        }

        public async Task<List<DCBSItem>> LoadXLS(string listName, string category)
        {
            string excelFileName = listName + ".xls";
            ListLoadingText = "Parsing " + excelFileName;
            var itemList = await ParseXLSFile(excelFileName, category);

            int itemNumber = 1;
            int totalItems = itemList.Count;
            foreach(var item in itemList)
            {
                ListLoadingText = "Loading " + item.Category + " (" + itemNumber.ToString() + " of " + totalItems.ToString() + ")";
                item.LoadInfo();
                if (item.ThumbnailRawBytes != null)
                {
                    item.Thumbnail = await BitmapImageFromBytes(item.ThumbnailRawBytes);
                }
                else
                {

                    item.Thumbnail = await LoadDefaultBitmapImage();
                }
                item.PurchaseCategoryChanged += ItemPurchaseCategoryChanged;
                AddItemToDatabase(item);
                ++itemNumber;
            }
            return itemList;
        }

        /// <summary>
        /// Selects all items that will be ordered from DCBS in the DCBS order file to prepare it for uploading to the
        /// website.
        /// </summary>
        /// <param name="itemsToDump">List of DCBS items</param>
        /// <returns>The complete path to the excel file.</returns>
        public async Task<string> PrepareDCBSOrderExcelFileForUpload(IList<DCBSItem> itemsToDump)
        {
            return await Task.Run(() =>
            {
                string path = "" + mDatabaseName + ".xls";
                string outPath = Path.GetFullPath(mDatabaseName + "_completed.xls");
                List<DCBSItem> mDCBSItems = new List<DCBSItem>();
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
                            var cell = row.GetCell(CODE);
                            if (cell != null)
                            {
                                string codeString = cell.ToString();
                                if (codeString.Contains("BB1"))
                                {
                                    var checkCell = row.GetCell(0);
                                    checkCell.SetCellValue(1);
                                }
                                else
                                {
                                    foreach (var item in itemsToDump)
                                    {
                                        if (item.PurchaseCategory == PurchaseCategories.Definite)
                                        {
                                            if (codeString == item.DCBSOrderCode)
                                            {
                                                var checkCell = row.GetCell(2);
                                                checkCell.SetCellValue(1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    using (FileStream outFile = new FileStream(outPath, FileMode.Create, FileAccess.Write))
                    {
                        hssfworkbook.Write(outFile);
                    }

                }
                return outPath;
            });
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

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        //*************************************
        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }


        #endregion
    }
}
