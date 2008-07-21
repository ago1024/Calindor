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
using Calindor.Server.Items;
using Calindor.Server.Entities;

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        private void handlePM(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                SendPMIncommingMessage msgSendPM = (SendPMIncommingMessage)msg;
                PlayerCharacter sendToPlayer = getPlayerByName(msgSendPM.RecipientName);

                if (sendToPlayer == null)
                {
                    RawTextOutgoingMessage msgToSender =
                        (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                    msgToSender.Color = PredefinedColor.Blue1;
                    msgToSender.Channel = PredefinedChannel.CHAT_LOCAL;
                    msgToSender.Text = "The one you seek is not here...";
                    pc.PutMessageIntoMyQueue(msgToSender);
                }
                else
                {
                    RawTextOutgoingMessage msgToSender =
                        (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                    msgToSender.Color = PredefinedColor.Purple1;
                    msgToSender.Channel = PredefinedChannel.CHAT_PERSONAL;
                    msgToSender.Text = "[PM to " + sendToPlayer.Name + ": " + msgSendPM.Text + "]";
                    pc.PutMessageIntoMyQueue(msgToSender);

                    RawTextOutgoingMessage msgToRecipient =
                        (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                    msgToRecipient.Color = PredefinedColor.Purple1;
                    msgToRecipient.Channel = PredefinedChannel.CHAT_PERSONAL;
                    msgToRecipient.Text = "[PM from " + pc.Name + ": " + msgSendPM.Text + "]";
                    sendToPlayer.PutMessageIntoMyQueue(msgToRecipient);
                }
            }
        }

        private void handleServerStats(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                RawTextOutgoingMessage msgRawTextOut =
                    (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawTextOut.Text = String.Format("Active players: {0}", activePlayerCharacters.Count);
                msgRawTextOut.Color = PredefinedColor.Blue1;
                pc.PutMessageIntoMyQueue(msgRawTextOut);
                return;
            }
        }

        private void handleGetTime(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                RawTextOutgoingMessage msgRawTextOut =
                    (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawTextOut.Text = String.Format("Game Time: " + calendar.ToString());
                msgRawTextOut.Color = PredefinedColor.Blue1;
                pc.PutMessageIntoMyQueue(msgRawTextOut);
                return;
            }
        }

        private void handleRawText(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                RawTextIncommingMessage msgRawText = (RawTextIncommingMessage)msg;

                // Handling for different types of raw text
                // TODO: Rewrite - add a separate module for handling server commands
                switch (msgRawText.Text[0])
                {
                    case('#'):
                        if (msgRawText.Text.ToLower() == "#save")
                        {
                            RawTextOutgoingMessage msgRawTextOut =
                                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;


                            try
                            {
                                pc.Serialize(pcSerializer);
                                msgRawTextOut.Text = "Another page in Book of Life is filled...";
                                msgRawTextOut.Color = PredefinedColor.Blue1;
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(LogSource.World, "Failed to serialize player " + pc.Name, ex);
                                msgRawTextOut.Text = "Your page in Book of Life remains blank...";
                                msgRawTextOut.Color = PredefinedColor.Red2;
                            }

                            pc.PutMessageIntoMyQueue(msgRawTextOut);
                            return;
                        }
                        if (msgRawText.Text.ToLower() == "#list_commands")
                        {
                            RawTextOutgoingMessage msgRawTextOut =
                                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                            msgRawTextOut.Color = PredefinedColor.Green3;
                            msgRawTextOut.Text = "Available commands: list_commands, save, follow, stop_following, release_followers, list_skills";
                            pc.PutMessageIntoMyQueue(msgRawTextOut);

                            msgRawTextOut =
                                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                            msgRawTextOut.Color = PredefinedColor.Green3;
                            msgRawTextOut.Text = "beam me, beam to x,y, beam to map, loc";
                            pc.PutMessageIntoMyQueue(msgRawTextOut);
                            return;
                        }
                        if (msgRawText.Text.ToLower() == "#stop_following")
                        {
                            pc.FollowingStopFollowing();
                            return;
                        }
                        if (msgRawText.Text.ToLower() == "#release_followers")
                        {
                            pc.FollowingReleaseFollowers();
                            return;
                        }
                        if (msgRawText.Text.ToLower().IndexOf("#follow") == 0)
                        {
                            RawTextOutgoingMessage msgRawTextOut =
                                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;

                            string[] tokens = msgRawText.Text.Split(' ');
                            if (tokens.Length != 2)
                            {
                                msgRawTextOut.Color = PredefinedColor.Red2;
                                msgRawTextOut.Text = "You are replied by silence...";
                                pc.PutMessageIntoMyQueue(msgRawTextOut);
                                return;
                            }

                            // Does character exist
                            PlayerCharacter pcTakenByHand = getPlayerByName(tokens[1]);
                            if (pcTakenByHand == null)
                            {
                                msgRawTextOut.Color = PredefinedColor.Blue1;
                                msgRawTextOut.Text = "The one you seek is not here...";
                                pc.PutMessageIntoMyQueue(msgRawTextOut);
                                return;
                            }

                            pc.FollowingFollow(pcTakenByHand);

                            return;
                        }
                        if ((msgRawText.Text.ToLower().IndexOf("#change_health") != -1) &&
                            serverConfiguration.EnableTestCommands)
                        {
                            string[] tokens = msgRawText.Text.Split(' ');
                            short changeVal = Convert.ToInt16(tokens[1]);
                            pc.EnergiesUpdateHealth(changeVal);
                            return;
                        }
                        if ((msgRawText.Text.ToLower().IndexOf("#resurrect") != -1) &&
                            serverConfiguration.EnableTestCommands)
                        {
                            pc.EnergiesResurrect();
                            return;
                        }
                        if ((msgRawText.Text.ToLower().IndexOf("#loc") != -1))
                        {
                            RawTextOutgoingMessage msgRawTextOut =
                                                                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                            msgRawTextOut.Color = PredefinedColor.Green3;
                            msgRawTextOut.Text = String.Format("You are on {0},{1}", pc.LocationX, pc.LocationY);
                            pc.PutMessageIntoMyQueue(msgRawTextOut);
                            return;
                        }
                        if ((msgRawText.Text.ToLower().IndexOf("#shutdown") != -1) && serverConfiguration.IsAdminUser(pc.Name))
                        {
                            StopSimulation();
                            return;
                        }
                        if ((msgRawText.Text.ToLower().IndexOf("#kick") != -1) && serverConfiguration.IsAdminUser(pc.Name))
                        {
                            string[] tokens = msgRawText.Text.Split(' ');
                            if (tokens.Length != 2)
                               return;

                            RawTextOutgoingMessage msgRawTextOut = (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;

                            PlayerCharacter pcToKick = getPlayerByName(tokens[1]);
                            if (pcToKick == null)
                            {
                                msgRawTextOut.Color = PredefinedColor.Blue1;
                                msgRawTextOut.Text = "The player does not exist";
                                pc.PutMessageIntoMyQueue(msgRawTextOut);
                                return;
                            }
                            else
                            {

                                msgRawTextOut.Color = PredefinedColor.Red3;
                                msgRawTextOut.Text = "You just got kicked by " + pc.Name;
                                pcToKick.PutMessageIntoMyQueue(msgRawTextOut);
                                pcToKick.LoginState = PlayerCharacterLoginState.LoggingOff;
                            }

                            return;
                        }
                        if ((msgRawText.Text.ToLower().IndexOf("#wall ") != -1) && serverConfiguration.IsAdminUser(pc.Name))
                        {
                            RawTextOutgoingMessage msgRawTextOut =
                                                                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                            msgRawTextOut.Color = PredefinedColor.Red3;
                            msgRawTextOut.Text = "Server message: " + msgRawText.Text.Substring(6);
                            sendMessageToAllPlayers(msgRawTextOut);
                            return;
                        }
                        if ((msgRawText.Text.ToLower().IndexOf("#beam me") != -1) &&
                            serverConfiguration.EnableTestCommands)
                        {
                            pc.LocationChangeMap(
                                serverConfiguration.StartingPoint.MapName,
                                serverConfiguration.StartingPoint.StartX,
                                serverConfiguration.StartingPoint.StartY);
                            return;
                        }
                        if ((msgRawText.Text.ToLower().IndexOf("#beam to") != -1) &&
                            serverConfiguration.EnableTestCommands)
                        {
                            RawTextOutgoingMessage msgRawTextOut =
                                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;

                            string mapname;
                            string coords;
                            string[] tokens = msgRawText.Text.Split(' ');
                            if (tokens.Length == 3)
                            {
                                mapname = null;
                                coords = tokens[2];
                            }
                            else if (tokens.Length == 4)
                            {
                                mapname = tokens[2];
                                coords = tokens[3];
                            }
                            else if (tokens.Length == 5)
                            {
                                mapname = tokens[3];
                                coords = tokens[4];
                            }
                            else
                            {
                                msgRawTextOut.Color = PredefinedColor.Red2;
                                msgRawTextOut.Text = "use #beam to [mapname] x,y";
                                pc.PutMessageIntoMyQueue(msgRawTextOut);
                                return;
                            }

                            try
                            {
                                tokens = coords.Split(',');

                                short newX = Convert.ToInt16(tokens[0]);
                                short newY = Convert.ToInt16(tokens[1]);

                                msgRawTextOut.Color = PredefinedColor.Green3;

                                if (mapname == null)
                                {
                                    msgRawTextOut.Text = String.Format("Teleporting to {0},{1}", newX, newY);
                                    pc.PutMessageIntoMyQueue(msgRawTextOut);
                                    pc.LocationChangeLocation(newX, newY);
                                }
                                else
                                {
                                    msgRawTextOut.Text = String.Format("Teleporting to map {2} {0},{1}", newX, newY, mapname);
                                    pc.PutMessageIntoMyQueue(msgRawTextOut);
                                    pc.LocationChangeMap(mapname, newX, newY);
                                }
                                return;
                            }
                            catch
                            {
                                msgRawTextOut.Color = PredefinedColor.Red2;
                                msgRawTextOut.Text = "use #beam to [mapname] x,y";
                                pc.PutMessageIntoMyQueue(msgRawTextOut);
                            }
                            return;
                        }

                        if ((msgRawText.Text.ToLower().IndexOf("#add_item") != -1) &&
                            serverConfiguration.EnableTestCommands)
                        {
                            string[] tokens = msgRawText.Text.Split(' ');
                            if (tokens.Length != 3)
                                return;

                            ushort itemID = Convert.ToUInt16(tokens[1]);
                            int quantity = Convert.ToInt32(tokens[2]);

                            ItemDefinition itmDef = ItemDefinitionCache.GetItemDefinitionByID(itemID);
                            if (itmDef == null)
                                return;
                            Item itm = new Item(itmDef);
                            itm.Quantity = quantity;
                            pc.InventoryUpdateItem(itm);

                        }
                        if (msgRawText.Text.ToLower() == "#list_skills")
                        {
                            pc.SkillsListSkills();
                        }
                        break;
                    case (':'):
                        {
                            RawTextOutgoingMessage msgRawTextOut =
                                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                            msgRawTextOut.Color = PredefinedColor.Grey1;
                            msgRawTextOut.Text = pc.Name + " " + msgRawText.Text.Substring(1);
                            sendMessageToAllPlayersNear(pc.LocationX, pc.LocationY, pc.LocationCurrentMap, msgRawTextOut);
                            break;
                        }
                    case ('@'):
                        {
                            RawTextOutgoingMessage msgRawTextOut =
                                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                            msgRawTextOut.Color = PredefinedColor.Grey1;
                            msgRawTextOut.Text = String.Format("[{0} @ global]: {1}", pc.Name, msgRawText.Text.Substring(1));
                            sendMessageToAllPlayers(msgRawTextOut);
                            break;
                        }
                    default:
                        {
                            RawTextOutgoingMessage msgRawTextOut =
                                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                            msgRawTextOut.Color = PredefinedColor.Grey1;
                            msgRawTextOut.Text = pc.Name + ": " + msgRawText.Text;
                            sendMessageToAllPlayersNear(pc.LocationX, pc.LocationY, pc.LocationCurrentMap, msgRawTextOut);
                            break;
                        }
                }
            }
        }

    }
}
