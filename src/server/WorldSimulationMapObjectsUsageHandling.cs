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

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        // TODO: Temporary implemenation. Must use sripting/configuration external to server for handling actions connected with map objects usage
        // TODO: Add checking distance from object. Execute only if close enough
        private void handleUseMapObject(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                pc.CancelCurrentTimeBasedAction(); //Cancel current time based action

                UseMapObjectIncommingMessage msgUseMapObject = (UseMapObjectIncommingMessage)msg;

                ChangeMapOutgoingMessage msgChangeMap = null;
                

                // Right now only handling for changing IP and IP insides maps!!!
                switch (pc.Location.CurrentMapName)
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

                                        mapManager.ChangeMapForPlayer(pc, "cont2map5_insides.elm",606,133);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;


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

                                        mapManager.ChangeMapForPlayer(pc, "map2.elm",707,162);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;


                                        break;
                                    }
                                case (1137): // IP cave
                                    {
                                        mapManager.ChangeMapForPlayer(pc, "misc1.elm",60,133);
                                        msgChangeMap = 
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

                                        break;
                                    }
                                case (73): // IP House, near veggies
                                    {
                                        mapManager.ChangeMapForPlayer(pc, "startmap_insides.elm",57,13);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

                                        break;
                                    }
                                case (63): // IP House, grandma
                                    {
                                        mapManager.ChangeMapForPlayer(pc, "startmap_insides.elm",15,55);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

                                        break;
                                    }
                                case (72): // IP Tavern
                                    {
                                        mapManager.ChangeMapForPlayer(pc, "startmap_insides.elm",18,13);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

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
                                        mapManager.ChangeMapForPlayer(pc, "startmap_insides.elm",52,49);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

                                        break;
                                    }
                                case (1023): // IP tower
                                    {
                                        mapManager.ChangeMapForPlayer(pc, "map7_insides.elm",19,66);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

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
                                        mapManager.ChangeMapForPlayer(pc, "startmap.elm",98,162);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

                                        break;
                                    }
                                case (108): // IP House, grandma
                                    {
                                        mapManager.ChangeMapForPlayer(pc, "startmap.elm",74,150);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

                                        break;
                                    }
                                case (107): // IP Tavern
                                    {
                                        mapManager.ChangeMapForPlayer(pc, "startmap.elm",66,132);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

                                        break;
                                    }
                                case (109): // IP House, hause near locked hause
                                    {
                                        mapManager.ChangeMapForPlayer(pc, "startmap.elm",89,106);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

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
                                        mapManager.ChangeMapForPlayer(pc, "startmap.elm",122,154);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

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
                                        mapManager.ChangeMapForPlayer(pc, "startmap.elm",41,69);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

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
                                        mapManager.ChangeMapForPlayer(pc, "startmap.elm",25,25);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;

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
                                        mapManager.ChangeMapForPlayer(pc, "startmap.elm",101,147);
                                        msgChangeMap =
                                            (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
                                        msgChangeMap.MapPath = pc.Location.CurrentMap.ClientFileName;
                                        break;
                                    }
                                case (2303):
                                    {
                                        RemoveActorOutgoingMessage msgRemoveActor =
                                            (RemoveActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ACTOR);
                                        msgRemoveActor.EntityID = pc.EntityID;
                                        pc.PutMessageIntoMyAndObserversQueue(msgRemoveActor);

                                        pc.Location.X = 605;
                                        pc.Location.Y = 143;

                                        AddNewEnhancedActorOutgoingMessage msgAddNewEnchangedActor =
                                            (AddNewEnhancedActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR);
                                        msgAddNewEnchangedActor.FromPlayerCharacter(pc);
                                        pc.PutMessageIntoMyAndObserversQueue(msgAddNewEnchangedActor);
                                        break;
                                    }
                                case (2301):
                                    {
                                        RemoveActorOutgoingMessage msgRemoveActor =
                                            (RemoveActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ACTOR);
                                        msgRemoveActor.EntityID = pc.EntityID;
                                        pc.PutMessageIntoMyAndObserversQueue(msgRemoveActor);

                                        pc.Location.X = 601;
                                        pc.Location.Y = 137;

                                        AddNewEnhancedActorOutgoingMessage msgAddNewEnchangedActor =
                                            (AddNewEnhancedActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR);
                                        msgAddNewEnchangedActor.FromPlayerCharacter(pc);
                                        pc.PutMessageIntoMyAndObserversQueue(msgAddNewEnchangedActor);
                                        break;
                                    }
                                case (2304):
                                    {
                                        RemoveActorOutgoingMessage msgRemoveActor =
                                            (RemoveActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ACTOR);
                                        msgRemoveActor.EntityID = pc.EntityID;
                                        pc.PutMessageIntoMyAndObserversQueue(msgRemoveActor);

                                        pc.Location.X = 599;
                                        pc.Location.Y = 137;

                                        AddNewEnhancedActorOutgoingMessage msgAddNewEnchangedActor =
                                            (AddNewEnhancedActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR);
                                        msgAddNewEnchangedActor.FromPlayerCharacter(pc);
                                        pc.PutMessageIntoMyAndObserversQueue(msgAddNewEnchangedActor);
                                        break;
                                    }
                                case (2302):
                                    {
                                        RemoveActorOutgoingMessage msgRemoveActor =
                                            (RemoveActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ACTOR);
                                        msgRemoveActor.EntityID = pc.EntityID;
                                        pc.PutMessageIntoMyAndObserversQueue(msgRemoveActor);

                                        pc.Location.X = 605;
                                        pc.Location.Y = 141;

                                        AddNewEnhancedActorOutgoingMessage msgAddNewEnchangedActor =
                                            (AddNewEnhancedActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR);
                                        msgAddNewEnchangedActor.FromPlayerCharacter(pc);
                                        pc.PutMessageIntoMyAndObserversQueue(msgAddNewEnchangedActor);
                                        break;
                                    }
                            }

                            break;
                        }
                    default:
                        break;
                }

                if (msgChangeMap != null)
                {
                    // Map should be changed. Send the message.
                    pc.PutMessageIntoMyQueue(msgStdKillAllActors);
                    pc.PutMessageIntoMyQueue(msgChangeMap);

                    AddNewEnhancedActorOutgoingMessage msgAddNewEnhanceActor = null;
                    msgAddNewEnhanceActor =
                        (AddNewEnhancedActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR);
                    msgAddNewEnhanceActor.FromPlayerCharacter(pc);
                    pc.PutMessageIntoMyAndObserversQueue(msgAddNewEnhanceActor);
                }
            }
        }
    }
}