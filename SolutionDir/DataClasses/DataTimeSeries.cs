﻿using System;

namespace TradeApplication.DataClasses
{
    /// <summary>
    /// TimeSeries data class, manage, aggregate, assemble timestamp in time intervals
    /// </summary>
    /// <remarks>
    /// TimeSeries is a very important class, since any time based data structure is dependent
    /// on TimeSeries to manage and align their data structure
    /// </remarks>
    public class DataTimeSeries : DataLoopedArray<int>
    {
        public readonly int TimeInterval; // in minutes
        private int[] timestamp_last { get; set; }

        /// <summary>
        /// DataTimeSeries constructor, set object time interval and pre-allocate looped array 
        /// </summary>
        /// <param name="interval">minute interval</param>
        /// <param name="allocn">number of rows to allocate</param>
        public DataTimeSeries(int interval,int allocn = 256) : base(allocn,2)
        {
            RowIdx = 0;
            TimeInterval = interval;

            timestamp_last = new int[2] { 0, 0 }; // zeroed timestamp_last
        }

        /// <summary>
        /// Get timestamp from new print and add to appropriate row in array
        /// RowsChanged tracks changing time intervals, RowIdx
        /// </summary>
        /// <param name="newprint">new print data array in DataCurrent.Print format</param>
        public void NewData(double[] newprint)
        {
            RowsChanged = 0;

            if (newprint.Length < 5) // minimal data verification
                return;

            int d = (int)newprint[0];
            int t = (int)newprint[1];

            // check if 'new' data, same timestamp or does not require update
            if ( ((d == timestamp_last[0]) && (t == timestamp_last[1])) ||
                 ((d == DataArray[RowIdx, 0]) && (t == DataArray[RowIdx, 1])) )
                return;
            
            int timediff; // time difference in minutes

            // initialize first value
            if (DataArray[RowIdx, 0] == 0) 
            {
                timediff = Core.MinutesDifference(d, t, Core.ORIGIN_DATE[0], Core.ORIGIN_DATE[1]);
                timediff = timediff / TimeInterval * TimeInterval;

                DataArray[RowIdx, 0] = Core.DateToDateInt( // too long
                    DateTime.Parse(Core.ORIGIN_DATE[0].ToString("0000-00-00")).AddDays(timediff / Core.MINUTES_IN_DAY) );
                DataArray[RowIdx, 1] = timediff % Core.MINUTES_IN_DAY;
                RowsChanged = 1;
                return;
            }

            timediff = Core.MinutesDifference(d, t, DataArray[RowIdx, 0], DataArray[RowIdx, 1]);
            
            if (timediff < TimeInterval) // does not require update
                return;

            RowsChanged = timediff / TimeInterval; // number of rows to move
            int ridx0 = (RowIdx + RowsChanged) % RowCount; // ridx, set to , % wrap around in array
            int dint0 = DataArray[RowIdx, 0];
                        
            timediff = DataArray[RowIdx, 1] + RowsChanged * TimeInterval; // difference used to decrement from last update
            RowsChanged = Math.Min(RowsChanged, RowCount);
            if (timediff < Core.MINUTES_IN_DAY)
            {
                // update without having to change date
                RowIdx = ridx0;
                for (int i0 = 0; i0 < RowsChanged; ++i0)
                {
                    DataArray[ridx0, 0] = dint0;
                    DataArray[ridx0, 1] = timediff;

                    ridx0 = (ridx0 == 0) ? RowCount - 1 : ridx0 - 1;
                    timediff -= TimeInterval; // previous timestamp interval
                }
            }
            else
            {
                // update tracking date change
                DateTime date0 = DateTime.Parse(DataArray[RowIdx, 0].ToString("0000-00-00"));
                date0 = date0.AddDays(timediff / Core.MINUTES_IN_DAY);
                dint0 = Core.DateToDateInt(date0); // end date int
                timediff = timediff % Core.MINUTES_IN_DAY;

                RowIdx = ridx0;
                for (int i0 = 1; i0 < RowsChanged; ++i0)
                {
                    DataArray[ridx0, 0] = dint0;
                    DataArray[ridx0, 1] = timediff;

                    ridx0 = (ridx0 == 0) ? RowCount - 1 : ridx0 - 1;
                    timediff -= TimeInterval; // previous timestamp interval
                    if (timediff < 0)
                    {
                        // change date, previous day
                        date0 = date0.AddDays(timediff / Core.MINUTES_IN_DAY - 1);
                        dint0 = Core.DateToDateInt(date0);
                        timediff += (-timediff / Core.MINUTES_IN_DAY + 1) * Core.MINUTES_IN_DAY; // reset, loop time
                    }
                }
            }
        }
    }
}
