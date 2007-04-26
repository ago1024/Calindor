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
        private ushort itemID;
        public ushort ItemID
        {
            get { return itemID; }
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
	
	
        // TODO: Flags
	
        // TODO: Temp?
        public ItemDefinition(ushort itemID, ushort imageID, string name)
        {
            this.itemID = itemID;
            this.imageID = imageID;
            this.name = name;
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
            addItemDefinition(new ItemDefinition(2, 2, "Wegetables"));
            // sunflowers
            addItemDefinition(new ItemDefinition(3, 29, "Tiger Lilly"));
        }

        private static void addItemDefinition(ItemDefinition itmDef)
        {
            innerDictionary.Add(itmDef.ItemID, itmDef);
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

        private short position;

        public short Position
        {
            get { return position; }
            set { position = value; }
        }
	

        private Item()
        {
        }

        public Item(ItemDefinition def)
        {
            itemDef = def;
            quantity = 0;
        }
    }

    public class ItemStorage
    {
        protected Item[] itemSlots = null;
        protected short filledSlotesCount = 0;
        public short FilledSlotsCount
        {
            get { return filledSlotesCount; }
        }
        public short Size
        {
            get { return (short)itemSlots.GetLength(0); }
        }

        private ItemStorage()
        {
        }

        public ItemStorage(int storageSize)
        {
            if (storageSize < 0)
                throw new ArgumentException("storageSize must be greater or equal 0");

            if (storageSize >= short.MaxValue)
                throw new ArgumentException("storageSize must be lesser then " + short.MaxValue);

            itemSlots = new Item[storageSize];
            filledSlotesCount = 0;
        }

        public short AddItemToFreeSlot(Item itm)
        {
            for (short i=0;i<itemSlots.GetLength(0);i++)
                if (GetItemAtPosition(i) == null)
                {
                    SetItemAtPosition(i, itm);
                    return i;
                }

            return -1;
        }

        public short AddItem(Item itm)
        {
            Item itmInSlot = null;

            for (short i = 0; i < itemSlots.GetLength(0); i++)
            {
                itmInSlot = GetItemAtPosition(i);
                if (itmInSlot != null)
                {
                    if (itmInSlot.Definition.ItemID == itm.Definition.ItemID)
                    {
                        // TODO: Check if quantity + quantity < Int32.MaxValue
                        itmInSlot.Quantity += itm.Quantity;
                        return i;
                    }
                }
            }

            return AddItemToFreeSlot(itm);
        }

        public bool SetItemAtPosition(short pos, Item itm)
        {
            if ((pos < 0) || (pos >= itemSlots.GetLength(0)))
                return false;

            if (itemSlots[pos] != null)
                return false;

            itemSlots[pos] = itm;
            itm.Position = pos;
            filledSlotesCount++;
            return true;
        }

        public bool RemoveItemAtPosition(short pos)
        {
            if ((pos < 0) || (pos >= itemSlots.GetLength(0)))
                return false;

            if (itemSlots[pos] == null)
                return false;

            itemSlots[pos] = null;
            filledSlotesCount--;
            return true;
        }

        public short RemoveItem(ushort itemID)
        {
            Item itmInSlot = null;

            for (short i = 0; i < itemSlots.GetLength(0); i++)
            {
                itmInSlot = GetItemAtPosition(i);
                if (itmInSlot != null)
                {
                    if (itmInSlot.Definition.ItemID == itemID)
                    {
                        RemoveItemAtPosition(i);
                        return i;
                    }
                }
            }

            return -1;
        }

        public Item GetItemAtPosition(short pos)
        {
            if ((pos < 0) || (pos >= itemSlots.GetLength(0)))
                return null;

            return itemSlots[pos];
        }

        private void clear()
        {
            for (int i = 0; i < itemSlots.GetLength(0); i++)
                itemSlots[i] = null;
            filledSlotesCount = 0;
        }

        #region Storage
        public virtual void Serialize(ISerializer sr)
        {
            // size
            sr.WriteValue((short)itemSlots.GetLength(0));

            // actual number of slots filled
            sr.WriteValue(filledSlotesCount);

            // items
            for (int i = 0; i < itemSlots.GetLength(0); i++)
            {
                if (itemSlots[i] == null)
                    continue;

                // write position
                sr.WriteValue((short)i);

                // write item id
                sr.WriteValue(itemSlots[i].Definition.ItemID);

                // write quantity
                sr.WriteValue(itemSlots[i].Quantity);
            }
        }

        public virtual void Deserialize(IDeserializer dsr)
        {
            // clear array
            clear();

            // size
            short sizeStored = dsr.ReadShort();

            if (sizeStored != itemSlots.GetLength(0))
                throw new DeserializationException("ItemStorage: sizeStored different than size of itemSlots");

            // number of filled slots
            short itemsToBeLoaded = dsr.ReadShort();

            if (itemsToBeLoaded > sizeStored)
                throw new DeserializationException("ItemStorage: itemsToBeLoaded greater than sizeStored");

            if (itemsToBeLoaded < 0)
                throw new DeserializationException("ItemStorage: itemsToBeLoaded lesser than 0");

            short position = -1;
            ushort itemID = 0;
            for (int i = 0; i < itemsToBeLoaded; i++)
            {
                // read position
                position = dsr.ReadShort();                    

                // get item definition
                itemID = dsr.ReadUShort();
                ItemDefinition itmDef = ItemDefinitionCache.GetItemDefinitionByID(itemID);

                if (itmDef == null)
                    throw new DeserializationException("ItemStorage: itemDef for ID " + itemID + " not found");

                // create item
                Item itm = new Item(itmDef);
                itm.Quantity = dsr.ReadSInt();

                if (itm.Quantity < 0)
                    throw new DeserializationException("ItemStorage: quantity of item at position " + position + " less than 0");

                // all ok
                if (!SetItemAtPosition(position, itm))
                    throw new DeserializationException("ItemStorage: slot " + position + " not accessible");
            }
        }
        #endregion
    }
}