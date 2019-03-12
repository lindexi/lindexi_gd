/**
/// ScrimpNet.Core Library
/// Copyright © 2005-2011
///
/// This module is Copyright © 2005-2011 Steve Powell
/// All rights reserved.
///
/// This library is free software; you can redistribute it and/or
/// modify it under the terms of the Microsoft Public License (Ms-PL)
/// 
/// This library is distributed in the hope that it will be
/// useful, but WITHOUT ANY WARRANTY; without even the implied
/// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
/// PURPOSE.  See theMicrosoft Public License (Ms-PL) License for more
/// details.
///
/// You should have received a copy of the Microsoft Public License (Ms-PL)
/// License along with this library; if not you may 
/// find it here: http://www.opensource.org/licenses/ms-pl.html
///
/// Steve Powell, spowell@scrimpnet.com
**/
using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlTypes;

namespace ScrimpNet
{
    public static partial class Utils
    {
        /// <summary>
        /// Date and time methods including SQL compatibility, and business logic (e.g. last days of month)
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords")]
        public static class Date
        {
            
            /// <summary>
            /// Get's abbreviation of time zone on local machine (EST, PDT, etc)
            /// </summary>
            public static string TZAbbreviation
            {
                get
                {
                    
                    string daylightName = "";
                    if (TimeZone.IsDaylightSavingTime(DateTime.Now, new System.Globalization.DaylightTime(DateTime.Now, DateTime.Now, new TimeSpan(0, 0, 30))))
                    {
                        daylightName = TimeZone.CurrentTimeZone.DaylightName;
                    }
                    else
                    {
                        daylightName = TimeZone.CurrentTimeZone.StandardName;
                    }
                    string zoneName = "";        
                    string[] parts = daylightName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        zoneName += part[0];
                    }
                    return zoneName;
                }
            }

            #region DayBegin
            /// <summary>
            /// Beginning of Day. Earliest datetime for current date
            /// </summary>
            /// <returns>DateTime</returns>
            public static DateTime DayBegin()
            {
                return DayBegin(DateTime.Now);
            }

            /// <summary>
            /// Beginning of Day. Earliest datetime for <b>date</b>
            /// </summary>
            /// <param name="date">Date value to convert</param>
            /// <returns>new datetime value</returns>
            public static DateTime DayBegin(DateTime date)
            {
                return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0);
            }

            #endregion DayBegin

            #region DayEnd
            /// <summary>
            /// End of Day. Return last datetime for current date.  23:59:59.800
            /// </summary>
            /// <returns>Datetime</returns>
            public static DateTime DayEnd()
            {
                return DayEnd(DateTime.Now);
            }

            /// <summary>
            /// End of Day. Return latest datetime for <b>date</b>
            /// </summary>
            /// <param name="date">Date to change into last datetime</param>
            /// <returns>new datetime value</returns>
            public static DateTime DayEnd(DateTime date)
            {
                return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 990);
            }
            #endregion DayEnd

            #region WeekBegin
            /// <summary>
            /// Returns the first day of week based on current date. Week starts on Sunday
            /// </summary>
            /// <returns>Date for first day of week</returns>
            public static DateTime WeekBegin()
            {
                return WeekBegin(DayOfWeek.Sunday);
            }

            /// <summary>
            /// Returns the first day of week based on current date based on defined day of week.
            /// </summary>
            /// <param name="firstDayOfWeek">First day of week</param>
            /// <returns>Date for first day of week</returns>
            public static DateTime WeekBegin(DayOfWeek firstDayOfWeek)
            {
                return WeekBegin(DateTime.Now, firstDayOfWeek);
            }

            /// <summary>
            /// Returns first day of week based on date.  Sunday is first day.
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <returns>Date for first day of week</returns>
            public static DateTime WeekBegin(DateTime date)
            {
                return WeekBegin(date, DayOfWeek.Sunday);
            }

            /// <summary>
            /// Returns first day of week based on date using arbitrary first day of week
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstDayOfWeek">First day of week</param>
            /// <returns>Date for first day of week</returns>
            public static DateTime WeekBegin(DateTime date, DayOfWeek firstDayOfWeek)
            {
                return WeekEnd(date, firstDayOfWeek).AddDays(-7);
            }
            #endregion WeekBegin

            #region WeekEnd
            /// <summary>
            /// End of Week. Returns the last day of the week for the current date
            /// </summary>
            /// <returns>datetime</returns>
            /// <remarks>Weeks are defined Sunday-Saturday</remarks>
            public static DateTime WeekEnd()
            {
                return WeekEnd(DayOfWeek.Sunday);
            } //WeekEnd()

            /// <summary>
            /// Returns the last day of the week for the current date.
            /// </summary>
            /// <param name="firstDayOfWeek">Day for start of week.  Often Sunday or Monday</param>
            /// <returns>Date for last day of week</returns>
            public static DateTime WeekEnd(DayOfWeek firstDayOfWeek)
            {
                return WeekEnd(DateTime.Now, firstDayOfWeek);
            }
            /// <summary>
            /// Returns the last day of the week.
            /// </summary>
            /// <param name="firstDayOfWeek">Day for start of week.  Often Sunday or Monday</param>
            /// <param name="date">Date to check</param>
            /// <returns>Date for last day of week</returns>
            public static DateTime WeekEnd(DateTime date, DayOfWeek firstDayOfWeek)
            {
                int delta = 0;
                switch (date.DayOfWeek)
                {
                    case System.DayOfWeek.Sunday: delta = 6; break;
                    case System.DayOfWeek.Monday: delta = 5; break;
                    case System.DayOfWeek.Tuesday: delta = 4; break;
                    case System.DayOfWeek.Wednesday: delta = 3; break;
                    case System.DayOfWeek.Thursday: delta = 2; break;
                    case System.DayOfWeek.Friday: delta = 1; break;
                    case System.DayOfWeek.Saturday: delta = 0; break;
                }
                return DayEnd(date.AddDays(delta));
            }

            /// <summary>
            /// Returns the last day of the week for which <b>date</b> is part. Week starts on Sunday.
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <returns>Date for last day of week.</returns>
            public static DateTime WeekEnd(DateTime date)
            {
                return WeekEnd(date, DayOfWeek.Sunday);
            } //WeekEnd()
            #endregion WeekEnd

            #region MonthBegin
            /// <summary>
            /// Gets first day for calendar Month for current date. Note: Returns date with 00:00:00.000 time component
            /// </summary>
            /// <returns>First day of month</returns>
            public static DateTime MonthBegin()
            {
                return MonthBegin(DateTime.Now);
            }

            /// <summary>
            /// Gets first day of Month for calendar Month for a specific <paramref name="date"/> Note: Returns date with 00:00:00.000 time component
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <returns>First day of month</returns>
            public static DateTime MonthBegin(DateTime date)
            {
                return new DateTime(date.Year, date.Month, 1, 0, 0, 0);
            }

            #endregion

            #region MonthEnd
            /// <summary>
            /// End of Month for current date().  Note: Returns date with 23:59:59.800 time component
            /// </summary>
            /// <returns>Latest datetime for current month</returns>
            public static DateTime MonthEnd()
            {
                return MonthEnd(DateTime.Now);

            } //MonthEnd()

            /// <summary>
            /// End of Month MonthEnd()  Note: Returns date with 23:59:59.999 time component
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <returns>Latest datetime for month</returns>
            public static DateTime MonthEnd(DateTime date)
            {
                return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 23, 23, 59, 999);
            } //MonthEnd()

       
            /// <summary>
            /// Determines if a value is a weekendday (Saturday,Sunday)
            /// </summary>
            /// <param name="date">Date to evaluate</param>
            /// <returns>True if date is weekendday (Saturday,Sunday)</returns>
            public static bool IsWeekEndDay(DateTime date)
            {
                return !IsWeekDay(date);
            }

            /// <summary>
            /// Determines if a value is a weekday (Monday-Friday)
            /// </summary>
            /// <param name="date">Date to evaluate</param>
            /// <returns>True if date is weekday (Monday-Friday)</returns>
            public static bool IsWeekDay(DateTime date)
            {
                if (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday)
                    return false;
                return true;
            }
            #endregion MonthEnd

            #region QuarterBegin
            /// <summary>
            /// Beginning of Quarter. Get earliest datetime value for <paramref name="quarter"/> for year starting in January
            /// </summary>
            /// <param name="quarter">Quarter number (1-4)</param>
            /// <returns>First date in quarter</returns>
            public static DateTime QuarterBegin(int quarter)
            {
                return QuarterBegin(quarter, DateTime.Now, SNetMonth.January);
            }

            /// <summary>
            /// Beginning of Quarter.  Get earliest date for <paramref name="quarter"/> with year starting in <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="quarter">Quarter to get</param>
            /// <param name="dateInYear">Random date in year that quarter will belong to.  Used to determine correct returned year</param>
            /// <param name="firstMonthOfYear">First Month of year. (Often fiscal year)</param>
            /// <returns>First date in quarter</returns>
            public static DateTime QuarterBegin(int quarter, DateTime dateInYear, SNetMonth firstMonthOfYear)
            {
                return QuarterBegin(quarter, dateInYear, new DateTime(DateTime.MinValue.Year, monthToInt(firstMonthOfYear), 1));
            }

            /// <summary>
            /// Beginning date of a specific quarter. Get earliest date for <paramref name="quarter"/> with year starting in <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="quarter">Quarter to return starting date for</param>
            /// <param name="dateInYear">Random date in year that quarter will belong to.  Used to determine correct returned year</param>
            /// <param name="firstDayOfYear">Date calendar starts. Year component ignored</param>
            /// <returns>DateTime of first day quarter begins</returns>
            public static DateTime QuarterBegin(int quarter, DateTime dateInYear, DateTime firstDayOfYear)
            {
                DateTime dte = YearBegin(dateInYear, firstDayOfYear);
                dte = dte.AddMonths((quarter - 1) * 3);
                return dte;
            }

            /// <summary>
            /// Returns the first date of a quarter for a particular date
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <returns>DateTime of first day of quarter</returns>
            public static DateTime QuarterBegin(DateTime date)
            {
                return QuarterBegin(date, SNetMonth.January);
            }

            /// <summary>
            /// Returns the first date of a current quarter for the current date for year beginning in January
            /// </summary>
            /// <returns>DateTime of first day of quarter</returns>
            public static DateTime QuarterBegin()
            {
                return QuarterBegin(DateTime.Now, SNetMonth.January);
            }

            /// <summary>
            /// Returns the first date of current quarter for the current date for year beginning in <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="firstMonthOfYear">First Month of year. (Often fiscal)</param>
            /// <returns>Date of first Month of year</returns>
            public static DateTime QuarterBegin(SNetMonth firstMonthOfYear)
            {
                return QuarterBegin(DateTime.Now, firstMonthOfYear);
            }

            /// <summary>
            /// Returns the first date of the quarter of <paramref name="date"/> for year beginning in <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstMonthOfYear">First Month of year. (Often fiscal)</param>
            /// <returns>Date of first part of quarter</returns>
            public static DateTime QuarterBegin(DateTime date, SNetMonth firstMonthOfYear)
            {
                return QuarterBegin(date, new DateTime(DateTime.MinValue.Year, monthToInt(firstMonthOfYear), 1));
            }

            /// <summary>
            /// Returns the first date for the quarter for <paramref name="date"/> for year beginning in <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstDayOfYear">First day of year. (Often fiscal)</param>
            /// <returns>Date of first day in quarter</returns>
            public static DateTime QuarterBegin(DateTime date, DateTime firstDayOfYear)
            {
                DateTime qBegin = YearBegin(date, firstDayOfYear); //Jan=1
                DateTime qEnd = MonthEnd(qBegin.AddMonths(3)); //Mar=3

                for (int quarter = 1; quarter <= 4; quarter++)
                {
                    if (qBegin <= date && date <= qEnd)
                    {
                        return qBegin;
                    }
                    qBegin = qBegin.AddMonths(3);
                    qEnd = MonthEnd(qBegin.AddMonths(3));
                }
                throw new NotImplementedException("Unable to find quarter start for date");
            }
            #endregion QuarterBegin

            #region QuarterEnd
            /// <summary>
            /// End of Quarter QuarterEnd()
            /// </summary>
            /// <returns>Last datetime value for current quarter</returns>
            public static DateTime QuarterEnd()
            {
                return QuarterEnd(DateTime.Now.Date, SNetMonth.January);
            }

            /// <summary>
            /// Get last date of quarter with year starting on <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="firstMonthOfYear">First Month of year. (Often fiscal)</param>
            /// <returns>First date of quarter</returns>
            public static DateTime QuarterEnd(SNetMonth firstMonthOfYear)
            {
                return QuarterEnd(DateTime.Now, firstMonthOfYear);
            }

            /// <summary>
            /// End of Quarter for a date for year starting in January
            /// </summary>
            /// <param name="date">Date which is part of a quarter</param>
            /// <returns>last datetime value for quarter</returns>
            public static DateTime QuarterEnd(DateTime date)
            {
                return QuarterEnd(date, SNetMonth.January);
            } //QuarterEnd

            /// <summary>
            /// End of current quarter of <paramref name="date"/> for year starting in <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstMonthOfYear">First Month in year. (Often fiscal year)</param>
            /// <returns>Last date in quarter</returns>
            public static DateTime QuarterEnd(DateTime date, SNetMonth firstMonthOfYear)
            {
                return QuarterEnd(date, new DateTime(DateTime.MinValue.Year, monthToInt(firstMonthOfYear), 1));

            }
            /// <summary>
            /// End of quarter for year beginning in January
            /// </summary>
            /// <param name="quarter">Quarter to find</param>
            /// <returns>Last date of quarter</returns>
            public static DateTime QuarterEnd(int quarter)
            {
                return QuarterEnd(quarter, DateTime.Now, SNetMonth.January);
            }

            /// <summary>
            /// Get last date in quarter for year beginning on <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="quarter">Quarter to check</param>
            /// <param name="dateInYear">Random date in year that quarter will belong to.  Used to determine correct returned year</param>
            /// <param name="firstMonthOfYear">First Month of year.  (Often fiscal)</param>
            /// <returns>DateTime of last day in quarter</returns>
            public static DateTime QuarterEnd(int quarter, DateTime dateInYear, SNetMonth firstMonthOfYear)
            {
                return QuarterEnd(quarter, dateInYear, new DateTime(DateTime.MinValue.Year, monthToInt(firstMonthOfYear), 1));
            }

            /// <summary>
            /// Get last date in quarter for year beginning on <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="quarter">Quarter to check</param>
            /// <param name="dateInYear">Random date in year that quarter will belong to.  Used to determine correct returned year</param>
            /// <param name="firstDayOfYear">Day year starts.</param>
            /// <returns>DateTime of last day in quarter</returns>
            public static DateTime QuarterEnd(int quarter, DateTime dateInYear, DateTime firstDayOfYear)
            {
                DateTime dte = QuarterBegin(quarter, dateInYear, firstDayOfYear);
                dte = dte.AddMonths(2);
                dte = MonthEnd(dte);
                return dte;
            }

            /// <summary>
            /// Returns the last date for the quarter for <paramref name="date"/> for year beginning in <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstDayOfYear">First day of year. (Often fiscal)</param>
            /// <returns>Date of last day in quarter</returns>
            public static DateTime QuarterEnd(DateTime date, DateTime firstDayOfYear)
            {
                DateTime qBegin = YearBegin(date, firstDayOfYear); //Jan=1
                DateTime qEnd = MonthEnd(qBegin.AddMonths(2)); //Mar=3

                for (int quarter = 1; quarter <= 4; quarter++)
                {
                    if (qBegin <= date && date <= qEnd)
                    {
                        return qEnd;
                    }
                    qBegin = qBegin.AddMonths(3);
                    qEnd = MonthEnd(qBegin.AddMonths(2));
                }
                throw new ArgumentException("Unable to find Quarter End For Quarter");
            }
            #endregion QuarterEnd

            #region YearBegin
            /// <summary>
            /// Get first day of year for current datetime with year starting in January
            /// </summary>
            /// <returns>DateTime for first day of year</returns>
            public static DateTime YearBegin()
            {
                return YearBegin(DateTime.Now, SNetMonth.January);
            }

            /// <summary>
            /// Get first day of year for current datetime with year starting in <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="firstMonthOfYear">First Month of year (Often fiscal)</param>
            /// <returns>Date of first day of year</returns>
            public static DateTime YearBegin(SNetMonth firstMonthOfYear)
            {
                return YearBegin(DateTime.Now, firstMonthOfYear);
            }

            /// <summary>
            /// Get first day of year for <paramref name="date"/> for year beginning in January
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <returns>Date of first day of year</returns>
            public static DateTime YearBegin(DateTime date)
            {
                return YearBegin(date, SNetMonth.January);
            }

            /// <summary>
            /// Get first day of year for <paramref name="date"/> for year beginning on <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstMonthOfYear">First Month of year (Often fiscal)</param>
            /// <returns>Date of first day of year</returns>
            public static DateTime YearBegin(DateTime date, SNetMonth firstMonthOfYear)
            {
                return YearBegin(date, new DateTime(DateTime.MinValue.Year, monthToInt(firstMonthOfYear), 1));
            }

            /// <summary>
            /// Get first day of year for <paramref name="date"/> for year beginning on <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstDayOfYear">Day year begins on (Often fiscal)</param>
            /// <returns>First date of year</returns>
            public static DateTime YearBegin(DateTime date, DateTime firstDayOfYear)
            {
                if (date.Month >= firstDayOfYear.Month)
                {
                    return MonthBegin(new DateTime(date.Year, firstDayOfYear.Month, firstDayOfYear.Day));
                }
                //----------------------------------------------------------
                // <date> is near end of year
                //----------------------------------------------------------
                if (date.Month < firstDayOfYear.Month)
                {
                    int year = date.Year - 1;
                    return MonthBegin(new DateTime(year, firstDayOfYear.Month, firstDayOfYear.Day));
                }
                throw new ArgumentException("Unable to calculate YearBegin");
            }


            #endregion

            #region YearEnd
            /// <summary>
            /// Get last day of year for current datetime with year starting in January
            /// </summary>
            /// <returns>DateTime for last day of year</returns>
            public static DateTime YearEnd()
            {
                return YearEnd(DateTime.Now, SNetMonth.January);
            }

            /// <summary>
            /// Get last day of year for current datetime with year starting in <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="firstMonthOfYear">last Month of year (Often fiscal)</param>
            /// <returns>Date of last day of year</returns>
            public static DateTime YearEnd(SNetMonth firstMonthOfYear)
            {
                return YearEnd(DateTime.Now, firstMonthOfYear);
            }

            /// <summary>
            /// Get last day of year for <paramref name="date"/> for year beginning in January
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <returns>Date of last day of year</returns>
            public static DateTime YearEnd(DateTime date)
            {
                return YearEnd(date, SNetMonth.January);
            }

            /// <summary>
            /// Get last day of year for <paramref name="date"/> for year beginning on <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstMonthOfYear">First day of year (Often fiscal)</param>
            /// <returns>Date of last day of year</returns>
            public static DateTime YearEnd(DateTime date, SNetMonth firstMonthOfYear)
            {
                DateTime yearBegin = YearBegin(date, firstMonthOfYear);
                return YearEnd(date, yearBegin);
            }

            /// <summary>
            /// Get last day of year for <paramref name="date"/> for year beginning on <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstDayOfYear">Month/Day year begins on (Often fiscal)</param>
            /// <returns>Last date of year</returns>
            public static DateTime YearEnd(DateTime date, DateTime firstDayOfYear)
            {
                DateTime dte = YearBegin(date, firstDayOfYear); //(2/13/2007,4/1/2006)=> 4/1/2006
                dte = dte.AddYears(1); //[4/1/2007]
                dte = YearBegin(dte, firstDayOfYear).AddDays(-1); // [3/31/2007]
                return MonthEnd(dte); // [3/31/2007 23:59:50.800]
            }


            #endregion

            #region DatePart
            #region DayOfYear
            /// <summary>
            /// Return the day of year for current date/time based on 1/1 calendar
            /// </summary>
            /// <returns></returns>
            public static int DayOfYear()
            {
                return DateTime.Now.DayOfYear;
            }

            /// <summary>
            /// Return the day of year for <paramref name="date"/> where calendar begins on <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="date">Date to calculate for</param>
            /// <param name="firstDayOfYear">First day of calendar year</param>
            /// <returns>The total number of days since the first of the year</returns>
            public static int DayOfYear(DateTime date, DateTime firstDayOfYear)
            {
                DateTime dte = YearBegin(date, firstDayOfYear);
                TimeSpan ts = date.Subtract(dte);
                return ts.Days;
            }

            /// <summary>
            /// Return a particular day of year with a calendar beginning on <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="day">Day number to get</param>
            /// <param name="dateInYear">Random date in year to use for calculating correct year</param>
            /// <param name="firstDayOfYear">First day in calendar. Year component is ignored</param>
            /// <returns>DateTime of a particular day of year</returns>
            public static DateTime DayOfYear(int day, DateTime dateInYear, DateTime firstDayOfYear)
            {
                DateTime dte = YearBegin(dateInYear, firstDayOfYear);
                return dte.AddDays(day);
            }
            #endregion DayOfYear

            #region Month
            /// <summary>
            /// Return Month of year for current date/time based on 1/1 calendar
            /// </summary>
            /// <returns>Current month</returns>
            public static int Month()
            {
                return DateTime.Now.Month;
            }

            /// <summary>
            /// Return Month of year for <paramref name="date"/> based on calendar beginning on <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstDayOfYear">First day of calendar year</param>
            /// <returns>Current Month for year</returns>
            public static int Month(DateTime date, DateTime firstDayOfYear)
            {
                DateTime dteCounter = YearBegin(date, firstDayOfYear);
                DateTime endDate = MonthEnd(date);
                int Month = 1;
                for (; dteCounter <= endDate; Month++)
                {
                    if (dteCounter >= endDate)
                        return Month;
                    dteCounter = dteCounter.AddMonths(1);
                }
                return Month - 1;

            }

            /// <summary>
            /// Return first day of Month based on calendar beginning on <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="month">Month number to get</param>
            /// <param name="dateInYear">Random date in year to use for calculating correct year</param>
            /// <param name="firstDayOfYear">First day in calendar. Year component is ignored</param>
            /// <returns>DateTime of first day of month</returns>
            public static DateTime Month(int month, DateTime dateInYear, DateTime firstDayOfYear)
            {
                DateTime dte = YearBegin(dateInYear, firstDayOfYear);
                return dte.AddMonths(month - 1);
            }
            #endregion Month

            #region Quarter
            /// <summary>
            /// Return the calendar quarter for the current date for year starting in January
            /// </summary>
            /// <returns>Quarter number (1-4) for current date</returns>
            public static int Quarter()
            {
                return Quarter(DateTime.Now.Date);
            }

            /// <summary>
            /// Return the calendar quarter for the current date for year starting in <paramref name="firstMonthOfYear"/>
            /// </summary>
            /// <param name="firstMonthOfYear">First of of year. (Often fiscal year)</param>
            /// <returns>Quarter number (1-4) for current date</returns>
            public static int Quarter(SNetMonth firstMonthOfYear)
            {
                return Quarter(DateTime.Now, firstMonthOfYear);
            }

            /// <summary>
            /// Return the calendar quarter for a specific date
            /// </summary>
            /// <param name="date">datetime to check</param>
            /// <returns>Quarter number (1-4) for <b>date</b></returns>
            public static int Quarter(DateTime date)
            {
                return Quarter(date, SNetMonth.January);
            } //Quarter

            /// <summary>
            /// Return the calendar quarter for a specific date for year that begins on a specific month
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstMonthOfYear">First Month of year. (Often fiscal year)</param>
            /// <returns>Quarter number (1-4) for current date</returns>
            public static int Quarter(DateTime date, SNetMonth firstMonthOfYear)
            {
                return Quarter(date, new DateTime(date.Year, monthToInt(firstMonthOfYear), 1));
            }

            /// <summary>
            /// Return the calendar quarter for a specific date for year that begins on <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstDayOfYear">First day of year (Often fiscal)</param>
            /// <returns>Quarter number (1-4) for current date</returns>
            /// <remarks>Note:  firstDayofYear is assumed to begin at the beginning of month</remarks>
            public static int Quarter(DateTime date, DateTime firstDayOfYear)
            {
                DateTime qBegin = YearBegin(date, firstDayOfYear); //Jan=1
                DateTime qEnd = MonthEnd(qBegin.AddMonths(2)); //Mar=3

                for (int quarter = 1; quarter <= 4; quarter++)
                {
                    if (qBegin <= date && date <= qEnd)
                        return quarter;
                    qBegin = qBegin.AddMonths(3);
                    qEnd = MonthEnd(qBegin.AddMonths(2));
                }
                throw new ArgumentException("Unable to find quarter for date");
            }

            #endregion Quarter

            #region Year
            /// <summary>
            /// Return year for current date/time based on 1/1 calendar
            /// </summary>
            /// <returns>Current year</returns>
            public static int Year()
            {
                return DateTime.Now.Year;
            }

            /// <summary>
            /// Return year for <paramref name="date"/> base on calendar beginning on <paramref name="firstDayOfYear"/>
            /// </summary>
            /// <param name="date">Date to check</param>
            /// <param name="firstDayOfYear">First day of calendar year</param>
            /// <returns>Year component of last calendar day of year</returns>
            public static int Year(DateTime date, DateTime firstDayOfYear)
            {
                return YearEnd(date, firstDayOfYear).Year;
            }

            #endregion Year
            #endregion DatePart
            private static int monthToInt(SNetMonth month)
            {
                return (int)month;
            }
            /// <summary>
            /// Month numbers.  Used when system begin counting month numbers from 0 instead of 1
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
            public enum SNetMonth
            {
                /// <summary>
                /// January
                /// </summary>
                January = 1,

                /// <summary>
                /// February
                /// </summary>
                February = 2,

                /// <summary>
                /// March
                /// </summary>
                March = 3,

                /// <summary>
                /// April
                /// </summary>
                April = 4,

                /// <summary>
                /// May
                /// </summary>
                May = 5,

                /// <summary>
                /// June
                /// </summary>
                June = 6,

                /// <summary>
                /// July
                /// </summary>
                July = 7,

                /// <summary>
                /// August
                /// </summary>
                August = 8,

                /// <summary>
                /// September
                /// </summary>
                September = 9,

                /// <summary>
                /// October
                /// </summary>
                October = 10,

                /// <summary>
                /// November
                /// </summary>
                November = 11,

                /// <summary>
                /// December
                /// </summary>
                December = 12
            }
            /// <summary>
            /// Convert a value into a datetime that is safe to persist into SQL2005 since .Net and SQL have different min/max values
            /// </summary>
            /// <param name="obj">Value to convert</param>
            /// <returns>DateTime adjusted to be between SqlDateTime.Min and SqlDateTime.Max</returns>
            public static DateTime ToSqlDate(object obj)
            {
                DateTime dt = Convert.ToDateTime(obj);
                if (dt < SqlMinDate)
                    return SqlMinDate;
				if (dt > SqlMaxDate)
					return SqlMaxDate;
                return dt;
            }

			

			/// <summary>
			/// Maximum date value that can safely be stored in SQL2005,2008
			/// </summary
			public static readonly DateTime SqlMaxDate = SqlDateTime.MaxValue.Value;

			/// <summary>
			/// Minimum date value that can safely be stored in SQL2005,2008
			/// </summary
			public static readonly DateTime SqlMinDate = SqlDateTime.MinValue.Value;

			/// <summary>
			/// Maximum date that can be serialized by JSON.  JSON serializes dates by converting all dates to UTC and thus any Dates.MaxValue will exceed boundry for timezones 12 hrs from UTC (UTC-12)
			/// </summary>
			public static readonly DateTime JsonMaxDate = DateTime.MaxValue.AddDays(-2);

			/// <summary>
			/// Maximum date that can be stored in SQL2005,2008 and serialized to JSON
			/// </summary>
			public static readonly DateTime SafeMaxDate = new DateTime(Math.Min(SqlDateTime.MaxValue.Value.Ticks, JsonMaxDate.Ticks));

			/// <summary>
			/// Minimum data that can be stored in SQL2005,2008 and serialized to JSON
			/// </summary>
			public static readonly DateTime SafeMinDate = SqlMinDate.AddDays(2);

			/// <summary>
			/// Returns a date that is both JSON serializable and can be stored in SQL2005,2008
			/// </summary>
			/// <param name="dt">Date that will be adjusted to a safe date</param>
			/// <returns>A data that is safe for persisting</returns>
			public static DateTime ToSafeDate(DateTime dt)
			{
				DateTime dtMin = SqlDateTime.MinValue.Value.AddDays(2);
				if (dt < SafeMinDate)
					return SafeMinDate;
				DateTime dtMax = new DateTime(Math.Min(SqlDateTime.MaxValue.Value.Ticks,JsonMaxDate.Ticks));
				if (dt > SafeMaxDate)
					return SafeMaxDate;
				return dt;
			}
        }
    }
}
