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
    }
}
