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
using Calindor.Server.Items;
using Calindor.Server.Entities;
using Calindor.Server.AI;

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
                        if ((msgRawText.Text.ToLower().IndexOf("#change_health") != -1 ||
                            msgRawText.Text.ToLower().IndexOf("#restore") != -1) &&
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
                        if ((msgRawText.Text.ToLower().IndexOf("#shapeshift") != -1))
                        {
                            RawTextOutgoingMessage msgRawTextOut = (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                            PlayerCharacter pcToChange = pc;
                            string shape = null;

                            string[] tokens = msgRawText.Text.Split(' ');
                            if (tokens.Length == 3 && serverConfiguration.IsAdminUser(pc.Name))
                            {
                                pcToChange = getPlayerByName(tokens[1]);
                                if (pcToChange == null)
                                {
                                    msgRawTextOut.Color = PredefinedColor.Red1;
                                    msgRawTextOut.Text = "The player does not exist";
                                    pc.PutMessageIntoMyQueue(msgRawTextOut);
                                    return;
                                }
                                shape = tokens[2];
                            }
                            else if (tokens.Length == 2)
                            {
                                shape = tokens[1];
                            }
                            else
                            {
                                return;
                            }

                            PredefinedModelType type;
                            int num;
                            if (Int32.TryParse(shape, out num))
                            {
                                type = (PredefinedModelType)num;
                                if (!playerModels.hasModel(type))
                                {
                                    msgRawTextOut.Color = PredefinedColor.Red1;
                                    msgRawTextOut.Text = "Invalid shape";
                                    pc.PutMessageIntoMyQueue(msgRawTextOut);
                                    return;
                                }
                            } else
                            {
                                shape = shape.ToLower();
                                if (!playerModels.hasModel(shape))
                                {
                                    msgRawTextOut.Color = PredefinedColor.Red1;
                                    msgRawTextOut.Text = "Invalid shape";
                                    pc.PutMessageIntoMyQueue(msgRawTextOut);
                                    return;
                                }
                                type = playerModels.getType(shape);
                            }
                            pcToChange.Appearance.Type = type;
                            pcToChange.LocationChangeLocation(pcToChange.LocationX, pcToChange.LocationY);
                        }
                        if ((msgRawText.Text.ToLower().IndexOf("#attach") != -1))
                        {
                            RawTextOutgoingMessage msgRawTextOut = (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                            msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                            PlayerCharacter pcToChange = pc;

                            string[] tokens = msgRawText.Text.Split(' ');
                            if (tokens.Length == 2 && serverConfiguration.IsAdminUser(pc.Name))
                            {
                                pcToChange = getPlayerByName(tokens[1]);
                                if (pcToChange == null)
                                {
                                    msgRawTextOut.Color = PredefinedColor.Red1;
                                    msgRawTextOut.Text = "The player does not exist";
                                    pc.PutMessageIntoMyQueue(msgRawTextOut);
                                    return;
                                }
                            }

                            if (pcToChange.IsAttached)
                                pcToChange.UnAttach();
                            else
                                pcToChange.AttachTo(PredefinedModelType.HORSE);
                            pcToChange.LocationChangeLocation(pcToChange.LocationX, pcToChange.LocationY);
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

        private class PlayerModels
        {
            private Dictionary<string, PredefinedModelType> models = new Dictionary<string, PredefinedModelType>();

            public PlayerModels()
            {
                models.Add("human_female", (PredefinedModelType)0);
                models.Add("human_male", (PredefinedModelType)1);
                models.Add("elf_female", (PredefinedModelType)2);
                models.Add("elf_male", (PredefinedModelType)3);
                models.Add("dwarf_female", (PredefinedModelType)4);
                models.Add("dwarf_male", (PredefinedModelType)5);
                models.Add("wraith", (PredefinedModelType)6);
                models.Add("cyclops", (PredefinedModelType)7);
                models.Add("beaver", (PredefinedModelType)8);
                models.Add("rat", (PredefinedModelType)9);
                models.Add("goblin_male_2", (PredefinedModelType)10);
                models.Add("armed_male_goblin", (PredefinedModelType)10);
                models.Add("goblin_female_1", (PredefinedModelType)11);
                models.Add("female_goblin", (PredefinedModelType)11);
                models.Add("deer", (PredefinedModelType)15);
                models.Add("bear_1", (PredefinedModelType)16);
                models.Add("grizzly", (PredefinedModelType)16);
                models.Add("grizzly_bear", (PredefinedModelType)16);
                models.Add("wolf", (PredefinedModelType)17);
                models.Add("white_rabbit", (PredefinedModelType)18);
                models.Add("brown_rabbit", (PredefinedModelType)19);
                models.Add("boar", (PredefinedModelType)20);
                models.Add("bear_2", (PredefinedModelType)21);
                models.Add("black_bear", (PredefinedModelType)21);
                models.Add("snake_1", (PredefinedModelType)22);
                models.Add("green_snake", (PredefinedModelType)22);
                models.Add("snake_2", (PredefinedModelType)23);
                models.Add("red_snake", (PredefinedModelType)23);
                models.Add("snake_3", (PredefinedModelType)24);
                models.Add("brown_snake", (PredefinedModelType)24);
                models.Add("snake_4", (PredefinedModelType)76);
                models.Add("sslessar", (PredefinedModelType)76);
                models.Add("fox", (PredefinedModelType)25);
                models.Add("puma", (PredefinedModelType)26);
                models.Add("ogre_male_1", (PredefinedModelType)27);
                models.Add("ogre", (PredefinedModelType)27);
                models.Add("goblin_male_1", (PredefinedModelType)28);
                models.Add("male_goblin", (PredefinedModelType)28);
                models.Add("orc_male_1", (PredefinedModelType)29);
                models.Add("male_orc", (PredefinedModelType)29);
                models.Add("orc_female_1", (PredefinedModelType)30);
                models.Add("female_orc", (PredefinedModelType)30);
                models.Add("skeleton", (PredefinedModelType)31);
                models.Add("gargoyle_1", (PredefinedModelType)32);
                models.Add("medium_gargoyle", (PredefinedModelType)32);
                models.Add("gargoyle_2", (PredefinedModelType)33);
                models.Add("tall_gargoyle", (PredefinedModelType)33);
                models.Add("gargoyle_3", (PredefinedModelType)34);
                models.Add("small_gargoyle", (PredefinedModelType)34);
                models.Add("troll", (PredefinedModelType)35);
                models.Add("chimeran_mountain_wolf", (PredefinedModelType)36);
                models.Add("mountain_chim", (PredefinedModelType)36);
                models.Add("gnome_female", (PredefinedModelType)37);
                models.Add("gnome_male", (PredefinedModelType)38);
                models.Add("orchan_female", (PredefinedModelType)39);
                models.Add("orchan_male", (PredefinedModelType)40);
                models.Add("draegoni_female", (PredefinedModelType)41);
                models.Add("draegoni_male", (PredefinedModelType)42);
                models.Add("skunk_1", (PredefinedModelType)43);
                models.Add("skunk", (PredefinedModelType)43);
                models.Add("racoon_1", (PredefinedModelType)44);
                models.Add("racoon", (PredefinedModelType)44);
                models.Add("unicorn_1", (PredefinedModelType)45);
                models.Add("unicorn", (PredefinedModelType)45);
                models.Add("chimeran_desert_wolf", (PredefinedModelType)46);
                models.Add("desert_chim", (PredefinedModelType)46);
                models.Add("chimeran_forest_wolf", (PredefinedModelType)47);
                models.Add("forest_chim", (PredefinedModelType)47);
                models.Add("chimeran_arctic_wolf", (PredefinedModelType)54);
                models.Add("arctic_chim", (PredefinedModelType)54);
                models.Add("bear_3", (PredefinedModelType)48);
                models.Add("polar_bear", (PredefinedModelType)48);
                models.Add("bear_4", (PredefinedModelType)49);
                models.Add("panda_bear", (PredefinedModelType)49);
                models.Add("panther", (PredefinedModelType)50);
                models.Add("leopard_1", (PredefinedModelType)52);
                models.Add("leopard", (PredefinedModelType)52);
                models.Add("leopard_2", (PredefinedModelType)53);
                models.Add("snow_leopard", (PredefinedModelType)53);
                models.Add("feran", (PredefinedModelType)51);
                models.Add("tiger_1", (PredefinedModelType)55);
                models.Add("tiger", (PredefinedModelType)55);
                models.Add("tiger_2", (PredefinedModelType)56);
                models.Add("snow_tiger", (PredefinedModelType)56);
                models.Add("armed_female_orc", (PredefinedModelType)57);
                models.Add("armed_male_orc", (PredefinedModelType)58);
                models.Add("armed_skeleton", (PredefinedModelType)59);
                models.Add("phantom_warrior", (PredefinedModelType)60);
                models.Add("imp", (PredefinedModelType)61);
                models.Add("brownie", (PredefinedModelType)62);
                models.Add("spider_big_1", (PredefinedModelType)67);
                models.Add("large_spider", (PredefinedModelType)67);
                models.Add("spider_big_2", (PredefinedModelType)68);
                models.Add("spider_big_3", (PredefinedModelType)69);
                models.Add("spider_big_4", (PredefinedModelType)71);
                models.Add("spider_small_1", (PredefinedModelType)64);
                models.Add("small_spider", (PredefinedModelType)64);
                models.Add("spider_small_2", (PredefinedModelType)65);
                models.Add("spider_small_3", (PredefinedModelType)66);
                models.Add("spider_small_4", (PredefinedModelType)72);
                models.Add("wood_sprite", (PredefinedModelType)70);
                models.Add("leprechaun", (PredefinedModelType)63);
                models.Add("giant_1", (PredefinedModelType)73);
                models.Add("giant", (PredefinedModelType)73);
                models.Add("hobgoblin", (PredefinedModelType)74);
                models.Add("yeti", (PredefinedModelType)75);
                models.Add("feros", (PredefinedModelType)77);
                models.Add("dragon1", (PredefinedModelType)78);
                models.Add("red_dragon", (PredefinedModelType)78);
                models.Add("dragon2", (PredefinedModelType)85);
                models.Add("black_dragon", (PredefinedModelType)85);
                models.Add("dragon3", (PredefinedModelType)87);
                models.Add("ice_dragon", (PredefinedModelType)87);
                models.Add("hawk", (PredefinedModelType)82);
                models.Add("falcon", (PredefinedModelType)83);
                models.Add("lion", (PredefinedModelType)84);
                models.Add("cockatrice", (PredefinedModelType)86);
                models.Add("chinstrap_penguin", (PredefinedModelType)81);
                models.Add("gentoo_penguin", (PredefinedModelType)79);
                models.Add("king_penguin", (PredefinedModelType)80);
                models.Add("bird_phoenix", (PredefinedModelType)91);
                models.Add("phoenix", (PredefinedModelType)91);
                models.Add("dragon2_blue", (PredefinedModelType)88);
                models.Add("blue_dragon", (PredefinedModelType)88);
                models.Add("dragon2_gray", (PredefinedModelType)89);
                models.Add("gray_dragon", (PredefinedModelType)89);
                models.Add("dragon2_pink", (PredefinedModelType)90);
                models.Add("pink_dragon", (PredefinedModelType)90);
                models.Add("mule1_black", (PredefinedModelType)92);
                models.Add("black_mule", (PredefinedModelType)92);
                models.Add("mule1_brown", (PredefinedModelType)93);
                models.Add("brown_mule", (PredefinedModelType)93);
                models.Add("mule", (PredefinedModelType)93);
                models.Add("mule1_gray", (PredefinedModelType)94);
                models.Add("gray_mule", (PredefinedModelType)94);
            }

            public PredefinedModelType getType(string name)
            {
                if (hasModel(name))
                    return models[name];

                return PredefinedModelType.HUMAN_FEMALE;
            }

            public bool hasModel(string name)
            {
                return models.ContainsKey(name);
            }

            public bool hasModel(PredefinedModelType type)
            {
                return models.ContainsValue(type);
            }
        }
        private PlayerModels playerModels = new PlayerModels();
    }
}
