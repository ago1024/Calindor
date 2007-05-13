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

namespace Calindor.Server.AI
{
    /// <summary>
    /// Abstract AI class
    /// </summary>
    public abstract class AIImplementation
    {
        protected ServerCharacter me = null;

        public virtual void AttachServerCharacter(ServerCharacter sc)
        {
            me = sc;
        }

        public abstract void Execute();
    }

    public class WonderingNonAggresiveAIImplementation : AIImplementation
    {
        public override void Execute()
        {
            if (me == null)
                throw new InvalidOperationException("Server character not attached");
            
            // Simulate something
            if (WorldRNG.NextDouble() < 0.01) //1%
            {
                short newX = (short)(me.LocationX + WorldRNG.Next(-5, 5));
                short newY = (short)(me.LocationY + WorldRNG.Next(-5, 5));
                me.LocationMoveTo(newX, newY);
            }
        }
    }
}