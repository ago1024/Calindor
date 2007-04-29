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
using Calindor.Misc.Predefines;
using Calindor.Server.Resources;
using Calindor.Server.Items;

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        // TODO: Temporary implemenation. Must use sripting/configuration external to server for handling actions connected with map objects usage
        // TODO: Add checking distance from object. Execute only if close enough
        // TODO: If change map fails and player move to IP, display massage and stop script
        private void handleUseMapObject(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                UseMapObjectIncommingMessage msgUseMapObject = (UseMapObjectIncommingMessage)msg;

                // Right now only handling for changing IP and IP insides maps!!!
                switch (pc.LocationCurrentMap.Name)
                {
                    case ("startmap.elm"):
                        {
                            switch (msgUseMapObject.TargetObjectID)
                            {
                                case (501): // Secret location
                                    {
                                        RawTextOutgoingMessage msgToSender =
                                            (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                                        msgToSender.Color = PredefinedColor.Blue1;
                                        msgToSender.Channel = PredefinedChannel.CHAT_LOCAL;
                                        msgToSender.Text = "The world blurs... and you are taken to a place you didn't expect...";
                                        pc.PutMessageIntoMyQueue(msgToSender);

                                        pc.LocationChangeMap("cont2map5_insides.elm",606,133);
                                        break;
                                    }
                                case (520): // Movement to WS
                                    {
                                        RawTextOutgoingMessage msgToSender =
                                            (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                                        msgToSender.Color = PredefinedColor.Blue1;
                                        msgToSender.Channel = PredefinedChannel.CHAT_LOCAL;
                                        msgToSender.Text = "This land seems deserted and dead...";
                                        pc.PutMessageIntoMyQueue(msgToSender);

                                        pc.LocationChangeMap("map2.elm", 707, 162);
                                        break;
                                    }
                                case (1137): // IP cave
                                    {
                                        pc.LocationChangeMap("misc1.elm", 60, 138);
                                        break;
                                    }
                                case (73): // IP House, near veggies
                                    {
                                        pc.LocationChangeMap("startmap_insides.elm", 57, 13);
                                        break;
                                    }
                                case (63): // IP House, grandma
                                    {
                                        pc.LocationChangeMap("startmap_insides.elm", 15, 55);
                                        break;
                                    }
                                case (72): // IP Tavern
                                    {
                                        pc.LocationChangeMap("startmap_insides.elm", 18, 13);
                                        break;
                                    }
                                case (193): // IP hause - locked
                                    {
                                        RawTextOutgoingMessage msgToSender =
                                            (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                                        msgToSender.Color = PredefinedColor.Blue1;
                                        msgToSender.Channel = PredefinedChannel.CHAT_LOCAL;
                                        msgToSender.Text = "You hear strange noises as you approach... and the doors are locked...";
                                        pc.PutMessageIntoMyQueue(msgToSender);
                                        break;
                                    }
                                case (97): // IP hause - south of closed hause
                                    {
                                        pc.LocationChangeMap("startmap_insides.elm", 52, 49);
                                        break;
                                    }
                                case (1023): // IP tower
                                    {
                                        pc.LocationChangeMap("map7_insides.elm", 19, 66);
                                        break;
                                    }
                                default:
                                    break;
                            }

                            break;
                        }
                    case ("startmap_insides.elm"):
                        {
                            switch (msgUseMapObject.TargetObjectID)
                            {
                                case (110): // IP House, near veggies
                                    {
                                        pc.LocationChangeMap("startmap.elm", 98, 162);
                                        break;
                                    }
                                case (108): // IP House, grandma
                                    {
                                        pc.LocationChangeMap("startmap.elm", 74, 150);
                                        break;
                                    }
                                case (107): // IP Tavern
                                    {
                                        pc.LocationChangeMap("startmap.elm", 66, 132);
                                        break;
                                    }
                                case (109): // IP House, hause near locked hause
                                    {
                                        pc.LocationChangeMap("startmap.elm", 89, 106);
                                        break;
                                    }
                                default:
                                    break;
                            }

                            break;
                        }
                    case ("map7_insides.elm"):
                        {
                            switch (msgUseMapObject.TargetObjectID)
                            {
                                case (1309): // IP Tower
                                    {
                                        pc.LocationChangeMap("startmap.elm", 122, 154);
                                        break;
                                    }
                            }

                            break;
                        }
                    case ("misc1.elm"):
                        {
                            switch (msgUseMapObject.TargetObjectID)
                            {
                                case (459): // IP cave
                                    {
                                        pc.LocationChangeMap("startmap.elm", 41, 69);
                                        break;
                                    }
                            }

                            break;
                        }
                    case ("map2.elm"):
                        {
                            switch (msgUseMapObject.TargetObjectID)
                            {
                                case (1015): // Movement from WS
                                    {
                                        pc.LocationChangeMap("startmap.elm", 25, 25);
                                        break;
                                    }
                            }

                            break;
                        }
                    case ("cont2map5_insides.elm"): //PV house
                        {
                            switch (msgUseMapObject.TargetObjectID)
                            {
                                case (2309): // Return from secret place
                                    {
                                        pc.LocationChangeMap("startmap.elm", 101, 147);
                                        break;
                                    }
                                case (2303):
                                    {
                                        pc.LocationChangeLocation(605, 143);
                                        break;
                                    }
                                case (2301):
                                    {
                                        pc.LocationChangeLocation(601,137);
                                        break;
                                    }
                                case (2304):
                                    {
                                        pc.LocationChangeLocation(599, 137);
                                        break;
                                    }
                                case (2302):
                                    {
                                        pc.LocationChangeLocation(605,141);
                                        break;
                                    }
                            }

                            break;
                        }
                    default:
                        break;
                }
            }
        }

        private void handleHarvest(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                HarvestIncommingMessage msgHarvest = (HarvestIncommingMessage)msg;

                switch (pc.LocationCurrentMap.Name)
                {
                    case ("startmap.elm"):
                        {
                            switch (msgHarvest.TargetObjectID)
                            {
                                case (1141):
                                    {
                                        HarvestableResourceDefinition rscDef =
                                            new HarvestableResourceDefinition(
                                            ItemDefinitionCache.GetItemDefinitionByID(2), 1, 2000, 1);
                                        pc.HarvestStart(rscDef);
                                        break;
                                    }
                                case (575):
                                    {
                                        HarvestableResourceDefinition rscDef =
                                            new HarvestableResourceDefinition(
                                            ItemDefinitionCache.GetItemDefinitionByID(3), 5, 2000, 1);
                                        pc.HarvestStart(rscDef);
                                        break;
                                    }
                                case (574):
                                    {
                                        HarvestableResourceDefinition rscDef =
                                            new HarvestableResourceDefinition(
                                            ItemDefinitionCache.GetItemDefinitionByID(3), 5, 2000, 2);
                                        pc.HarvestStart(rscDef);
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
        }
    }
}