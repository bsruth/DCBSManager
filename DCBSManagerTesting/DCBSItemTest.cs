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
            newItem.DCBSOrderCode = "APR230189";
            newItem.LoadFromPreviewsWorld();

           

            Assert.AreEqual(false, true);

        }

      


        [TestMethod]
        public void GetSiteFromOrderCode_TEST()
        {

            var newItem = new DCBSItem();

            Assert.AreEqual(Distributor.Invalid, newItem.Distributor);

            newItem.DCBSOrderCode = "";
            Assert.AreEqual(Distributor.Invalid, newItem.Distributor);

            const string LunarOrderCode = "0423DC003";
            newItem.DCBSOrderCode = LunarOrderCode;
            Assert.AreEqual(Distributor.Lunar, newItem.Distributor);

            const string PenguinISBN = "9781506734729";
            newItem.DCBSOrderCode = PenguinISBN;
            Assert.AreEqual(Distributor.PRH, newItem.Distributor);

            const string PenguinComic = "75960609600803531";
            newItem.DCBSOrderCode = PenguinComic;
            Assert.AreEqual(Distributor.PRH, newItem.Distributor);

            const string DiamondOrderCode = "APR230038";
            newItem.DCBSOrderCode = DiamondOrderCode;
            Assert.AreEqual(Distributor.Diamond, newItem.Distributor);

            const string DeepDiscountBundleCode = "APR23_DDC_A";
            newItem.DCBSOrderCode = DeepDiscountBundleCode;
            Assert.AreEqual(Distributor.Bundle, newItem.Distributor);

            newItem.DCBSOrderCode = "slk07oijllkjg8";
            Assert.AreEqual(Distributor.Unknown, newItem.Distributor);


        }
    }
}

