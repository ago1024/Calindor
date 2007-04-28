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
using Calindor.Server.Serialization;
using System.Collections.Generic;

namespace Calindor.Server.Items
{
    public class ItemDefinition
    {
        private ushort definitionID;
        public ushort ID
        {
            get { return definitionID; }
        }

        private ushort imageID;
        public ushort ImageID
        {
            get { return imageID; }
        }

        private string  name;
        public string  Name
        {
            get { return name; }
        }

        private byte clientFlags;
        public byte ClientFlags
        {
            get { return clientFlags; }
        }        
	
        // TODO: Temp?
        public ItemDefinition(ushort id, ushort imageID, string name)
        {
            this.definitionID = id;
            this.imageID = imageID;
            this.name = name;
            this.clientFlags = 0x0;
            this.clientFlags |= 0x4; // all items are stackable
            this.clientFlags |= 0x2; // all items resources
            
            // TODO: Item will have weight and cubic size and will fill storage based on those paramaters

            /* 
             * Warning: code does not support non-stackable items, meaning two items of the same type cannot be
             * in two different slots in the same storage
             */
        }
    }

    public class ItemDefinitionDictionary : Dictionary<ushort, ItemDefinition>
    {
    }

    public class ItemDefinitionCache
    {
        private static ItemDefinitionDictionary innerDictionary = new ItemDefinitionDictionary();

        static ItemDefinitionCache()
        { 
            // TODO: Temp, should be loaded from scripts

            // soverigns
            addItemDefinition(new ItemDefinition(1, 3, "Sovereigns"));
            // wegetables
            addItemDefinition(new ItemDefinition(2, 2, "Vegetables"));
            // tiger lilly
            addItemDefinition(new ItemDefinition(3, 29, "Tiger Lilly"));
            // rusted iron sword
            addItemDefinition(new ItemDefinition(4, 101, "Rusty Iron Sword"));
            // royals
            addItemDefinition(new ItemDefinition(5, 100, "Royals"));
            // iron sword
            addItemDefinition(new ItemDefinition(6, 101, "Iron Sword"));
        }

        private static void addItemDefinition(ItemDefinition itmDef)
        {
            if (innerDictionary.ContainsKey(itmDef.ID))
                throw new ArgumentException("ItemID: " + itmDef.ID + " already in cache");
            innerDictionary.Add(itmDef.ID, itmDef);
        }

        public static ItemDefinition GetItemDefinitionByID(ushort itemID)
        {
            if (innerDictionary.ContainsKey(itemID))
                return innerDictionary[itemID];
            else
                return null;
        }
    }

    public class Item
    {
        private ItemDefinition itemDef = null;
        public ItemDefinition Definition
        {
            get { return itemDef; }
        }

        private int quantity;
        public int Quantity
        {
            get { return quantity; }
            set { quantity = value; }
        }

        private byte slot;

        public byte Slot
        {
            get { return slot; }
            set { slot = value; }
        }
	

        private Item()
        {
        }

        public Item(ItemDefinition def)
        {
            itemDef = def;
            quantity = 0;
            slot = 0;
        }
    }

    public class ItemStorage
    {
        protected Item[] itemSlots = null;
        protected byte filledSlotsCount = 0;
        protected byte totalSlotsCount = 0;

        public byte FilledSlotsCount
        {
            get { return filledSlotsCount; }
        }
        public byte Size
        {
            get { return totalSlotsCount; }
        }

        private ItemStorage()
        {
        }

        public ItemStorage(byte storageSize)
        {
            if (storageSize > 250)
                throw new ArgumentException("Item storage cannot be larger than 250. Use multile storages");

            itemSlots = new Item[storageSize];
            filledSlotsCount = 0;
            totalSlotsCount = storageSize;
        }

        /// <summary>
        /// Add/Updates/Removes item from storage
        /// </summary>
        /// <param name="itm"></param>
        /// <returns>Modified item or null if item cannot be added to storage</returns>
        public Item UpdateItem(Item itm)
        {
            if (itm == null)
                throw new ArgumentNullException("itm cannot be null");
            
            // Try to find already existing
            Item itmInStore = FindItemByDefinitionID(itm.Definition.ID);
            if (itmInStore != null)
            {
                itmInStore.Quantity += itm.Quantity;
                if (itmInStore.Quantity <= 0)
                {
                    itmInStore.Quantity = 0;
                    return RemoveItemAtSlot(itmInStore.Slot);
                }
                else
                    return itmInStore;
            }

            // Not found. Need to add at first free location
            for (byte i = 0; i < totalSlotsCount; i++)
                if (IsSlotFree(i))
                    return InsertItemToSlot(i, itm);

            // If we are here, item cannot be added to storage
            return null;
        }

        public Item FindItemByDefinitionID(ushort definitionID)
        {
            Item itmInStore = null;
            for (byte i = 0; i < totalSlotsCount; i++)
            {
                itmInStore = GetItemAtSlot(i);
                if (itmInStore != null)
                    if (itmInStore.Definition.ID == definitionID)
                        return itmInStore;
            }

            return null;
        }

        public Item RemoveItemAtSlot(byte slot)
        {
            if (slot >= totalSlotsCount)
                throw new ArgumentException("Slot " + slot + " outside the size of storage(" + totalSlotsCount + ")");

            Item _return = GetItemAtSlot(slot);

            if (_return != null)
            {
                itemSlots[slot] = null;
                filledSlotsCount--;
            }

            return _return;
        }

        public Item GetItemAtSlot(byte slot)
        {
            if (slot >= totalSlotsCount)
                throw new ArgumentException("Slot " + slot + " outside the size of storage(" + totalSlotsCount + ")");

            return itemSlots[slot];
        }
        /// <summary>
        /// Inserts item to an empty slot. If slot is not empty, throws exception
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="itm"></param>
        /// <returns></returns>
        public Item InsertItemToSlot(byte slot, Item itm)
        {
            if (slot >= totalSlotsCount)
                throw new ArgumentException("Slot " + slot + " outside the size of storage(" + totalSlotsCount + ")");

            if (!IsSlotFree(slot))
                throw new ArgumentException("Slot " + slot +  " is not free.");

            itemSlots[slot] = itm;
            itm.Slot = slot;
            filledSlotsCount++;
            return itm;
        }

        public bool IsSlotFree(byte slot)
        {
            if (slot >= totalSlotsCount)
                throw new ArgumentException("Slot " + slot + " outsize the size of storage(" + totalSlotsCount + ")");

            return itemSlots[slot] == null;
        }

        private void clear()
        {
            for (byte i = 0; i < totalSlotsCount; i++)
                itemSlots[i] = null;
            filledSlotsCount = 0;
        }

        #region Storage
        public virtual void Serialize(ISerializer sr)
        {
            // size
            sr.WriteValue(totalSlotsCount);

            // actual number of slots filled
            sr.WriteValue(filledSlotsCount);

            Item itm = null;

            // items
            for (byte i = 0; i < totalSlotsCount; i++)
            {
                itm = GetItemAtSlot(i);

                if (itm == null)
                    continue;

                // write position
                sr.WriteValue(itm.Slot);

                // write item id
                sr.WriteValue(itm.Definition.ID);

                // write quantity
                sr.WriteValue(itm.Quantity);
            }
        }

        public virtual void Deserialize(IDeserializer dsr)
        {
            // clear array
            clear();

            // size
            byte sizeStored = dsr.ReadByte();

            if (sizeStored != totalSlotsCount)
                throw new DeserializationException("Deserialized size different than storage size");

            // number of filled slots
            byte itemsToBeLoaded = dsr.ReadByte();

            if (itemsToBeLoaded > sizeStored)
                throw new DeserializationException("Number of items to deserialize greater than storage size");
            
            byte position = 0;
            ushort itemID = 0;
            for (byte i = 0; i < itemsToBeLoaded; i++)
            {
                // read position
                position = dsr.ReadByte();

                if (position >= totalSlotsCount)
                    throw new DeserializationException("Item position greater than storage size, Item: " 
                        + i + ", Position: " + position);

                if (!IsSlotFree(position))
                    throw new ArgumentException("Position " + position + " is not free.");

                // get item definition
                itemID = dsr.ReadUShort();
                ItemDefinition itmDef = ItemDefinitionCache.GetItemDefinitionByID(itemID);

                if (itmDef == null)
                    throw new DeserializationException("ItemStorage: Item definition for ID " + itemID + " not found, Item: "
                        + i + ", Position: " + position);

                // create item
                Item itm = new Item(itmDef);
                itm.Quantity = dsr.ReadSInt();
                if (itm.Quantity < 0)
                    throw new DeserializationException("ItemStorage: Item quantity less than 0, Item: "
                        + i + ", Position: " + position);
                                
                // all ok (itm.Position set here)
                InsertItemToSlot(position, itm);
                    
            }
        }
        #endregion
    }
}