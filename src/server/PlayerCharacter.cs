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
using Calindor.Server.Messaging;
using Calindor.Server.Entities;
using System.IO;

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

    public class PlayerCharacter : Entity
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

        private PlayerCharacter()
        {
        }

        public PlayerCharacter(ServerClientConnection conn)
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
                sr.Start(Name, PlayerCharacterDataType.PCAppearance, "VER.1.0.0");
                    sr.WriteValue(this.Name);
                    Appearance.Serialize(sr);
                sr.End();

                /*sr.Start(Name, PlayerCharacterDataType.PCAttributes, "VER.1.0.0");
                    BasicAttributesCurrent.Serialize(sr);
                    BasicAttributesBase.Serialize(sr);
                    CrossAttributesCurrent.Serialize(sr);
                    CrossAttributesBase.Serialize(sr);
                    NexusesCurrent.Serialize(sr);
                    NexusesBase.Serialize(sr);
                    SkillsCurrent.Serialize(sr);
                    SkillsBase.Serialize(sr);
                    MiscAttributesCurrent.Serialize(sr);
                    MiscAttributesBase.Serialize(sr);
                    sr.WriteValue(this.FoodLevel);
                    sr.WriteValue(this.PickPoints);
                sr.End();*/

                sr.Start(Name, PlayerCharacterDataType.PCLocation, "VER.1.1.0");
                    Location.Serialize(sr);
                sr.End();
            }
        }

        public void Deserialize(PlayerCharacterDeserializer dsr)
        {
            dsr.Start(Name, PlayerCharacterDataType.PCAppearance, "VER.1.0.0");
                this.Name = dsr.ReadString();
                Appearance.Deserialize(dsr);
            dsr.End();

            /*/dsr.Start(Name, PlayerCharacterDataType.PCAttributes, "VER.1.0.0");
                BasicAttributesCurrent.Deserialize(dsr);
                BasicAttributesBase.Deserialize(dsr);
                CrossAttributesCurrent.Deserialize(dsr);
                CrossAttributesBase.Deserialize(dsr);
                NexusesCurrent.Deserialize(dsr);
                NexusesBase.Deserialize(dsr);
                SkillsCurrent.Deserialize(dsr);
                SkillsBase.Deserialize(dsr);
                MiscAttributesCurrent.Deserialize(dsr);
                MiscAttributesBase.Deserialize(dsr);
                this.FoodLevel = dsr.ReadSByte();
                this.PickPoints = dsr.ReadShort();
            dsr.End();*/

            dsr.Start(Name, PlayerCharacterDataType.PCLocation, "VER.1.1.0");
                Location.Deserialize(dsr);
            dsr.End();

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

        public void PutMessageIntoMyQueue(OutgoingMessage msg)
        {
            outgoingMessages.Enqueue(msg);
        }

        public void PutMessageIntoObserversQueue(OutgoingMessage msg)
        {
            foreach (Entity en in entitiesObservers)
                if (en is PlayerCharacter)
                    (en as PlayerCharacter).PutMessageIntoMyQueue(msg);
        }

        public void PutMessageIntoMyAndObserversQueue(OutgoingMessage msg)
        {
            PutMessageIntoMyQueue(msg);

            PutMessageIntoObserversQueue(msg);
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