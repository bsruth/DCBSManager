using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Text;

namespace DCBSManager
{
    class PurchaseCategoriesToColorConverter: IValueConverter
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
                case PurchaseCategories.Total:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF888888"));
                case PurchaseCategories.Maybe:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000055"));
                case PurchaseCategories.Definite:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF005500"));
                case PurchaseCategories.Retail:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555500"));
                case PurchaseCategories.Received:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000000"));
                case PurchaseCategories.NotReceived:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));
            }

            return new SolidColorBrush((Color)Colors.Black);

        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}