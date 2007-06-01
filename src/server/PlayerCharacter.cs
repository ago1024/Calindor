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
using System.IO;
using Calindor.Server.Messaging;
using Calindor.Server.Entities;
using Calindor.Server.Items;
using Calindor.Misc.Predefines;


namespace Calindor.Server
{
    public enum PlayerCharacterLoginState
    {
        NotInitialized,
        ClientVersionCorrect,
        ClientVersionIncorrect,
        LoginSuccesfull,
        LoggingOff
    }

    public class PlayerCharacter : EntityImplementation
    {
        // Communication data
        private ServerClientConnection playerConnection = null;

        private PlayerCharacterLoginState loginState = PlayerCharacterLoginState.NotInitialized;

        public PlayerCharacterLoginState LoginState
        {
            get { return loginState; }
            set { loginState = value; }
        }

        private long lastHeartBeat = 0;
        private bool isDeserialized = false; // Set to 'true' after deserialization
        private bool forceSerialization = false; // Force to serialize even without deserialization

        // Messages received from connection
        private IncommingMessagesQueue incommingMessages =
            new IncommingMessagesQueue();

        // Messages that are to be put into connection
        private OutgoingMessagesQueue outgoingMessages =
            new OutgoingMessagesQueue();

        private PlayerCharacter() : base(PredefinedEntityImplementationKind.ENTITY)
        {
        }

        public PlayerCharacter(ServerClientConnection conn) : base(PredefinedEntityImplementationKind.ENTITY)
        {
            if (conn == null)
                throw new ArgumentNullException("conn");

            playerConnection = conn;
        }

        public void UpdateHeartBeat()
        {
            // Don't change to WorldCalendar based. WorldCalendar can have the diffent time flow than computer clock.
            lastHeartBeat = DateTime.Now.Ticks;
        }

        public void ForceSerialization()
        {
            forceSerialization = true;
        }

        /// <summary>
        /// Checks if player should be logged off and starts the procedure
        /// </summary>
        public void CheckForLoggingOff()
        {
            // Don't change to WorldCalendar based. WorldCalendar can have the diffent time flow than computer clock.
            if (LoginState != PlayerCharacterLoginState.NotInitialized)
            {
                // Timeout
                long diff = Math.Abs(((long)(DateTime.Now.Ticks - lastHeartBeat)));
                if (diff > 300000000) //TODO: Configurable?
                {
                    // No message in 30 seconds... close
                    LoginState = PlayerCharacterLoginState.LoggingOff;
                }

                // Connection broken
                if (playerConnection.ConnectionBroken)
                {
                    // Internal error on connection... close
                    LoginState = PlayerCharacterLoginState.LoggingOff;
                }

                // If purging player -> close connection.
                if (LoginState == PlayerCharacterLoginState.LoggingOff)
                    playerConnection.ForceClose();
            }
        }

        #region Storage
        public void Serialize(PlayerCharacterSerializer sr)
        {
            if (isDeserialized || forceSerialization) // Serialize only if it was deserialized or force (create character)
            {
                // Appearance
                try
                {
                    sr.Start(Name, PlayerCharacterDataType.PCAppearance, "VER.1.0.0");
                    sr.WriteValue(this.Name);
                    appearance.Serialize(sr);
                }
                finally
                {
                    sr.End();
                }
                
                // Location
                try
                {
                    sr.Start(Name, PlayerCharacterDataType.PCLocation, "VER.1.1.0");
                    location.Serialize(sr);
                }
                finally
                {
                    sr.End();
                }

                // Inventory
                try
                {
                    sr.Start(Name, PlayerCharacterDataType.PCInventory, "VER.1.0.0");
                    inventory.Serialize(sr);
                }
                finally
                {
                    sr.End();
                }

                // Skills
                try
                {
                    sr.Start(Name, PlayerCharacterDataType.PCSkills, "VER.1.0.0");
                    skills.Serialize(sr);
                }
                finally
                {
                    sr.End();
                }

                // Energies
                try
                {
                    sr.Start(Name, PlayerCharacterDataType.PCEnergies, "VER.1.0.0");
                    energies.Serialize(sr);
                }
                finally
                {
                    sr.End();
                }

            }
        }

        public void Deserialize(PlayerCharacterDeserializer dsr)
        {
            // Appearance
            try
            {
                dsr.Start(Name, PlayerCharacterDataType.PCAppearance, "VER.1.0.0");
                this.Name = dsr.ReadString();
                appearance.Deserialize(dsr);
            }
            finally
            {
                dsr.End();
            }

            // Location
            try
            {
                dsr.Start(Name, PlayerCharacterDataType.PCLocation, "VER.1.1.0");
                location.Deserialize(dsr);
            }
            finally
            {
                dsr.End();
            }

            // Inventory
            try
            {
                dsr.Start(Name, PlayerCharacterDataType.PCInventory, "VER.1.0.0");
                inventory.Deserialize(dsr);
            }
            finally
            {
                dsr.End();
            }

            // Skills
            try
            {
                dsr.Start(Name, PlayerCharacterDataType.PCSkills, "VER.1.0.0");
                skills.Deserialize(dsr);
            }
            finally
            {
                dsr.End();
            }

            // Energies
            try
            {
                dsr.Start(Name, PlayerCharacterDataType.PCEnergies, "VER.1.0.0");
                energies.Deserialize(dsr);
            }
            finally
            {
                dsr.End();
            }

            isDeserialized = true;
        }
        #endregion
            
        #region Message Exchange

        public void GetMessages()
        {
            IncommingMessage msg = null;
            while ((msg = playerConnection.GetMessageFromINQueue()) != null)
                incommingMessages.Enqueue(msg);
        }

        public IncommingMessage GetMessageFromQueue()
        {
            if (incommingMessages.Count > 0)
                return incommingMessages.Dequeue();
            else
                return null;
        }

        public void SendMessages()
        {
            OutgoingMessage msg = null;

            while (outgoingMessages.Count > 0)
            {
                msg = outgoingMessages.Peek();

                // Try to add to connection, if failed quit
                if (!playerConnection.PutMessageIntoOUTQueue(msg))
                    return;
                else
                {
                    // Success, remove the message
                    outgoingMessages.Dequeue();
                }
            }
        }

        public override void PutMessageIntoMyQueue(OutgoingMessage msg)
        {
            outgoingMessages.Enqueue(msg);
        }

        #endregion

        #region Movement Handling
        public override void LocationChangeMapAtEnterWorld()
        {
            mapManager.ChangeMapForEntity(this, location, location.LoadedMapMame, true, location.X, location.Y);

            ChangeMapOutgoingMessage msgChangeMap =
                (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
            msgChangeMap.MapPath = location.CurrentMap.ClientFileName;
            PutMessageIntoMyQueue(msgChangeMap);

            // Teleport In - send to player ONLY - no obserwers yet
            TeleportInOutgoingMessage msgTeleportIn =
                (TeleportInOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.TELEPORT_IN);
            msgTeleportIn.X = location.X;
            msgTeleportIn.Y = location.Y;
            PutMessageIntoMyQueue(msgTeleportIn);

            // Display - send to player ONLY - observers will get it with the next round of visibility
            PutMessageIntoMyQueue(visibilityDisplayEntityImplementation());
        }
        #endregion

        #region Creation Handling
        protected override bool isEntityImplementationInCreationPhase()
        {
            return LoginState != PlayerCharacterLoginState.LoginSuccesfull;
        }
        #endregion

        #region NPC Conversation
        protected ServerCharacter npcInConversation = null;
        protected bool isNPCCloseEnoughForConversation()
        {
            if (npcInConversation == null)
                return false;
            
            double distance = Double.MaxValue;
            DistanceCalculationResult result = getDistanceToEntity(npcInConversation, out distance);
            
            if (result != DistanceCalculationResult.CALC_OK)
                return false;

            if (distance < 5.0)
                return true;

            return false;
        }
        public void NPCConversationStart(ServerCharacter sc)
        {
            if (sc == null)
                throw new ArgumentNullException("sc");
            
            npcInConversation = sc;

            if (!isNPCCloseEnoughForConversation())
            {
                RawTextOutgoingMessage msgRawText = 
                    (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawText.Color = PredefinedColor.Blue1;
                msgRawText.Text = "You are too far away to talk...";
                PutMessageIntoMyQueue(msgRawText);
                return;
            }

            npcInConversation.PlayerConversationStart(this);
        }
        public void NPCConversationRespond(ServerCharacter sc, ushort optionID)
        {
            if (sc == null)
                throw new ArgumentNullException("sc");

            if (sc != npcInConversation)
                return;

            if (!isNPCCloseEnoughForConversation())
            {
                RawTextOutgoingMessage msgRawText =
                    (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawText.Color = PredefinedColor.Blue1;
                msgRawText.Text = "You are too far away to talk...";
                PutMessageIntoMyQueue(msgRawText);
                return;
            }

            npcInConversation.PlayerConversationResponseSelected(this, optionID);
        }
        #endregion

        #region Fill Messages
        public void FillOutgoingMessage(HereYourStatsOutgoingMessage msg)
        {
            msg.FromEnergies(energies);
        }
        #endregion

        #region Energies Handling

        protected override void energiesEntityDied()
        {
            // Change to shadows dimension
            locationChangeDimension(PredefinedDimension.SHADOWS);

            // Send message
            RawTextOutgoingMessage msgRawText =
                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
            msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
            msgRawText.Color = PredefinedColor.Red2;
            msgRawText.Text = "You feel shadows' cold touch on your skin...";
            PutMessageIntoMyQueue(msgRawText);
        }

        #endregion

    }

    public class PlayerCharacterList : List<PlayerCharacter>
    {
    }

    public class NamePlayerCharacterDictionary : Dictionary<string, PlayerCharacter>
    {
    }

    
}
