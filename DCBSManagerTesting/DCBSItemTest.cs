using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DCBSManager;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace DCBSManagerTesting
{
    [TestClass]
    public class DCBSItemTest
    {
        [TestMethod]
        public void GetItemFromPreviews_TEST()
        {
            DCBSItem newItem = new DCBSItem();
            //newItem.DCBSOrderCode = "AUG180971";
            //newItem.DCBSOrderCode = "AUG180001";
            //newItem.DCBSOrderCode = "AUG180467";
            //newItem.DCBSOrderCode = "AUG181847";
            //newItem.DCBSOrderCode = "AUG181821";
            newItem.DCBSOrderCode = "AUG181945";
            newItem.LoadFromPreviewsWorld();

           

            Assert.AreEqual(false, true);

        }

        enum Distributor
        {
            Diamond,
            PRH,
            Lunar,
            Bundle,
            Unknown
        };

        Distributor GetDistributorFromOrderCode(string orderCode)
        {
            const string LunarRegexPattern = @"^\d{4}[a-zA-Z0-9]{2}\d{3}$";
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


        [TestMethod]
        public void GetSiteFromOrderCode_TEST()
        {

            const string LunarOrderCode = "0423DC003";
            Assert.AreEqual(Distributor.Lunar, GetDistributorFromOrderCode(LunarOrderCode));

            const string PenguinISBN = "9781506734729";
            const string PenguinComic = "75960609600803531";
            Assert.AreEqual(Distributor.PRH, GetDistributorFromOrderCode(PenguinISBN));
            Assert.AreEqual(Distributor.PRH, GetDistributorFromOrderCode(PenguinComic));

            const string DiamondOrderCode = "APR230038";
            Assert.AreEqual(Distributor.Diamond, GetDistributorFromOrderCode(DiamondOrderCode));

            const string DeepDiscountBundleCode = "APR23_DDC_A";
            Assert.AreEqual(Distributor.Bundle, GetDistributorFromOrderCode(DeepDiscountBundleCode));

        }
    }
}

