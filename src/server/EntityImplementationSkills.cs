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
        private int attackersCount = 0;
        public bool CombatIsAttacking
        {
            get 
            {
                if (executedTimeBasedAction != null)
                    return (executedTimeBasedAction is AttackTimeBasedAction);
                else
                    return false;
            }
        }
        
        public int CombatGetNumberOfAttackers()
        {
            return attackersCount; 
        }
        
        /// <summary>
        /// Selects one of the attackers and initiates attack with it
        /// </summary>
        public void CombatInitiateAttackOnAnyAttacker()
        {
            TimeBasedActionList selectedActions = new TimeBasedActionList();
            foreach(ITimeBasedAction action in affectingTimeBasedActions)
                if (action is AttackTimeBasedAction)
                    selectedActions.Add(action);
            
            if (selectedActions.Count == 0)
                return;
            
            int index = WorldRNG.Next(0, selectedActions.Count);
            
            AttackTimeBasedAction attackAction =
                (AttackTimeBasedAction)selectedActions[index];
            
            CombatInitiateAttack(attackAction.Attacker);
                 
        }
        
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
                SendLocalChatMessage("It's already dead...", PredefinedColor.Red2);
                return;
            }
            
            // Check distance
            if (!combatIsInDistanceToAttack(defender))
            {
                SendLocalChatMessage("You need to get closer to attack...", 
                                     PredefinedColor.Red2);
                return;
            }
            
            // Checks ok. Start attack for attacker.
            AttackTimeBasedAction attackDefender = 
                new AttackTimeBasedAction(this, defender);
            attackDefender.Activate();

            // Start attack for defender only if it is not attacking already
            if (!defender.CombatIsAttacking)
            {
                AttackTimeBasedAction attackAttacker = 
                    new AttackTimeBasedAction(defender, this);
                attackAttacker.Activate();
            }
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
        
        private ushort combatGetPenalizedDesenseSkillLevel(EntityImplementation defender)
        {
            ushort defenseSkillLevel = 
                defender.skills.GetSkill(EntitySkillType.DefenseDodge).CurrentLevel;
            
            // Change defense level based on attackers count
            double penalty = 1.0 / (double)defender.CombatGetNumberOfAttackers();
            penalty += 0.4;
            if (penalty > 1.0) penalty = 1.0;
            if (penalty < 0.5) penalty = 0.5; // No less than 50%
            defenseSkillLevel = (ushort)(defenseSkillLevel * penalty);
            
            return defenseSkillLevel;
        }
        
        public bool CombatGetHitChance(EntityImplementation defender, out double chance)
        {
            chance = 0.0;
            
            if (!combatIsInDistanceToAttack(defender))
                return false;
            
            // TODO: Use attack/defense descriptors in final implementation
            // TODO: Implement new rules for hitting
            
            
            
            int levelDiff = 
                skills.GetSkill(EntitySkillType.AttackUnarmed).CurrentLevel 
                    - combatGetPenalizedDesenseSkillLevel(defender);

            // 50% at equal levels
            double skillSuccessRate = 0.0;
            if (levelDiff < 0)
                skillSuccessRate = (10.0 + levelDiff) * 0.05; // Linear
            else
                skillSuccessRate = (20.0 + levelDiff) * 0.025; // Linear

           chance = skillSuccessRate;

            if (chance < 0.001)
                chance = 0.001; // Always a chance for lucky success 1/1000
            if (chance > 0.995)
                chance = 0.995; // Always a chance for failure 5/1000
            
            return true;
        }
        
        protected PredefinedActorCommand combatGetAnimationForAttack()
        {
            // TODO: Implement
            return PredefinedActorCommand.attack_up_1;
        }
        
        /// <summary>
        /// Invoked on attacker
        /// </summary>
        /// <param name="defender">
        /// A <see cref="EntityImplementation"/>
        /// </param>
        public void CombatHitDefender(EntityImplementation defender)
        {
            // Send attacker animation frame
            SendAnimationCommand(combatGetAnimationForAttack());            
            
            // If defender is not attacking, send 'pain' command
            if (!defender.CombatIsAttacking)
                defender.SendAnimationCommand(PredefinedActorCommand.pain1);
         
            // TODO: Use attack/defense descriptors in final implementation
            
            // Calculate damage
            int topDamageValue = 5 + skills.GetSkill(EntitySkillType.AttackUnarmed).CurrentLevel;
            defender.EnergiesUpdateHealth((short)(WorldRNG.Next(0,topDamageValue) * -1));
            
            // Award attack experience
            AttackActionDescriptor atckDescriptor = new AttackActionDescriptor(2000, 1000); //TODO: Time values are meaningless for now
            atckDescriptor.AddExperienceDescriptor(
                new ExperienceDescriptor(EntitySkillType.AttackUnarmed, 
                    combatGetPenalizedDesenseSkillLevel(defender), 10));
            
            SkillsAwardExperience(atckDescriptor);       
        }
        
        /// <summary>
        /// Invoked on defender
        /// </summary>
        /// <param name="attacker">
        /// A <see cref="EntityImplementation"/>
        /// </param>
        public void CombatEvadeAttacker(EntityImplementation attacker)
        {
            // Send attacker animation frame
            attacker.SendAnimationCommand(combatGetAnimationForAttack());
            
            // TODO: Use attack/defense descriptors in final implementation
            
            // Award defense experience
            DefendActionDescriptor defDescriptor = new DefendActionDescriptor(2000, 1000); //TODO: Time values are meaningless for now
            defDescriptor.AddExperienceDescriptor(
                new ExperienceDescriptor(
                    EntitySkillType.DefenseDodge,
                    attacker.skills.GetSkill(EntitySkillType.AttackUnarmed).CurrentLevel,
                    10));
            
            SkillsAwardExperience(defDescriptor);          
        }
        
        public uint CombatGetActionTime()
        {
            // TODO: Random for now, should be based on attributes / skills
            return (uint)(1500 + WorldRNG.Next(0,1500)); 
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