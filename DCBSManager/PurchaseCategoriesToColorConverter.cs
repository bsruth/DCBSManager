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
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF888888"));
                case PurchaseCategories.Maybe:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000055"));
                case PurchaseCategories.Definite:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF005500"));
                case PurchaseCategories.Retail:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555500"));
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