using System;
using System.Windows.Data;
using System.Windows;


namespace DCBSManager
{
	public class InverseBooleanToVisibilityConverter : IValueConverter {
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter,
				System.Globalization.CultureInfo culture) {
					if ( (value is bool) == false) {
						throw new InvalidOperationException("The target must be a boolean");
					}

					bool show = (bool)value;
			return ( (!show) ? Visibility.Visible : Visibility.Collapsed );
		}

		public object ConvertBack(object value, Type targetType, object parameter,
				System.Globalization.CultureInfo culture) {
					if (value is Visibility) {
						throw new InvalidOperationException("The target must be a Visiblity state");
					}

					return !(((Visibility)value).Equals(Visibility.Visible));
		}

		#endregion
	}
}
