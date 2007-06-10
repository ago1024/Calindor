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
using Calindor.Misc;

namespace Calindor.Server.AI
{
    /// <summary>
    /// Abstract AI class
    /// </summary>
    public abstract class AIImplementation : TimeBasedExecution
    {
        protected ServerCharacter me = null;
        private long lastDecitionTicks = DateTime.Now.Ticks;

        public virtual void AttachServerCharacter(ServerCharacter sc)
        {
            me = sc;
        }
        
        private AIImplementation():base(0)
        {
        }

        protected AIImplementation(uint milisBetweenDecisions):base(milisBetweenDecisions)
        {
        }
    }

    public class WonderingDumbNonAggresiveAIImplementation : AIImplementation
    {
        private short habitationRegionCenterX = -1;
        private short habitationRegionCenterY = -1;
        private double habitationRegionDiameterSquare = 0.0;
        private ushort maxAxisMove = 0;
        private WonderingDumbNonAggresiveAIImplementation():base(0)
        {
        }

        public WonderingDumbNonAggresiveAIImplementation(
            short habitationRegionCenterX, short habitationRegionCenterY, ushort habitationRegionDiameter, 
            uint milisBetweenDecisions):base(milisBetweenDecisions)
        {
            this.habitationRegionCenterX = habitationRegionCenterX;
            this.habitationRegionCenterY = habitationRegionCenterY;
            habitationRegionDiameterSquare = habitationRegionDiameter * habitationRegionDiameter;
            maxAxisMove = (ushort)(habitationRegionDiameter * 0.4);
        }

        private bool isLocationWithinHabitationRegion(short x, short y)
        {
            double dist = ((x - habitationRegionCenterX) * (x - habitationRegionCenterX)) +
                ((y - habitationRegionCenterY) * (y - habitationRegionCenterY));
            if (dist > habitationRegionDiameterSquare)
                return false;
            else
                return true;
        }
        protected override void execute()
        {
            if (me == null)
                throw new InvalidOperationException("Server character not attached");

            if (!me.EnergiesIsAlive)
                return; // Only for living entities

            // Make decision
            if (WorldRNG.NextDouble() < 0.35) //35%
            {
                // Move to new location (try up to 5 times / lame)
                for (int i = 0; i < 5; i++)
                {
                    short newX = (short)(me.LocationX + WorldRNG.Next(-maxAxisMove, maxAxisMove));
                    short newY = (short)(me.LocationY + WorldRNG.Next(-maxAxisMove, maxAxisMove));
                    
                    // is walkable
                    if (!me.LocationCurrentMap.IsLocationWalkable(newX, newY))
                        continue;

                    // is within habitation region
                    if (isLocationWithinHabitationRegion(newX, newY))
                    {
                        me.LocationMoveTo(newX, newY);
                        break;
                    }
                }
            }
            else
            {
                // Stay at current location
            }
        }
    }
}