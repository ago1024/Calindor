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
using Calindor.Misc.Predefines;
using Calindor.Server.Entities;
using Calindor.Server.Maps;
using Calindor.Server.TimeBasedActions;

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        private Thread innerThread = null;
        private bool isWorking = false;

        private ILogger logger = new DummyLogger();
        public ILogger Logger
        {
            get { return logger; }
            set { logger = value; }
        }

        // Configuration
        private ServerConfiguration serverConfiguration = null;

        // Maps
        private MapManager mapManager = null;

        // Time based actions 
        private TimeBasedActionsManager timeBasedActionsManager = null;

        // List of active players
        private PlayerCharacterList activePlayers =
            new PlayerCharacterList();

        // List of new players to be added to the active ones
        private PlayerCharacterList newPlayers =
            new PlayerCharacterList();

        // List of players to be removed from active ones
        private PlayerCharacterList removedPlayers =
            new PlayerCharacterList();

        // Dictionary of logged in players by Name
        private NamePlayerCharacterDictionary loggedInPlayersByName =
            new NamePlayerCharacterDictionary();

        // Dictionary of world entities by EntityID
        private EntityIDEntityDictionary worldEntitiesByEntityID =
            new EntityIDEntityDictionary();
        
        

        #region STARDARD REUSABLE MESSAGES
        /*
         * CAUTION: These messages will be used for a number of connection at once. They cannot be modified.
         */
        // Opening message
        RawTextOutgoingMessage msgStdOpeningMessage 
            = (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);

        // Wrong client version
        RawTextOutgoingMessage msgStdWrongClientVersion
            = (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);

        UpgradeTooOldOutgoingMessage msgStdUpgradeClient
            = (UpgradeTooOldOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.UPGRADE_TOO_OLD);

        // LogInOk
        LogInOkOutgoingMessage msgStdLogInOk
            = (LogInOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.LOG_IN_OK);

        // CreateCharOk
        CreateCharOkOutgoingMessage msgStdCreateCharOk
            = (CreateCharOkOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CREATE_CHAR_OK);

        // You Dont Exist
        YouDontExistOutgoingMessage msgStdYouDontExist
            = (YouDontExistOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.YOU_DONT_EXIST);

        // Kill All Actors
        KillAllActorsOutgoingMessage msgStdKillAllActors
            = (KillAllActorsOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.KILL_ALL_ACTORS);

        private void createStandardMessages()
        {
            // Preparing opening message
            msgStdOpeningMessage.Text = serverConfiguration.OpeningScreen;
            msgStdOpeningMessage.Color = PredefinedColor.Green3;
            msgStdOpeningMessage.Channel = PredefinedChannel.CHAT_SERVER;

            // Preparting wrong client version
            msgStdWrongClientVersion.Text =
                string.Format("The protocol version of client is different than protocol version of Calindor ({0},{1}). The connection will be closed.", 
                ProtocolVersion.FirstDigit, ProtocolVersion.SecondDigit);
            msgStdWrongClientVersion.Channel = PredefinedChannel.CHAT_SERVER;
            msgStdWrongClientVersion.Color = PredefinedColor.Red3;

        }

        #endregion

        public WorldSimulation(ServerConfiguration conf, MapManager maps)
        {
            if (conf == null)
                throw new ArgumentNullException("conf");

            if (maps == null)
                throw new ArgumentNullException("maps");

            serverConfiguration = conf;
            mapManager = maps;
            timeBasedActionsManager = new TimeBasedActionsManager();

            pcSerializer = new PlayerCharacterSerializer(serverConfiguration.DataStoragePath);
            pcDeserializer = new PlayerCharacterDeserializer(serverConfiguration.DataStoragePath);
            pcAuthentication = new PlayerCharacterAuthentication(serverConfiguration.DataStoragePath);
        }

        public bool StartSimulation()
        {
            Logger.LogProgress(LogSource.World, "WorldSimulation starting");
            
            // Check access to storage
            if (!pcAuthentication.IsStorageAccessible())
            {
                Logger.LogError(LogSource.World, "Storage at path " + serverConfiguration.DataStoragePath +
                    " is not accessible.", null);
                return false;
            }

            // Creating thread
            ThreadStart ts = new ThreadStart(threadMain);
            innerThread = new Thread(ts);
            isWorking = true;
            innerThread.Start();

            return true;
        }

        public void StopSimulation()
        {
            isWorking = false;
        }



        private void threadMain()
        {
            // Initialize standard messages
            createStandardMessages();

            // Adds server characters to the world
            addServerCharacters();

            while (isWorking)
            {
                // Perform world simulation/update players state
                // 1. Global events (time)
                // 2. Map events (weather)
                // 2.5 Calculate entity visibility
                // 2.6 Check entity followers
                // 3. Player events
                // 4. Remove players
                // 4.5 Time based actions
                // 5. Register new players

                // STEP1. Handle global events
                handleGlobalEvents();

                // STEP2. ToDo

                // STEP2.5. Entity visibility
                // TODO: Should be done for all entities, not only players
                foreach (PlayerCharacter pc in activePlayers)
                    pc.ResetVisibleEntities();

                foreach (PlayerCharacter pc in activePlayers)
                    pc.UpdateVisibleEntities();
                
                // This is done only for player characters
                foreach (PlayerCharacter pc in activePlayers)
                    handleVisibilityChangeEvents(pc);

                // STEP2.6 Entity followers
                // TODO: Should be done for all entities, not only players
                foreach (PlayerCharacter pc in activePlayers)
                    pc.FollowingCheckForStopFollowing();
                
                // STEP3. Player events
                IncommingMessage msg = null;

                foreach (PlayerCharacter pc in activePlayers)
                {
                    // Get from queue
                    pc.GetMessages();

                    // Process
                    while ((msg = pc.GetMessageFromQueue()) != null)
                        processMessage(pc, msg);

                    // Put into queue
                    pc.SendMessages();

                    // Check for logg off conditions
                    pc.CheckForLoggingOff();

                    // If logged off -> add to remove list
                    // TODO: Add local grue event
                    if (pc.LoginState == PlayerCharacterLoginState.LoggingOff)
                        removedPlayers.Add(pc);
                }
               
                // STEP4. Remove logged off players
                if (removedPlayers.Count > 0)
                {
                    foreach (PlayerCharacter pc in removedPlayers)
                    {
                        try
                        {
                            pc.Serialize(pcSerializer); // Save state
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(LogSource.World, "Failed to serialize player " + pc.Name, ex);
                        }

                        removePlayerFromWorld(pc);
                    }

                    removedPlayers.Clear();
                }

                // STEP4.5 Execute time based actions
                timeBasedActionsManager.ExecuteActions();
                
                // STEP5. Register new players
                Monitor.TryEnter(newPlayers, 10);

                try
                {
                    activePlayers.AddRange(newPlayers);
                    newPlayers.Clear();
                }
                finally
                {

                    Monitor.Exit(newPlayers);
                }

                // Sleep
                Thread.Sleep(50);
            }

            Logger.LogProgress(LogSource.World, "WorldSimulation stopping");
        }

        private void processMessage(PlayerCharacter pc, IncommingMessage msg)
        {
            pc.UpdateHeartBeat();


            try
            {
                switch (msg.MessageType)
                {
                    case (IncommingMessageType.SEND_OPENING_SCREEN):
                        handleSendOpeningScreen(pc, msg);
                        break;
                    case (IncommingMessageType.SEND_VERSION):
                        handleSendVersion(pc, msg);
                        break;
                    case (IncommingMessageType.LOG_IN):
                        handleLogIn(pc, msg);
                        break;
                    case (IncommingMessageType.HEART_BEAT):
                        pc.UpdateHeartBeat();
                        break;
                    case (IncommingMessageType.CREATE_CHAR):
                        handleCreateChar(pc, msg);
                        break;
                    case(IncommingMessageType.MOVE_TO):
                        handleMoveTo(pc, msg);
                        break;
                    case(IncommingMessageType.SEND_PM):
                        handlePM(pc, msg);
                        break;
                    case(IncommingMessageType.SIT_DOWN):
                        handleSitDown(pc, msg);
                        break;
                    case(IncommingMessageType.TURN_LEFT):
                        handleTurnLeft(pc, msg);
                        break;
                    case (IncommingMessageType.TURN_RIGHT):
                        handleTurnRight(pc, msg);
                        break;
                    case (IncommingMessageType.RAW_TEXT):
                        handleRawText(pc, msg);
                        break;
                    case(IncommingMessageType.USE_MAP_OBJECT):
                        handleUseMapObject(pc, msg);
                        break;
                    case(IncommingMessageType.LOOK_AT_INVENTORY_ITEM):
                        handleLookAtInventoryItem(pc, msg);
                        break;
                    case(IncommingMessageType.DROP_ITEM):
                        handleDropItem(pc, msg);
                        break;
                    case(IncommingMessageType.MOVE_INVENTORY_ITEM):
                        handleMoveInventoryItem(pc, msg);
                        break;
                    case(IncommingMessageType.HARVEST):
                        handleHarvest(pc, msg);
                        break;
                    default:
                        Logger.LogWarning(LogSource.World,
                            string.Format("Message {0} - no action taken", msg.ToString()), null);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(LogSource.World,
                    string.Format("Failed to process message {0}", msg.ToString()), ex);
            }
        }

        public bool AddNewPlayer(PlayerCharacter player)
        {
            Monitor.Enter(newPlayers);

            try
            {
                newPlayers.Add(player);
                return true;
            }
            finally
            {
                Monitor.Exit(newPlayers);
            }
        }

        private void sendMessageToAllPlayers(OutgoingMessage msg)
        {
            foreach (PlayerCharacter pc in activePlayers)
                pc.PutMessageIntoMyQueue(msg);
        }

        /// <summary>
        /// Performs all necessary operations to add player to the world
        /// </summary>
        /// <param name="pc"></param>
        private void addPlayerToWorld(PlayerCharacter pc)
        {
            if (loggedInPlayersByName.ContainsKey(pc.Name.ToLower()))
                // This should not happen. It should be checked before making login successful
                throw new InvalidOperationException("Player is already in 'by name' dictionary!"); 

            loggedInPlayersByName[pc.Name.ToLower()] = pc;

            addEntityImplementationToWorld(pc);
        }

        private void addEntityImplementationToWorld(EntityImplementation enImpl)
        {
            // Searching for the next free entityID
            for (UInt16 i = 1; i < UInt16.MaxValue; i++)
                if (!worldEntitiesByEntityID.ContainsKey(i))
                {
                    enImpl.EntityID = i;
                    worldEntitiesByEntityID[i] = enImpl;
                    break;
                }

            if (enImpl.EntityID == 0)
                throw new InvalidOperationException("Could not allocate entityID to entity.");

            // Connect entity to time based actions manager
            enImpl.TimeBasedActionSetManager(timeBasedActionsManager);

            // Connecct entity to map manager
            enImpl.LocationSetMapManager(mapManager);
        }

        private PlayerCharacter getPlayerByName(string playerName)
        {
            if (loggedInPlayersByName.ContainsKey(playerName.ToLower()))
                return loggedInPlayersByName[playerName.ToLower()];
            else
                return null;
        }

        private void removePlayerFromWorld(PlayerCharacter pc)
        {
            // Total active players
            activePlayers.Remove(pc);

            // By Name dictionary
            if (loggedInPlayersByName.ContainsKey(pc.Name.ToLower()))
                loggedInPlayersByName.Remove(pc.Name.ToLower());

            removeEntityImplementationFromWorld(pc);
        }

        public void removeEntityImplementationFromWorld(EntityImplementation enImpl)
        {
            // By EntityID dictionary
            if (worldEntitiesByEntityID.ContainsKey(enImpl.EntityID))
                worldEntitiesByEntityID.Remove(enImpl.EntityID);

            // Cancel time based actions
            enImpl.TimeBasedActionCancelCurrent();

            // Remove from current map
            enImpl.LocationLeaveMapAtExitWorld();

            // Stop following
            enImpl.FollowingStopFollowing();

            enImpl.TimeBasedActionSetManager(null);
            enImpl.LocationSetMapManager(null);

        }

        private void addServerCharacters()
        {
            // TODO: Implement based on scripts!!!

            // create npc
            ServerCharacter npcOwyn = new ServerCharacter(PredefinedEntityImplementationKind.SERVER_NPC);
            EntityAppearance appearance = new EntityAppearance();
            appearance.Boots = PredefinedModelBoots.BOOTS_BROWN;
            appearance.Hair = PredefinedModelHair.HAIR_BLOND;
            appearance.Head = PredefinedModelHead.HEAD_2;
            appearance.Pants = PredefinedModelPants.PANTS_BLUE;
            appearance.Shirt = PredefinedModelShirt.SHIRT_LIGHTBROWN;
            appearance.Skin = PredefinedModelSkin.SKIN_PALE;
            appearance.Type = PredefinedEntityType.HUMAN_MALE;
            npcOwyn.Name = "Owyn";
            npcOwyn.CreateSetInitialAppearance(appearance);
            EntityLocation location = new EntityLocation();
            location.CurrentMap = mapManager.StartPointMap;
            location.Z = 0;
            location.X = (short)(mapManager.StartPointX);
            location.Y = (short)(mapManager.StartPointY + 5);
            location.Rotation = 180;
            location.IsSittingDown = false;
            npcOwyn.CreateSetInitialLocation(location);

            // add npc to the world
            addEntityImplementationToWorld(npcOwyn);
            npcOwyn.LocationChangeMapAtEnterWorld();

            

        }
    }

    public sealed class WorldRNG
    {
        private static Random rand = new Random();

        public static double NextDouble()
        {
            return rand.NextDouble();
        }

        public static int Next(int minValue, int maxValue)
        {
            return rand.Next(minValue, maxValue);
        }
    }
}