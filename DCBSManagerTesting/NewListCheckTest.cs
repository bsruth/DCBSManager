using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DCBSManager;

namespace DCBSManagerTesting
{
    [TestClass]
    public class NewListCheckTest
    {
        [TestMethod]
        public void CheckForNewList_TEST()
        {
            var newList = ListLoader.CheckForUpdatedList();

            if(String.IsNullOrEmpty(newList))
            {
                Console.WriteLine("No New List");
            } else
            {
                Console.WriteLine(newList);
            }
        }


        [TestMethod]
        public void ParseDetailsPage_TEST()
        {
            string detailuri = "https://www.dcbservice.com/product/SEP150019/halo-escalation-24";
            System.Net.WebRequest detailReq = System.Net.WebRequest.Create(detailuri);
            detailReq.Proxy = null;
            System.Net.WebResponse detailResp = detailReq.GetResponse();
            System.IO.StreamReader detailSr = new System.IO.StreamReader(detailResp.GetResponseStream());

            var detailPageText = detailSr.ReadToEnd().Trim();

            var item = DCBSItem.ParsePageText(detailPageText);
        }
    }
}
