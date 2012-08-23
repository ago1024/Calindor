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
using Calindor.Server.Actions;

namespace Calindor.Server.Resources
{
    public class HarvestableResourceDescriptor
    {
        private ItemDefinition harvestedItem;
        public ItemDefinition HarvestedItem
        {
            get { return harvestedItem; }
        }

        private ActionDescriptor performedAction;
        public ActionDescriptor PerformedAction
        {
            get { return performedAction; }
        }
	

        private int quantityPerHarvest;
        public int QuantityPerHarvest
        {
            get { return quantityPerHarvest; }
        }

        private HarvestableResourceDescriptor()
        {
        }

        public HarvestableResourceDescriptor(ItemDefinition itmDef, ActionDescriptor action, int quantity)
        {
            if (itmDef == null)
                throw new ArgumentNullException("itmDef");

            if (action == null)
                throw new ArgumentNullException("action");

            harvestedItem = itmDef;
            performedAction = action;
            quantityPerHarvest = quantity;
        }
	
    }
}