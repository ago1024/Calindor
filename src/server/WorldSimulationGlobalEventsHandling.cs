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
using Calindor.Server.Messaging;

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        // World calendar
        private WorldCalendar calendar = new WorldCalendar();
        private UInt16 lastMinuteOfTheDay = UInt16.MaxValue; // Initial value
        

        private void handleGlobalEvents()
        {
            handleCalendarEvents();
        }

        private void handleCalendarEvents()
        {
            // TODO: Test
            calendar.UpdateTime();

            if (calendar.MinuteOfTheDay != lastMinuteOfTheDay)
            { 
                // Minute changes. Send information.
                NewMinuteOutgoingMessage msg =
                    (NewMinuteOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.NEW_MINUTE);
             
                msg.MinuteOfTheDay = calendar.MinuteOfTheDay;

                sendMessageToAllPlayers(msg);

                lastMinuteOfTheDay = msg.MinuteOfTheDay;
            }
        }
    }
}