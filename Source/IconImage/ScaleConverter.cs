﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IconImage;

[ValueConversion(typeof(double), typeof(double))]
public class ScaleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((value is not double sourceValue) || !double.TryParse(parameter?.ToString(), out double factor))
			return DependencyProperty.UnsetValue;

		return sourceValue / factor;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((value is not double targetValue) || !double.TryParse(parameter?.ToString(), out double factor))
			return DependencyProperty.UnsetValue;

		return targetValue * factor;
	}
}