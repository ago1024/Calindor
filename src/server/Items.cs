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
	
        // TODO: Flags
	
    }

    public class ItemDefinitionFactory
    {
        public static Item GetItemByID(ushort itemID)
        {
            // TODO: Implement
            return null;
        }
    }

    public class Item
    {
        private ItemDefinition itemDef = null;

        private int quantity;

        public int Quantity
        {
            get { return quantity; }
            set { quantity = value; }
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

        private ItemStorage()
        {
        }

        public ItemStorage(int storageSize)
        {
            if (storageSize < 0)
                throw new ArgumentException("storageSize must be greater or equal 0");

            itemSlots = new Item[storageSize];
        }

        public virtual void Serialize(ISerializer sr)
        {
            // TODO: Implement
        }

        public virtual void Deserialize(IDeserializer dsr)
        {
            // TODO: Implement
        }
    }
}