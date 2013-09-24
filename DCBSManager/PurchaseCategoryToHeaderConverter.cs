using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DCBSManager
{
    class PurchaseCategoryToHeaderConverter: IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {


            PurchaseCategories purchaseCategory;

            if ((value is PurchaseCategories) == false)
            {
                string valueString = value as string;
                if (valueString != null)
                {
                    if (string.Compare(valueString, "None") == 0)
                    {
                        purchaseCategory = PurchaseCategories.None;
                    }
                    else if (string.Compare(valueString, "Maybe") == 0)
                    {
                        purchaseCategory = PurchaseCategories.Maybe;
                    }
                    else if (string.Compare(valueString, "Definite") == 0)
                    {
                        purchaseCategory = PurchaseCategories.Definite;
                    }
                    else if (string.Compare(valueString, "Retail") == 0)
                    {
                        purchaseCategory = PurchaseCategories.Retail;
                    }
                    else
                    {
                        throw new InvalidOperationException("The string value to convert must be a SystemMessageType");
                    }
                }
                else
                {
                    throw new InvalidOperationException("The value to convert must be convertible to a SystemMessageType");
                }
                
            }
            else
            {
                purchaseCategory = (PurchaseCategories)value;
            }




            switch (purchaseCategory)
            {
                case PurchaseCategories.None:
                    return "Total";
                case PurchaseCategories.Maybe:
                    return "Maybe";
                case PurchaseCategories.Definite:
                    return "DCBS";
                case PurchaseCategories.Retail:
                    return "Retail";
            }

            return "UNKNOWN";

        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}