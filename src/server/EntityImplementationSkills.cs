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
using Calindor.Server.Actions;
using System;
using System.Collections.Generic;

namespace Calindor.Server
{
    public abstract partial class EntityImplementation : Entity
    {
        #region Harvest Skills
        public void HarvestStart(HarvestableResourceDescriptor rscDef)
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

        public double HarvestGetSuccessRate(HarvestableResourceDescriptor rscDef)
        {
            // TODO: Implement
            double _return = 0.5; // Chance at base level

            ActionDescriptor actDef = rscDef.PerformedAction;
            foreach (ExperienceDescriptor xpDesc in actDef.ExperienceDescriptors)
            {
                int levelDiff = skills.GetCurrentLevelDifference(xpDesc.Skill, xpDesc.BaseLevel);
                _return += levelDiff * 0.05;
            }

            if (_return < 0.01)
                _return = 0.01; // Always a chance for lucky success 1/100
            if (_return > 0.99)
                _return = 0.99; // Always a chance for failure = 1/100

            return _return;
        }

        public uint HarvestGetActionTime(HarvestableResourceDescriptor rscDef)
        {
            // TODO: Implement

            ActionDescriptor actDef = rscDef.PerformedAction;
            int _return = (int)actDef.BaseTime;

            foreach (ExperienceDescriptor xpDesc in actDef.ExperienceDescriptors)
            {
                int levelDiff = skills.GetCurrentLevelDifference(xpDesc.Skill, xpDesc.BaseLevel);
                _return -= levelDiff * 100;
            }

            if (_return < actDef.MinTime)
                _return = (int)actDef.MinTime; // Can't work faster than min time

            if (_return < 0)
                return 0;
            else
                return (uint)_return;
        }

        public void HarvestItemHarvested(HarvestableResourceDescriptor rscDef)
        {
            Item itm = new Item(rscDef.HarvestedItem);
            itm.Quantity = rscDef.QuantityPerHarvest;
            InventoryUpdateItem(itm);
            SkillsAwardExperience(rscDef.PerformedAction);
        }

        #endregion

        #region General Skills
        public void SkillsAwardExperience(ActionDescriptor actDesc)
        {
            int xpAwarded = 0;
            ushort baseLevel = 0;
            EntitySkill skill = null;

            foreach (ExperienceDescriptor xpDesc in actDesc.ExperienceDescriptors)
            {
                skill = skills.GetSkill(xpDesc.Skill);

                int levelDiff = skills.GetCurrentLevelDifference(xpDesc.Skill, xpDesc.BaseLevel);

                // TODO: Modify rule
                xpAwarded = (int)xpDesc.BaseExperience;
                xpAwarded -= levelDiff * (int)xpDesc.BaseExperience / 20;
                
                if (xpAwarded < 0)
                    xpAwarded = 0;

                baseLevel = skill.BaseLevel;
                skills.AwardXPToSkill(xpDesc.Skill, (uint)xpAwarded);

                RawTextOutgoingMessage msgRawText =
                    (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawText.Color = PredefinedColor.Grey1;
                msgRawText.Text = skill.Name + ": +" + xpAwarded + "xp";
                PutMessageIntoMyQueue(msgRawText);

                if (skill.BaseLevel != baseLevel)
                {
                    RawTextOutgoingMessage msgRawTextLevel =
                        (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                    msgRawTextLevel.Channel = PredefinedChannel.CHAT_LOCAL;
                    msgRawTextLevel.Color = PredefinedColor.Blue1;
                    msgRawTextLevel.Text = skill.Name + ": Level " + skill.BaseLevel;
                    PutMessageIntoMyQueue(msgRawTextLevel);
                }
            }

            
        }

        public void SkillsListSkills()
        {
            // Skill == 0 -> not defined

            // Sort by name
            EntitySkillList sortList = new EntitySkillList();
            for (int i = 1; i < skills.Count; i++)
                sortList.Add(skills.GetSkill((EntitySkillType)i));

            sortList.Sort(new EntitySkillNameComparer());
            
            // Send messages            
            foreach(EntitySkill skill in sortList)
            {
                RawTextOutgoingMessage msgRawText =
                    (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawText.Color = PredefinedColor.Grey1;
                msgRawText.Text = skill.Name + ": (" + skill.CurrentLevel + "/" + skill.BaseLevel +
                    ")(" + skill.XP + "/" + skill.NextLevelXP + ")";
                PutMessageIntoMyQueue(msgRawText);
            }
        }
        #endregion
    }
}