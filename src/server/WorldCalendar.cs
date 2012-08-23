/*
 * Copyright (C) 2007 Krzysztof 'DeadwooD' Smiechowicz
 * Original project page: http://sourceforge.net/projects/calindor/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Calindor.Server
{
    public class WorldCalendar
    {
        // TODO: Add counting days/weeks/months/years
        private UInt32 secondOfTheDay = 0; //TODO: Load from persistent storage
        private long lastSecondChangeTicks = long.MinValue;

        public  UInt32 TicksPerInGameSecond
        {
            get { return 10000000; } // TODO: Configurable?
        }

        public UInt16 Second
        {
            get { return (UInt16)(secondOfTheDay % 60); }
        }

        public UInt16 Minute
        {
            get { return (UInt16)(secondOfTheDay / 60 % 60); }
        }

        public UInt16 Hour
        {
            get { return (UInt16)(secondOfTheDay / 3600); }
        }

        public override string ToString()
        {
            return string.Format("{0:d02}:{1:d02}:{2:d02}", Hour, Minute, Second);
        }

        /// <summary>
        /// In range (0,359)
        /// </summary>
        public UInt16 MinuteOfTheDay
        {
            get { return (UInt16)(secondOfTheDay / 60); }
        }

        public void UpdateTime()
        {
            long currentTicks = DateTime.Now.Ticks;
            if (lastSecondChangeTicks == long.MinValue)
            {
                lastSecondChangeTicks = currentTicks;
            }
            else
            {
                long diff = currentTicks - lastSecondChangeTicks;
                if (diff > TicksPerInGameSecond)
                {
                    // Change second
                    UInt32 secondsToAdd = (UInt32)(diff / TicksPerInGameSecond);
                    secondOfTheDay += secondsToAdd;
                    if (secondOfTheDay >= 21600)
                        secondOfTheDay -= 21600; //Day reset
                    lastSecondChangeTicks = currentTicks - (diff % TicksPerInGameSecond);
                }
            }
        }
    }
}
