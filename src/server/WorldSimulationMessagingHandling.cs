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
                            msgRawTextOut.Text = "Available commands: list_commands, save, take_hand, follow, stop_following, release_followers";
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
                        if (msgRawText.Text.ToLower().IndexOf("#add_item") != -1)
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
                        break;
                    default:
                        break;
                }
            }
        }
    }
}