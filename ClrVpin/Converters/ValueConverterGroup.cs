using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;

namespace ClrVpin.Converters;

public class ValueConverterGroup : List<IValueConverter>, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        // run each converter passing the value of the previous result (or the seed) into the subsequent converter
        return this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}