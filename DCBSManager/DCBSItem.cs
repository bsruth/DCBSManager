using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DCBSManager
{
    enum PurchaseCategories
    {
        None = 0,
        Maybe = 1,
        Definite = 2,
        Retail = 3
    };

    class DCBSItem
    {

        public DCBSItem()
        {
            PurchaseCategory = PurchaseCategories.None;
        }

        public string DCBSOrderCode
        {
            get;
            set;
        }
        public string Title
        {
            get;
            set;
        }
        public double RetailPrice
        {
            get;
            set;
        }
        public string DCBSDiscount
        {
            get;
            set;
        }
        public string Category
        {
            get;
            set;
        }

        public double DCBSPrice
        {
            get;
            set;
        }
        public Int64 PID
        {
            get;
            set;
        }
        public string Description{
            get;
            set;
        }

        public string ImgURL
        {
            get;
            set;
        }

        public PurchaseCategories PurchaseCategory
        {
            get;
            set;
        }

        public string DisplayValue
        {
            get
            {
                return Category + "   " + DCBSOrderCode + "   " + Title + "   " + RetailPrice.ToString() + "   " + DCBSDiscount + "   " + DCBSPrice;
            }
        }

        public byte[] ThumbnailRawBytes
        {
            get;
            set;
        }


        public BitmapImage Thumbnail
        {
            get;
            set;
        }

        public void LoadInfo()
        {
            ImgURL = "http://media.dcbservice.com/small/" + DCBSOrderCode + ".jpg";

            ThumbnailRawBytes = (new WebClient()).DownloadData(ImgURL);

           string uri = "http://www.dcbservice.com/search.aspx?search=" + DCBSOrderCode;
            System.Net.WebRequest req = System.Net.WebRequest.Create(uri);
            req.Proxy = null;
            System.Net.WebResponse resp = req.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());

            var pageText = sr.ReadToEnd().Trim();

            string dynamicContentPattern = @"<!--START DYNAMIC STUFF -->([\s\S]+)<!-- END DYNAMIC STUFF -->";

            MatchCollection dynamicContentMatches;

            Regex dynamicContentRegex = new Regex(dynamicContentPattern);
            // Get matches of pattern in text
            dynamicContentMatches = dynamicContentRegex.Matches(pageText);

            string dynamicPageText = "";
            if (dynamicContentMatches.Count >= 1 && dynamicContentMatches[0].Groups.Count >= 2)
            {
                dynamicPageText = dynamicContentMatches[0].Groups[1].ToString();
            }

            //trim out tabs and newlines
            dynamicPageText = Regex.Replace(dynamicPageText, @"\t|\n|\r", "");

           string pattern = @"category\.aspx\?id=\d+\&pid=(\d+)\'>";
            MatchCollection matches;

            Regex defaultRegex = new Regex(pattern);
            // Get matches of pattern in text
            matches = defaultRegex.Matches(dynamicPageText);

            if (matches.Count >= 2 && matches[1].Groups.Count >= 2)
            {
                PID = Int64.Parse(matches[1].Groups[1].ToString());
            }

            string descriptionPattern = @"</strong>[\s\S]*</a>[\s\S]*<br>[\s\S]*<br>([\s\S]+?)<table";
            MatchCollection descMatches;

            Regex descRegex = new Regex(descriptionPattern);
            // Get matches of pattern in text
            descMatches = descRegex.Matches(dynamicPageText);

            if (descMatches.Count >= 1 && descMatches[0].Groups.Count >= 2)
            {
                Description = descMatches[0].Groups[1].ToString();
            }

        }

        public override String ToString()
        {
            return DisplayValue;
        }

    }
}
