using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Photostudio.Models
{
    public class Calendar
    {
        public int IdHall { get; private set; }
        public DateTime StartWeekDate { get;private set; }
        public DateTime EndWeekDate { get;private set; }
        public List<DateTime[]> ListDates { get; private set; } = new List<DateTime[]>();
        public DateTime[] IntervalDays { get; private set; }
        public Calendar(int idHall,DateTime startWeekDate, DateTime endWeekDate, List<DateTime[]> listDates)
        {
            IdHall = idHall;
            StartWeekDate = startWeekDate;
            EndWeekDate = endWeekDate;
            ListDates = listDates;
            IntervalDays = GetIntervalDays();
        }

        public string CalendarInterval()
        {
            string interval = string.Empty;
            CultureInfo ci = new CultureInfo("ru-RU");
            interval = $"{StartWeekDate.ToString("M",ci)} - {EndWeekDate.ToString("M",ci)}, {EndWeekDate.Year}";
            return interval;
        }

        public int GetIdDayOfWeek(DateTime date)
        {
            int index = 0;
            if (date.DayOfWeek == 0)
            {
                index = 7;
            }
            else
            {
                index = (int)date.DayOfWeek;
            }
            return index;
        }

        public DateTime[] GetIntervalDays()
        {
            DateTime startDay = new DateTime(StartWeekDate.Year,StartWeekDate.Month,StartWeekDate.Day);
            DateTime[] interval = new DateTime[7];
            for (int i = 0; i < 7; i++)
            {
                interval[i] = startDay;
                startDay = startDay.AddDays(1);
            }
            return interval;
        }

        public bool isContainedTimeInInterval(int hour,int dayOfWeek)
        {
            DateTime selectedDateTime = new DateTime(IntervalDays[dayOfWeek - 1].Year, IntervalDays[dayOfWeek - 1].Month, IntervalDays[dayOfWeek - 1].Day);
            selectedDateTime = selectedDateTime.AddHours(hour);

            for (int i = 0; i < ListDates.Count; i++)
            {
                if (selectedDateTime >= ListDates[i][0] && selectedDateTime < ListDates[i][1])
                {
                    return true;
                }
            }
            return false;
        }

        public bool isSuccessOrder(int hour,int dayOfWeek)
        {
            DateTime selectedDateTime = new DateTime(IntervalDays[dayOfWeek - 1].Year, IntervalDays[dayOfWeek - 1].Month, IntervalDays[dayOfWeek - 1].Day);
            selectedDateTime = selectedDateTime.AddHours(hour);
            if (selectedDateTime < DateTime.Now)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string IntervalDaysToString(int index)
        {
            CultureInfo ci = new CultureInfo("ru-RU");
            return IntervalDays[index].ToString("dd MMM", ci);
        }
    }
}
