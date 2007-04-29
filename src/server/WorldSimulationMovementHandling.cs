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
using Calindor.Server.Messaging;
using Calindor.Misc.Predefines;
using Calindor.Server.Maps;
using Calindor.Server.TimeBasedActions;
using Calindor.Server.Entities;

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        private void handleMoveTo(PlayerCharacter pc, IncommingMessage msg)
        { 
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                MoveToIncommingMessage msgMoveTo = (MoveToIncommingMessage)msg;

                pc.LocationMoveTo(msgMoveTo.X, msgMoveTo.Y);
            }
            
        }

        private void handleSitDown(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                SitDownIncommingMessage msgSitDown = (SitDownIncommingMessage)msg;

                if (msgSitDown.ShouldSit)
                {
                    pc.LocationSitDown();
                }
                else
                {
                    pc.LocationStandUp();
                }
            }
        }

        private void handleTurnLeft(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                pc.LocationTurnLeft();
            }
        }

        private void handleTurnRight(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                pc.LocationTurnRight();
            }
        }
    }
}