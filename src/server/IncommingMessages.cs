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

namespace Calindor.Server.Messaging
{
    // ABSTRACTS
    public enum IncommingMessageType
    {
        RAW_TEXT = 0,
        MOVE_TO = 1,
        SEND_PM = 2,
        SIT_DOWN = 7,
        SEND_OPENING_SCREEN = 9,
        SEND_VERSION = 10,
        TURN_LEFT  = 11,
        TURN_RIGHT  = 12,
        HEART_BEAT = 14,
        USE_MAP_OBJECT = 16,
        LOOK_AT_INVENTORY_ITEM = 19,
        MOVE_INVENTORY_ITEM = 20,
        HARVEST = 21,
        DROP_ITEM = 22,
        TOUCH_PLAYER = 28,
        RESPOND_TO_NPC = 29,
        LOG_IN = 140,
        CREATE_CHAR = 141,
        UNKNOWN = 256 //THIS MESSAGE DOES NOT EXIST
    }

    public abstract class IncommingMessage
    {
        protected IncommingMessageType messageType = IncommingMessageType.RAW_TEXT;
        protected UInt16 length = 0;

        public IncommingMessageType MessageType
        {
            get { return messageType; }
        }

        public UInt16 Length
        {
            get { return length; }
        }

        public virtual void Deserialize(byte[] stream, int startIndex)
        {
            // Check type
            if (GetMessageType(stream, startIndex) != (byte)messageType)
                throw new MessageDeserializeException("Stream [TYPE] not compatibile with message.");

            // Read length
            length = (UInt16)(BitConverter.ToUInt16(stream, startIndex + 1) + (UInt16)2);

            // Specific data
            deserializeSpecific(stream, startIndex);
        }

        public abstract IncommingMessage CreateNew();

        protected virtual void deserializeSpecific(byte[] stream, int startIndex)
        {
        }

        public virtual IncommingMessage DeserializeToNew(byte[] stream, int startIndex)
        {
            IncommingMessage _return = CreateNew();
            _return.Deserialize(stream, startIndex);
            return _return;
        }

        public static byte GetMessageType(byte[] stream, int startIndex)
        {
            return stream[startIndex];
        }

        public override string ToString()
        {
            return "(" + MessageType.ToString() + ")";
        }

    }

    public class IncommingMessagesQueue : Queue<IncommingMessage>
    {
    }

    public class MessageDeserializeException : ApplicationException
    {
        public MessageDeserializeException(string message) : base(message) { }
    }

    // FACTORY
    public sealed class IncommingMessagesFactory
    {
        private static IncommingMessage[] knownMessages = new IncommingMessage[256];

        static IncommingMessagesFactory()
        {
            // Add known messages
            knownMessages[(int)IncommingMessageType.SEND_VERSION] = new SendVersionIncommingMessage();
            knownMessages[(int)IncommingMessageType.SEND_OPENING_SCREEN] = new SendOpeningScreenIncommingMessage();
            knownMessages[(int)IncommingMessageType.LOG_IN] = new LogInIncommingMessage();
            knownMessages[(int)IncommingMessageType.HEART_BEAT] = new HeartBeatIncommingMessage();
            knownMessages[(int)IncommingMessageType.CREATE_CHAR] = new CreateCharIncommingMessage();
            knownMessages[(int)IncommingMessageType.MOVE_TO] = new MoveToIncommingMessage();
            knownMessages[(int)IncommingMessageType.SEND_PM] = new SendPMIncommingMessage();
            knownMessages[(int)IncommingMessageType.SIT_DOWN] = new SitDownIncommingMessage();
            knownMessages[(int)IncommingMessageType.TURN_LEFT] = new TurnLeftIncommingMessage();
            knownMessages[(int)IncommingMessageType.TURN_RIGHT] = new TurnRightIncommingMessage();
            knownMessages[(int)IncommingMessageType.RAW_TEXT] = new RawTextIncommingMessage();
            knownMessages[(int)IncommingMessageType.USE_MAP_OBJECT] = new UseMapObjectIncommingMessage();
            knownMessages[(int)IncommingMessageType.LOOK_AT_INVENTORY_ITEM] = new LookAtInventoryItemIncommingMessage();
            knownMessages[(int)IncommingMessageType.DROP_ITEM] = new DropItemIncommingMessage();
            knownMessages[(int)IncommingMessageType.MOVE_INVENTORY_ITEM] = new MoveInventoryItemIncommingMessage();
            knownMessages[(int)IncommingMessageType.HARVEST] = new HarvestIncommingMessage();
            knownMessages[(int)IncommingMessageType.TOUCH_PLAYER] = new TouchPlayerIncommingMessage();
            knownMessages[(int)IncommingMessageType.RESPOND_TO_NPC] = new RespondToNPCIncommingMessage();
        }
        
        /// <summary>
        /// Deserializes message from stream. Throws/passes a number of exception which need to be caugh at upper level.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static IncommingMessage Deserialize(byte[] stream, int startIndex)
        {
            IncommingMessage _return = null;
            
            byte type = IncommingMessage.GetMessageType(stream, startIndex);

            if (knownMessages[type] != null)
                _return = knownMessages[type].DeserializeToNew(stream, startIndex);

            if (_return == null) // Message is unknown so far but it is required to process due to heartbeat
            {
                _return = new UnknownIncommingMessage();
                _return.Deserialize(stream, startIndex);
            }

            return _return;
        }
    }

    // UNKNOWN MESSAGE
    public class UnknownIncommingMessage : IncommingMessage
    {
        private byte deserializedType = 0;

        public UnknownIncommingMessage()
        {
            messageType = IncommingMessageType.UNKNOWN;
        }

        public override IncommingMessage CreateNew()
        {
            return new UnknownIncommingMessage();
        }

        public override void Deserialize(byte[] stream, int startIndex)
        {
            deserializedType = stream[startIndex];
        }

        public override string ToString()
        {
            return base.ToString() + "(TYPE: " + deserializedType + ")";
        }

    }



    // IMPLEMENTATION

    public class SendVersionIncommingMessage : IncommingMessage
    {

        private UInt16 protocolVersionFirstDigit = 0;
        private UInt16 protocolVersionSecondDigit = 0;
        private byte[] clientVersion = new byte[4];
        private byte[] hostIP = new byte[4];
        private UInt16 hostPort = 0;

        public UInt16 ProtocolVersionFirstDigit
        {
            get { return protocolVersionFirstDigit; }
        }

        public UInt16 ProtocolVersionSecondDigit
        {
            get { return protocolVersionSecondDigit; }
        }

        public SendVersionIncommingMessage()
        {
            messageType = IncommingMessageType.SEND_VERSION;
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            protocolVersionFirstDigit = BitConverter.ToUInt16(stream, 3);
            protocolVersionSecondDigit = BitConverter.ToUInt16(stream, 5);
            
            for (int i = 0; i < 4; i++)
            {
                clientVersion[i] = stream[7 + i];
                hostIP[i] = stream[11 + i];
            }
            
            hostPort = (UInt16)(stream[15] << 8);
            hostPort += stream[16];
        }

        public override IncommingMessage CreateNew()
        {
            return new SendVersionIncommingMessage();
        }
    }

    public class SendOpeningScreenIncommingMessage : IncommingMessage
    {
        public SendOpeningScreenIncommingMessage()
        {
            messageType = IncommingMessageType.SEND_OPENING_SCREEN;
        }

        public override IncommingMessage CreateNew()
        {
            return new SendOpeningScreenIncommingMessage();
        }
    }

    public class LogInIncommingMessage : IncommingMessage
    {
        private string userName = "";
        private string password = "";

        public string UserName
        {
            get { return userName; }
        }

        public string Password
        {
            get { return password; }
        }

        public LogInIncommingMessage()
        {
            messageType = IncommingMessageType.LOG_IN;
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            userName = "";
            password = "";

            for (int i = startIndex + 3; i < startIndex + Length - 1; i++)
            {
                // Find first space
                if (stream[i] == 32)
                {
                    for (int j = startIndex + 3; j < i; j++)
                        userName += (char)stream[j];
                    for (int j = i + 1; j < startIndex + Length - 1; j++)
                        password += (char)stream[j];
                    break;
                }
            }
        }

        public override IncommingMessage CreateNew()
        {
            return new LogInIncommingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + UserName + "," + Password + ")";
        }
    }

    public class HeartBeatIncommingMessage : IncommingMessage
    {
        public HeartBeatIncommingMessage()
        {
            messageType = IncommingMessageType.HEART_BEAT;
        }

        public override IncommingMessage CreateNew()
        {
            return new HeartBeatIncommingMessage();
        }
    }

    public class CreateCharIncommingMessage : IncommingMessage
    {
        private PredefinedModelHead head = PredefinedModelHead.HEAD_1;
        public PredefinedModelHead Head
        {
            get { return head; }
        }

        private PredefinedEntityType type = PredefinedEntityType.HUMAN_FEMALE;
        public PredefinedEntityType Type
        {
            get { return type; }
        }

        private PredefinedModelSkin skin = PredefinedModelSkin.SKIN_BROWN;
        public PredefinedModelSkin Skin
        {
            get { return skin; }
        }

        private PredefinedModelHair hair = PredefinedModelHair.HAIR_BLACK;
        public PredefinedModelHair Hair
        {
            get { return hair; }
        }

        private PredefinedModelShirt shirt = PredefinedModelShirt.SHIRT_BLACK;
        public PredefinedModelShirt Shirt
        {
            get { return shirt; }
        }

        private PredefinedModelPants pants = PredefinedModelPants.PANTS_BLACK;
        public PredefinedModelPants Pants
        {
            get { return pants; }
        }
        
        private PredefinedModelBoots boots = PredefinedModelBoots.BOOTS_BLACK;
        public PredefinedModelBoots Boots
        {
            get { return boots; }
        }

        private string userName = "";
        private string password = "";

        public string UserName
        {
            get { return userName; }
        }

        public string Password
        {
            get { return password; }
        }
        
        public CreateCharIncommingMessage()
        {
            messageType = IncommingMessageType.CREATE_CHAR;
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            userName = "";
            password = "";

            for (int i = startIndex + 3; i < startIndex + Length - 7; i++)
            {
                // Find first space
                if (stream[i] == 32)
                {
                    for (int j = startIndex + 3; j < i; j++)
                        userName += (char)stream[j];
                    for (int j = i + 1; j < startIndex + Length - 8/*Without \0*/; j++)
                        password += (char)stream[j];
                    break;
                }
            }

            skin = (PredefinedModelSkin)stream[startIndex + Length - 7];
            hair = (PredefinedModelHair)stream[startIndex + Length - 6];
            shirt = (PredefinedModelShirt)stream[startIndex + Length - 5];
            pants = (PredefinedModelPants)stream[startIndex + Length - 4];
            boots = (PredefinedModelBoots)stream[startIndex + Length - 3];
            type = (PredefinedEntityType)stream[startIndex + Length - 2];
            head = (PredefinedModelHead)stream[startIndex + Length - 1];
        }

        public override IncommingMessage CreateNew()
        {
            return new CreateCharIncommingMessage();
        }
    }

    public class MoveToIncommingMessage: IncommingMessage
    {
        private Int16 x;

        public Int16 X
        {
            get { return x; }
        }

        private Int16 y;

        public Int16 Y
        {
            get { return y; }
        }
	
	
        public MoveToIncommingMessage()
        {
            messageType = IncommingMessageType.MOVE_TO;
        }

        public override IncommingMessage CreateNew()
        {
            return new MoveToIncommingMessage();
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            x = BitConverter.ToInt16(stream, startIndex + 3);
            y = BitConverter.ToInt16(stream, startIndex + 5);
        }

        public override string ToString()
        {
            return base.ToString() + "(" + X + ", " + Y + ")";
        }
    }

    public class SendPMIncommingMessage : IncommingMessage
    {
        private string recipientName;
        public string RecipientName
        {
            get { return recipientName; }
        }

        private string text;
        public string Text
        {
            get { return text; }
        }
	
        public SendPMIncommingMessage()
        {
            messageType = IncommingMessageType.SEND_PM;
        }

        public override IncommingMessage CreateNew()
        {
            return new SendPMIncommingMessage();
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            recipientName = "";
            text = "";

            for (int i = startIndex + 3; i < startIndex + Length; i++)
            {
                // Find first space
                if (stream[i] == 32)
                {
                    for (int j = startIndex + 3; j < i; j++)
                        recipientName += (char)stream[j];
                    for (int j = i + 1; j < startIndex + Length; j++)
                        text += (char)stream[j];
                    break;
                }
            }
        }

        public override string ToString()
        {
            return base.ToString() + "(" + RecipientName + ":" + Text + ")";
        }
    }

    public class SitDownIncommingMessage : IncommingMessage
    {
        private bool shouldSit = false;
        public bool ShouldSit
        {
            get { return shouldSit; }
        }

        public SitDownIncommingMessage()
        {
            messageType = IncommingMessageType.SIT_DOWN;
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            byte b = stream[startIndex + 3];
            if (b == 1)
                shouldSit = true;
            else
                shouldSit = false;
        }

        public override IncommingMessage CreateNew()
        {
            return new SitDownIncommingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(SHOULD_SIT=" + ShouldSit + ")";
        }
    }

    public class TurnLeftIncommingMessage : IncommingMessage
    {
        public TurnLeftIncommingMessage()
        {
            messageType = IncommingMessageType.TURN_LEFT;
        }

        public override IncommingMessage CreateNew()
        {
            return new TurnLeftIncommingMessage();
        }
    }

    public class TurnRightIncommingMessage : IncommingMessage
    {
        public TurnRightIncommingMessage()
        {
            messageType = IncommingMessageType.TURN_RIGHT;
        }

        public override IncommingMessage CreateNew()
        {
            return new TurnRightIncommingMessage();
        }
    }

    public class RawTextIncommingMessage : IncommingMessage
    {
        private string text;

        public string Text
        {
            get { return text; }
        }
	
        public RawTextIncommingMessage()
        {
            messageType = IncommingMessageType.RAW_TEXT;
        }

        public override IncommingMessage CreateNew()
        {
            return new RawTextIncommingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + Text + ")";
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            text = "";

            for (int i = startIndex + 3; i < startIndex + Length; i++)
            {
                text += (char)stream[i];
            }
        }
    }

    public class UseMapObjectIncommingMessage : IncommingMessage
    {
        private int targetObjectID = -1;
        private int objectdUsedOnTarget = -1;

        public int TargetObjectID
        {
            get { return targetObjectID; }
        }

        public int ObjectdUsedOnTarget
        {
            get { return objectdUsedOnTarget; }
        }

        public UseMapObjectIncommingMessage()
        {
            messageType = IncommingMessageType.USE_MAP_OBJECT;
        }

        public override IncommingMessage CreateNew()
        {
            return new UseMapObjectIncommingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + TargetObjectID + ", " + ObjectdUsedOnTarget + ")";
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            targetObjectID = BitConverter.ToInt32(stream, startIndex + 3);
            objectdUsedOnTarget = BitConverter.ToInt32(stream, startIndex + 7);
        }
    }

    public class LookAtInventoryItemIncommingMessage : IncommingMessage
    {
        private byte slot;
        public byte Slot
        {
            get { return slot; }
        }
	
        public LookAtInventoryItemIncommingMessage()
        {
            messageType = IncommingMessageType.LOOK_AT_INVENTORY_ITEM;
        }

        public override IncommingMessage CreateNew()
        {
            return new LookAtInventoryItemIncommingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + slot + ")";
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            slot = stream[startIndex + 3];
        }
    }

    public class DropItemIncommingMessage : IncommingMessage
    {
        private byte slot;
        public byte Slot
        {
            get { return slot; }
        }

        private int quantity;
        public int Quantity
        {
            get { return quantity; }
        }
	

        public DropItemIncommingMessage()
        {
            messageType = IncommingMessageType.DROP_ITEM;
        }

        public override IncommingMessage CreateNew()
        {
            return new DropItemIncommingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + slot + ", " + quantity + ")";
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            slot = stream[startIndex + 3];
            quantity = BitConverter.ToInt32(stream, startIndex + 4);
        }
    }

    public class MoveInventoryItemIncommingMessage : IncommingMessage
    {
        private byte slot;
        public byte Slot
        {
            get { return slot; }
        }

        private byte newSlot;
	    public byte NewSlot
	    {
	        get { return newSlot;}
	    }
	

        public MoveInventoryItemIncommingMessage()
        {
            messageType = IncommingMessageType.MOVE_INVENTORY_ITEM;
        }

        public override IncommingMessage CreateNew()
        {
            return new MoveInventoryItemIncommingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + slot + ", " + newSlot + ")";
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            slot = stream[startIndex + 3];
            newSlot = stream[startIndex + 4];
        }
    }

    public class HarvestIncommingMessage : IncommingMessage
    {
        private int targetObjectID = -1;

        public int TargetObjectID
        {
            get { return targetObjectID; }
        }

        public HarvestIncommingMessage()
        {
            messageType = IncommingMessageType.HARVEST;
        }

        public override IncommingMessage CreateNew()
        {
            return new HarvestIncommingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + TargetObjectID + ")";
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            targetObjectID = BitConverter.ToUInt16(stream, startIndex + 3);
        }
    }

    public class TouchPlayerIncommingMessage : IncommingMessage
    {
        private int targetEntityID = -1;

        public ushort TargetEntityID
        {
            get { return (ushort)targetEntityID; }
        }

        public TouchPlayerIncommingMessage()
        {
            messageType = IncommingMessageType.TOUCH_PLAYER;
        }

        public override IncommingMessage CreateNew()
        {
            return new TouchPlayerIncommingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + TargetEntityID + ")";
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            targetEntityID = BitConverter.ToInt32(stream, startIndex + 3);
        }
    }

    public class RespondToNPCIncommingMessage : IncommingMessage
    {
        private short targetEntityID = -1;

        public ushort TargetEntityID
        {
            get { return (ushort)targetEntityID; }
        }

        private short optionID = -1;
        public ushort OptionID
        {
            get { return (ushort)optionID; }
        }
	

        public RespondToNPCIncommingMessage()
        {
            messageType = IncommingMessageType.RESPOND_TO_NPC;
        }

        public override IncommingMessage CreateNew()
        {
            return new RespondToNPCIncommingMessage();
        }

        public override string ToString()
        {
            return base.ToString() + "(" + TargetEntityID + ", " + OptionID + ")";
        }

        protected override void deserializeSpecific(byte[] stream, int startIndex)
        {
            targetEntityID = BitConverter.ToInt16(stream, startIndex + 3);
            optionID = BitConverter.ToInt16(stream, startIndex + 5);
        }
    }

}
