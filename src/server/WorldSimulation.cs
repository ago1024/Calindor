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
using Calindor.Server.AI; //TODO: Remove when script available

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
        private PlayerCharacterList activePlayerCharacters =
            new PlayerCharacterList();

        // List of new players to be added to the active ones
        private PlayerCharacterList newPlayers =
            new PlayerCharacterList();

        // List of players to be removed from active ones
        private PlayerCharacterList removedPlayers =
            new PlayerCharacterList();

        // List of active server characters
        private ServerCharacterList activeServerCharacters =
            new ServerCharacterList();

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
                // 2.7 AI
                // 3. Player events
                // 4. Remove players
                // 4.5 Time based actions
                // 5. Register new players

                // STEP1. Handle global events
                handleGlobalEvents();

                // STEP2. ToDo

                // STEP2.5. Entity visibility
                // Recalculate - server and player
                foreach (PlayerCharacter pc in activePlayerCharacters)
                    pc.ResetVisibleEntities();
                foreach (ServerCharacter sc in activeServerCharacters)
                    sc.ResetVisibleEntities();

                foreach (PlayerCharacter pc in activePlayerCharacters)
                    pc.UpdateVisibleEntities();
                foreach (ServerCharacter sc in activeServerCharacters)
                    sc.UpdateVisibleEntities();
                
                // Send - player
                foreach (PlayerCharacter pc in activePlayerCharacters)
                    handleVisibilityChangeEvents(pc);

                // STEP2.6 Entity followers
                foreach (PlayerCharacter pc in activePlayerCharacters)
                    pc.FollowingCheckForStopFollowing();
                foreach (ServerCharacter sc in activeServerCharacters)
                    sc.FollowingCheckForStopFollowing();

                // STEP2.7 AI
                foreach (ServerCharacter sc in activeServerCharacters)
                    sc.AIExecute();
                
                // STEP3. Player events
                IncommingMessage msg = null;

                foreach (PlayerCharacter pc in activePlayerCharacters)
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

                        removeEntityImplementationFromWorld(pc);
                    }

                    removedPlayers.Clear();
                }

                // STEP4.5 Execute time based actions
                timeBasedActionsManager.ExecuteActions();
                
                // STEP5. Register new players
                Monitor.TryEnter(newPlayers, 10);

                try
                {
                    activePlayerCharacters.AddRange(newPlayers);
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
                    case(IncommingMessageType.TOUCH_PLAYER):
                        handleTouchPlayer(pc, msg);
                        break;
                    case (IncommingMessageType.RESPOND_TO_NPC):
                        handleRespondToNPC(pc, msg);
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
            foreach (PlayerCharacter pc in activePlayerCharacters)
                pc.PutMessageIntoMyQueue(msg);
        }

        private void addEntityImplementationToWorld(EntityImplementation enImpl)
        {
            // Basic implementation check
            if (!(enImpl is PlayerCharacter) && !(enImpl is ServerCharacter))
                throw new ArgumentException("Unknown type of EntityImplementation: " + enImpl.GetType().ToString());

            // Type specific test operations
            if (enImpl is PlayerCharacter)
            {
                if (loggedInPlayersByName.ContainsKey(enImpl.Name.ToLower()))
                    // This should not happen. It should be checked before making login successful
                    throw new InvalidOperationException("Player is already in 'by name' dictionary!"); 
            }

            if (enImpl is ServerCharacter)
            {
                if (activeServerCharacters.Contains(enImpl as ServerCharacter))
                    // This should not happen unless bug in script logic.
                    throw new InvalidOperationException("Server character " + enImpl.Name + " already on the list of active characters!");
            }

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


            // Type specific final operations
            if (enImpl is PlayerCharacter)
            {
                loggedInPlayersByName[enImpl.Name.ToLower()] = (enImpl as PlayerCharacter);
            }

            if (enImpl is ServerCharacter)
            {
                activeServerCharacters.Add(enImpl as ServerCharacter);
            }
        }

        private PlayerCharacter getPlayerByName(string playerName)
        {
            if (loggedInPlayersByName.ContainsKey(playerName.ToLower()))
                return loggedInPlayersByName[playerName.ToLower()];
            else
                return null;
        }

        private Entity getEntityByEntityID(ushort entityID)
        {
            if (worldEntitiesByEntityID.ContainsKey(entityID))
                return worldEntitiesByEntityID[entityID];
            else
                return null;
        }

        private void removeEntityImplementationFromWorld(EntityImplementation enImpl)
        {

            // Basic implementation check
            if (!(enImpl is PlayerCharacter) && !(enImpl is ServerCharacter))
                throw new ArgumentException("Unknown type of EntityImplementation: " + enImpl.GetType().ToString());

            // Type specific remove operations
            if (enImpl is PlayerCharacter)
            {
                // Total active players
                activePlayerCharacters.Remove(enImpl as PlayerCharacter);

                // By Name dictionary
                if (loggedInPlayersByName.ContainsKey(enImpl.Name.ToLower()))
                    loggedInPlayersByName.Remove(enImpl.Name.ToLower());
            }

            if (enImpl is ServerCharacter)
            { 
                // Total server characters
                activeServerCharacters.Remove(enImpl as ServerCharacter);
            }

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
            EntityAppearance appearance = new EntityAppearance(PredefinedModelType.HUMAN_MALE);
            appearance.Boots = PredefinedModelBoots.BOOTS_BROWN;
            appearance.Hair = PredefinedModelHair.HAIR_BROWN;
            appearance.Head = PredefinedModelHead.HEAD_1;
            appearance.Pants = PredefinedModelPants.PANTS_BLUE;
            appearance.Shirt = PredefinedModelShirt.SHIRT_LIGHTBROWN;
            appearance.Skin = PredefinedModelSkin.SKIN_PALE;
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
            npcOwyn.CreateRecalculateInitialEnergies();

            for (int i = 0; i < 3; i++)
            {
                // create rat
                ServerCharacter rat1 = new ServerCharacter(PredefinedEntityImplementationKind.SERVER_ENTITY);
                EntityAppearance appearancerat = new EntityAppearance(PredefinedModelType.RAT);
                rat1.CreateSetInitialAppearance(appearancerat);
                rat1.Name = "Rat";
                EntityLocation locationrat = new EntityLocation();
                locationrat.CurrentMap = mapManager.StartPointMap;
                locationrat.Z = 0;
                locationrat.X = (short)(27 + i);
                locationrat.Y = (short)(31 + i);
                locationrat.Rotation = 180;
                locationrat.IsSittingDown = false;
                rat1.CreateSetInitialLocation(locationrat);

                // AI
                WonderingDumbNonAggresiveAIImplementation aiImpl =
                    new WonderingDumbNonAggresiveAIImplementation(locationrat.X, locationrat.Y, 20, 2000);
                rat1.AIAttach(aiImpl);

                // add rat to the world
                addEntityImplementationToWorld(rat1);
                rat1.LocationChangeMapAtEnterWorld();
                rat1.CreateRecalculateInitialEnergies();
            }

            for (int i = 0; i < 1; i++)
            {
                // create rat
                ServerCharacter rat1 = new ServerCharacter(PredefinedEntityImplementationKind.SERVER_ENTITY);
                EntityAppearance appearancerat = new EntityAppearance(PredefinedModelType.RAT);
                rat1.CreateSetInitialAppearance(appearancerat);
                rat1.Name = "Rat";
                EntityLocation locationrat = new EntityLocation();
                locationrat.CurrentMap = mapManager.StartPointMap;
                locationrat.Z = 0;
                locationrat.X = (short)(108 + i);
                locationrat.Y = (short)(166 + i);
                locationrat.Rotation = 180;
                locationrat.IsSittingDown = false;
                rat1.CreateSetInitialLocation(locationrat);

                // AI
                WonderingDumbNonAggresiveAIImplementation aiImpl =
                    new WonderingDumbNonAggresiveAIImplementation(locationrat.X, locationrat.Y, 20, 2000);
                rat1.AIAttach(aiImpl);

                // add rat to the world
                addEntityImplementationToWorld(rat1);
                rat1.LocationChangeMapAtEnterWorld();
                rat1.CreateRecalculateInitialEnergies();
            }


            for (int i = 0; i < 2; i++)
            {
                // create rabbit
                ServerCharacter rabbit1 = new ServerCharacter(PredefinedEntityImplementationKind.SERVER_ENTITY);
                EntityAppearance appearanceRabbit = new EntityAppearance(PredefinedModelType.BROWN_RABBIT);
                rabbit1.CreateSetInitialAppearance(appearanceRabbit);
                rabbit1.Name = "Rabbit";
                EntityLocation locationRabbit = new EntityLocation();
                locationRabbit.CurrentMap = mapManager.StartPointMap;
                locationRabbit.Z = 0;
                locationRabbit.X = (short)(165 + i);
                locationRabbit.Y = (short)(120 + i);
                locationRabbit.Rotation = 180;
                locationRabbit.IsSittingDown = false;
                rabbit1.CreateSetInitialLocation(locationRabbit);

                // AI
                WonderingDumbNonAggresiveAIImplementation aiImpl = 
                    new WonderingDumbNonAggresiveAIImplementation(locationRabbit.X, locationRabbit.Y, 30,3000);
                rabbit1.AIAttach(aiImpl);

                // add rabbit to the world
                addEntityImplementationToWorld(rabbit1);
                rabbit1.LocationChangeMapAtEnterWorld();
                rabbit1.CreateRecalculateInitialEnergies();
            }

            for (int i = 0; i < 2; i++)
            {
                // create rabbit
                ServerCharacter rabbit1 = new ServerCharacter(PredefinedEntityImplementationKind.SERVER_ENTITY);
                EntityAppearance appearanceRabbit = new EntityAppearance(PredefinedModelType.BROWN_RABBIT);
                rabbit1.CreateSetInitialAppearance(appearanceRabbit);
                rabbit1.Name = "Rabbit";
                EntityLocation locationRabbit = new EntityLocation();
                locationRabbit.CurrentMap = mapManager.StartPointMap;
                locationRabbit.Z = 0;
                locationRabbit.X = (short)(56 + i);
                locationRabbit.Y = (short)(110 + i);
                locationRabbit.Rotation = 180;
                locationRabbit.IsSittingDown = false;
                rabbit1.CreateSetInitialLocation(locationRabbit);

                // AI
                WonderingDumbNonAggresiveAIImplementation aiImpl =
                    new WonderingDumbNonAggresiveAIImplementation(locationRabbit.X, locationRabbit.Y, 30, 3000);
                rabbit1.AIAttach(aiImpl);

                // add rabbit to the world
                addEntityImplementationToWorld(rabbit1);
                rabbit1.LocationChangeMapAtEnterWorld();
                rabbit1.CreateRecalculateInitialEnergies();
            }

            for (int i = 0; i < 2; i++)
            {
                // create snake
                ServerCharacter snake1 = new ServerCharacter(PredefinedEntityImplementationKind.SERVER_ENTITY);
                EntityAppearance appearancesnake = new EntityAppearance(PredefinedModelType.SNAKE_1);
                snake1.CreateSetInitialAppearance(appearancesnake);
                snake1.Name = "Snake";
                EntityLocation locationsnake = new EntityLocation();
                locationsnake.CurrentMap = mapManager.StartPointMap;
                locationsnake.Z = 0;
                locationsnake.X = (short)(19 + i);
                locationsnake.Y = (short)(159 + i);
                locationsnake.Rotation = 180;
                locationsnake.IsSittingDown = false;
                snake1.CreateSetInitialLocation(locationsnake);

                // AI
                WonderingDumbNonAggresiveAIImplementation aiImpl =
                    new WonderingDumbNonAggresiveAIImplementation(locationsnake.X, locationsnake.Y, 40, 3000);
                snake1.AIAttach(aiImpl);

                // add snake to the world
                addEntityImplementationToWorld(snake1);
                snake1.LocationChangeMapAtEnterWorld();
                snake1.CreateRecalculateInitialEnergies();
            }


            // create troll
            ServerCharacter troll1 = new ServerCharacter(PredefinedEntityImplementationKind.SERVER_ENTITY);
            EntityAppearance appearancetroll = new EntityAppearance(PredefinedModelType.TROLL);
            troll1.CreateSetInitialAppearance(appearancetroll);
            troll1.Name = "Troll";
            EntityLocation locationtroll = new EntityLocation();
            locationtroll.CurrentMap = mapManager.StartPointMap;
            locationtroll.Z = 0;
            locationtroll.X = 131;
            locationtroll.Y = 160;
            locationtroll.Rotation = 90;
            locationtroll.IsSittingDown = false;
            troll1.CreateSetInitialLocation(locationtroll);

            // add troll to the world
            addEntityImplementationToWorld(troll1);
            troll1.LocationChangeMapAtEnterWorld();
            troll1.CreateRecalculateInitialEnergies();

            // create deer
            ServerCharacter deer1 = new ServerCharacter(PredefinedEntityImplementationKind.SERVER_ENTITY);
            EntityAppearance appearancedeer = new EntityAppearance(PredefinedModelType.DEER);
            deer1.CreateSetInitialAppearance(appearancedeer);
            deer1.Name = "Deer";
            EntityLocation locationdeer = new EntityLocation();
            locationdeer.CurrentMap = mapManager.StartPointMap;
            locationdeer.Z = 0;
            locationdeer.X = 156;
            locationdeer.Y = 97;
            locationdeer.Rotation = 0;
            locationdeer.IsSittingDown = false;
            deer1.CreateSetInitialLocation(locationdeer);

            // AI
            WonderingDumbNonAggresiveAIImplementation aiImplDeer = 
                new WonderingDumbNonAggresiveAIImplementation(locationdeer.X, locationdeer.Y, 50, 5000);
            deer1.AIAttach(aiImplDeer);

            // add deer to the world
            addEntityImplementationToWorld(deer1);
            deer1.LocationChangeMapAtEnterWorld();
            deer1.CreateRecalculateInitialEnergies();

            // create beaver
            ServerCharacter beaver1 = new ServerCharacter(PredefinedEntityImplementationKind.SERVER_ENTITY);
            EntityAppearance appearancebeaver = new EntityAppearance(PredefinedModelType.BEAVER);
            beaver1.CreateSetInitialAppearance(appearancebeaver);
            beaver1.Name = "Beaver";
            EntityLocation locationbeaver = new EntityLocation();
            locationbeaver.CurrentMap = mapManager.StartPointMap;
            locationbeaver.Z = 0;
            locationbeaver.X = 74;
            locationbeaver.Y = 32;
            locationbeaver.Rotation = 0;
            locationbeaver.IsSittingDown = false;
            beaver1.CreateSetInitialLocation(locationbeaver);

            // AI
            WonderingDumbNonAggresiveAIImplementation aiImplbeaver = 
                new WonderingDumbNonAggresiveAIImplementation(locationbeaver.X, locationbeaver.Y, 50, 5000);
            beaver1.AIAttach(aiImplbeaver);

            // add beaver to the world
            addEntityImplementationToWorld(beaver1);
            beaver1.LocationChangeMapAtEnterWorld();
            beaver1.CreateRecalculateInitialEnergies();

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