using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCBSManager
{
    class CostCalculator : INotifyPropertyChanged
    {

        #region Members
        double _totalCost = 0.0;
        double _retailTotalCost = 0.0;
        double _itemsCost = 0.0;
        int _itemCount = 0;
        ObservableCollection<DCBSItem> _itemsToCalculate = null;
        PurchaseCategories _purchaseCategory = PurchaseCategories.None;

        #endregion

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

        #region Properties
        public double TotalCost
        {
            get
            {
                return _totalCost;
            }
            private set
            {
                _totalCost = value;
                OnPropertyChanged("TotalCost");
            }
        }

        public PurchaseCategories PurchaseCategory
        {
            get
            {
                return _purchaseCategory;
            }

            private set
            {
                _purchaseCategory = value;
                OnPropertyChanged("PurchaseCategory");
            }
        }

        public double ItemsCost
        {
            get
            {
                return _itemsCost;
            }
            private set
            {
                if (_itemsCost >= 0)
                {
                    _itemsCost = value;
                    OnPropertyChanged("ItemsCost");
                }
            }
        }

        public int ItemCount
        {
            get
            {
                return _itemCount;
            }
            private set
            {
                if (_itemCount != value)
                {
                    _itemCount = value;
                    OnPropertyChanged("ItemCount");
                }
            }
        }


        public double RetailTaxPercentage
        {
            get;
            set;
        }

        public double TaxPercentage
        {
            get;
            set;
        }

        public double ShippingCost
        {
            get;
            set;
        }

        public double HandlingCost
        {
            get;
            set;
        }

        public double IndividualBagBoardCost
        { 
            get; 
            set; 
        }

        public double RetailIndividualBagBoardCost
        {
            get;
            set;
        }

        

        #endregion


        public CostCalculator(ObservableCollection<DCBSItem> itemsToCalculate, PurchaseCategories categoryToCalculate)
        {
            _itemsToCalculate = itemsToCalculate;
            _itemsToCalculate.CollectionChanged += _itemsToCalculate_CollectionChanged;
            PurchaseCategory = categoryToCalculate;
        }

        void _itemsToCalculate_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CalculateCostOfItems(_itemsToCalculate);
        }

        private void CalculateCostOfItems(IList<DCBSItem> itemsList) {

            //TODO: this doesn't take into account items that don't get bags/boards

            var retailItems = itemsList.Where(item => item.PurchaseCategory == PurchaseCategories.Retail);
            var retailCost = retailItems.Sum(item => item.RetailPrice);
            var retailCount = retailItems.Count();
            var retailTotal = (retailCost + (retailCount * RetailIndividualBagBoardCost)) * (1 + RetailTaxPercentage);

            var dcbsItems = itemsList.Where(item => item.PurchaseCategory == PurchaseCategories.Definite);
            var dcbsCost = dcbsItems.Sum(item => item.DCBSPrice);
            var dcbsCount = dcbsItems.Count();
            var dcbsTotal = ((dcbsCost + (dcbsCount * IndividualBagBoardCost)) * (1 + TaxPercentage));
            if (dcbsCount > 0)
            {
                dcbsTotal += (ShippingCost + HandlingCost);
            }



            var maybeItems = itemsList.Where(item => item.PurchaseCategory == PurchaseCategories.Maybe);
            var maybeCost = maybeItems.Sum(item => item.DCBSPrice);
            var maybeCount = maybeItems.Count();
            var maybeTotal = (maybeCost + (maybeCount * IndividualBagBoardCost)) * (1 + TaxPercentage);


            TotalCost = retailTotal + dcbsTotal + maybeTotal;
            ItemCount = retailCount + dcbsCount + maybeCount;


        }
    }
}
