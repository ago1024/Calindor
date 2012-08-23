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
using Calindor.Server.Entities;
using Calindor.Server.Messaging;

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        /// <summary>
        /// Adds/removes entities according to visibility
        /// </summary>
        /// <param name="pc"></param>
        public void handleVisibilityChangeEvents(PlayerCharacter pc)
        {
            pc.VisibilityUpdateVisibleEntities();
        }
    }
}