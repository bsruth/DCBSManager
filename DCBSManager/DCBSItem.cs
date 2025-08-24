using System;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace DCBSManager
{
    public enum PurchaseCategories
    {
        None = 0,
        Maybe = 1,
        Definite = 2,
        Retail = 3,
        Received = 4,
        Total = 5,
        NotReceived = 6,
        Matt = 7,
        MattReceived = 8

    };
    public enum Distributor
    {
        Diamond,
        PRH,
        Lunar,
        Bundle,
        Unknown,
        Invalid
    };

    public class DCBSItem : INotifyPropertyChanged
    {
        const string _creatorToDescriptionSeparator = " %%% ";
        static Int64 deepDiscountPID = 0;

        #region Members
        PurchaseCategories _purchaseCategory = PurchaseCategories.None;
        Distributor _distributor = Distributor.Invalid;
        string _orderCode = "";
        #endregion

        #region Events

        public event PurchaseCategoryChangedRoutedEventHandler PurchaseCategoryChanged; //fired when the purchase category has been changed

        #endregion

        public DCBSItem()
        {
        }

        #region Properties
        public string DCBSOrderCode
        {
            get
            {
                return _orderCode;
            }
            set
            {
                _orderCode = value;
                this.Distributor = Distributor.Invalid;
            }
        }

        public Distributor Distributor
        {
            get
            {
                if (_distributor == Distributor.Invalid)
                {
                    _distributor = GetDistributorFromOrderCode(DCBSOrderCode);
                }
                return _distributor;

            }
            private set
            {
                _distributor = value;
            }

        }

        public string Title
        {
            get;
            set;
            //get
            //{
            //    return "Some Title";
            //}
            //set
            //{

            //}
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
            //get
            //{
            //    return "50";
            //}
            //set
            //{

            //}
        }
        public string Category
        {
            get;
            set;
            //get
            //{
            //    return "Marvel";
            //}
            //set
            //{

            //}
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
        public string Description
        {
            get;
            set;
        }

        public string ImgURL
        {
            get;
            set;
        }

        public static IEnumerable<string> GetImageURLFromOrderCodeAndDistributor(string orderCode, Distributor distributor)
        {


            switch (distributor)
            {
                case Distributor.Lunar:
                    yield return String.Format("https://media.lunardistribution.com/images/covers/{0}.jpg", orderCode);
                    yield break;

                case Distributor.Diamond:
                    yield return String.Format("http://media.dcbservice.com/small/{0}.jpg", orderCode);
                    yield break;

                case Distributor.PRH:
                    yield return String.Format("https://images.randomhouse.com/cover/{0}", orderCode);
                    yield return String.Format("https://images4.penguinrandomhouse.com/cover/{0}", orderCode);
                    yield break;

                case Distributor.Unknown:
                case Distributor.Invalid:
                case Distributor.Bundle:
                    yield break;
            }
        }
        public static Distributor GetDistributorFromOrderCode(string orderCode)
        {

            if (string.IsNullOrEmpty(orderCode))
            {
                return Distributor.Invalid;
            }

            const string LunarRegexPattern = @"^\d{4}[a-zA-Z0-9]{2}\d{3,}$";
            if (Regex.Match(orderCode, LunarRegexPattern).Success)
            {
                return Distributor.Lunar;
            }

            const string PRHRegexPattern = @"^(?:\d{13}|\d{17})$";
            if (Regex.Match(orderCode, PRHRegexPattern).Success)
            {
                return Distributor.PRH;
            }

            const string DiamondRegexPattern = @"^[A-Z]{3}[0-9]{6}[A-Z]*$";
            if (Regex.Match(orderCode, DiamondRegexPattern).Success)
            {
                return Distributor.Diamond;
            }

            const string DeepDiscountBundlePattern = @"^[A-Z]{3}[0-9]{2}_[A-Z]+_[A-Z]$";
            if (Regex.Match(orderCode, DeepDiscountBundlePattern).Success)
            {
                return Distributor.Bundle;
            }


            return Distributor.Unknown;
        }

        public PurchaseCategories PurchaseCategory
        {
            get
            {
                return _purchaseCategory;
            }
            set
            {
                if (value != _purchaseCategory)
                {
                    _purchaseCategory = value;
                    OnPropertyChanged("PurchaseCategory");
                    if (PurchaseCategoryChanged != null)
                    {
                        PurchaseCategoryChanged(this, new PurchaseCategoryChangedRoutedEventArgs() { NewPurchaseCategory = value });
                    }
                }
            }
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
        #endregion



        public static DCBSItem ParsePageText(string pageText)
        {
            DCBSItem tmpItem = new DCBSItem();

            string dynamicContentPattern = @"<div class=""productdetail[^>]+>([\s\S]+)<div class=""clear-fix""";

            MatchCollection dynamicContentMatches;

            Regex dynamicContentRegex = new Regex(dynamicContentPattern);
            // Get matches of pattern in text
            dynamicContentMatches = dynamicContentRegex.Matches(pageText);

            string dynamicPageText = "";
            if (dynamicContentMatches.Count >= 1 && dynamicContentMatches[0].Groups.Count >= 2)
            {
                dynamicPageText = dynamicContentMatches[0].Groups[1].ToString();
            }

            string pidPattern = @"data-val=""(\d+)""";
            MatchCollection pidMatches;
            Regex pidRegex = new Regex(pidPattern);
            pidMatches = pidRegex.Matches(dynamicPageText);

            tmpItem.PID = 0;
            if (pidMatches.Count >= 1 && pidMatches[0].Groups.Count >= 2)
            {
                tmpItem.PID = Int64.Parse(pidMatches[0].Groups[1].ToString());
            }


            string publisherPattern = @">Publisher:([^<]+)";
            MatchCollection publisherMatches;
            Regex publisherRegex = new Regex(publisherPattern);
            publisherMatches = publisherRegex.Matches(dynamicPageText);

            if (publisherMatches.Count >= 1 && publisherMatches[0].Groups.Count >= 2)
            {
                tmpItem.Category = publisherMatches[0].Groups[1].ToString().Trim();
            }


            tmpItem.Description = "";

            string writerPattern = @">Writer:([^<]+)";
            MatchCollection writerMatches;
            Regex writerRegex = new Regex(writerPattern);
            writerMatches = writerRegex.Matches(dynamicPageText);

            string writer = "";
            if (writerMatches.Count >= 1 && writerMatches[0].Groups.Count >= 2)
            {
                writer = writerMatches[0].Groups[1].ToString().Trim();
                tmpItem.Description += "(W) " + writer + " ";
            }


            string artistPattern = @">Artist:([^<]+)";
            MatchCollection artistMatches;
            Regex artistRegex = new Regex(artistPattern);
            artistMatches = artistRegex.Matches(dynamicPageText);

            string artist = "";
            if (artistMatches.Count >= 1 && artistMatches[0].Groups.Count >= 2)
            {
                artist = artistMatches[0].Groups[1].ToString().Trim();
                tmpItem.Description += "(A) " + artist + " ";
            }

            string coverArtistPattern = @">Cover Artist:([^<]+)";
            MatchCollection coverArtistMatches;
            Regex coverArtistRegex = new Regex(coverArtistPattern);
            coverArtistMatches = coverArtistRegex.Matches(dynamicPageText);

            string coverArtist = "";
            if (coverArtistMatches.Count >= 1 && coverArtistMatches[0].Groups.Count >= 2)
            {
                coverArtist = coverArtistMatches[0].Groups[1].ToString().Trim();
                tmpItem.Description += "(CA) " + coverArtist + " ";
            }

            string releasePattern = @" Date:([^<]+)";
            MatchCollection releaseMatches;
            Regex releaseRegex = new Regex(releasePattern);
            releaseMatches = releaseRegex.Matches(dynamicPageText);

            string release = "";
            if (releaseMatches.Count >= 1 && releaseMatches[0].Groups.Count >= 2)
            {
                release = releaseMatches[0].Groups[1].ToString().Trim();
                tmpItem.Description += "(Release) " + release + " ";
            }

            tmpItem.Description += " " + _creatorToDescriptionSeparator;

            //trim out tabs and newlines
            dynamicPageText = Regex.Replace(dynamicPageText, @"\t|\n|\r", "");

            string descriptionPattern = @"<div class=""detaildatacol"">[\s\S]*?<p>([^<]+)";

            MatchCollection descMatches;

            Regex descRegex = new Regex(descriptionPattern);
            // Get matches of pattern in text
            descMatches = descRegex.Matches(dynamicPageText);

            if (descMatches.Count >= 1 && descMatches[0].Groups.Count >= 2)
            {
                tmpItem.Description += descMatches[0].Groups[1].ToString();
            }
            return tmpItem;
        }

        public void LoadFromDistributors()
        {
            switch (this.Distributor)
            {
                case Distributor.PRH:
                    PID = long.Parse(DCBSOrderCode);
                    break;
                case Distributor.Lunar:
                    {
                        var pidString = DCBSOrderCode;
                        StringBuilder sb = new StringBuilder(DCBSOrderCode);
                        sb.Remove(4, 2);
                        pidString = sb.ToString();
                        PID = long.Parse(pidString);
                    }
                    break;
                case Distributor.Diamond:
                    {
                        var pidString = DCBSOrderCode.Substring(3);
                        PID = long.Parse(pidString);
                    }
                    break;

                case Distributor.Invalid:
                case Distributor.Unknown:
                case Distributor.Bundle:
                    return;
            }

            var possibleUrls = GetImageURLFromOrderCodeAndDistributor(DCBSOrderCode, this.Distributor);
            bool imageFound = false;

            foreach (var url in possibleUrls)
            {
                if (string.IsNullOrEmpty(url)) continue;

                ImgURL = url;
                try
                {
                    ThumbnailRawBytes = (new WebClient()).DownloadData(ImgURL);
                    imageFound = true;
                    break;
                }
                catch (Exception)
                {
                    // Try next URL
                    continue;
                }
            }

            if (!imageFound)
            {
                //no images succeeded, use the default
                ImgURL = "";
                ThumbnailRawBytes = null;
            }
        }

        public void LoadFromPreviewsWorld()
        {

            string remainingPageText = "";
            try
            {
                string url = "https://previewsworld.com//Catalog/" + DCBSOrderCode;
                System.Net.WebRequest detailReq = System.Net.WebRequest.Create(url);
                detailReq.Proxy = null;
                System.Net.WebResponse detailResp = detailReq.GetResponse();
                System.IO.StreamReader detailSr = new System.IO.StreamReader(detailResp.GetResponseStream());
                this.PID = deepDiscountPID++;
                remainingPageText = detailSr.ReadToEnd().Trim();
                //thumbnail
                Regex thumbnailPattern = new Regex(@"id=""MainContentImage""\s*src=""([^""]+)""");

                var thumbnailMatches = thumbnailPattern.Matches(remainingPageText);
                if (thumbnailMatches.Count >= 1 && thumbnailMatches[0].Groups.Count >= 2)
                {
                    var imgRelative = thumbnailMatches[0].Groups[1].ToString();
                    var previewImage = Regex.Replace(imgRelative, "CatalogImage", "CatalogThumbnail");
                    ImgURL = "https://previewsworld.com/" + previewImage;
                    try
                    {
                        ThumbnailRawBytes = (new WebClient()).DownloadData(ImgURL);
                    }
                    catch (Exception)
                    {
                        //image failed, just use the no_image url
                        ImgURL = "";
                        ThumbnailRawBytes = null;
                    }

                }

                string publisherPattern = @"Publisher"">([^<]+)([\s\S]+)";
                MatchCollection publisherMatches;
                Regex publisherRegex = new Regex(publisherPattern);
                publisherMatches = publisherRegex.Matches(remainingPageText);

                if (publisherMatches.Count >= 1 && publisherMatches[0].Groups.Count >= 2)
                {
                    Category = publisherMatches[0].Groups[1].ToString().Trim();
                    Category = Regex.Replace(Category, "&amp;", "&");

                    if (thumbnailMatches[0].Groups.Count >= 3)
                    {
                        remainingPageText = thumbnailMatches[0].Groups[2].ToString();
                    }

                }


                string creatorSectionPattern = @"<div class=""Creators"">([\s\S]+?)<\/div>([\s\S]+)";
                var creatorRegex = new Regex(creatorSectionPattern);
                var creatorMatches = creatorRegex.Matches(remainingPageText);
                string creatorSection = "";
                if (creatorMatches.Count >= 1 && creatorMatches[0].Groups.Count >= 2)
                {
                    creatorSection = creatorMatches[0].Groups[1].ToString().Trim();
                    if (thumbnailMatches[0].Groups.Count >= 3)
                    {
                        remainingPageText = thumbnailMatches[0].Groups[2].ToString();
                    }
                }
                Description = "";

                string writerPattern = @"\(W[^\)]*\)([^\(]+)";
                MatchCollection writerMatches;
                Regex writerRegex = new Regex(writerPattern);
                writerMatches = writerRegex.Matches(creatorSection);

                string writer = "";
                if (writerMatches.Count >= 1 && writerMatches[0].Groups.Count >= 2)
                {
                    writer = writerMatches[0].Groups[1].ToString().Trim();
                    writer = Regex.Replace(writer, "\\s+", " ");
                    Description += "(W) " + writer + " ";
                }


                string artistPattern = @"\(A[^\)]*\)([^(\(|<)]+)";
                MatchCollection artistMatches;
                Regex artistRegex = new Regex(artistPattern);
                artistMatches = artistRegex.Matches(creatorSection);

                string artist = "";
                if (artistMatches.Count >= 1 && artistMatches[0].Groups.Count >= 2)
                {
                    artist = artistMatches[0].Groups[1].ToString().Trim();
                    artist = Regex.Replace(artist, "\\s+", " ");
                    Description += "(A) " + artist + " ";
                }

                string coverArtistPattern = @"[\(|\/]CA[^\)]*\)([^(\(|<)]+)";
                MatchCollection coverArtistMatches;
                Regex coverArtistRegex = new Regex(coverArtistPattern);
                coverArtistMatches = coverArtistRegex.Matches(creatorSection);

                string coverArtist = "";
                if (coverArtistMatches.Count >= 1 && coverArtistMatches[0].Groups.Count >= 2)
                {
                    coverArtist = coverArtistMatches[0].Groups[1].ToString().Trim();
                    coverArtist = Regex.Replace(coverArtist, "\\s+", " ");
                    Description += "(CA) " + coverArtist + " ";
                }




                string releasePattern = @"ReleaseDate"">([^<]+)";
                MatchCollection releaseMatches;
                Regex releaseRegex = new Regex(releasePattern);
                releaseMatches = releaseRegex.Matches(remainingPageText);

                string release = "";
                if (releaseMatches.Count >= 1 && releaseMatches[0].Groups.Count >= 2)
                {
                    release = releaseMatches[0].Groups[1].ToString().Trim();
                    Description += "(Release) " + release + " ";
                }

                Description += " " + _creatorToDescriptionSeparator;

                //trim out tabs and newlines

                string descriptionPattern = @"<div class=""Creators"">[\s\S]*?<\/div>([\s\S]+?)<div";


                Regex descRegex = new Regex(descriptionPattern);
                // Get matches of pattern in text
                var descMatches = descRegex.Matches(remainingPageText);

                if (descMatches.Count >= 1 && descMatches[0].Groups.Count >= 2)
                {
                    var descriptionHTML = descMatches[0].Groups[1].ToString();
                    descriptionHTML = Regex.Replace(descriptionHTML, @"\s+", " ");
                    if (descriptionHTML != "")
                    {
                        Description += Regex.Replace(descriptionHTML, @"<[^>]*>|&[^;]+;", "");
                    }
                }
            }
            catch (Exception ex)
            {
                var blah = ex.ToString();
            }

        }

        public void LoadFromWebsite()
        {
            try
            {
                var possibleUrls = GetImageURLFromOrderCodeAndDistributor(DCBSOrderCode, Distributor.Diamond);
                bool imageFound = false;

                foreach (var url in possibleUrls)
                {
                    if (string.IsNullOrEmpty(url)) continue;

                    ImgURL = url;
                    try
                    {
                        ThumbnailRawBytes = (new WebClient()).DownloadData(ImgURL);
                        imageFound = true;
                        break;
                    }
                    catch (Exception)
                    {
                        // Try next URL
                        continue;
                    }
                }

                if (!imageFound)
                {
                    //no images succeeded, use the default
                    ImgURL = "";
                    ThumbnailRawBytes = null;
                }

                string uri = "https://www.dcbservice.com/search.aspx?search=" + DCBSOrderCode;
                System.Net.WebRequest req = System.Net.WebRequest.Create(uri);
                req.Proxy = null;
                System.Net.WebResponse resp = req.GetResponse();
                System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());

                var pageText = sr.ReadToEnd().Trim();

                string detailURLPattern = @"""/product/" + DCBSOrderCode + @"([^""]+)";

                Regex detailContentRegex = new Regex(detailURLPattern, RegexOptions.IgnoreCase);
                // Get matches of pattern in text
                var detailURLMatches = detailContentRegex.Matches(pageText);

                string detailURL = "";
                if (detailURLMatches.Count >= 1 && detailURLMatches[0].Groups.Count >= 2)
                {
                    detailURL = detailURLMatches[0].Groups[1].ToString();
                }

                string detailuri = "https://www.dcbservice.com/product/" + DCBSOrderCode + detailURL;
                System.Net.WebRequest detailReq = System.Net.WebRequest.Create(detailuri);
                detailReq.Proxy = null;
                System.Net.WebResponse detailResp = detailReq.GetResponse();
                System.IO.StreamReader detailSr = new System.IO.StreamReader(detailResp.GetResponseStream());

                var detailPageText = detailSr.ReadToEnd().Trim();

                var tmpItem = ParsePageText(detailPageText);

                PID = tmpItem.PID;
                Category = tmpItem.Category;
                Description = tmpItem.Description;
            }
            catch (Exception ex)
            {
                var blah = ex.ToString();
            }
        }

        public override String ToString()
        {
            return DisplayValue;
        }

        public void MoveToPreviousPurchaseCategory()
        {
            if (PurchaseCategory == PurchaseCategories.None)
            {
                PurchaseCategory = PurchaseCategories.MattReceived;
            }
            else if (PurchaseCategory == PurchaseCategories.Matt)
            {
                PurchaseCategory = PurchaseCategories.Received;
            }
            else
            {
                PurchaseCategory = (PurchaseCategories)((Int64)(PurchaseCategory) - 1);
            }
        }

        public void MoveToNextPurchaseCategory()
        {
            int numberOfSelectableCategories = Enum.GetNames(typeof(PurchaseCategories)).Length;

            PurchaseCategory = (PurchaseCategories)(((Int64)(PurchaseCategory) + 1) % numberOfSelectableCategories);
            if (PurchaseCategory == PurchaseCategories.NotReceived || PurchaseCategory == PurchaseCategories.Total)
            {
                PurchaseCategory = PurchaseCategories.Matt;
            }
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

    #region Custom RoutedEventArgs for Custom Events

    //used to inform subscribed objects that
    //the the current purchase category has been changed
    public class PurchaseCategoryChangedRoutedEventArgs : RoutedEventArgs
    {
        public PurchaseCategories NewPurchaseCategory
        {
            get;
            set;
        }


    }

    public delegate void PurchaseCategoryChangedRoutedEventHandler(object sender, PurchaseCategoryChangedRoutedEventArgs e);

    #endregion
}
