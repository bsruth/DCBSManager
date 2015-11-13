using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DCBSManager;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace DCBSManagerTesting
{
    [TestClass]
    public class ExcelListProcessingTests
    {
        [TestMethod]
        public void GetItemCount_TEST()
        {
            ListLoader xlsLoader = new ListLoader();
            string xlsFilePath = @"November2015.xls";
            var itemListTask = xlsLoader.ParseXLSFile(xlsFilePath, "Previews");
            Task.WaitAll(itemListTask);
            var awaiter = itemListTask.GetAwaiter();
            var itemList = awaiter.GetResult();
            const int ExpectedCount = 4;
            Assert.AreEqual(ExpectedCount, itemList.Count, "Expected number of items not found");

        }

        [TestMethod]
        public void LoadToDatabase_TEST()
        {
            ListLoader myLoader = new ListLoader();
            string listName = "November2015";
            string dbFilePath = @"November2015.sqlite";
            myLoader.SetupDatabase(listName);
            var dbLoadingTask = myLoader.LoadXLS(listName, "Previews");
            Task.WaitAll(dbLoadingTask);
            var awaiter = dbLoadingTask.GetAwaiter();
            var itemList = awaiter.GetResult();
            const int ExpectedCount = 4;
            Assert.AreEqual(ExpectedCount, itemList.Count, "Expected number of items not found");
            File.Delete(dbFilePath);
        }


    }
}
