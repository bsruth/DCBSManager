using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
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
using System.Windows.Media.Imaging;
using System.Web;

namespace DCBSManager
{
    public class DCBSList
    {
        public string _month;
        public string _year;
        public DCBSList(string baseFileName)
        {
            ListBaseFileName = baseFileName;
            ListDatabaseFileName = baseFileName + ".sqlite";
            string fileNamePattern = @"([a-zA-Z]+)(\d{4})";
            MatchCollection fileNameMatches;
            Regex filenameRegex = new Regex(fileNamePattern);
            fileNameMatches = filenameRegex.Matches(baseFileName);
            if (fileNameMatches.Count == 1)
            {
                _year = fileNameMatches[0].Groups[2].ToString();
                _month = fileNameMatches[0].Groups[1].ToString();
                ListItemString = fileNameMatches[0].Groups[2].ToString();
                ListItemString += "/";
                ListItemString += DateTime.ParseExact(fileNameMatches[0].Groups[1].ToString(), "MMMM", CultureInfo.CurrentCulture).Month.ToString("00");
                ListItemString += " " + fileNameMatches[0].Groups[1].ToString();
            }
            else
            {
                ListItemString = ListBaseFileName;
            }
        }

        public string ListBaseFileName { get; private set; }
        public string ListItemString { get; private set; }
        public string ListDatabaseFileName { get; private set; }

        public override String ToString()
        {
            return ListItemString;
        }
    }

    public class ListLoader : INotifyPropertyChanged
    {

        List<string> codes = new List<string>();

        ObservableCollection<DCBSItem> _definiteItems = new ObservableCollection<DCBSItem>();
        ObservableCollection<DCBSItem> _maybeItems = new ObservableCollection<DCBSItem>();
        ObservableCollection<DCBSItem> _retailItems = new ObservableCollection<DCBSItem>();
        ObservableCollection<DCBSItem> _purchaseItems = new ObservableCollection<DCBSItem>();
        ObservableCollection<DCBSItem> _notReceivedItems = new ObservableCollection<DCBSItem>();

        bool _loadingNewList = false;
        string _listLoadingText = "Loading";

        //Database information        
        const string all_items_table_name = "all_items";
        const string col_code = "code";
        const int col_code_index = 0;
        const string col_title = "title";
        const int col_title_index = 1;
        const string col_retail = "retail";
        const int col_retail_index = 2;
        const string col_discount = "discount";
        const int col_discount_index = 3;
        const string col_category = "category";
        const int col_category_index = 4;
        const string col_dcbsprice = "dcbs_price";
        const int col_dcbsprice_index = 5;
        const string col_pid = "pid";
        const int col_pid_index = 6;
        const string col_description = "description";
        const int col_description_index = 7;
        const string col_thumbnail = "thumbnail";
        const int col_thumbnail_index = 8;
        const string col_purchase_category = "purchase_category";
        const int col_purchase_category_index = 9;
        
        //Excel column indexes for DCBS
        const int CODE = 1;
        const int TITLE = 3;
        const int COST = 4;
        const int DISC = 5;
        const int DCBS = 6;

        //excel column indexes for Kowabunga
        //same as DCBS, plus
        const int SHIP_DATE = 14;
        const int WRITER = 15;
        const int ARTIST = 16;
        const int COVER_ART = 17;
        const int DESC = 18;

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

        public ObservableCollection<DCBSItem> NotReceivedItems
        {
            get
            {
                return _notReceivedItems;
            }

            private set
            {
                if (value != null)
                {
                    _notReceivedItems = value;
                    OnPropertyChanged("NotReceivedItems");
                }
            }
        }

        public DCBSList CurrentList { get; private set; }
        #endregion


        public ListLoader()
        {
        }


        public async Task<List<DCBSItem>> LoadList(DCBSList listName)
        {
           NewListLoading = true;
            if (File.Exists(listName.ListDatabaseFileName))
            {
                LoadedItems = await LoadFromDatabase(listName.ListDatabaseFileName);
                CurrentList = listName;
            }
            else
            {
                SetupDatabase(listName.ListDatabaseFileName);
                CurrentList = listName;
                LoadedItems = await LoadXLS(listName.ListBaseFileName, "");
            }


            UpdateCategoryLists(ref _definiteItems, PurchaseCategories.Definite);
            UpdateCategoryLists(ref _maybeItems, PurchaseCategories.Maybe);
            UpdateCategoryLists(ref _retailItems, PurchaseCategories.Retail);
            UpdateCategoryLists(ref _purchaseItems, PurchaseCategories.Retail, PurchaseCategories.Definite);
            UpdateCategoryLists(ref _notReceivedItems, PurchaseCategories.Retail, PurchaseCategories.Definite);

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

        public static DCBSList DownloadList(string fileName)
        {
            //download excel file
            string fileURL = "http://media.dcbservice.com/downloads/" + fileName;
            var downloadClient = new WebClient();
            downloadClient.DownloadFile(fileURL, ".\\" + fileName);
            return new DCBSList(Path.GetFileNameWithoutExtension(fileName));
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
                var dcbsList = new DCBSList(file);
             
                filesList.Add(dcbsList);
            }
            var sortedFiles = filesList.OrderByDescending(file => file.ListItemString).ToList();
            return sortedFiles;
        }

        public List<DCBSItem> GetSelectedItems()
        {

            var selectedList = this.LoadedItems
                .Where(item => item.PurchaseCategory != PurchaseCategories.None);

            return selectedList.ToList();
        }

        public void SetupDatabase(string databaseFileName)
        {
            if (File.Exists(databaseFileName) == false)
            {
                SQLiteConnection.CreateFile(databaseFileName);
            }

            using (var conn = new SQLiteConnection(@"Data Source=" + databaseFileName + ";Version=3;"))
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
            using (var conn = new SQLiteConnection(@"Data Source=" + CurrentList.ListDatabaseFileName + ";Version=3;"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
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

                using (var conn = new SQLiteConnection(@"Data Source=" + databaseName + ";Version=3;"))
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
                                var item = new DCBSItem
                                {
                                    DCBSOrderCode = ret.GetFieldValue<string>(col_code_index),
                                    Title = HttpUtility.HtmlDecode(ret.GetFieldValue<string>(col_title_index)),
                                    RetailPrice = ret.GetFieldValue<double>(col_retail_index),
                                    DCBSDiscount = ret.GetFieldValue<string>(col_discount_index),
                                    Category = HttpUtility.HtmlDecode(ret.GetFieldValue<string>(col_category_index)),
                                    DCBSPrice = ret.GetFieldValue<double>(col_dcbsprice_index),
                                    PID = ret.GetFieldValue<Int64>(col_pid_index),
                                    Description = HttpUtility.HtmlDecode(ret.GetFieldValue<string>(col_description_index))
                                };
                                if (ret.IsDBNull(col_thumbnail_index) == true)
                                {
                                    item.ThumbnailRawBytes = null;
                                    item.Thumbnail = await LoadDefaultBitmapImage();
                                }
                                else
                                {
                                    item.ThumbnailRawBytes = ret.GetFieldValue<byte[]>(col_thumbnail_index);
                                    item.Thumbnail = await BitmapImageFromBytes(item.ThumbnailRawBytes);

                                }

                                item.PurchaseCategory = (PurchaseCategories)ret.GetFieldValue<Int64>(col_purchase_category_index);
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

            using (var conn = new SQLiteConnection(@"Data Source=" + CurrentList.ListDatabaseFileName + ";Version=3;"))
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
            UpdateCategoryLists(ref _purchaseItems, PurchaseCategories.Retail, PurchaseCategories.Definite, PurchaseCategories.Received);
            UpdateCategoryLists(ref _notReceivedItems, PurchaseCategories.Retail, PurchaseCategories.Definite);
            return true;
        }

        /// <summary>
        /// Filters by either title or category.
        /// </summary>
        /// <param name="filterText"></param>
        /// <returns></returns>
        static public async Task<List<DCBSItem>> FilterList(string filterText, List<DCBSItem> listToFilter)
        {
            return await Task<List<DCBSItem>>.Factory.StartNew(() =>
            {
                if(string.IsNullOrEmpty(filterText))
                {
                    return listToFilter;
                }

                return (from item in listToFilter
                        where item.Title.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            item.Category.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0
                        select item).ToList();
            }); ;
        }

        public List<DCBSItem> FilterToPurchaseCategory(params PurchaseCategories[] categories)
        {
            return (from item in LoadedItems
                   where categories.Contains(item.PurchaseCategory)
                   select item).ToList();
        }

        public static async Task<BitmapImage> BitmapImageFromBytes(byte[] bytes)
        {
            BitmapImage image = null;
            MemoryStream stream = null;


           await App.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        using (stream = new MemoryStream(bytes))
                        {
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
                    }
                    catch (Exception)
                    {

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
                //item.LoadFromWebsite();
                item.LoadFromPreviewsWorld();
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


        public enum OrderStore
        {
            DCBS,
            Kowabunga
        };

        private void AddItemsToSheet(ISheet orderSheet, IList<DCBSItem> itemsToAdd)
        {
            System.Collections.IEnumerator rows = orderSheet.GetRowEnumerator();
            while (rows.MoveNext())
            {
                IRow row = (IRow)rows.Current;
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
                            foreach (var item in itemsToAdd)
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
        }


        /// <summary>
        /// Selects all items that will be ordered from DCBS in the DCBS order file to prepare it for uploading to the
        /// website.
        /// </summary>
        /// <param name="itemsToDump">List of DCBS items</param>
        /// <returns>The complete path to the excel file.</returns>
        public async Task<string> PrepareDCBSOrderExcelFileForUpload(IList<DCBSItem> itemsToDump, OrderStore store)
        {
            return await Task.Run(() =>
            {
                string path = "" + CurrentList.ListBaseFileName+ ".xls";
                string outPath = Path.GetFullPath(CurrentList.ListBaseFileName + "_completed.xls");
                if(store == OrderStore.Kowabunga)
                {
                    path = "Kowabunga_Order_Form_" + CurrentList._year + "_" + CurrentList._month + ".xlsx";
                    outPath = Path.GetFullPath("Kowabunga_Order_Form_" + CurrentList._year + "_" + CurrentList._month + "_completed.xlsx");
                    
                }
                if(File.Exists(path) == false)
                {
                    return "";
                }
                List<DCBSItem> mDCBSItems = new List<DCBSItem>();
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (store == OrderStore.Kowabunga)
                    {
                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet = workbook.GetSheetAt(0);
                        AddItemsToSheet(sheet, itemsToDump);
                        XSSFFormulaEvaluator.EvaluateAllFormulaCells(workbook);
                        using (FileStream outFile = new FileStream(outPath, FileMode.Create, FileAccess.Write))
                        {
                            workbook.Write(outFile);
                        }
                    } else
                    {
                        var workbook = new HSSFWorkbook(file);
                        ISheet sheet = workbook.GetSheetAt(0);
                        AddItemsToSheet(sheet, itemsToDump);
                        HSSFFormulaEvaluator.EvaluateAllFormulaCells(workbook);
                        using (FileStream outFile = new FileStream(outPath, FileMode.Create, FileAccess.Write))
                        {
                            workbook.Write(outFile);
                        }
                    }
                    
                }
                return outPath;
            });
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
