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
using Calindor.Server.Entities;

namespace Calindor.Server.AI
{
    /// <summary>
    /// Abstract AI class
    /// </summary>
    public abstract class AIImplementation : TimeBasedSkippingExecution
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

        protected void moveSomewhere()
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

        protected void maybeMove()
        {
            // Make decision
            if (WorldRNG.NextDouble() < 0.35) //35%
            {
                moveSomewhere();
            }
            else
            {
                // Stay at current location
            }
        }

        protected void shouldIFight()
        {
            // TODO: Run if low morale (requires morale implementation)

            // If not attacking anyone, attack any attacker
            if (!me.CombatIsAttacking)
                me.CombatInitiateAttackOnAnyAttacker();
        }

        protected override void execute()
        {
            if (me == null)
                throw new InvalidOperationException("Server character not attached");

            if (!me.EnergiesIsAlive)
                return; // Only for living entities

            if (me.CombatGetNumberOfAttackers() > 0)
            {
                // Combat mode
                shouldIFight();
            }
            else
            {
                // Peace mode
                maybeMove();
            }

        }
    }

    public class AggresiveAIImplementation : WonderingDumbNonAggresiveAIImplementation
    {
        public AggresiveAIImplementation(
            short habitationRegionCenterX, short habitationRegionCenterY, ushort habitationRegionDiameter,
            uint milisBetweenDecisions)
            : base(habitationRegionCenterX, habitationRegionCenterY, habitationRegionDiameter, milisBetweenDecisions)
        {
        }

        protected override void execute()
        {
            if (me == null)
                throw new InvalidOperationException("Server character not attached");

            if (!me.EnergiesIsAlive)
                return; // Only for living entities

            if (me.CombatGetNumberOfAttackers() > 0)
            {
                // Combat mode
                shouldIFight();
            }
            else
            {
                EntityImplementation closest = null;
                int distance = 0;
                foreach (Entity entity in me.VisibleEntities)
                {
                    if (me == entity)
                        continue;

                    if (entity is PlayerCharacter)
                    {
                        if (!(entity as PlayerCharacter).EnergiesIsAlive)
                            continue;
                        int dx = me.LocationX - entity.LocationX;
                        int dy = me.LocationY - entity.LocationY;
                        int d = dx * dx + dy * dy;
                        if (closest == null || d < distance)
                        {
                            distance = d;
                            closest = entity as PlayerCharacter;
                        }
                    }
                }

                if (closest != null)
                {
                    if (me.CombatIsInDistanceToAttack(closest))
                        me.CombatInitiateAttack(closest);
                    else
                        me.LocationMoveTo(closest.LocationX, closest.LocationY);
                    return;
                }

                // Peace mode
                maybeMove();
            }
        }
    }
}
