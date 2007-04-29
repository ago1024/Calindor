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
using Calindor.Server.Items;

namespace Calindor.Server.Resources
{
    public class HarvestableResourceDefinition
    {
        private ItemDefinition harvestedItem;
        public ItemDefinition HarvestedItem
        {
            get { return harvestedItem; }
        }

        private ushort baseHarvestLevel;
        public ushort BaseHarvestLevel
        {
            get { return baseHarvestLevel; }
        }

        private uint baseHarvestTime;
        public uint BaseHarvestTime
        {
            get { return baseHarvestTime; }
        }

        private int quantityPerHarvest;
        public int QuantityPerHarvest
        {
            get { return quantityPerHarvest; }
        }

        private HarvestableResourceDefinition()
        {
        }

        public HarvestableResourceDefinition(ItemDefinition itmDef, ushort level, uint time, int quantity)
        {
            if (itmDef == null)
                throw new ArgumentNullException("itmDef");

            harvestedItem = itmDef;
            baseHarvestLevel = level;
            baseHarvestTime = time;
            quantityPerHarvest = quantity;
        }
	
    }
}