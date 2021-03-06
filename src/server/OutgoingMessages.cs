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
using Calindor.Misc.Predefines;
using Calindor.Misc;
using Calindor.Server.Items;
using Calindor.Server.Entities;

namespace Calindor.Server.Messaging
{
    // ABSTRACTS
    public enum OutgoingMessageType
    {
        RAW_TEXT = 0,
        ADD_NEW_ACTOR = 1,
        ADD_ACTOR_COMMAND = 2,
        YOU_ARE = 3,
        NEW_MINUTE = 5,
        REMOVE_ACTOR = 6,
        CHANGE_MAP = 7,
        KILL_ALL_ACTORS = 9,
        TELEPORT_IN = 12,
        TELEPORT_OUT = 13,
        HERE_YOUR_STATS = 18,
        HERE_YOUR_INVENTORY = 19,
        INVENTORY_ITEM_TEXT = 20,
        GET_NEW_INVENTORY_ITEM  = 21,
        REMOVE_ITEM_FROM_INVENTORY = 22,
        NPC_TEXT = 30,
        NPC_OPTIONS_LIST = 31,
        SEND_NPC_INFO = 33,
        GET_ACTOR_DAMAGE = 47,
        GET_ACTOR_HEAL = 48,
        SEND_PARTIAL_STAT = 49,
        ADD_NEW_ENHANCED_ACTOR = 51,
        GET_3D_OBJ_LIST = 74,
        GET_3D_OBJ = 75,
        REMOVE_3D_OBJ = 76,
        SEND_BUFFS = 78,
        UPGRADE_TOO_OLD = 241,
        YOU_DONT_EXIST = 249,
        LOG_IN_OK = 250,
        LOG_IN_NOT_OK = 251,
        CREATE_CHAR_OK = 252,
        CREATE_CHAR_NOT_OK = 253,
    }

    public abstract class OutgoingMessage
    {
        protected OutgoingMessageType messageType = OutgoingMessageType.RAW_TEXT;
        protected UInt16 length = 0;

        public OutgoingMessageType MessageType
        {
            get { return messageType; }
        }
        /// <summary>
        /// Lenght of whole data stream. Cannot be less than 3.
        /// </summary>
        public virtual UInt16 Length
        {
            get { return length; }
        }

        public virtual byte [] Serialize()
        {
            byte[] _return = new byte[Length];

            // Put type
            _return[0] = (byte)MessageType;

            // Put length
            InPlaceBitConverter.GetBytes((UInt16)(Length - 2)/*length of length is not counted*/, _return, 1);

            // Specific data
            serializeSpecific(_return);

            return _return;
        }

        public abstract OutgoingMessage CreateNew();

        protected virtual void serializeSpecific(byte[] _return){}

        public override string ToString()
        {
            return "(" + MessageType.ToString() + ")";
        }

    }

    public class OutgoingMessagesQueue : Queue<OutgoingMessage>
    {
    }

    public class MessageSerializeException : ApplicationException
    {
        public MessageSerializeException(string message) : base(message) { }
    }

    // FACTORY
    public sealed class OutgoingMessagesFactory
    {
        private static OutgoingMessage[] knownMessages = new OutgoingMessage[256];

        static OutgoingMessagesFactory()
        {
            // Add known messages
            knownMessages[(int)OutgoingMessageType.RAW_TEXT] = new RawTextOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.UPGRADE_TOO_OLD] = new UpgradeTooOldOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.YOU_ARE] = new YouAreOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.CHANGE_MAP] = new ChangeMapOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.LOG_IN_OK] = new LogInOkOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR] = new AddNewEnhancedActorOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.NEW_MINUTE] = new NewMinuteOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.HERE_YOUR_STATS] = new HereYourStatsOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.CREATE_CHAR_OK] = new CreateCharOkOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.CREATE_CHAR_NOT_OK] = new CreateCharNotOkOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.YOU_DONT_EXIST] = new YouDontExistOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.LOG_IN_NOT_OK] = new LogInNotOkOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.REMOVE_ACTOR] = new RemoveActorOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.TELEPORT_IN] = new TeleportInOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.TELEPORT_OUT] = new TeleportOutOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.ADD_ACTOR_COMMAND] = new AddActorCommandOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.KILL_ALL_ACTORS] = new KillAllActorsOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.HERE_YOUR_INVENTORY] = new HereYourInventoryOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.GET_NEW_INVENTORY_ITEM] = new GetNewInventoryItemOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.REMOVE_ITEM_FROM_INVENTORY] = new RemoveItemFromInventoryOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.INVENTORY_ITEM_TEXT] = new InventoryItemTextOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.NPC_TEXT] = new NPCTextOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.SEND_NPC_INFO] = new SendNPCInfoOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.NPC_OPTIONS_LIST] = new NPCOptionsListOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.GET_ACTOR_DAMAGE] = new GetActorDamageOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.GET_ACTOR_HEAL] = new GetActorHealOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.SEND_PARTIAL_STAT] = new SendPartialStatOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.ADD_NEW_ACTOR] = new AddNewActorOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.SEND_BUFFS] = new SendBuffsOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.GET_3D_OBJ_LIST] = new Get3dObjListOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.GET_3D_OBJ] = new Get3dObjOutgoingMessage();
            knownMessages[(int)OutgoingMessageType.REMOVE_3D_OBJ] = new Remove3dObjOutgoingMessage();
        }

        public static OutgoingMessage Create(OutgoingMessageType type)
        {
            /*
             * TODO: Since all messages are create by a factory,
             * it is possible to optimize memory usage by implementing
             * a caching mechanizm in the factory. A message object would
             * have to be stored on the list in the factory and signalized
             * weather it is used or not. 'Creating' a object would start it
             * usage, and putting in into network stream would signal stop of
             * usage.
             */

            OutgoingMessage _return = null;

            if (knownMessages[(int)type] != null)
                _return = knownMessages[(int)type].CreateNew();

            return _return;
        }
    }



    // IMPLEMENTATION

    public class RawTextOutgoingMessage : OutgoingMessage
    {
        protected string text = "";

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public override UInt16 Length
        {
            get
            {
                return (UInt16)(5 + Text.Length /*3+2+text*/);
            }
        }

        protected PredefinedColor color = PredefinedColor.Red1;
        public PredefinedColor Color
        {
            get { return color; }
            set { color = value; }
        }

        protected PredefinedChannel channel = PredefinedChannel.CHAT_LOCAL;
        public PredefinedChannel Channel
        {
            get { return channel; }
            set { channel = value; }
        }

        public RawTextOutgoingMessage()
        {
            messageType = OutgoingMessageType.RAW_TEXT;
        }

        public override OutgoingMessage CreateNew()
        {
            return new RawTextOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            // Add channel
            _return[3] = (byte)Channel;

            // Add color
            _return[4] = (byte)(127 + Color);

            // Add string
            InPlaceBitConverter.GetBytes(Text, _return, 5);
        }

        public override string ToString()
        {
            return base.ToString() + "(" + Text + ", " + Color + ", " + Channel + ")";
        }
    }

    public class UpgradeTooOldOutgoingMessage : OutgoingMessage
    {
        public UpgradeTooOldOutgoingMessage()
        {
            messageType = OutgoingMessageType.UPGRADE_TOO_OLD;
            length = 3;
        }

        public override OutgoingMessage CreateNew()
        {
            return new UpgradeTooOldOutgoingMessage();
        }
    }

    public class YouDontExistOutgoingMessage : OutgoingMessage
    {
        public YouDontExistOutgoingMessage()
        {
            messageType = OutgoingMessageType.YOU_DONT_EXIST;
            length = 3;
        }

        public override OutgoingMessage CreateNew()
        {
            return new YouDontExistOutgoingMessage();
        }
    }

    public class KillAllActorsOutgoingMessage : OutgoingMessage
    {
        public KillAllActorsOutgoingMessage()
        {
            messageType = OutgoingMessageType.KILL_ALL_ACTORS;
            length = 3;
        }

        public override OutgoingMessage CreateNew()
        {
            return new KillAllActorsOutgoingMessage();
        }
    }

    public class YouAreOutgoingMessage : OutgoingMessage
    {
        protected UInt16 entityID = 0;
        public UInt16 EntityID
        {
            get { return entityID; }
            set { entityID = value; }
        }

        public YouAreOutgoingMessage()
        {
            messageType = OutgoingMessageType.YOU_ARE;
            length = 5;
        }

        public override OutgoingMessage CreateNew()
        {
            return new YouAreOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(EntityID, _return, 3);
        }

        public override string ToString()
        {
            return base.ToString() + "(" + EntityID + ")";
        }
    }

    public class ChangeMapOutgoingMessage : OutgoingMessage
    {
        public override UInt16 Length
        {
            get
            {
                return (UInt16)(4 + MapPath.Length); /*3 + mapPath + 1*/
            }
        }

        // TODO: Possibly add some dictionary with configurable map locations?
        protected string mapPath = "";
        public string MapPath
        {
            get { return mapPath; }
            set { mapPath = value; }
        }

        public ChangeMapOutgoingMessage()
        {
            messageType = OutgoingMessageType.CHANGE_MAP;
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(MapPath, _return, 3);

            // Add null terminator
            _return[_return.Length - 1] = 0x00;
        }

        public override string ToString()
        {
            return base.ToString() + "(" + MapPath + ")";
        }

        public override OutgoingMessage CreateNew()
        {
            return new ChangeMapOutgoingMessage();
        }
    }

    public class LogInOkOutgoingMessage : OutgoingMessage
    {
        public LogInOkOutgoingMessage()
        {
            messageType = OutgoingMessageType.LOG_IN_OK;
            length = 3;
        }

        public override OutgoingMessage CreateNew()
        {
            return new LogInOkOutgoingMessage();
        }
    }

    public class CreateCharOkOutgoingMessage : OutgoingMessage
    {
        public CreateCharOkOutgoingMessage()
        {
            messageType = OutgoingMessageType.CREATE_CHAR_OK;
            length = 3;
        }

        public override OutgoingMessage CreateNew()
        {
            return new CreateCharOkOutgoingMessage();
        }
    }

    public class CreateCharNotOkOutgoingMessage : OutgoingMessage
    {
        private string message = "";
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public override UInt16 Length
        {
            get
            {
                return (UInt16)(4 + Message.Length); /* 3 + message + 1*/
            }
        }
        public CreateCharNotOkOutgoingMessage()
        {
            messageType = OutgoingMessageType.CREATE_CHAR_NOT_OK;
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(Message, _return, 3);
            _return[Length - 1] = 0x00;
        }

        public override OutgoingMessage CreateNew()
        {
            return new CreateCharNotOkOutgoingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + Message + ")";
        }
    }


    public class LogInNotOkOutgoingMessage : OutgoingMessage
    {
        private string message = "";
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public override UInt16 Length
        {
            get
            {
                return (UInt16)(4 + Message.Length); /* 3 + message + 1*/
            }
        }
        public LogInNotOkOutgoingMessage()
        {
            messageType = OutgoingMessageType.LOG_IN_NOT_OK;
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(Message, _return, 3);
            _return[Length - 1] = 0x00;
        }

        public override OutgoingMessage CreateNew()
        {
            return new LogInNotOkOutgoingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + Message + ")";
        }
    }

    public class AddNewEnhancedActorOutgoingMessage : OutgoingMessage
    {

        protected UInt16 entityID = 0;
        public UInt16 EntityID
        {
            get { return entityID; }
        }

        protected string entityName = "";
        public String EntityName
        {
            get { return entityName; }
        }

        protected UInt16 entityScale = 0x4000;
        public UInt16 EntityScale
        {
            get { return entityScale; }
        }

        private bool hasAttachment = false;
        public bool HasAttachment
        {
            get { return hasAttachment; }
        }

        protected byte attachmentType = (byte)PredefinedModelType.HORSE;
        public byte AttachmentType
        {
            get { return attachmentType; }
        }


        public override UInt16 Length
        {
            get
            {
                return (UInt16)(37 + EntityName.Length); /*31 + name + 1 + 5*/
            }
        }

        protected byte [] innerDataAppearance = new byte[9];
        protected short[] innerDataLocation = new short[5];
        protected short[] innerDataEnergies = new short[3];
        protected byte kindOfEntityImplementation = 0;

        public AddNewEnhancedActorOutgoingMessage()
        {
            messageType = OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR;
        }

        public void FromEntityImplementation(EntityImplementation enImpl)
        {
            entityID = enImpl.EntityID;
            entityName = enImpl.Name;
            kindOfEntityImplementation = (byte)enImpl.EntityImplementationKind;
            entityScale = (UInt16)(0x4000 * enImpl.Scale);
            hasAttachment = enImpl.IsAttached;
            attachmentType = (byte)enImpl.AttachmentType;
        }

        public void FromAppearance(EntityAppearance appearance)
        {
            // Appearance
            // TODO:What if wielding items?
            innerDataAppearance[0] = (byte)appearance.Skin;
            innerDataAppearance[1] = (byte)appearance.Hair;
            innerDataAppearance[2] = (byte)appearance.Shirt;
            innerDataAppearance[3] = (byte)appearance.Pants;
            innerDataAppearance[4] = (byte)appearance.Boots;
            innerDataAppearance[5] = (byte)appearance.Type;
            innerDataAppearance[6] = (byte)appearance.Head;
            innerDataAppearance[7] = 0;
            if (appearance.IsTransparent)
                innerDataAppearance[7] |= 0x01;
            innerDataAppearance[8] = (byte)appearance.Eyes;
        }

        public void FromEnergies(EntityEnergies energies)
        {
            innerDataEnergies[0] = energies.MaxHealth;
            innerDataEnergies[1] = energies.CurrentHealth;
            innerDataEnergies[2] = energies.IsAlive ? (short)1 : (short)0;
        }

        public void FromLocation(EntityLocation location)
        {
            // Location
            innerDataLocation[0] = location.X;
            innerDataLocation[1] = location.Y;
            innerDataLocation[2] = location.Z;
            innerDataLocation[3] = location.Rotation;
            innerDataLocation[4] = location.IsSittingDown ? (short)1 : (short)0;
        }

        protected override void serializeSpecific(byte[] _return)
        {
            // Entity ID
            InPlaceBitConverter.GetBytes(EntityID, _return, 3);

            // X
            InPlaceBitConverter.GetBytes(innerDataLocation[0], _return, 5);

            if ((innerDataAppearance[7] & 0x01) == 0x01)
                _return[6] |= 0x08; // is transparent

            // Y
            InPlaceBitConverter.GetBytes(innerDataLocation[1], _return, 7);

            // Z
            InPlaceBitConverter.GetBytes(innerDataLocation[2], _return, 9);

            // Rotation
            InPlaceBitConverter.GetBytes(innerDataLocation[3], _return, 11);


            // TODO: Change

            // actor type
            _return[13] = innerDataAppearance[5];
            _return[14] = 0x00;

            // skin
            _return[15] = innerDataAppearance[0];

            // hair
            _return[16] = innerDataAppearance[1];

            // shirt
            _return[17] = innerDataAppearance[2];

            // pants
            _return[18] = innerDataAppearance[3];

            // boots
            _return[19] = innerDataAppearance[4];

            // head
            _return[20] = innerDataAppearance[6];

            // shield
            _return[21] = 11/*none*/;//0x0B;

            // weapon
            _return[22] = 0/*none*/;//0x00;

            // cape
            _return[23] = 30/*none*/;//0x00;

            // helmet
            _return[24] = 20/*none*/;//0x14;

            // frame
            if (innerDataEnergies[2] == 1)
            {
                if (innerDataLocation[4] == 1)
                    _return[25] = (byte)PredefinedActorFrame.frame_sit_idle;
                else
                    _return[25] = (byte)PredefinedActorFrame.frame_idle;
            }
            else
            {
                _return[25] = (byte)PredefinedActorFrame.frame_die1;
            }

            // max health
            InPlaceBitConverter.GetBytes(innerDataEnergies[0], _return, 26);

            // cur health
            InPlaceBitConverter.GetBytes(innerDataEnergies[1], _return, 28);

            // kind of actor
            _return[30] = kindOfEntityImplementation;



            // EntityName
            // TODO: Combine name with guild??
            InPlaceBitConverter.GetBytes(EntityName, _return, 31);

            // Add null terminator
            _return[31 + EntityName.Length] = 0x00;

            // scale
            InPlaceBitConverter.GetBytes(EntityScale, _return, 31 + EntityName.Length + 1);

            // attachment type
            if (HasAttachment)
                _return[31 + EntityName.Length + 3] = AttachmentType;
            else
                _return[31 + EntityName.Length + 3] = 0xff;

            // eyes
            _return[31 + EntityName.Length + 4] = innerDataAppearance[8];

            // neck
            _return[31 + EntityName.Length + 5] = 0;
        }

        public override OutgoingMessage CreateNew()
        {
            return new AddNewEnhancedActorOutgoingMessage();
        }

        public override string ToString()
        {
            // TODO: Implement
            return base.ToString() + "(" + EntityID + ", " + EntityName + ")";
        }
    }

    public class NewMinuteOutgoingMessage : OutgoingMessage
    {
        protected UInt16 minuteOfTheDay = 0;
        public UInt16 MinuteOfTheDay
        {
            get { return minuteOfTheDay; }
            set { minuteOfTheDay = value; }
        }

        public NewMinuteOutgoingMessage()
        {
            messageType = OutgoingMessageType.NEW_MINUTE;
            length = 5;
        }

        public override OutgoingMessage CreateNew()
        {
            return new NewMinuteOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(MinuteOfTheDay, _return, 3);
        }

        public override string ToString()
        {
            return base.ToString() + "(" + MinuteOfTheDay + ")";
        }
    }

    public class HereYourStatsOutgoingMessage : OutgoingMessage
    {
        private byte[] statsBuffer = null;
        public HereYourStatsOutgoingMessage()
        {
            messageType = OutgoingMessageType.HERE_YOUR_STATS;
            length = 229;
            statsBuffer = new byte[length - 3];
        }
        protected override void serializeSpecific(byte[] _return)
        {
            Array.Copy(statsBuffer, 0, _return, 3, statsBuffer.GetLength(0));
        }

        public void FromEnergies(EntityEnergies energies)
        {
            // Health
            InPlaceBitConverter.GetBytes(energies.CurrentHealth, statsBuffer, 84);
            InPlaceBitConverter.GetBytes(energies.MaxHealth, statsBuffer, 86);
        }

        public override OutgoingMessage CreateNew()
        {
            return new HereYourStatsOutgoingMessage();
        }
    }

    public class RemoveActorOutgoingMessage : OutgoingMessage
    {
        protected UInt16 entityID = 0;
        public UInt16 EntityID
        {
            get { return entityID; }
            set { entityID = value; }
        }

        public RemoveActorOutgoingMessage()
        {
            messageType = OutgoingMessageType.REMOVE_ACTOR;
            length = 5;
        }
        public override OutgoingMessage CreateNew()
        {
            return new RemoveActorOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(EntityID, _return, 3);
        }

        public override string ToString()
        {
            return base.ToString() + "(" + EntityID + ")";
        }
    }

    public class TeleportInOutgoingMessage : OutgoingMessage
    {
        private Int16 x;

        public Int16 X
        {
            get { return x; }
            set { x = value; }
        }

        private Int16 y;

        public Int16 Y
        {
            get { return y; }
            set { y = value; }
        }

        public TeleportInOutgoingMessage()
        {
            messageType = OutgoingMessageType.TELEPORT_IN;
            length = 7;
        }

        public override OutgoingMessage CreateNew()
        {
            return new TeleportInOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            // X
            InPlaceBitConverter.GetBytes(X, _return, 3);

            // Y
            InPlaceBitConverter.GetBytes(Y, _return, 5);
        }
    }

    public class TeleportOutOutgoingMessage : OutgoingMessage
    {
        private Int16 x;

        public Int16 X
        {
            get { return x; }
            set { x = value; }
        }

        private Int16 y;

        public Int16 Y
        {
            get { return y; }
            set { y = value; }
        }

        public TeleportOutOutgoingMessage()
        {
            messageType = OutgoingMessageType.TELEPORT_OUT;
            length = 7;
        }

        public override OutgoingMessage CreateNew()
        {
            return new TeleportOutOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            // X
            InPlaceBitConverter.GetBytes(X, _return, 3);

            // Y
            InPlaceBitConverter.GetBytes(Y, _return, 5);
        }
    }

    public class AddActorCommandOutgoingMessage : OutgoingMessage
    {
        private UInt16 entityID;

        public UInt16 EntityID
        {
            get { return entityID; }
            set { entityID = value; }
        }

        private PredefinedActorCommand command;

        public PredefinedActorCommand Command
        {
            get { return command; }
            set { command = value; }
        }


        public AddActorCommandOutgoingMessage()
        {
            messageType = OutgoingMessageType.ADD_ACTOR_COMMAND;
            length = 6;
        }
        public override OutgoingMessage CreateNew()
        {
            return new AddActorCommandOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(EntityID, _return, 3);
            _return[5] = (byte)Command;
        }

        public override string ToString()
        {
            return base.ToString() + "(" + EntityID + ", " + Command + ")";
        }
    }

    public class HereYourInventoryOutgoingMessage : OutgoingMessage
    {
        public override ushort Length
        {
            get
            {
                return (ushort)(4 + itemsCount * 8); /*(3+1+items)*/
            }
        }

        protected byte[] itemsBuffer = null;
        protected byte itemsCount = 0;

        public HereYourInventoryOutgoingMessage()
        {
            messageType = OutgoingMessageType.HERE_YOUR_INVENTORY;
        }

        public override OutgoingMessage CreateNew()
        {
            return new HereYourInventoryOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            // Number of items
            _return[3] = itemsCount;

            // Items
            Array.Copy(itemsBuffer, 0, _return, 4, itemsCount * 8);
        }

        public void FromInventory(EntityInventory inv)
        {
            if (inv == null)
                throw new ArgumentNullException("pc is null");

            itemsCount = inv.FilledSlotsCount;

            itemsBuffer = new byte[itemsCount * 8];
            byte itemsCopied = 0;
            for (byte i = 0; i < inv.Size; i++)
            {
                Item itm = inv.GetItemAtSlot(i);

                if (itm == null)
                    continue;

                //image id
                InPlaceBitConverter.GetBytes(itm.Definition.ImageID, itemsBuffer, (itemsCopied*8)+0);
                // quantity
                InPlaceBitConverter.GetBytes(itm.Quantity, itemsBuffer, (itemsCopied * 8) + 2);
                // pos
                itemsBuffer[(itemsCopied * 8) + 6] = itm.Slot;
                // flags
                itemsBuffer[(itemsCopied * 8) + 7] = itm.Definition.ClientFlags;

                itemsCopied++;
            }

        }

    }

    public class GetNewInventoryItemOutgoingMessage : OutgoingMessage
    {
        protected byte[] itemBuffer = new byte[8];

        public GetNewInventoryItemOutgoingMessage()
        {
            messageType = OutgoingMessageType.GET_NEW_INVENTORY_ITEM;
            length = 11;
        }

        public override OutgoingMessage CreateNew()
        {
            return new GetNewInventoryItemOutgoingMessage();
        }

        public void FromItem(Item itm)
        {
            if (itm == null)
                throw new ArgumentNullException("itm is null");

            //image id
            InPlaceBitConverter.GetBytes(itm.Definition.ImageID, itemBuffer, 0);
            // quantity
            InPlaceBitConverter.GetBytes(itm.Quantity, itemBuffer, 2);
            // pos
            itemBuffer[6] = itm.Slot;
            // flags
            itemBuffer[7] = itm.Definition.ClientFlags;
        }

        protected override void serializeSpecific(byte[] _return)
        {
            Array.Copy(itemBuffer, 0, _return, 3, 8);
        }
    }

    public class RemoveItemFromInventoryOutgoingMessage : OutgoingMessage
    {
        private byte slot;

        public byte Slot
        {
            get { return slot; }
            set { slot = value; }
        }

        public RemoveItemFromInventoryOutgoingMessage()
        {
            messageType = OutgoingMessageType.REMOVE_ITEM_FROM_INVENTORY;
            length = 4;
        }

        public override OutgoingMessage CreateNew()
        {
            return new RemoveItemFromInventoryOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            _return[3] = slot;
        }

    }

    public class InventoryItemTextOutgoingMessage : OutgoingMessage
    {
        private string text;

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public override ushort Length
        {
            get
            {
                return (ushort)(3 + Text.Length);
            }
        }

        public InventoryItemTextOutgoingMessage()
        {
            messageType = OutgoingMessageType.INVENTORY_ITEM_TEXT;
        }

        public override OutgoingMessage CreateNew()
        {
            return new InventoryItemTextOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(Text, _return, 3);
        }

        public override string ToString()
        {
            return base.ToString() + "(" + Text + ")";
        }
    }

    public class NPCTextOutgoingMessage : OutgoingMessage
    {
        private string text;

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public override ushort Length
        {
            get
            {
                return (ushort)(3 + Text.Length);
            }
        }

        public NPCTextOutgoingMessage()
        {
            messageType = OutgoingMessageType.NPC_TEXT;
        }

        public override OutgoingMessage CreateNew()
        {
            return new NPCTextOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(Text, _return, 3);
        }

        public override string ToString()
        {
            return base.ToString() + "(" + Text + ")";
        }
    }

    public class SendNPCInfoOutgoingMessage : OutgoingMessage
    {
        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private byte portrait;

        public byte Portrait
        {
            get { return portrait; }
            set { portrait = value; }
        }

        public SendNPCInfoOutgoingMessage()
        {
            messageType = OutgoingMessageType.SEND_NPC_INFO;
            length = 24;
        }

        public override OutgoingMessage CreateNew()
        {
            return new SendNPCInfoOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            string name20 = "";
            if (Name.Length > 20)
                name20 = Name.Substring(0, 20);
            else
                name20 = Name;
            InPlaceBitConverter.GetBytes(name20, _return, 3);
            for (int i = name20.Length; i < 20; i++)
                _return[i + 3] = 0; // fill with 0

            _return[23] = Portrait;
        }

        public override string ToString()
        {
            return base.ToString() + "(" + Name + ", " + Portrait + ")";
        }
    }

    public class NPCOptionsListOutgoingMessage : OutgoingMessage
    {
        private ushort npcEntityID;

        public ushort NPCEntityID
        {
            get { return npcEntityID; }
            set { npcEntityID = value; }
        }

        public override ushort Length
        {
            get
            {
                return (ushort)(3 + optionsBuffeSize);
            }
        }

        private byte[] optionsBuffer = null;
        private int optionsBuffeSize = 0;

        public NPCOptionsListOutgoingMessage()
        {
            messageType = OutgoingMessageType.NPC_OPTIONS_LIST;
        }

        public override OutgoingMessage CreateNew()
        {
            return new NPCOptionsListOutgoingMessage();
        }

        public void FromNPCOptions(NPCOptions options)
        {
            // Calculate size
            foreach (NPCOption option in options.Options)
                optionsBuffeSize += (7 + option.Text.Length);

            optionsBuffer = new byte[optionsBuffeSize];

            // Copy data
            int currentIndex = 0;
            foreach (NPCOption option in options.Options)
            {
                InPlaceBitConverter.GetBytes((ushort)(option.Text.Length + 1), optionsBuffer, currentIndex);
                currentIndex += 2;
                InPlaceBitConverter.GetBytes(option.Text, optionsBuffer, currentIndex);
                currentIndex += option.Text.Length + 1;
                InPlaceBitConverter.GetBytes(option.OptionID, optionsBuffer, currentIndex);
                currentIndex += 2;
                InPlaceBitConverter.GetBytes(NPCEntityID, optionsBuffer, currentIndex);
                currentIndex += 2;
            }

        }

        protected override void serializeSpecific(byte[] _return)
        {
            Array.Copy(optionsBuffer, 0, _return, 3, optionsBuffeSize);
        }
    }

    public class GetActorDamageOutgoingMessage : OutgoingMessage
    {
        private UInt16 entityID;

        public UInt16 EntityID
        {
            get { return entityID; }
            set { entityID = value; }
        }

        private ushort damage;

        public ushort Damage
        {
            get { return damage; }
            set { damage = value; }
        }


        public GetActorDamageOutgoingMessage()
        {
            messageType = OutgoingMessageType.GET_ACTOR_DAMAGE;
            length = 7;
        }
        public override OutgoingMessage CreateNew()
        {
            return new GetActorDamageOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(EntityID, _return, 3);
            InPlaceBitConverter.GetBytes(Damage, _return, 5);
        }

        public override string ToString()
        {
            return base.ToString() + "(" + EntityID + ", " + Damage + ")";
        }
    }

    public class GetActorHealOutgoingMessage : OutgoingMessage
    {
        private UInt16 entityID;

        public UInt16 EntityID
        {
            get { return entityID; }
            set { entityID = value; }
        }

        private ushort heal;

        public ushort Heal
        {
            get { return heal; }
            set { heal = value; }
        }


        public GetActorHealOutgoingMessage()
        {
            messageType = OutgoingMessageType.GET_ACTOR_HEAL;
            length = 7;
        }
        public override OutgoingMessage CreateNew()
        {
            return new GetActorHealOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(EntityID, _return, 3);
            InPlaceBitConverter.GetBytes(Heal, _return, 5);
        }

        public override string ToString()
        {
            return base.ToString() + "(" + EntityID + ", " + Heal + ")";
        }
    }

    public class SendPartialStatOutgoingMessage : OutgoingMessage
    {
        private PredefinedPartialStatType statType;

        public PredefinedPartialStatType StatType
        {
            get { return statType; }
            set { statType = value; }
        }

        private int _value;

        public int Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public SendPartialStatOutgoingMessage()
        {
            messageType = OutgoingMessageType.SEND_PARTIAL_STAT;
            length = 8;
        }

        public override string ToString()
        {
            return base.ToString() + "(" + StatType.ToString() + ", " + Value + ")";
        }

        protected override void serializeSpecific(byte[] _return)
        {
            _return[3] = (byte)StatType;
            InPlaceBitConverter.GetBytes(Value, _return, 4);
        }

        public override OutgoingMessage CreateNew()
        {
            return new SendPartialStatOutgoingMessage();
        }

    }


    public class AddNewActorOutgoingMessage : OutgoingMessage
    {
        /*
        struct wire_protocol
        {
            char message_type;      //  0:
            short message_length;   //  1:
            short actor_id;         //  3:
            short x_pos;            //  5: additional buffs are encoded in the last 5 bits
            short y_pos;            //  7: additional buffs are encoded in the last 5 bits
            short buffs;            //  9:
            short z_rot;            // 11:
            char actor_type;        // 13:
            char animation_frame;   // 14:
            short max_health;       // 15:
            short cur_health;       // 17:
            char kind_of_actor;     // 19:
            string name;            // 20: null terminated
            short scale;            // 20 + name.Length + 1
            char attachment_type;   // 20 + name.Length + 3
        };
        */

        protected UInt16 entityID = 0;
        public UInt16 EntityID
        {
            get { return entityID; }
        }

        protected string entityName = "";
        public String EntityName
        {
            get { return entityName; }
        }

        protected UInt16 entityScale = 0x4000;
        public UInt16 EntityScale
        {
            get { return entityScale; }
        }

        private bool hasAttachment = false;
        public bool HasAttachment
        {
            get { return hasAttachment; }
        }

        protected byte attachmentType = (byte)PredefinedModelType.MULE_BLACK;
        public byte AttachmentType
        {
            get { return attachmentType; }
        }

        public override UInt16 Length
        {
            get
            {
                if (HasAttachment)
                    return (UInt16)(24 + EntityName.Length); /*23 + name + 1*/
                else
                    return (UInt16)(23 + EntityName.Length); /*22 + name + 1*/
            }
        }

        protected short[] innerDataLocation = new short[5];
        protected short[] innerDataEnergies = new short[3];
        protected byte kindOfEntityImplementation = 0;
        protected byte modelType = 0;
        protected byte specialModifiers = 0;

        public AddNewActorOutgoingMessage()
        {
            messageType = OutgoingMessageType.ADD_NEW_ACTOR;
        }

        public void FromEntityImplementation(EntityImplementation enImpl)
        {
            entityID = enImpl.EntityID;
            entityName = enImpl.Name;
            kindOfEntityImplementation = (byte)enImpl.EntityImplementationKind;
        }

        public void FromEnergies(EntityEnergies energies)
        {
            innerDataEnergies[0] = energies.MaxHealth;
            innerDataEnergies[1] = energies.CurrentHealth;
            innerDataEnergies[2] = energies.IsAlive ? (short)1 : (short)0;
        }

        public void FromLocation(EntityLocation location)
        {
            // Location
            innerDataLocation[0] = location.X;
            innerDataLocation[1] = location.Y;
            innerDataLocation[2] = location.Z;
            innerDataLocation[3] = location.Rotation;
            innerDataLocation[4] = location.IsSittingDown ? (short)1 : (short)0;
        }

        public void FromAppearance(EntityAppearance appearance)
        {
            if (appearance.IsEnhancedModel)
                throw new ArgumentException("Appearance is enhanced");

            modelType = (byte)appearance.Type;
            specialModifiers = 0;
            if (appearance.IsTransparent)
                specialModifiers |= 0x01;

        }

        protected override void serializeSpecific(byte[] _return)
        {
            // Entity ID
            InPlaceBitConverter.GetBytes(EntityID, _return, 3);

            // X
            InPlaceBitConverter.GetBytes(innerDataLocation[0], _return, 5);

            if ((specialModifiers & 0x01) == 0x01)
                _return[6] |= 0x08; // is transparent

            // Y
            InPlaceBitConverter.GetBytes(innerDataLocation[1], _return, 7);

            // Z
            InPlaceBitConverter.GetBytes(innerDataLocation[2], _return, 9);

            // Rotation
            InPlaceBitConverter.GetBytes(innerDataLocation[3], _return, 11);

            // actor type
            _return[13] = modelType;

            // frame
            if (innerDataEnergies[2] == 1)
            {
                if (innerDataLocation[4] == 1)
                    _return[14] = (byte)PredefinedActorFrame.frame_sit_idle;
                else
                    _return[14] = (byte)PredefinedActorFrame.frame_idle;
            }
            else
            {
                _return[14] = (byte)PredefinedActorFrame.frame_die1;
            }

            // max health
            InPlaceBitConverter.GetBytes(innerDataEnergies[0], _return, 15);

            // cur health
            InPlaceBitConverter.GetBytes(innerDataEnergies[1], _return, 17);

            // kind of actor
            _return[19] = kindOfEntityImplementation;


            // EntityName
            InPlaceBitConverter.GetBytes(EntityName, _return, 20);

            // Add null terminator
            _return[20 + EntityName.Length] = 0x00;

            // Scale
            InPlaceBitConverter.GetBytes(EntityScale, _return, 21 + EntityName.Length);

            // Attachment
            if (HasAttachment && _return.Length > 23 + EntityName.Length)
                _return[23 + EntityName.Length] = AttachmentType;

        }

        public override OutgoingMessage CreateNew()
        {
            return new AddNewActorOutgoingMessage();
        }

        public override string ToString()
        {
            // TODO: Implement
            return base.ToString() + "(" + EntityID + ", " + EntityName + ")";
        }
    }

    public class SendBuffsOutgoingMessage : OutgoingMessage
    {
        private UInt16 entityID;

        public UInt16 EntityID
        {
            get { return entityID; }
            set { entityID = value; }
        }

        private byte buffs;

        public bool IsTransparent
        {
            get { return (buffs & 0x01) == 0x01; }
            set
            {
                if (value)
                    buffs |= 0x01;
                else
                    buffs &= 0xFE;
            }

        }

        public SendBuffsOutgoingMessage()
        {
            messageType = OutgoingMessageType.SEND_BUFFS;
            length = 6;
        }
        public override OutgoingMessage CreateNew()
        {
            return new SendBuffsOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(EntityID, _return, 3);
            _return[5] = buffs;
        }
    }

    public class Get3dObjOutgoingMessage : OutgoingMessage
    {

        public struct MapObj3D
        {
            public UInt16 ObjX;
            public UInt16 ObjY;
            public Single RotX;
            public Single RotY;
            public Single RotZ;
            public UInt16 Id;
            public String E3DFile;
        }

        public MapObj3D Obj;

        public override ushort Length
        {
            get {
                return (ushort)(3 + 18 + Obj.E3DFile.Length + 1);
            }
        }


        public Get3dObjOutgoingMessage()
        {
            messageType = OutgoingMessageType.GET_3D_OBJ;
        }

        public override OutgoingMessage CreateNew()
        {
            return new Get3dObjOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(Obj.ObjX, _return, 3);
            InPlaceBitConverter.GetBytes(Obj.ObjY, _return, 5);
            InPlaceBitConverter.GetBytes(Obj.RotX, _return, 7);
            InPlaceBitConverter.GetBytes(Obj.RotY, _return, 11);
            InPlaceBitConverter.GetBytes(Obj.RotZ, _return, 15);
            InPlaceBitConverter.GetBytes(Obj.Id, _return, 19);
            InPlaceBitConverter.GetBytes(Obj.E3DFile, _return, 21);
        }
    }

    public class Get3dObjListOutgoingMessage : OutgoingMessage
    {
        public struct MapObj3D
        {
            public UInt16 ObjX;
            public UInt16 ObjY;
            public Single RotX;
            public Single RotY;
            public Single RotZ;
            public UInt16 Id;
            public String E3DFile;
        }

        private IList<MapObj3D> objs;
        public IList<MapObj3D> Objs
        {
            get { return objs; }
        }


        public override ushort Length
        {
            get
            {
                int length = 4;
                foreach (MapObj3D obj in Objs)
                    length += 18 + obj.E3DFile.Length + 1;
                return (ushort)length;
            }
        }

        public Get3dObjListOutgoingMessage()
        {
            messageType = OutgoingMessageType.GET_3D_OBJ_LIST;
            objs = new List<MapObj3D>();
        }

        public override OutgoingMessage CreateNew()
        {
            return new Get3dObjListOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            int offset = 4;
            _return[3] = (byte)Objs.Count;

            foreach (MapObj3D obj in Objs)
            {
                InPlaceBitConverter.GetBytes(obj.ObjX, _return, offset + 0);
                InPlaceBitConverter.GetBytes(obj.ObjY, _return, offset + 2);
                InPlaceBitConverter.GetBytes(obj.RotX, _return, offset + 4);
                InPlaceBitConverter.GetBytes(obj.RotY, _return, offset + 8);
                InPlaceBitConverter.GetBytes(obj.RotZ, _return, offset + 12);
                InPlaceBitConverter.GetBytes(obj.Id, _return, offset + 16);
                InPlaceBitConverter.GetBytes(obj.E3DFile, _return, offset + 18);
                offset += 18 + obj.E3DFile.Length + 1;
            }
        }
    }

    public class Remove3dObjOutgoingMessage : OutgoingMessage
    {
        public Int16 Id;

        public Remove3dObjOutgoingMessage()
        {
            messageType = OutgoingMessageType.REMOVE_3D_OBJ;
            length = 5;
        }

        public override OutgoingMessage CreateNew()
        {
            return new Remove3dObjOutgoingMessage();
        }

        protected override void serializeSpecific(byte[] _return)
        {
            InPlaceBitConverter.GetBytes(Id, _return, 3);
        }
    }

}
