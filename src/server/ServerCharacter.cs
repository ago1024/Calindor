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
using Calindor.Server.Items;
using Calindor.Server.AI;
using Calindor.Server.TimeBasedActions;
using Calindor.Server.Entities;


namespace Calindor.Server
{

    public class ServerCharacter : EntityImplementation
    {
        // TEMPORARY FIELD! TODO: REMOVE WHEN SCRIPTS AVAILABLE
        public int MaxCombatXP = 1;
        
        public ServerCharacter(PredefinedEntityImplementationKind kind) : base(kind)
        {

        }

        #region Message Exchange
        public override void PutMessageIntoMyQueue(Calindor.Server.Messaging.OutgoingMessage msg)
        {
            return; // There is no queue for server character
        }
        public override void SendLocalChatMessage (string message, PredefinedColor color)
        {
            return; // There is no local chat for server character
        }

        #endregion

        #region Movement Handling
        public override void LocationChangeMapAtEnterWorld()
        {
            mapManager.ChangeMapForEntity(this, location, location.CurrentMapName, true, location.X, location.Y);
        }
        #endregion

        #region Creation Handling
        protected EntityLocation templateLocation = null;
        protected uint milisToRespawn = 0;

        protected override bool isEntityImplementationInCreationPhase()
        {
            return true;
        }
        
        public override void CreateRecalculateInitialEnergies()
        {
            if (!isEntityImplementationInCreationPhase())
                throw new InvalidOperationException("This method can only be used during creation!");

            // TODO: Recalculate based on attributes/perks/items
            energies.SetMaxHealth((short)(25 + WorldRNG.Next(0,50)));

            energies.UpdateCurrentHealth(energies.GetHealthDifference());
        }

        public override void CreateSetInitialLocation(EntityLocation location)
        {
            base.CreateSetInitialLocation(location);
            
            // Build template
            templateLocation = this.location.CreateCopy();
            
        }

        public void CreateApplyTemplates()
        {
            // Location
            location = templateLocation.CreateCopy();
        }

        public void CreateApplyInitialState()
        {
            CreateRecalculateInitialEnergies();

            LocationChangeDimension((PredefinedDimension)location.Dimension);
            
            // TODO: Move to separate method
            // Set random fighting skills
            skills.GetSkill(EntitySkillType.AttackUnarmed).AddXP((uint)WorldRNG.Next(0, MaxCombatXP));
            skills.GetSkill(EntitySkillType.DefenseDodge).AddXP((uint)WorldRNG.Next(0, MaxCombatXP));
        }

        public void CreateSetRespawnTime(uint milisToRespawn)
        {
            this.milisToRespawn = milisToRespawn;
        }
        #endregion

        #region Player Conversation Handling
        protected PlayerCharacterConversationStateList playersInConversation =
            new PlayerCharacterConversationStateList();
        // TODO: This is hardcoded implemenation to support Owyn/Cerdiss only!!!!
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

            if (Name == "Owyn")
            {
                SendNPCInfoOutgoingMessage msgSendNPCInfo =
                    (SendNPCInfoOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.SEND_NPC_INFO);
                msgSendNPCInfo.Name = "Owyn";
                msgSendNPCInfo.Portrait = 4;
                pcConvToHandle.PlayerInConversation.PutMessageIntoMyQueue(msgSendNPCInfo);
                sendConversationPage(pcConvToHandle);
            }

            if (Name == "Cerdiss")
            {
                SendNPCInfoOutgoingMessage msgSendNPCInfo =
                    (SendNPCInfoOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.SEND_NPC_INFO);
                msgSendNPCInfo.Name = "Cerdiss";
                msgSendNPCInfo.Portrait = 100;
                pcConvToHandle.PlayerInConversation.PutMessageIntoMyQueue(msgSendNPCInfo);
                sendConversationPage(pcConvToHandle);
            }
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

            if (Name == "Owyn")
            {
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
                    case (0):
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
                        msgNPCText.Text = "My name is Owyn and I am a traveling merchant. Apart from buying and selling I also know a few priest prayers that can heal people.";
                        pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCText);
                        pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCOptionsList);
                        break;
                    case (3):
                        {
                            // Get all sellable items. Change them into rolays.
                            double royalsTotal = 0.0;
                            Item itm = null;
                            // Vegetables
                            itm = pcConv.PlayerInConversation.InventoryGetItemByDefinition(ItemDefinitionCache.GetItemDefinitionByID(2));
                            if (itm != null)
                            {
                                royalsTotal += itm.Quantity * 0.5;
                                itm.Quantity *= -1;
                                pcConv.PlayerInConversation.InventoryUpdateItem(itm);
                            }
                            // Tiger Lilly
                            itm = pcConv.PlayerInConversation.InventoryGetItemByDefinition(ItemDefinitionCache.GetItemDefinitionByID(3));
                            if (itm != null)
                            {
                                royalsTotal += itm.Quantity * 0.9;
                                itm.Quantity *= -1;
                                pcConv.PlayerInConversation.InventoryUpdateItem(itm);
                            }
                            // Red Snapdragon
                            itm = pcConv.PlayerInConversation.InventoryGetItemByDefinition(ItemDefinitionCache.GetItemDefinitionByID(7));
                            if (itm != null)
                            {
                                royalsTotal += itm.Quantity * 0.7;
                                itm.Quantity *= -1;
                                pcConv.PlayerInConversation.InventoryUpdateItem(itm);
                            }

                            msgNPCText.Text = "I will trade your items for " + Math.Round(royalsTotal, 0) + " royals.";
                            pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCText);
                            pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCOptionsList);
                            itm = new Item(ItemDefinitionCache.GetItemDefinitionByID(5));
                            itm.Quantity = (int)Math.Round(royalsTotal, 0);
                            pcConv.PlayerInConversation.InventoryUpdateItem(itm);
                            break;
                        }
                    case (4):
                        {
                            // Get royals. If enough, subtract and heal
                            Item itm = null;
                            bool notEnough = false;
                            // Royals
                            itm = pcConv.PlayerInConversation.InventoryGetItemByDefinition(ItemDefinitionCache.GetItemDefinitionByID(5));
                            if (itm != null)
                            {
                                if (itm.Quantity >= 100)
                                {
                                    pcConv.PlayerInConversation.EnergiesRestoreAllHealth();
                                    itm.Quantity = -100;
                                    pcConv.PlayerInConversation.InventoryUpdateItem(itm);
                                    msgNPCText.Text = "You have been healed my friend. Take better care in future.... oh and thank you for 100 royals... my friend...";
                                }
                                else
                                    notEnough = true;
                            }
                            else
                                notEnough = true;

                            if (notEnough)
                                msgNPCText.Text = "I'm sorry but I require a small donation of 100 royals for my prayers... my friend...";


                            pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCText);
                            pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCOptionsList);
                            break;
                        }
                    default:
                        throw new ArgumentException("Unsupported conversation state");
                }
            }

            if (Name == "Cerdiss")
            {
                NPCOption op = null;
                NPCOptions ops = new NPCOptions();

                switch (pcConv.State)
                {
                    case (0):
                        msgNPCText.Text = "I CAN HEAR YOUR THOUGHS, SHADOW";
                        pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCText);
                        op = new NPCOption();
                        op.Text = "I want to return to life...";
                        op.OptionID = 1;
                        ops.Add(op);
                        msgNPCOptionsList.FromNPCOptions(ops);
                        pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCOptionsList);
                        break;
                    case (1):
                        msgNPCText.Text = "BEGONE....";
                        pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCText);
                        msgNPCOptionsList.FromNPCOptions(ops);
                        pcConv.PlayerInConversation.PutMessageIntoMyQueue(msgNPCOptionsList);
                        pcConv.PlayerInConversation.EnergiesResurrect();
                        break;
                    default:
                        throw new ArgumentException("Unsupported conversation state");
                }
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

            if (Name == "Owyn")
            {
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

            if (Name == "Cerdiss")
            {
                // Analyze response to current state
                if ((optionID == 1))
                {
                    pcConv.State = 1;
                    sendConversationPage(pcConv);
                    return;
                }
            }
        }
        #endregion

        #region AI Handling
        protected AIImplementation myAI = null;
        public void AIAttach(AIImplementation ai)
        {
            myAI = ai;
            myAI.AttachServerCharacter(this);
        }

        public void AIExecute()
        {
            if (myAI != null)
                myAI.Execute();
        }
        #endregion

        #region Energies Handling

        protected override void energiesEntityDied()
        {            base.energiesEntityDied();

            // Animate death
            AddActorCommandOutgoingMessage msgAddActorCommand =
                (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
            msgAddActorCommand.EntityID = EntityID;
            msgAddActorCommand.Command = PredefinedActorCommand.die1;
            PutMessageIntoMyAndObserversQueue(msgAddActorCommand);

            // Respawn
            RespawnTimeBasedAction respawn = new RespawnTimeBasedAction(this, milisToRespawn);
            respawn.Activate();
            
        }

        public void EnergiesRespawn()
        {
            if (energies.IsAlive)
                return;

            // Remove from view
            RemoveActorOutgoingMessage msgRemoveActor =
                (RemoveActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ACTOR);
            msgRemoveActor.EntityID = EntityID;
            PutMessageIntoMyAndObserversQueue(msgRemoveActor);

            // Apply templates
            CreateApplyTemplates();

            // Reset state
            CreateApplyInitialState();

            // Add to view
            PutMessageIntoMyAndObserversQueue(visibilityDisplayEntityImplementation());
        }
        #endregion
        
        #region Calendar Events Handling
        public override void CalendarNewMinute(ushort minuteOfTheDay)
        {
            // Heal a bit
            if (energies.GetHealthDifference() != 0)
            {
                short healedHealth = (short)WorldRNG.Next(1,4);
                EnergiesUpdateHealth(healedHealth);
            }
        }
        #endregion

    }

    public class ServerCharacterList : List<ServerCharacter>
    {
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
