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
        Retail = 3
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

                //get writer, artist and cover artist
                string creatorsPattern = @"\([WCA/]+\)[\s\w,]+";
                MatchCollection creatorsMatches;

                Regex creatorRegex = new Regex(creatorsPattern);
                // Get matches of pattern in text
                creatorsMatches = creatorRegex.Matches(dynamicPageText);

                Description = "";
                List<string> creators = new List<string>();
                foreach (var creator in creatorsMatches)
                {
                    var creatorString = creator.ToString();
                    if (creatorString.Contains('\r'))
                    {
                        creatorString = creatorString.Substring(0, creatorString.IndexOf('\r'));
                    }
                    creatorString = creatorString.Trim();
                    if (creators.Contains(creatorString) == false)
                    {
                        creators.Add(creatorString);
                        Description += (creatorString + " ");
                        Console.WriteLine(creatorString);
                    }

                }

                int endOfCreatorsIndex = 0;
                if (creators.Count > 0)
                {
                    endOfCreatorsIndex = dynamicPageText.IndexOf(creators.Last()) + creators.Last().Length;
                    Description += _creatorToDescriptionSeparator; 
                }
                dynamicPageText = dynamicPageText.Substring(endOfCreatorsIndex);


                //trim out tabs and newlines
                dynamicPageText = Regex.Replace(dynamicPageText, @"\t|\n|\r", "");

                string pidPattern = @"category\.aspx\?id=\d+\&pid=(\d+)\'>";
                MatchCollection pidMatches;

                Regex pidRegex = new Regex(pidPattern);
                pidMatches = pidRegex.Matches(dynamicPageText);

                PID = 0;
                if (pidMatches.Count >= 2 && pidMatches[1].Groups.Count >= 2)
                {
                    PID = Int64.Parse(pidMatches[1].Groups[1].ToString());
                }

                string descriptionPattern = @"([\s\S]+?)<table";
                MatchCollection descMatches;

                Regex descRegex = new Regex(descriptionPattern);
                // Get matches of pattern in text
                descMatches = descRegex.Matches(dynamicPageText);

                if (descMatches.Count >= 1 && descMatches[0].Groups.Count >= 2)
                {
                    Description += descMatches[0].Groups[1].ToString();
                }


                

            }catch(Exception ex)
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
