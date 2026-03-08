using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;
using imsapp_desktop.Services;

namespace imsapp_desktop.Converters;

public class CurrencyFormatConverter : IValueConverter
{
    private static string Symbol => ServiceLocator.Branding.Current.CurrencySymbol;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is decimal d)
            return Symbol + " " + d.ToString("N2", CultureInfo.InvariantCulture);
        if (value is double dbl)
            return Symbol + " " + dbl.ToString("N2", CultureInfo.InvariantCulture);
        return Symbol + " 0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
