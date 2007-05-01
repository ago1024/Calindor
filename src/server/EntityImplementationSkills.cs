/*
 * Copyright (C) 2007 Krzysztof 'DeadwooD' Smiechowicz
 * Original project page: http://sourceforge.net/projects/calindor/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 */

using Calindor.Server.Entities;
using Calindor.Server.Resources;
using Calindor.Server.Messaging;
using Calindor.Server.TimeBasedActions;
using Calindor.Server.Items;
using Calindor.Misc.Predefines;

namespace Calindor.Server
{
    public abstract partial class EntityImplementation : Entity
    {
        #region Harvest Skills
        public void HarvestStart(HarvestableResourceDefinition rscDef)
        {
            timeBasedActionsManager.AddAction(
                new HarvestTimeBasedAction(this, rscDef));

            RawTextOutgoingMessage msgRawText =
                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
            msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
            msgRawText.Color = PredefinedColor.Blue1;
            msgRawText.Text = "You started harvesting " + rscDef.HarvestedItem.Name;
            PutMessageIntoMyQueue(msgRawText);
        }

        public double HarvestGetSuccessRate(HarvestableResourceDefinition rscDef)
        {
            // TODO: Implement
            return 0.5;
        }

        public uint HarvestGetActionTime(HarvestableResourceDefinition rscDef)
        {
            // TODO: Implement
            return 2000;
        }

        public void HarvestItemHarvested(HarvestableResourceDefinition rscDef)
        {
            // TODO: Implement
            Item itm = new Item(rscDef.HarvestedItem);
            itm.Quantity = rscDef.QuantityPerHarvest;
            InventoryUpdateItem(itm);
            SkillsAwardExperience(rscDef.BaseHarvestLevel);
        }

        #endregion

        #region General Skills
        public void SkillsAwardExperience(ushort baseLevel)
        {
            // TODO: Implement
            RawTextOutgoingMessage msgRawText =
                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
            msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
            msgRawText.Color = PredefinedColor.Grey1;
            msgRawText.Text = "Plants Harvesting: +200xp";
            PutMessageIntoMyQueue(msgRawText);
        }
        
        #endregion
    }
}