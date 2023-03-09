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
                    switch (valueString)
                    {
                        case "Pass":
                            purchaseCategory = PurchaseCategories.None;
                            break;
                        case "Maybe":
                            purchaseCategory = PurchaseCategories.Maybe;
                            break;
                        case "Definite":
                            purchaseCategory = PurchaseCategories.Definite;
                            break;
                        case "Retail":
                            purchaseCategory = PurchaseCategories.Retail;
                            break;
                        case "Total":
                            purchaseCategory = PurchaseCategories.Total;
                            break;
                        case "Received":
                            purchaseCategory = PurchaseCategories.Received;
                            break;
                        case "NotReceived":
                            purchaseCategory = PurchaseCategories.NotReceived;
                            break;
                        default:
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
                    return "Pass";
                case PurchaseCategories.Maybe:
                    return "Maybe";
                case PurchaseCategories.Definite:
                    return "Mail Order";
                case PurchaseCategories.Retail:
                    return "Retail";
                case PurchaseCategories.Total:
                    return "Total";
                case PurchaseCategories.Received:
                    return "Received";
                case PurchaseCategories.NotReceived:
                    return "Not Received";
                case PurchaseCategories.Matt:
                    return "Matt";
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