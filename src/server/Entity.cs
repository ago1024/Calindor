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

namespace Calindor.Server.Entities
{
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

        protected EntityAppearance appearance = new EntityAppearance();
        public EntityAppearance Appearance
        {
            get { return appearance; }
            set { appearance = value; }
        }

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
                        if ((Math.Abs(location.X - testedEntity.location.X) < 30) &&
                            (Math.Abs(location.Y - testedEntity.location.Y) < 30))
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
    }

    public class EntityIDEntityDictionary : Dictionary<UInt16, Entity>
    {
    }

    public class EntityList : List<Entity>
    {
    }
}
