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
using System.Collections.Generic;
using System.Text;
using Calindor.Server.Maps;
using Calindor.Server.Items;
using Calindor.Server.Serialization;
using Calindor.Misc.Predefines;

namespace Calindor.Server.Entities
{
    #region Entity
    /// <summary>
    /// Represents a single, living(or undead :P) entity in the game
    /// </summary>
    public abstract class Entity
    {
        protected string name = "";
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        protected UInt16 entityID = 0;
        public UInt16 EntityID
        {
            get { return entityID; }
            set { entityID = value; }
        }

        #region NOT_USED
        /*protected BasicAttributes basicAttributesCurrent = new BasicAttributes();
        public BasicAttributes BasicAttributesCurrent
        {
            get { return basicAttributesCurrent; }
        }

        protected BasicAttributes basicAttributesBase = new BasicAttributes();
        public BasicAttributes BasicAttributesBase
        {
            get { return basicAttributesBase; }
        }

        protected CrossAttributes crossAttributesCurrent = new CrossAttributes();
        public CrossAttributes CrossAttributesCurrent
        {
            get { return crossAttributesCurrent; }
        }

        protected CrossAttributes crossAttributesBase = new CrossAttributes();
        public CrossAttributes CrossAttributesBase
        {
            get { return crossAttributesBase; }
        }

        protected Nexuses nexusesCurrent = new Nexuses();
        public Nexuses NexusesCurrent
        {
            get { return nexusesCurrent; }
        }

        protected Nexuses nexusesBase = new Nexuses();
        public Nexuses NexusesBase
        {
            get { return nexusesBase; }
        }

        protected Skills skillsCurrent = new Skills();
        public Skills SkillsCurrent
        {
            get { return skillsCurrent; }
        }

        protected Skills skillsBase = new Skills();
        public Skills SkillsBase
        {
            get { return skillsBase; }
        }

        protected MiscAttributes miscAttributesCurrent = new MiscAttributes();
        public MiscAttributes MiscAttributesCurrent
        {
            get { return miscAttributesCurrent; }
        }

        protected MiscAttributes miscAttributesBase = new MiscAttributes();
        public MiscAttributes MiscAttributesBase
        {
            get { return miscAttributesBase; }
        }

        protected sbyte foodLevel = 0;
        public sbyte FoodLevel
        {
            get { return foodLevel; }
            set { foodLevel = value; }
        }

        protected short pickPoints = 0;
        public short PickPoints
        {
            get { return pickPoints; }
            set { pickPoints = value; }
        }*/
        #endregion

        #region Appearance
        protected EntityAppearance appearance = new EntityAppearance(PredefinedModelType.HUMAN_FEMALE);
        #endregion

        #region Location
        protected EntityLocation location = new EntityLocation();
        public short LocationX
        {
            get { return location.X; }
        }
        public short LocationY
        {
            get { return location.Y; }
        }
        public Map LocationCurrentMap
        {
            get { return location.CurrentMap; }
        }
        protected double getDistanceToEntity(Entity en)
        {
            if (location.CurrentMap != en.location.CurrentMap)
                return Double.MaxValue;

            return Math.Sqrt(((location.X - en.location.X) * (location.X - en.location.X)) +
                ((location.Y - en.location.Y) * (location.Y - en.location.Y)));
        }
        #endregion

        #region Inventory
        protected EntityInventory inventory = new EntityInventory();
        #endregion

        #region Visibility

        // Entities visibility
        protected EntityList entitiesVisibleNow = new EntityList();
        protected EntityList entitiesVisiblePrev = new EntityList();
        protected EntityList entitiesVisibleRemoved = new EntityList();
        protected EntityList entitiesVisibleAdded = new EntityList();
        // Entities to whom this entity is visible
        protected EntityList entitiesObservers = new EntityList();

        public void ResetVisibleEntities()
        {
            entitiesVisiblePrev.Clear();
            entitiesObservers.Clear();
            EntityList temp = entitiesVisiblePrev;
            entitiesVisiblePrev = entitiesVisibleNow;
            entitiesVisibleNow = temp;
        }

        public void UpdateVisibleEntities()
        {
            entitiesVisibleNow.Clear();

            if (location.CurrentMap != null)
            {
                IEnumerator<Entity> enumEntities = location.CurrentMap.EntitiesOnMap;

                enumEntities.Reset();

                while (enumEntities.MoveNext())
                {
                    Entity testedEntity = enumEntities.Current;

                    if (testedEntity != this)
                    {

                        // TODO: For now just a simple condition, change for future
                        if (getDistanceToEntity(testedEntity) < 30.0)
                        {
                            addVisibleEntity(testedEntity); // I can see you
                            testedEntity.addObserverEntity(this); // You know that I can see you
                        }
                    }
                }
            }
        }

        private void addVisibleEntity(Entity en)
        {
            entitiesVisibleNow.Add(en);
        }

        protected void calculateRemovedVisibleEntities()
        {
            entitiesVisibleRemoved.Clear();

            foreach (Entity en in entitiesVisiblePrev)
                if (!entitiesVisibleNow.Contains(en))
                    entitiesVisibleRemoved.Add(en);

        }

        protected void calculateAddedVisibleEntities()
        {
            entitiesVisibleAdded.Clear();

            foreach (Entity en in entitiesVisibleNow)
                if (!entitiesVisiblePrev.Contains(en))
                    entitiesVisibleAdded.Add(en);

        }
         
        private void addObserverEntity(Entity en)
        {
            if (!entitiesObservers.Contains(en))
                entitiesObservers.Add(en);
        }
        #endregion

        #region Skills
        protected EntitySkills skills = new EntitySkills();
        #endregion

        #region Energies
        protected EntityEnergies energies = new EntityEnergies();
        #endregion

        #region Attributes
        // TODO
        #endregion

        #region Perks
        // TODO
        #endregion
    }

    public class EntityIDEntityDictionary : Dictionary<UInt16, Entity>
    {
    }

    public class EntityList : List<Entity>
    {
    }
    #endregion

    #region Entity Appearance
    public class EntityAppearance
    {
        private byte[] innerData = new byte[7];

        public PredefinedModelHead Head
        {
            get { return (PredefinedModelHead)innerData[6]; }
            set { innerData[6] = (byte)value; }
        }

        public PredefinedModelType Type
        {
            get { return (PredefinedModelType)innerData[5]; }
        }

        public PredefinedModelSkin Skin
        {
            get { return (PredefinedModelSkin)innerData[0]; }
            set { innerData[0] = (byte)value; }
        }

        public PredefinedModelHair Hair
        {
            get { return (PredefinedModelHair)innerData[1]; }
            set { innerData[1] = (byte)value; }
        }

        public PredefinedModelShirt Shirt
        {
            get { return (PredefinedModelShirt)innerData[2]; }
            set { innerData[2] = (byte)value; }
        }

        public PredefinedModelPants Pants
        {
            get { return (PredefinedModelPants)innerData[3]; }
            set { innerData[3] = (byte)value; }
        }

        public PredefinedModelBoots Boots
        {
            get { return (PredefinedModelBoots)innerData[4]; }
            set { innerData[4] = (byte)value; }
        }

        public virtual void Serialize(ISerializer sr)
        {
            for (int i = 0; i < innerData.Length; i++)
                sr.WriteValue(innerData[i]);
        }

        public virtual void Deserialize(IDeserializer dsr)
        {
            for (int i = 0; i < innerData.Length; i++)
                innerData[i] = dsr.ReadByte();
        }

        private EntityAppearance()
        {
        }

        public EntityAppearance(PredefinedModelType type)
        {
            innerData[5] = (byte)type;
        }

        public bool IsEnhancedModel
        {
            get
            {
                if ((Type == PredefinedModelType.HUMAN_FEMALE) ||
                    (Type == PredefinedModelType.HUMAN_MALE) ||
                    (Type == PredefinedModelType.ELF_FEMALE) ||
                    (Type == PredefinedModelType.ELF_MALE) ||
                    (Type == PredefinedModelType.DWARF_FEMALE) ||
                    (Type == PredefinedModelType.DWARF_MALE) ||
                    (Type == PredefinedModelType.DRAEGONI_FEMALE) ||
                    (Type == PredefinedModelType.DRAEGONI_MALE) ||
                    (Type == PredefinedModelType.GNOME_FEMALE) ||
                    (Type == PredefinedModelType.GNOME_MALE) ||
                    (Type == PredefinedModelType.ORCHAN_FEMALE) ||
                    (Type == PredefinedModelType.ORCHAN_MALE))
                    return true;
                else
                    return false;
            }
        }
    }
    #endregion

    #region Entity Location
    public class EntityLocation
    {
        private short[] innerData = new short[5];

        public short X
        {
            get { return innerData[0]; }
            set { innerData[0] = value; }
        }

        public short Y
        {
            get { return innerData[1]; }
            set { innerData[1] = value; }
        }

        public short Z
        {
            get { return innerData[2]; }
            set { innerData[2] = value; }
        }

        public short Rotation
        {
            get { return innerData[3]; }
            set { innerData[3] = value; }
        }

        public bool IsSittingDown
        {
            get { if (innerData[4] == 1) return true; else return false; }
            set { if (value) innerData[4] = 1; else innerData[4] = 0; }
        }

        /// <summary>
        /// Name of the map deserialized from file
        /// </summary>
        private string loadedMapName;

        /// <summary>
        /// Should only be used at login time
        /// </summary>
        public string LoadedMapMame
        {
            get { return loadedMapName; }
        }

        public string CurrentMapName
        {
            get
            {
                if (CurrentMap == null)
                    return "__NULL__";
                else
                    return CurrentMap.Name;
            }
        }

        private Map currentMap = null;
        public Map CurrentMap
        {
            get { return currentMap; }
            set { currentMap = value; }
        }

        public virtual void Serialize(ISerializer sr)
        {
            for (int i = 0; i < innerData.Length; i++)
                sr.WriteValue(innerData[i]);
            sr.WriteValue(CurrentMapName);
        }

        public virtual void Deserialize(IDeserializer dsr)
        {
            for (int i = 0; i < innerData.Length; i++)
                innerData[i] = dsr.ReadShort();
            loadedMapName = dsr.ReadString();
        }

        public void RatateBy(short additionalRotation)
        {
            innerData[3] = (short)((int)(innerData[3] + additionalRotation) % 360);
            if (innerData[3] < 0)
                innerData[3] += 360;
        }
    }
    #endregion

    #region Entity Inventory
    public class EntityInventory : ItemStorage
    {
        public EntityInventory()
            : base(36)
        {
        }
    }
    #endregion

    #region Entity Skills
    public enum EntitySkillType
    {
        Undefined = 0,
        HarvestingPlants = 1,
        AttackUnarmed = 2,
        DefenseDodge = 3,
        HarvestingMinerals = 4,
        HarvestingOres = 5
    }

    public class EntitySkill
    {
        // TODO: Should be loaded from script and stored at different location
        private static uint[] levels = new uint[200];
        static EntitySkill()
        {
            levels[0] = 400;
            levels[1] = (uint)(levels[0] * 2.1);
            levels[2] = (uint)(levels[1] * 1.6);
            levels[3] = (uint)(levels[2] * 1.5);
            for (int i = 4; i < 10; i++)
                levels[i] = (uint)(levels[i - 1] * 1.38);

            for (int i = 10; i < 25; i++)
                levels[i] = (uint)(levels[i - 1] * 1.22);

            for (int i = 25; i < 50; i++)
                levels[i] = (uint)(levels[i - 1] * 1.135);

            for (int i = 50; i < 100; i++)
                levels[i] = (uint)(levels[i - 1] * 1.0475);

            for (int i = 100; i < 200; i++)
                levels[i] = (uint)(levels[i - 1] * 1.03);

            return;
        }






        private EntitySkillType type = EntitySkillType.Undefined;
        public EntitySkillType Type
        {
            get { return type; }
        }

        public ushort CurrentLevel
        {
            get { return BaseLevel; } // TODO + modifier applied
        }

        private uint nextLevelXP = 0; // not serialized
        public uint NextLevelXP
        {
            get { return nextLevelXP; }
        }

        private uint xp = 0; // serialized
        public uint XP
        {
            get { return xp; }
        }

        private ushort precalculatedBaseLevel = 0; // not serialized
        public ushort BaseLevel
        {
            get { return precalculatedBaseLevel; } 
        }

        private string name = "Undefined";
        public string Name
        {
            get { return name; }
        }
	

        private EntitySkill()
        {

        }

        public void AddXP(uint xpToAdd)
        {
            // Add XP
            xp += xpToAdd;

            // Precalculate base level
            if (XP >= nextLevelXP)
                precalculateBaseLevel();

        }

        public void ZeroXP()
        {
            xp = 0;
            precalculateBaseLevel();
        }

        private void precalculateBaseLevel()
        {
            // TODO: Experience level model should be loaded from scripts (?)
            for (int i = 0; i < levels.GetLength(0); i++)
            {
                if (xp < levels[i])
                {
                    precalculatedBaseLevel = (ushort)i;
                    nextLevelXP = levels[i];
                    break;
                }
            }
        }

        public EntitySkill(EntitySkillType type, string name)
        {
            this.type = type;
            this.name = name;
            ZeroXP();
        }
    }

    public class EntitySkillList : List<EntitySkill>
    {
    }

    public class EntitySkillNameComparer : IComparer<EntitySkill>
    {
        #region IComparer<EntitySkill> Members

        public int Compare(EntitySkill x, EntitySkill y)
        {
            return x.Name.CompareTo(y.Name);
        }

        #endregion
    }

    public class EntitySkills
    {
        private EntitySkill[] innerData = new EntitySkill[6];
        public int Count
        {
            get { return innerData.GetLength(0); }
        }

        public EntitySkills()
        {
            addSkill(EntitySkillType.Undefined, "Undefined");
            addSkill(EntitySkillType.HarvestingPlants, "Harvesting(Plants)");
            addSkill(EntitySkillType.AttackUnarmed, "Attack(Unarmed)");
            addSkill(EntitySkillType.DefenseDodge, "Defense(Dodge)");
            addSkill(EntitySkillType.HarvestingMinerals, "Harvesting(Minerals)");
            addSkill(EntitySkillType.HarvestingOres, "Harvesting(Ores)");
        }

        private void addSkill(EntitySkillType type, string name)
        {
            EntitySkill skill = new EntitySkill(type, name);
            setSkill(skill);
        }

        private void setSkill(EntitySkill skill)
        {
            innerData[(int)skill.Type] = skill;
        }

        public EntitySkill GetSkill(EntitySkillType type)
        {
            return innerData[(int)type];
        }

        public int GetCurrentLevelDifference(EntitySkillType type, ushort level)
        {
            return (GetSkill(type).CurrentLevel - level);
        }

        public void Clear()
        {
            for (int i = 0; i < innerData.GetLength(0); i++)
                innerData[i].ZeroXP();
        }

        public void AwardXPToSkill(EntitySkillType type, uint xp)
        {
            GetSkill(type).AddXP(xp);
        }

        public virtual void Serialize(ISerializer sr)
        {
            for (int i = 0; i < innerData.GetLength(0); i++)
            {
                EntitySkill skill = innerData[i];
                sr.WriteValue((byte)skill.Type);
                sr.WriteValue(skill.XP);
            }
                
        }

        public virtual void Deserialize(IDeserializer dsr)
        {
            Clear();

            for (int i = 0; i < innerData.GetLength(0); i++)
            {
                EntitySkill skill = GetSkill((EntitySkillType)dsr.ReadByte()); // Skill created in ctor
                skill.AddXP(dsr.ReadUInt());
            }
        }
    }
    #endregion

    #region Entity Energies
    public class EntityEnergies
    {
        private short maxHealth = 50; // TODO: Calculate based on attributes, items, etc. Not serializable

        public short MaxHealth
        {
            get { return maxHealth; }
        }

        private short currentHealth;

        public short CurrentHealth
        {
            get { return currentHealth; }
        }
	
	

        public void Serialize(ISerializer sr)
        {
            sr.WriteValue(currentHealth);
        }

        public void Deserialize(IDeserializer dsr)
        {
            currentHealth = dsr.ReadShort();
            if (currentHealth > maxHealth)
                throw new DeserializationException("CurrentHealth greater than MaxHealth");
        }

        private void checkCurrentHealthIsInBounds()
        {
            if (currentHealth > maxHealth)
                currentHealth = maxHealth;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="updValue"></param>
        /// <returns>Actual change value</returns>
        public short UpdateCurrentHealth(short updValue)
        {
            short _return = currentHealth;

            currentHealth += updValue;

            checkCurrentHealthIsInBounds();

            _return = (short)(currentHealth - _return);

            return _return;
        }

        public void SetMaxHealth(short newValue)
        {
            if (newValue < 0)
                throw new ArgumentException("New value cannot be less than 0");
            maxHealth = newValue;
            checkCurrentHealthIsInBounds();
        }

        public short GetHealthDifference()
        {
            return (short)(maxHealth - currentHealth);
        }
    }
    #endregion
}
