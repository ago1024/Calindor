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

        protected int getMilisSinceLastExecution()
        {
            return (int)((DateTime.Now.Ticks - lastExecutedTick)) / 10000;
        }

        protected void updateLastExecutionTime()
        {
            lastExecutedTick = DateTime.Now.Ticks;
        }

        protected void setMilisBetweenExecutions(uint milisBetweenExecutions)
        {
            this.milisBetweenExecutions = milisBetweenExecutions;
        }

        public virtual void Execute()
        {
            if (getMilisSinceLastExecution() > milisBetweenExecutions)
            {
                execute();
                updateLastExecutionTime();
            }
        }

        protected abstract void execute();
    }
}
