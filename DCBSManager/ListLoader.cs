using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DCBSManager
{
    class ListLoader
    {
        List<string> codes = new List<string>();
        List<DCBSItem> mSelectedItems = new System.Collections.Generic.List<DCBSItem>();
        public List<DCBSItem> mLoadedItems;
        SQLiteConnection mDatabaseConnection = null;
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


        string mDatabaseName;

        const int CODE = 1;
        const int TITLE = 3;
        const int COST = 4;
        const int DISC = 5;
        const int DCBS = 6;

        string[] pids = null;
        int pidIndex = 0;

        public ListLoader(string databaseName)
        {
            mDatabaseName = databaseName;

            

            if (File.Exists(databaseName + ".sqlite"))
            {
                mLoadedItems = LoadFromDatabase(databaseName);
                int i = 0;
            }
            else
            {
                SetupDatabase(databaseName);
                codes = LoadCodes("codes.txt");
                mLoadedItems = LoadXLS();
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
            using (var conn = new SQLiteConnection(@"Data Source=August2013.sqlite;Version=3;"))
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
                    cmd.Parameters.AddWithValue("P@URCHASECATEGORY", item.PurchaseCategory);
                    
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

        public List<DCBSItem> LoadFromDatabase(string databaseName)
        {
            List<DCBSItem> itemsList = new List<DCBSItem>();

            using (var conn = new SQLiteConnection(@"Data Source=August2013.sqlite;Version=3;"))
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
                            item.ThumbnailRawBytes = ret.GetFieldValue<byte[]>(8);
                            item.Thumbnail = BitmapImageFromBytes(item.ThumbnailRawBytes);
                            item.PurchaseCategory = (PurchaseCategories)ret.GetFieldValue<Int64>(9);
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

        public static BitmapImage BitmapImageFromBytes(byte[] bytes)
        {
            BitmapImage image = null;
            MemoryStream stream = null;
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
                throw;
            }
            finally
            {
                stream.Close();
                stream.Dispose();
            }
            return image;
        }

        public void ListItemsInDatabase()
        {
            using (var conn = new SQLiteConnection(@"Data Source=August2013.sqlite;Version=3;"))
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

        public List<DCBSItem> LoadXLS()
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
                                    if (codes.Contains(codeString))
                                    {
                                        newItem.DCBSOrderCode = codeString;
                                        newItem.Title = row.GetCell(TITLE).ToString();
                                        newItem.RetailPrice = double.Parse(row.GetCell(COST).ToString(), NumberStyles.Currency);
                                        newItem.DCBSDiscount = row.GetCell(DISC).ToString();
                                        newItem.DCBSPrice = double.Parse(row.GetCell(DCBS).ToString(), NumberStyles.Currency);
                                        newItem.Category = currentCategory;
                                        newItem.LoadInfo();
                                        newItem.Thumbnail = BitmapImageFromBytes(newItem.ThumbnailRawBytes);
                                        AddItemToDatabase(newItem);
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
    }
}
