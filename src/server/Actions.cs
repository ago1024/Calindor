/*
 * Copyright (C) 2007 Krzysztof 'DeadwooD' Smiechowicz
 * Original project page: http://sourceforge.net/projects/calindor/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 */
using System.Collections.Generic;
using Calindor.Server.Entities;
using System;

namespace Calindor.Server.Actions
{
    public abstract class ActionDescriptor
    {
        private uint baseTime = 0;
        public uint BaseTime
        {
            get { return baseTime; }
        }

        private uint minTime = 0;
        public uint MinTime
        {
            get { return minTime; }
        }

        private ExperienceDescriptorList experienceDescriptors = new ExperienceDescriptorList();
        public ExperienceDescriptorList ExperienceDescriptors
        {
            get { return experienceDescriptors; }
        }

        private ActionDescriptor()
        {

        }

        public ActionDescriptor(uint baseTime, uint minTime)
        {
            this.baseTime = baseTime;
            this.minTime = minTime;
        }

        public void AddExperienceDescriptor(ExperienceDescriptor xpDesc)
        { 
            // TODO: Check if the skill is not already on the list (?)
            experienceDescriptors.Add(xpDesc);
        }

    }

    public class ExperienceDescriptor
    {
        private EntitySkillType skill = EntitySkillType.Undefined;
        public EntitySkillType Skill
        {
            get { return skill; }
        }

        private ushort baseLevel = 0;
        public ushort BaseLevel
        {
            get { return baseLevel; }
        }

        private uint baseExperience = 0;
        public uint BaseExperience
        {
            get { return baseExperience; }
        }
        
        private  ExperienceDescriptor()
        {

        }

        public ExperienceDescriptor(EntitySkillType skill, ushort baseLevel, uint baseExperience)
        {
            this.skill = skill;
            this.baseLevel = baseLevel;
            this.baseExperience = baseExperience;
        }
     }

    public class ExperienceDescriptorList : List<ExperienceDescriptor>
    {
    }



    public class HarvestActionDescriptor : ActionDescriptor
    {
        public HarvestActionDescriptor(uint baseTime, uint minTime) : base(baseTime, minTime)
        {
            // baseTime is based on resource definition
            // baseLevel is based on resource definition
            // baseExperience is based on resource definition
        }
    }
    
    // TODO: Use at later stage when more than one combat skill will be available
    public class AttackActionDescriptor : ActionDescriptor
    {
        public AttackActionDescriptor(uint baseTime, uint minTime) : base(baseTime, minTime)
        {
            // baseTime is based on weapon type (?)
            // baseLevel is based on weapon type (?)
            // baseExperience is based on weapon type (?)
        }
    }

    // TODO: Use at later stage when more than one combat skill will be available
    public class DefendActionDescriptor : ActionDescriptor
    {
        public DefendActionDescriptor(uint baseTime, uint minTime) : base(baseTime, minTime)
        {
            // baseTime is based on (?)
            // baseLevel is based on (?)
            // baseExperience is based (?)
        }
    }

}