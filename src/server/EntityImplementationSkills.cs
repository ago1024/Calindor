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

            // Add success rate comment
            double successRate = HarvestGetSuccessRate(rscDef);
            if (successRate < 0.5)
            {
                if (successRate < 0.25)
                {
                    msgRawText.Text += " and you feel you will need a lot of luck.";
                }
                else
                {
                    msgRawText.Text += " and you feel you will need some luck.";
                }
            }
            else
                msgRawText.Text += " and you feel confident about it.";

            PutMessageIntoMyQueue(msgRawText);
        }

        public double HarvestGetSuccessRate(HarvestableResourceDescriptor rscDef)
        {
            double _return = 0.0;
            int skillsUsed = 0;
            double skillSuccessRate = 0.0;

            ActionDescriptor actDef = rscDef.PerformedAction;
            foreach (ExperienceDescriptor xpDesc in actDef.ExperienceDescriptors)
            {
                skillsUsed++;
                int levelDiff = skills.GetCurrentLevelDifference(xpDesc.Skill, xpDesc.BaseLevel);

                if (levelDiff < 0)
                    skillSuccessRate = (10.0 + levelDiff) * 0.05; // Linear
                else
                    skillSuccessRate = (20.0 + levelDiff) * 0.025; // Linear

                _return += skillSuccessRate;
            }

            _return /= skillsUsed;

            if (_return < 0.001)
                _return = 0.001; // Always a chance for lucky success 1/1000
            if (_return > 0.995)
                _return = 0.995; // Always a chance for failure 5/1000

           // RawTextOutgoingMessage msgRawText =
           //(RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
           // msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
           // msgRawText.Color = PredefinedColor.Grey1;
           // msgRawText.Text = "Chance: " + _return;
           // PutMessageIntoMyQueue(msgRawText);

            return _return;
        }

        public uint HarvestGetActionTime(HarvestableResourceDescriptor rscDef)
        {
            ActionDescriptor actDef = rscDef.PerformedAction;

            int _return = 0;
            int skillsUsed = 0;
            int skillTime = 0;

            foreach (ExperienceDescriptor xpDesc in actDef.ExperienceDescriptors)
            {
                skillsUsed++;
                int levelDiff = skills.GetCurrentLevelDifference(xpDesc.Skill, xpDesc.BaseLevel);

                if (levelDiff < 0)
                    skillTime = (int)((-levelDiff) * (actDef.BaseTime / 10)); // Linear
                else
                    skillTime = (int)((-levelDiff) * (actDef.BaseTime / 20)); // Linear

                _return += (int)(actDef.BaseTime + skillTime);
            }

            _return /= skillsUsed;

            if (_return < actDef.MinTime)
                _return = (int)actDef.MinTime; // Can't work faster than min time

           // RawTextOutgoingMessage msgRawText =
           //(RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
           // msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
           // msgRawText.Color = PredefinedColor.Grey1;
           // msgRawText.Text = "Time: " + _return;
           // PutMessageIntoMyQueue(msgRawText);

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
                xpAwarded = (int)xpDesc.BaseExperience;

                if (levelDiff < 0)
                {
                    // Unlimited if below base level
                    xpAwarded += (int)(xpAwarded * 0.05 * -levelDiff); // Linear
                }
                else
                {
                    // Get only for the next 10 levels
                    xpAwarded -= (int)(xpAwarded * levelDiff / 10.0); // Linear
                }
                                
                if (xpAwarded > 0)
                {
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