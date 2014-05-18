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

namespace DCBSManager
{
    class DCBSList
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

    class ListLoader : INotifyPropertyChanged
    {

        List<string> codes = new List<string>();
        List<DCBSItem> mSelectedItems = new System.Collections.Generic.List<DCBSItem>();
        public List<DCBSItem> mLoadedItems;
        SQLiteConnection mDatabaseConnection = null;
        Queue<DCBSItem> mItemsLeftToAddToCart = new Queue<DCBSItem>();
        WebBrowser mWebBrowser = new WebBrowser();


        ObservableCollection<DCBSItem> _definiteItems = new ObservableCollection<DCBSItem>();
        ObservableCollection<DCBSItem> _maybeItems = new ObservableCollection<DCBSItem>();
        ObservableCollection<DCBSItem> _retailItems = new ObservableCollection<DCBSItem>();
        ObservableCollection<DCBSItem> _purchaseItems = new ObservableCollection<DCBSItem>();



        int _selectedItemsCount = 0;
        int _dcbsItemsCount = 0;
        int _retailItemsCount = 0;
        int _maybeItemsCount = 0;

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
            if (listName.ListItemKey == DCBSList.ListItemKeys.NewList)
            {
                var updatedList = CheckForUpdatedList();
                if (updatedList != "")
                {
                    SetupDatabase(updatedList);
                    //codes = LoadCodes("codes.txt");
                    mDatabaseName = updatedList;
                    mLoadedItems = await LoadXLS();
                }
                
            }
            else
            {
                string databaseFileName = listName.ListBaseFileName + ".sqlite";
                if (File.Exists(databaseFileName))
                {
                    mLoadedItems = await LoadFromDatabase(listName.ListBaseFileName);
                    mDatabaseName = listName.ListBaseFileName;
                }
            }

            UpdateCategoryLists(ref _definiteItems, new PurchaseCategories[] { PurchaseCategories.Definite });
            UpdateCategoryLists(ref _maybeItems, new PurchaseCategories[] { PurchaseCategories.Maybe });
            UpdateCategoryLists(ref _retailItems, new PurchaseCategories[] { PurchaseCategories.Retail });
            UpdateCategoryLists(ref _purchaseItems, new PurchaseCategories[] { PurchaseCategories.Retail, PurchaseCategories.Definite });
            return mLoadedItems;
        }


        private void UpdateCategoryLists(ref ObservableCollection<DCBSItem> collectionToUpdate, PurchaseCategories[] categoryToUpdate) {

            collectionToUpdate.Clear();
            var selectedItems = this.mLoadedItems.Where(item =>  categoryToUpdate.Contains(item.PurchaseCategory));
            foreach (var item in selectedItems)
            {
                collectionToUpdate.Add(item);
            }

        }

        private String CheckForUpdatedList()
        {
            String updatedList = "";
           

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
                           //download excel file
                           string fileURL = "http://media.dcbservice.com/downloads/" + fileName;
                           var downloadClient = new WebClient();
                           downloadClient.DownloadFile(fileURL, ".\\" + fileName);
                           updatedList = Path.GetFileNameWithoutExtension(fileName);
                           return updatedList;
                       }


                    
                }
            }
            catch (Exception ex)
            {
                var blah = ex.ToString();
            }

            return updatedList;

        }
        public List<DCBSList> GetAvailableDatabases()
        {
            var workingDir = new DirectoryInfo(@".");
            var files = from file in workingDir.EnumerateFiles(@"*.sqlite")
                        select Path.GetFileNameWithoutExtension(file.Name);
            //            {
            //                ListBaseFileName = Path.GetFileNameWithoutExtension(file.Name),
            //                 string fileNamePattern = @"category\.aspx\?id=\d+\&pid=(\d+)\'>";
            //    MatchCollection pidMatches;
            //    Regex pidRegex = new Regex(pidPattern);
            //    pidMatches = pidRegex.Matches(dynamicPageText);

            //                ListItemKey = DCBSList.ListItemKeys.Database
            //            };

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

            var selectedList = this.mLoadedItems
                .Where(item => item.PurchaseCategory != PurchaseCategories.None);

            return selectedList.ToList();
        }

        public List<DCBSItem> GetDCBSCartItems()
        {
            var cartItems = this.mLoadedItems
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

        void SetupDatabase(string databaseFileName)
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
            List<DCBSItem> itemsList = new List<DCBSItem>();

            using (var conn = new SQLiteConnection(@"Data Source=" + databaseName + ".sqlite;Version=3;"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM all_items;";
                    var ret = cmd.ExecuteReader();
                    while (ret.Read())
                    {
                        try
                        {
                            var item = new DCBSItem();
                            item.DCBSOrderCode = ret.GetFieldValue<string>(0);
                            item.Title = ret.GetFieldValue<string>(1);
                            item.RetailPrice = ret.GetFieldValue<double>(2);
                            item.DCBSDiscount = ret.GetFieldValue<string>(3);
                            item.Category = ret.GetFieldValue<string>(4);
                            item.DCBSPrice = ret.GetFieldValue<double>(5);
                            item.PID = ret.GetFieldValue<Int64>(6);
                            item.Description = ret.GetFieldValue<string>(7);
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
                    }
                   
                }

            }

            return itemsList;
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


            UpdateCategoryLists(ref _definiteItems, new PurchaseCategories[]{PurchaseCategories.Definite});
            UpdateCategoryLists(ref _maybeItems, new PurchaseCategories[]{PurchaseCategories.Maybe});
            UpdateCategoryLists(ref _retailItems, new PurchaseCategories[]{PurchaseCategories.Retail});
            UpdateCategoryLists(ref _purchaseItems, new PurchaseCategories[] { PurchaseCategories.Retail, PurchaseCategories.Definite });
            return true;
        }

        /// <summary>
        /// Filters by either title or category.
        /// </summary>
        /// <param name="filterText"></param>
        /// <returns></returns>
        public async Task<List<DCBSItem>> FilterList(string filterText)
        {
            
            var filterTask = Task<List<DCBSItem>>.Factory.StartNew(() =>  { 
                
                return mLoadedItems.Where(item => {

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

        public async Task<List<DCBSItem>> LoadXLS()
        {
            string path = "" + mDatabaseName + ".xls";
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
                                    //if (currentCategory == "Previews")
                                    //{
                                        newItem.DCBSOrderCode = codeString;
                                        newItem.Title = row.GetCell(TITLE).ToString();
                                        newItem.RetailPrice = double.Parse(row.GetCell(COST).ToString(), NumberStyles.Currency);
                                        newItem.DCBSDiscount = row.GetCell(DISC).ToString();
                                        newItem.DCBSPrice = double.Parse(row.GetCell(DCBS).ToString(), NumberStyles.Currency);
                                        newItem.Category = currentCategory;
                                        newItem.LoadInfo();
                                        if (newItem.ThumbnailRawBytes != null)
                                        {
                                            newItem.Thumbnail = await BitmapImageFromBytes(newItem.ThumbnailRawBytes);
                                        }
                                        else
                                        {

                                            newItem.Thumbnail = await LoadDefaultBitmapImage();                            
                                        }
                                        newItem.PurchaseCategoryChanged += ItemPurchaseCategoryChanged;
                                        AddItemToDatabase(newItem);
                                        mDCBSItems.Add(newItem);
                                    //}
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

        public async Task<bool> DumpTabSeparatedValues(string fileName, IList<DCBSItem> itemsToDump)
        {
            using (var dumpFileStream = new StreamWriter(fileName))
            {

                foreach (var item in itemsToDump)
                {
                    var tsvString = item.ToTabSeparatedValues();
                    await dumpFileStream.WriteLineAsync(tsvString);
                }
            }
            return true;
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
