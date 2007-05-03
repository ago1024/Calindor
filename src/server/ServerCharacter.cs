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
using Calindor.Misc.Predefines;
using Calindor.Server.Messaging;


namespace Calindor.Server
{

    public class ServerCharacter : EntityImplementation
    {
        public ServerCharacter(PredefinedEntityImplementationKind kind) : base(kind)
        {

        }

        #region Message Exchange
        public override void PutMessageIntoMyQueue(Calindor.Server.Messaging.OutgoingMessage msg)
        {
            return; // There is no queue for server character
        }
        #endregion

        #region Movement Handling
        public override void LocationChangeMapAtEnterWorld()
        {
            mapManager.ChangeMapForEntity(this, location, location.CurrentMapName, true, location.X, location.Y);
        }
        #endregion

        #region Creation Handling
        protected override bool isEntityImplementationInCreationPhase()
        {
            return true;
        }
        #endregion

        #region Player Conversation Handling
        protected PlayerCharacterConversationStateList playersInConversation =
            new PlayerCharacterConversationStateList();
        // TODO: This is hardcoded implemenation to support Owyn only!!!!
        // TODO: Remove conversation state (on what condition?) timeout on last received message??
        public void PlayerConversationStart(PlayerCharacter pc)
        {
            if (pc == null)
                throw new ArgumentNullException("pc");

            // If already talking?
            PlayerCharacterConversationState pcConvToHandle = getConversationState(pc);

            // New conversation
            if (pcConvToHandle == null)
            {
                pcConvToHandle = new PlayerCharacterConversationState();
                pcConvToHandle.PlayerInConversation = pc;
                playersInConversation.Add(pcConvToHandle);
            }

            pcConvToHandle.State = 0;

            SendNPCInfoOutgoingMessage msgSendNPCInfo =
                (SendNPCInfoOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.SEND_NPC_INFO);
            msgSendNPCInfo.Name = "Owyn";
            msgSendNPCInfo.Portrait = 4;
            pcConvToHandle.PlayerInConversation.PutMessageIntoMyQueue(msgSendNPCInfo);
            sendConversationPage(pcConvToHandle);
        }

        protected PlayerCharacterConversationState getConversationState(PlayerCharacter pc)
        {
            foreach (PlayerCharacterConversationState pcConv in playersInConversation)
                if (pcConv.PlayerInConversation == pc)
                    return pcConv;

            return null;
        }

        protected void sendConversationPage(PlayerCharacterConversationState pcConv)
        {
            NPCTextOutgoingMessage msgNPCText = 
                (NPCTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.NPC_TEXT);
            NPCOptionsListOutgoingMessage msgNPCOptionsList =
                (NPCOptionsListOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.NPC_OPTIONS_LIST);
            msgNPCOptionsList.NPCEntityID = EntityID;

            // TODO: Fill from scripts
            NPCOption op = null;
            NPCOptions ops = new NPCOptions();
            op = new NPCOption();
            op.Text = "Where am I?";
            op.OptionID = 1;
            ops.Add(op);
            op = new NPCOption();
            op.Text = "Who are you?";
            op.OptionID = 2;
            ops.Add(op);
            op = new NPCOption();
            op.Text = "Sell items";
            op.OptionID = 3;
            ops.Add(op);
            op = new NPCOption();
            op.Text = "Heal me";
            op.OptionID = 4;
            ops.Add(op);
            msgNPCOptionsList.FromNPCOptions(ops);

            switch (pcConv.State)
            {
                case(0):
                    msgNPCText.Text = "Hello " + pcConv.PlayerInConversation.Name + ". How may I help you?";
                    pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCText);
                    pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCOptionsList);
                    break;
                case (1):
                    msgNPCText.Text = "Why.. you are on Calindor. Where else would you want to be?";
                    pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCText);
                    pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCOptionsList);
                    break;
                case (2):
                    msgNPCText.Text = "My name is Owyn and I am a traveling merchant. Apart from buying and selling I also know a few priest prayer that can heal people.";
                    pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCText);
                    pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCOptionsList);
                    break;
                case (3):
                    // TODO: Implement sell
                    msgNPCText.Text = "Your items were worth XX to me.";
                    pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCText);
                    pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCOptionsList);
                    break;
                case (4):
                    // TODO: Implement heal
                    msgNPCText.Text = "You have been healed my friend. Take better care in future.... oh and thank you for 100 royals... my friend...";
                    pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCText);
                    pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCOptionsList);
                    break;
                default:
                    throw new ArgumentException("Unsupported conversation state");
            }
        }

        public void PlayerConversationResponseSelected(PlayerCharacter pc, ushort optionID)
        {
            if (pc == null)
                throw new ArgumentNullException("pc");

            // Find conversation state
            PlayerCharacterConversationState pcConv = getConversationState(pc);
            if (pc == null)
                return;

            // Analyze response to current state
            if ((optionID == 1))
            {
                pcConv.State = 1;
                sendConversationPage(pcConv);
                return;
            }

            if ((optionID == 2))
            {
                pcConv.State = 2;
                sendConversationPage(pcConv);
                return;
            }

            if ((optionID == 3))
            {
                pcConv.State = 3;
                sendConversationPage(pcConv);
                return;
            }

            if ((optionID == 4))
            {
                pcConv.State = 4;
                sendConversationPage(pcConv);
                return;
            }

        }
        #endregion
    }

    public class PlayerCharacterConversationState
    {
        private PlayerCharacter playerInConversation;

        public PlayerCharacter PlayerInConversation
        {
            get { return playerInConversation; }
            set { playerInConversation = value; }
        }

        // TODO: Exchange with objects from conversation graph when script becomes available
        private int state;

        public int State
        {
            get { return state; }
            set { state = value; }
        }
	
    }

    public class PlayerCharacterConversationStateList : List<PlayerCharacterConversationState>
    {
    }

    public class NPCOption
    {
        private string text;

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        private ushort optionID;

        public ushort OptionID
        {
            get { return optionID; }
            set { optionID = value; }
        }
    }

    public class NPCOptionList : List<NPCOption>
    {
    }

    public class NPCOptions
    {
        private NPCOptionList options = new NPCOptionList();
        public NPCOptionList Options
        {
            get { return options; }
        }

        public void Add(NPCOption option)
        {
            options.Add(option);
        }
    }
}
