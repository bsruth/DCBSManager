﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DCBSManager
{
    public enum PurchaseCategories
    {
        None = 0,
        Maybe = 1,
        Definite = 2,
        Retail = 3,
        Total = 4
    };

    class DCBSItem : INotifyPropertyChanged
    {
        const string _creatorToDescriptionSeparator = " %%% ";

        #region Members
        PurchaseCategories _purchaseCategory = PurchaseCategories.None;
        #endregion

        #region Events

        public event PurchaseCategoryChangedRoutedEventHandler PurchaseCategoryChanged; //fired when the purchase category has been changed

        #endregion

        public DCBSItem()
        {
        }

        public string DCBSOrderCode
        {
            get;
            set;
            //get
            //{
            //    return "APR12345";
            //}
            //set
            //{

            //}
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
        public string Description{
            get;
            set;
//            get
//            {
//                return @"Each panel control implements distinct layout logic performed in Measure() and Arrange(). Measure determines the size of the panel and each of its children. Arrange() determines the rectangle where each control renders.
//
//The last child of the DockPanel fills the remaining space. You can disable this behavior by setting the LastChild property to false.
//
//The stack panel asks each child for its desired size and then stacks them. The stack panel calls Measure() on each child with an available size of infinity and then uses the child's desired size.";
//            }
//            set
//            {

//            }
        }

        public string ImgURL
        {
            get;
            set;
        }

        public PurchaseCategories PurchaseCategory
        {
            get
            {
                //return PurchaseCategories.Definite;
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
            //get
            //{
            //    var defaultImage = new BitmapImage();
            //    defaultImage.BeginInit();
            //    defaultImage.UriSource = new Uri("pack://application:,,,/DCBSManager;component/Images/no_image.jpg", UriKind.Absolute);
            //    defaultImage.EndInit();
            //    return defaultImage;
            //}
            //set
            //{

            //}
        }

        public void LoadInfo()
        {
            try
            {
                ImgURL = "http://media.dcbservice.com/small/" + DCBSOrderCode + ".jpg";


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

                string uri = "http://www.dcbservice.com/search.aspx?search=" + DCBSOrderCode;
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


                string detailuri = "http://www.dcbservice.com/product/" + DCBSOrderCode + "/" + detailURL;
                System.Net.WebRequest detailReq = System.Net.WebRequest.Create(detailuri);
                detailReq.Proxy = null;
                System.Net.WebResponse detailResp = detailReq.GetResponse();
                System.IO.StreamReader detailSr = new System.IO.StreamReader(detailResp.GetResponseStream());

                var detailPageText = detailSr.ReadToEnd().Trim();

                string dynamicContentPattern = @"<div class=""productdetail"">([\s\S]+)<div class=""clear-fix""";

                MatchCollection dynamicContentMatches;

                Regex dynamicContentRegex = new Regex(dynamicContentPattern);
                // Get matches of pattern in text
                dynamicContentMatches = dynamicContentRegex.Matches(detailPageText);

                string dynamicPageText = "";
                if (dynamicContentMatches.Count >= 1 && dynamicContentMatches[0].Groups.Count >= 2)
                {
                    dynamicPageText = dynamicContentMatches[0].Groups[1].ToString();
                }

                string pidPattern = @"data-val=""(\d+)""";
                //string pidPattern = @"category\.aspx\?id=\d+\&pid=(\d+)\'>";
                MatchCollection pidMatches;
                Regex pidRegex = new Regex(pidPattern);
                pidMatches = pidRegex.Matches(dynamicPageText);

                PID = 0;
                if (pidMatches.Count >= 1 && pidMatches[0].Groups.Count >= 2)
                {
                    PID = Int64.Parse(pidMatches[0].Groups[1].ToString());
                }


                string publisherPattern = @">Publisher:([^<]+)";
                //string pidPattern = @"category\.aspx\?id=\d+\&pid=(\d+)\'>";
                MatchCollection publisherMatches;
                Regex publisherRegex = new Regex(publisherPattern);
                publisherMatches = publisherRegex.Matches(dynamicPageText);

                //string publisher = "";
                if (publisherMatches.Count >= 1 && publisherMatches[0].Groups.Count >= 2)
                {
                    Category = publisherMatches[0].Groups[1].ToString().Trim();
                }


                Description = "";

                string writerPattern = @">Writer:([^<]+)";
                //string pidPattern = @"category\.aspx\?id=\d+\&pid=(\d+)\'>";
                MatchCollection writerMatches;
                Regex writerRegex = new Regex(writerPattern);
                writerMatches = writerRegex.Matches(dynamicPageText);

                string writer = "";
                if (writerMatches.Count >= 1 && writerMatches[0].Groups.Count >= 2)
                {
                    writer = writerMatches[0].Groups[1].ToString().Trim();
                    Description += "(W) " + writer + " ";
                }


                string artistPattern = @">Artist:([^<]+)";
                //string pidPattern = @"category\.aspx\?id=\d+\&pid=(\d+)\'>";
                MatchCollection artistMatches;
                Regex artistRegex = new Regex(artistPattern);
                artistMatches = artistRegex.Matches(dynamicPageText);

                string artist = "";
                if (artistMatches.Count >= 1 && artistMatches[0].Groups.Count >= 2)
                {
                    artist = artistMatches[0].Groups[1].ToString().Trim();
                     Description += "(A) " + artist + " ";
                }

                string coverArtistPattern = @">Cover Artist:([^<]+)";
                //string pidPattern = @"category\.aspx\?id=\d+\&pid=(\d+)\'>";
                MatchCollection coverArtistMatches;
                Regex coverArtistRegex = new Regex(coverArtistPattern);
                coverArtistMatches = coverArtistRegex.Matches(dynamicPageText);

                string coverArtist = "";
                if (coverArtistMatches.Count >= 1 && coverArtistMatches[0].Groups.Count >= 2)
                {
                    coverArtist = coverArtistMatches[0].Groups[1].ToString().Trim();
                     Description += "(CA) " + coverArtist + " ";
                }

                string releasePattern = @" Date:([^<]+)";
                //string pidPattern = @"category\.aspx\?id=\d+\&pid=(\d+)\'>";
                MatchCollection releaseMatches;
                Regex releaseRegex = new Regex(releasePattern);
                releaseMatches = releaseRegex.Matches(dynamicPageText);

                string release = "";
                if (releaseMatches.Count >= 1 && releaseMatches[0].Groups.Count >= 2)
                {
                    release = releaseMatches[0].Groups[1].ToString().Trim();
                    Description += "(Release) " + release + " ";
                }

                Description += " " + _creatorToDescriptionSeparator;

                //trim out tabs and newlines
                dynamicPageText = Regex.Replace(dynamicPageText, @"\t|\n|\r", "");

                string descriptionPattern = @"<div class=""detaildatacol"">[\s\S]*?<p>([^<]+)";

                MatchCollection descMatches;

                Regex descRegex = new Regex(descriptionPattern);
                // Get matches of pattern in text
                descMatches = descRegex.Matches(dynamicPageText);

                if (descMatches.Count >= 1 && descMatches[0].Groups.Count >= 2)
                {
                    Description += descMatches[0].Groups[1].ToString();
                }




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


        public String ToTabSeparatedValues()
        {
            return String.Format("\t{0}\t{1}\t{2}\t{3}\t{4:C}\t{5}\t{6:C}",
                DCBSOrderCode,
                PurchaseCategory == PurchaseCategories.Definite ? "1" : "0",
                PurchaseCategory == PurchaseCategories.Retail ? "1" : "",
                Title,
                RetailPrice,
                DCBSDiscount,
                DCBSPrice
                );
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
