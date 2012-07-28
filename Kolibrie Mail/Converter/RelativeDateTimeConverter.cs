﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Kolibrie_Mail.Converter
{
    class RelativeDateTimeConverter: IValueConverter
    {
        private const int Minute = 60;
        private const int Hour = Minute * 60;
        private const int Day = Hour * 24;
        private const int Year = Day * 365;

        private readonly Dictionary<long, Func<TimeSpan, string>> _thresholds = new Dictionary<long, Func<TimeSpan, string>>
        {
            {2, t => "a second ago"},
            {Minute,  t => String.Format("{0} seconds ago", (int)t.TotalSeconds)},
            {Minute * 2,  t => "a minute ago"},
            {Hour,  t => String.Format("{0} minutes ago", (int)t.TotalMinutes)},
            {Hour * 2,  t => "an hour ago"},
            {Day,  t => String.Format("{0} hours ago", (int)t.TotalHours)},
            {Day * 2,  t => "yesterday"},
            {Day * 30,  t => String.Format("{0} days ago", (int)t.TotalDays)},
            {Day * 60,  t => "last month"},
            {Year,  t => String.Format("{0} months ago", (int)t.TotalDays / 30)},
            {Year * 2,  t => "last year"},
            {Int64.MaxValue,  t => String.Format("{0} years ago", (int)t.TotalDays / 365)}
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dateTime = (DateTime)value;
            var difference = DateTime.UtcNow - dateTime.ToUniversalTime();

            return _thresholds.First(t => difference.TotalSeconds < t.Key).Value(difference);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

}
