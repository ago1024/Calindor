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

namespace Calindor.Misc
{
    public abstract class TimeBasedExecution
    {
        private long lastExecutedTick = DateTime.Now.Ticks;
        private uint milisBetweenExecutions = 0;

        private TimeBasedExecution()
        {
        }

        protected TimeBasedExecution(uint milisBetweenExecutions)
        {
            setMilisBetweenExecutions(milisBetweenExecutions);
        }

        protected uint getMilisSinceLastExecution()
        {
            return (uint)((DateTime.Now.Ticks - lastExecutedTick)) / 10000;
        }
        
        protected uint getExecutionsCount()
        {
            uint executionsCount 
                = getMilisSinceLastExecution() / milisBetweenExecutions;
            
            // IMMEDIATE_EXECUTE does not increase the number of executions, it
            // forces one 'now'
            if ((executionsCount == 0) && 
                (checkPreconditions() == PreconditionsResult.IMMEDIATE_EXECUTE))
                executionsCount++;
            
            return executionsCount;
        }

        protected void updateLastExecutionTime()
        {
            lastExecutedTick = DateTime.Now.Ticks;
        }

        /// <summary>
        /// Sets miliseconds between executions.
        /// </summary>
        /// <param name="milisBetweenExecutions">
        /// If  value is 0, it's changed do 1 <see cref="System.UInt32"/>
        /// </param>
        protected void setMilisBetweenExecutions(uint milisBetweenExecutions)
        {
            if (milisBetweenExecutions == 0)
                milisBetweenExecutions = 1;
            
            this.milisBetweenExecutions = milisBetweenExecutions;
        }

        public abstract void Execute();
            
        protected abstract void execute();

        protected virtual PreconditionsResult checkPreconditions()
        {
            return PreconditionsResult.EXECUTE;
        }
    }
    
    public abstract class TimeBasedSkippingExecution : TimeBasedExecution
    {
        protected TimeBasedSkippingExecution(uint milisBetweenExecutions):
            base(milisBetweenExecutions)
        {
            setMilisBetweenExecutions(milisBetweenExecutions);
        }
        
        public override void Execute()
        {
            PreconditionsResult pResult = checkPreconditions();
            if (pResult == PreconditionsResult.NO_EXECUTE)
                return;

            /*
             * This implementation skips execution if time between 2 calls
             * to Execute() is greater than 2*milisBetweenExecution.
             * THIS IS DONE ON PURPOSE to ballance the 'slowlynes' over
             * all objects by 'loosing' an execution()
             */
            uint executionsCount = getExecutionsCount();
            
            if (executionsCount > 0)
            {
                execute();
                updateLastExecutionTime();
            }
        }
    }
    
    public abstract class TimeBasedNonSkippingExecution : TimeBasedExecution
    {
        protected TimeBasedNonSkippingExecution(uint milisBetweenExecutions):
            base(milisBetweenExecutions)
        {
            setMilisBetweenExecutions(milisBetweenExecutions);
        }

        public override void Execute()
        {
            PreconditionsResult pResult = checkPreconditions();
            if (pResult == PreconditionsResult.NO_EXECUTE)
                return;

            /*
             * This implementation WILL NOT SKIP any execution that should
             * take place. Use this approach with care as it may block other
             * objects from operation.
             */
            
            uint executionsCount = getExecutionsCount();
            
            // Update just before real execution as it might take time
            if (executionsCount > 0)
                updateLastExecutionTime();

            for(uint i = 0; i < executionsCount; i++)
            {
                pResult = checkPreconditions();
                if (pResult == PreconditionsResult.NO_EXECUTE)
                    return;
                
                execute();
            }
        }
    }

    public enum PreconditionsResult
    {
        EXECUTE,
        NO_EXECUTE,
        IMMEDIATE_EXECUTE
    }
}
