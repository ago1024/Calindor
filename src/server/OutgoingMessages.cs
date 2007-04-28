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
        ADD_ACTOR_COMMAND = 2,
        YOU_ARE = 3,
        NEW_MINUTE = 5,
        REMOVE_ACTOR = 6,
        CHANGE_MAP = 7,
        KILL_ALL_ACTORS = 9,
        TELEPORT_IN = 12,
        TELEPORT_OUT = 13,
        //HERE_YOUR_STATS = 18,
        HERE_YOUR_INVENTORY = 19,
        INVENTORY_ITEM_TEXT = 20,
        GET_NEW_INVENTORY_ITEM  = 21,
        REMOVE_ITEM_FROM_INVENTORY = 22,
        ADD_NEW_ENHANCED_ACTOR = 51,
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
//            knownMessages[(int)OutgoingMessageType.HERE_YOUR_STATS] = new HereYourStatsOutgoingMessage();
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

        }

        public static OutgoingMessage Create(OutgoingMessageType type)
        {
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
            set { entityID = value; }
        }

        protected string entityName = "";
        public String EntityName
        {
            get { return entityName; }
            set { entityName = value; }
        }

        public override UInt16 Length
        {
            get
            {
                return (UInt16)(32 + EntityName.Length); /*31 + name + 1*/ 
            }
        }

        protected byte [] innerDataAppearance = new byte[7];
        protected short[] innerDataLocation = new short[5];

        public AddNewEnhancedActorOutgoingMessage()
        {
            messageType = OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR;
        }

        public void FromPlayerCharacter(PlayerCharacter pc)
        {
            EntityID = pc.EntityID;
            EntityName = pc.Name;

            // Appearance 
            // TODO:What if wielding items?
            innerDataAppearance[0] = (byte)pc.Appearance.Skin;
            innerDataAppearance[1] = (byte)pc.Appearance.Hair;
            innerDataAppearance[2] = (byte)pc.Appearance.Shirt;
            innerDataAppearance[3] = (byte)pc.Appearance.Pants;
            innerDataAppearance[4] = (byte)pc.Appearance.Boots;
            innerDataAppearance[5] = (byte)pc.Appearance.Type;
            innerDataAppearance[6] = (byte)pc.Appearance.Head;

            // Location
            innerDataLocation[0] = pc.Location.X;
            innerDataLocation[1] = pc.Location.Y;
            innerDataLocation[2] = pc.Location.Z;
            innerDataLocation[3] = pc.Location.Rotation;
            innerDataLocation[4] = pc.Location.IsSittingDown ? (short)1:(short)0;
        }

        protected override void serializeSpecific(byte[] _return)
        {
            // Entity ID
            InPlaceBitConverter.GetBytes(EntityID, _return, 3);

            // X
            InPlaceBitConverter.GetBytes(innerDataLocation[0], _return, 5);
            
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
            if (innerDataLocation[4] == 1)
                _return[25] = (byte)PredefinedActorFrame.frame_sit_idle;
            else
                _return[25] = (byte)PredefinedActorFrame.frame_stand;

            // max health
            _return[26] = 0x78;
            _return[27] = 0x00;

            // cur health
            _return[28] = 0x78;
            _return[29] = 0x00;
            
            // kind of actor
            _return[30] = 0x01;



            // EntityName
            // TODO: Combine name with guild??
            InPlaceBitConverter.GetBytes(EntityName, _return, 31);

            // Add null terminator
            _return[31 + EntityName.Length] = 0x00;
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

    /*public class HereYourStatsOutgoingMessage : OutgoingMessage
    {
        private byte[] statsBuffer = null;
        public HereYourStatsOutgoingMessage()
        {
            messageType = OutgoingMessageType.HERE_YOUR_STATS;
            length = 193;
            statsBuffer = new byte[length - 3];
        }
        protected override void serializeSpecific(byte[] _return)
        {
            for (int i = 0; i < statsBuffer.Length; i++)
                _return[i + 3] = statsBuffer[i];
        }

        /// <summary>
        /// Copies selected player properties to internal buffer
        /// </summary>
        /// <param name="pc"></param>
        public void FromPlayerCharacter(PlayerCharacter pc)
        {
            // Basic attributes
            InPlaceBitConverter.GetBytes(pc.BasicAttributesCurrent.Physique, statsBuffer, 0);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesBase.Physique, statsBuffer, 2);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesCurrent.Coordination, statsBuffer, 4);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesBase.Coordination, statsBuffer, 6);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesCurrent.Reasoning, statsBuffer, 8);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesBase.Reasoning, statsBuffer, 10);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesCurrent.Will, statsBuffer, 12);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesBase.Will, statsBuffer, 14);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesCurrent.Instinct, statsBuffer, 16);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesBase.Instinct, statsBuffer, 18);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesCurrent.Vitality, statsBuffer, 20);
            InPlaceBitConverter.GetBytes(pc.BasicAttributesBase.Vitality, statsBuffer, 22);
        }

        public override OutgoingMessage CreateNew()
        {
            return new HereYourStatsOutgoingMessage();
        }
    }*/

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
}
