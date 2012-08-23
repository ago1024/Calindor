/*
 * Copyright (C) 2007-2008 Krzysztof 'DeadwooD' Smiechowicz
 * Original project page: http://sourceforge.net/projects/calindor/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 */

using System;

namespace Calindor.Misc.Profiling
{
    // Abstracts
    public interface IPerformanceProfiler
    {
        void StartCycle();
        void StopCycle();
        string Name
        {
            get;
        }
            
    }
    
    public class PerformanceProfilerEventArgs : EventArgs
    {
    }
    
    public delegate void PerformanceProfilerEventHandler(object o, PerformanceProfilerEventArgs args);
    
    public class PerformanceProfilerException : ApplicationException
    {
        public PerformanceProfilerException(string message):base(message)
        {
        }
    }
    
    // Implementation
    public class ExecutionTimeProfiler : IPerformanceProfiler
    {
        private enum ExecutionTimeProfilerState
        {
            ExpectingStart = 0,
            ExpectingStop = 0
        }
        
        private ExecutionTimeProfilerState state =
            ExecutionTimeProfilerState.ExpectingStart;
        
        public event PerformanceProfilerEventHandler PeriodElapsed;
        
        private void firePeriodElapsedEvent()
        {
            if (PeriodElapsed != null)
            {
                ExecutionTimeProfilerEventArgs args =
                    new ExecutionTimeProfilerEventArgs();
                args.AverageTicksPerCycleLastPeriod = periodAverageTicksPerCycle;
                args.AverageTicksPerCycleTotal = totalAverageTicksPerCycle;
                args.ProfilerName = Name;
                
                PeriodElapsed(this, args);
            }
        }
        
        private string name = "UNNAMED";
        public string Name
        {
            get { return name; }
        }
        
        // Data used for profiling
        private long periodLengthInTicks = 0;
        private long periodStartTicks = 0;
        private long cycleStartTicks = 0;
        private long cycleStopTicks = 0;
        
        // Period
        private long periodCycleDifferenceTicksSum = 0;
        private int periodCyclesCount = 0;
        private long periodAverageTicksPerCycle = 0;
        
        // Total
        private long totalPeriodDifferenceTicksSum = 0;
        private int totalPeriodsCount = 0;
        private long totalAverageTicksPerCycle = 0;
        
        private ExecutionTimeProfiler()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">
        /// A <see cref="System.String"/>
        /// </param>
        /// <param name="periodLength">
        /// Lenght of averaging period in miliseconds  <see cref="System.Int32"/>
        /// </param>
        public ExecutionTimeProfiler(string name, int periodLength)
        {
            this.name = name;
            this.periodLengthInTicks = periodLength * 10000;
            startNewPeriod();
        }
        
        private void startNewPeriod()
        {
            periodStartTicks = DateTime.Now.Ticks;
        }
        
        private void checkForPeriodEnd()
        {
            if ((cycleStopTicks - periodStartTicks) > periodLengthInTicks)
            {
                if (periodCyclesCount > 0)
                    periodAverageTicksPerCycle = periodCycleDifferenceTicksSum / periodCyclesCount;
                else
                    periodAverageTicksPerCycle = -1;

                totalPeriodDifferenceTicksSum += periodAverageTicksPerCycle;
                totalPeriodsCount++;
                
                if (totalPeriodsCount > 0)
                    totalAverageTicksPerCycle = totalPeriodDifferenceTicksSum / totalPeriodsCount;
                else
                    totalAverageTicksPerCycle = -1;

                firePeriodElapsedEvent();

                resetPeriodData();

                startNewPeriod();
            }
        }
        
        private void resetPeriodData()
        {
            periodCycleDifferenceTicksSum = 0;
            periodCyclesCount = 0;
        }
        
        private void calculateDataForCycle()
        {
            periodCycleDifferenceTicksSum += (cycleStopTicks - cycleStartTicks);
            periodCyclesCount++;
        }
        
        public void StartCycle()
        {
            if (state != ExecutionTimeProfilerState.ExpectingStart)
                throw new PerformanceProfilerException("Profiler should expect start");
            
            cycleStartTicks = DateTime.Now.Ticks;
            
            state = ExecutionTimeProfilerState.ExpectingStop;
        }
        
        public void StopCycle()
        {
            if (state != ExecutionTimeProfilerState.ExpectingStop)
                throw new PerformanceProfilerException("Profiler should expect stop");
            
            cycleStopTicks = DateTime.Now.Ticks;
            
            calculateDataForCycle();
            
            checkForPeriodEnd();
            
            state = ExecutionTimeProfilerState.ExpectingStart;
        }
    }
    
    public class ExecutionTimeProfilerEventArgs : PerformanceProfilerEventArgs
    {
        private long averageTicksPerCycleLastPeriod = 0;
        public long AverageTicksPerCycleLastPeriod
        {
            get { return averageTicksPerCycleLastPeriod; }
            set { averageTicksPerCycleLastPeriod = value; }
        }

        private long averageTicksPerCycleTotal = 0;
        public long AverageTicksPerCycleTotal
        {
            get { return averageTicksPerCycleTotal; }
            set { averageTicksPerCycleTotal = value; }
        }
        
        private string profilerName = "UNNAMED";
        public string ProfilerName
        {
            get { return profilerName; }
            set { profilerName = value; }
        }
    }

}
