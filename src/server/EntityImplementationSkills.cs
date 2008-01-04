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
            HarvestTimeBasedAction harvest =
                new HarvestTimeBasedAction(this, rscDef);
            harvest.Activate();

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

        #region Combat Handling
        public void CombatInitiateAttack(EntityImplementation defender)
        {
            // Check if not me
            if (this == defender)
                return;

            // Check if not NPC
            if ((defender is ServerCharacter) && 
                ((defender as ServerCharacter).EntityImplementationKind == PredefinedEntityImplementationKind.ENTITY_NPC))
                return;

            // Check if entity is alive
            if (!defender.EnergiesIsAlive)
            {
                RawTextOutgoingMessage msgRawTextOut =
                    (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawTextOut.Color = PredefinedColor.Red2;
                msgRawTextOut.Text = "It's already dead...";
                PutMessageIntoMyQueue(msgRawTextOut);
                return;
            }
            
            // TODO: Check if it's not already fighting (remove in future when n<->n combat is available)
            

            // Check distance
            if (!combatIsInDistanceToAttack(defender))
            {
                RawTextOutgoingMessage msgRawTextOut =
                    (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawTextOut.Color = PredefinedColor.Red2;
                msgRawTextOut.Text = "You need to get closer to attack...";
                PutMessageIntoMyQueue(msgRawTextOut);
                return;
            }
            
            // Rotate to face defender
            // TODO: Should be moved to combat action
            LocationTurnToFace(defender.LocationX, defender.LocationY);
            defender.LocationTurnToFace(LocationX, LocationY);
            
            
            // Send animation frame
            // TODO: Only if not already in combat
            // TODO: Should be moved to combat action
            AddActorCommandOutgoingMessage msgAddActorCommandAttacker =
                (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
            msgAddActorCommandAttacker.EntityID = EntityID;
            msgAddActorCommandAttacker.Command = PredefinedActorCommand.enter_combat;
            PutMessageIntoMyAndObserversQueue(msgAddActorCommandAttacker);

            AddActorCommandOutgoingMessage msgAddActorCommandDefender =
                (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
            msgAddActorCommandDefender.EntityID = defender.EntityID;
            msgAddActorCommandDefender.Command = PredefinedActorCommand.enter_combat;
            defender.PutMessageIntoMyAndObserversQueue(msgAddActorCommandDefender);
            
            // Checks ok. Start combat
            AttackTimeBasedAction attackDefender = new AttackTimeBasedAction(this, defender);
            attackDefender.Activate();
            // TODO: Only if defender is not attacking anyone already
            AttackTimeBasedAction attackAttacker = new AttackTimeBasedAction(defender, this);
            attackAttacker.Activate();
        }
        
        private bool combatIsInDistanceToAttack(EntityImplementation defender)
        {
            double distance = Double.MaxValue;
            DistanceCalculationResult result = getDistanceToEntity(defender, out distance);

            if (result != DistanceCalculationResult.CALC_OK)
                return false;
            
            // TODO: allowed distance is based on weapon type
            if (distance > 3.0)
                return false;
            
            return true;
        }
        
        public bool CombatAttack(EntityImplementation defender)
        {
            if (!combatIsInDistanceToAttack(defender))
                return false;
            
            // Send animation frame
            AddActorCommandOutgoingMessage msgAddActorCommand =
                (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
            msgAddActorCommand.EntityID = EntityID;
            msgAddActorCommand.Command = PredefinedActorCommand.attack_up_1;
            PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
            
            
            // TODO: Implement
            
            int topDamageValue = (skills.GetSkill(EntitySkillType.AttackUnarmed).CurrentLevel + 1) * 5;
            defender.EnergiesUpdateHealth((short)(WorldRNG.Next(0,topDamageValue) * -1));
            AttackActionDescriptor atckDescriptor = new AttackActionDescriptor(2000, 1000); //TODO: Time values are meaningless for now
            atckDescriptor.AddExperienceDescriptor(new ExperienceDescriptor(EntitySkillType.AttackUnarmed, 2, 10));
            SkillsAwardExperience(atckDescriptor);
            
            return true;
        }
        
        public void CombatDefend()
        {
            // Send animation frame
            /*AddActorCommandOutgoingMessage msgAddActorCommand =
                (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
            msgAddActorCommand.EntityID = EntityID;
            msgAddActorCommand.Command = PredefinedActorCommand.pain1;
            PutMessageIntoMyAndObserversQueue(msgAddActorCommand);*/

            // TODO: Implement
            DefendActionDescriptor defDescriptor = new DefendActionDescriptor(2000, 1000); //TODO: Time values are meaningless for now
            defDescriptor.AddExperienceDescriptor(new ExperienceDescriptor(EntitySkillType.DefenseDodge, 2, 10));
            SkillsAwardExperience(defDescriptor);
        }
        
        public void CombatStopFighting()
        {
            //TODO: Check if this is a last fight performed, if yes, 
            //      send animation frame
            AddActorCommandOutgoingMessage msgAddActorCommandAttacker =
                (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
            msgAddActorCommandAttacker.EntityID = EntityID;
            msgAddActorCommandAttacker.Command = PredefinedActorCommand.leave_combat;
            PutMessageIntoMyAndObserversQueue(msgAddActorCommandAttacker);
            
            RawTextOutgoingMessage msgRawText =
                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
            msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
            msgRawText.Color = PredefinedColor.Blue1;
            msgRawText.Text = "You stopped fighting.";
            PutMessageIntoMyQueue(msgRawText);
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