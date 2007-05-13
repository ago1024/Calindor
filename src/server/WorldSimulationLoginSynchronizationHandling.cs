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
using System.Text;
using System.Threading;
using Calindor.Server.Messaging;
using Calindor.Server.Entities;
using Calindor.Server.Maps;
using Calindor.Server.Items;

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        private PlayerCharacterSerializer pcSerializer = null;// Used for storing pc data
        private PlayerCharacterDeserializer pcDeserializer = null;// Used for restoring pc data
        private PlayerCharacterAuthentication pcAuthentication = null;// User for login

        private void handleSendVersion(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.NotInitialized)
            {
                SendVersionIncommingMessage svMsg = (SendVersionIncommingMessage)msg;

                if (serverConfiguration.CheckProtocolVersion)
                {
                    if (svMsg.ProtocolVersionFirstDigit == ProtocolVersion.FirstDigit &&
                        svMsg.ProtocolVersionSecondDigit == ProtocolVersion.SecondDigit)
                    {
                        pc.LoginState = PlayerCharacterLoginState.ClientVersionCorrect;
                    }
                    else
                    {
                        // Wrong version
                        pc.LoginState = PlayerCharacterLoginState.ClientVersionIncorrect;
                        pc.PutMessageIntoMyQueue(msgStdWrongClientVersion);
                        pc.LoginState = PlayerCharacterLoginState.LoggingOff; // Start purging procedure
                    }
                }
                else
                {
                    // Always correct
                    pc.LoginState = PlayerCharacterLoginState.ClientVersionCorrect;
                }
            }
        }

        private void handleCreateChar(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.ClientVersionCorrect)
            {
                CreateCharIncommingMessage msgCreateChar = (CreateCharIncommingMessage)msg;

                // Is acceptable character name
                if (!pcAuthentication.IsAcceptablePlayerName(msgCreateChar.UserName))
                {
                    CreateCharNotOkOutgoingMessage msgCreateCharNotOk =
                        (CreateCharNotOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CREATE_CHAR_NOT_OK);
                    msgCreateCharNotOk.Message = "This character name is not acceptable.";
                    pc.PutMessageIntoMyQueue(msgCreateCharNotOk);
                    return;
                }

                // Does character exist?
                if (pcAuthentication.Exists(msgCreateChar.UserName))
                {
                    // Character already exists
                    CreateCharNotOkOutgoingMessage msgCreateCharNotOk =
                        (CreateCharNotOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CREATE_CHAR_NOT_OK);
                    msgCreateCharNotOk.Message = "A character with that name already exists.";
                    pc.PutMessageIntoMyQueue(msgCreateCharNotOk);
                    return;
                }

               

               

                // TODO: Add check for appearace values

                // All ok. Create a character
                try
                {
                    pcAuthentication.Create(msgCreateChar.UserName, msgCreateChar.Password);
                }
                catch (Exception ex)
                {
                    logger.LogError(LogSource.World, "Failed to create player " + pc.Name, ex);
                    CreateCharNotOkOutgoingMessage msgCreateCharNotOk =
                        (CreateCharNotOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CREATE_CHAR_NOT_OK);
                    msgCreateCharNotOk.Message = "Server could not create the character.";
                    pc.PutMessageIntoMyQueue(msgCreateCharNotOk);
                    return;
                }

                // Clear character (current state of character is undefined!)
                pc.ClearEntityImplementation();

                EntityAppearance appearance = new EntityAppearance(msgCreateChar.Type);

                appearance.Head = msgCreateChar.Head;
                appearance.Skin = msgCreateChar.Skin;
                appearance.Hair = msgCreateChar.Hair;
                appearance.Shirt = msgCreateChar.Shirt;
                appearance.Pants = msgCreateChar.Pants;
                appearance.Boots = msgCreateChar.Boots;
                pc.Name = msgCreateChar.UserName;
                pc.CreateSetInitialAppearance(appearance);

                EntityLocation location = new EntityLocation();
                short deviation = mapManager.StartPointDeviation;
                location.X = (short)(mapManager.StartPointX + (sbyte)WorldRNG.Next(-deviation, deviation));
                location.Y = (short)(mapManager.StartPointY + (sbyte)WorldRNG.Next(-deviation, deviation));
                location.Z = 0;
                location.Rotation = 0;
                location.CurrentMap = mapManager.StartPointMap;
                pc.CreateSetInitialLocation(location);

		        // TODO: Apply race specific factors

                // Add bonus items
                ItemDefinition itmDef = null;
                
                // Royals
                itmDef = ItemDefinitionCache.GetItemDefinitionByID(5);
                if (itmDef != null)
                {
                    Item itm = new Item(itmDef);
                    itm.Quantity = 100;
                    pc.InventoryUpdateItem(itm);
                }

                // Recalculate enerigies
                pc.CreateRecalculateInitialEnergies();
                
                // Store data
                try
                {
                    pc.ForceSerialization();
                    pc.Serialize(pcSerializer);
                }
                catch (Exception ex)
                {
                    logger.LogError(LogSource.World, "Failed to serialize player " + pc.Name, ex);
                    CreateCharNotOkOutgoingMessage msgCreateCharNotOk =
                        (CreateCharNotOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CREATE_CHAR_NOT_OK);
                    msgCreateCharNotOk.Message = "Server could not create the character.";
                    pc.PutMessageIntoMyQueue(msgCreateCharNotOk);
                    return;
                }

                // Send char created ok message
                pc.PutMessageIntoMyQueue(msgStdCreateCharOk);
            }
        }

        private void handleLogIn(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.ClientVersionCorrect)
            {
                LogInIncommingMessage msgLogIn = (LogInIncommingMessage)msg;

                // Is acceptable character name
                if (!pcAuthentication.IsAcceptablePlayerName(msgLogIn.UserName))
                {
                    LogInNotOkOutgoingMessage msgLogInNotOk =
                        (LogInNotOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.LOG_IN_NOT_OK);
                    msgLogInNotOk.Message = "This character name is not acceptable.";
                    pc.PutMessageIntoMyQueue(msgLogInNotOk);
                    return;
                }

                // Does character exist
                if (!pcAuthentication.Exists(msgLogIn.UserName))
                {
                    pc.PutMessageIntoMyQueue(msgStdYouDontExist);
                    return;
                }

                
                try
                {
                    // Check the password
                    if (!pcAuthentication.Authenticate(msgLogIn.UserName, msgLogIn.Password))
                    {
                        LogInNotOkOutgoingMessage msgLogInNotOk =
                            (LogInNotOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.LOG_IN_NOT_OK);
                        msgLogInNotOk.Message = "The password is wrong!";
                        pc.PutMessageIntoMyQueue(msgLogInNotOk);
                        return;
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(LogSource.World, "Failed to authenticate player: " + pc.Name, ex);
                    LogInNotOkOutgoingMessage msgLogInNotOk =
                        (LogInNotOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.LOG_IN_NOT_OK);
                    msgLogInNotOk.Message = "Server could not load the character!";
                    pc.PutMessageIntoMyQueue(msgLogInNotOk);
                    return;
                }


                // Check if already logged in
                if (getPlayerByName(msgLogIn.UserName) != null)
                {
                    LogInNotOkOutgoingMessage msgLogInNotOk =
                        (LogInNotOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.LOG_IN_NOT_OK);
                    msgLogInNotOk.Message = "You are already logged in!";
                    pc.PutMessageIntoMyQueue(msgLogInNotOk);
                    return;
                }

                // Clear character (current state of character is undefined!)
                pc.ClearEntityImplementation();

                pc.Name = msgLogIn.UserName; // Temporary setting user name so that deserialization may work

                // Deserialize user data
                try
                {
                    pc.Deserialize(pcDeserializer);
                }
                catch (Exception ex)
                {
                    logger.LogError(LogSource.World, "Failed to deserialize player: " + pc.Name, ex);
                    LogInNotOkOutgoingMessage msgLogInNotOk =
                        (LogInNotOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.LOG_IN_NOT_OK);
                    msgLogInNotOk.Message = "Server could not load the character!";
                    pc.PutMessageIntoMyQueue(msgLogInNotOk);
                    return;
                }

                try
                {
                    // Add to dictionaries / Get EntityID
                    addEntityImplementationToWorld(pc);
                }
                catch (Exception ex)
                {
                    logger.LogError(LogSource.World, "Failed to register player: " + pc.Name, ex);
                    LogInNotOkOutgoingMessage msgLogInNotOk =
                        (LogInNotOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.LOG_IN_NOT_OK);
                    msgLogInNotOk.Message = "Server could not register the character!";
                    pc.PutMessageIntoMyQueue(msgLogInNotOk);
                    return;
                }

                // TODO: Send initial data to client

                // New Minute
                NewMinuteOutgoingMessage msgNewMinute =
                    (NewMinuteOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.NEW_MINUTE);
                msgNewMinute.MinuteOfTheDay = calendar.MinuteOfTheDay;
                pc.PutMessageIntoMyQueue(msgNewMinute);

                // You Are
                YouAreOutgoingMessage msgYouAre =
                    (YouAreOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.YOU_ARE);
                msgYouAre.EntityID = pc.EntityID;
                pc.PutMessageIntoMyQueue(msgYouAre);

                // Change Map
                pc.LocationChangeMapAtEnterWorld();

                // Here Your Inventory
                HereYourInventoryOutgoingMessage msgHereYourInventory =
                    (HereYourInventoryOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.HERE_YOUR_INVENTORY);
                pc.FillOutgoingMessage(msgHereYourInventory);
                pc.PutMessageIntoMyQueue(msgHereYourInventory);

                // Here Your Stats
                HereYourStatsOutgoingMessage msgHereYourStats =
                    (HereYourStatsOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.HERE_YOUR_STATS);
                pc.FillOutgoingMessage(msgHereYourStats);
                pc.PutMessageIntoMyQueue(msgHereYourStats);

                // Log In Ok
                pc.PutMessageIntoMyQueue(msgStdLogInOk);

                // All is OK
                pc.LoginState = PlayerCharacterLoginState.LoginSuccesfull;
            }
        }

        private void handleSendOpeningScreen(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.ClientVersionCorrect ||
                pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                // Send opening message
                pc.PutMessageIntoMyQueue(msgStdOpeningMessage);
            }
        }

    }
}