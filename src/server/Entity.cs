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
using Calindor.Server.TimeBasedActions;
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

        protected EntityLocation location = new EntityLocation();
        public EntityLocation Location
        {
            get { return location; }
            set { location = value; }
        }

        protected EntityInventory inventory = new EntityInventory();

        // Time based action
        // TODO: Probably an entity might have more than one time based action
        protected ITimeBasedAction tbAction = null;

        public void SetTimeBasedAction(ITimeBasedAction actionToSet)
        {
            CancelCurrentTimeBasedAction();

            tbAction = actionToSet;
        }

        public void CancelCurrentTimeBasedAction()
        {
            if (tbAction != null)
            {
                tbAction.Cancel();
                tbAction = null;
            }
        }

        #region Visibility

        // Entities visibility
        protected EntityList entitiesVisibleNow = new EntityList();
        protected EntityList entitiesVisiblePrev = new EntityList();
        private EntityList entitiesVisibleRemoved = new EntityList();
        private EntityList entitiesVisibleAdded = new EntityList();
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

            if (Location.CurrentMap != null)
            {
                IEnumerator<Entity> enumEntities = Location.CurrentMap.EntitiesOnMap;

                enumEntities.Reset();

                while (enumEntities.MoveNext())
                {
                    Entity testedEntity = enumEntities.Current;

                    if (testedEntity != this)
                    {

                        // TODO: For now just a simple condition, change for future
                        if ((Math.Abs(Location.X - testedEntity.Location.X) < 30) &&
                            (Math.Abs(Location.Y - testedEntity.Location.Y) < 30))
                        {
                            entitiesVisibleNow.Add(testedEntity); // I can see you
                            testedEntity.AddObserverEntity(this); // You know that I can see you
                        }
                    }
                }
            }
        }

        public void AddVisibleEntity(Entity en)
        {
            entitiesVisibleNow.Add(en);
        }

        public EntityList GetRemovedVisibleEntities()
        {
            entitiesVisibleRemoved.Clear();

            foreach (Entity en in entitiesVisiblePrev)
                if (!entitiesVisibleNow.Contains(en))
                    entitiesVisibleRemoved.Add(en);

            return entitiesVisibleRemoved;
        }

        public EntityList GetAddedVisibleEntities()
        {
            entitiesVisibleAdded.Clear();

            foreach (Entity en in entitiesVisibleNow)
                if (!entitiesVisiblePrev.Contains(en))
                    entitiesVisibleAdded.Add(en);

            return entitiesVisibleAdded;
        }

        public void AddObserverEntity(Entity en)
        {
            if (!entitiesObservers.Contains(en))
                entitiesObservers.Add(en);
        }
        #endregion

        #region Following
        private EntityList followers = new EntityList();
        protected Entity entityToFollow = null;
        public bool IsFollowedByEntities
        {
            get { return followers.Count > 0; }
        }

        public bool FollowsEntity
        {
            get { return entityToFollow != null; }
        }

        private bool addFollower(Entity follower)
        {
            if (follower.IsFollowedByEntities)
                return false;

            if (!followers.Contains(follower))
                followers.Add(follower);

            return true;
        }
        
        private void removeFollower(Entity follower)
        {
            if (followers.Contains(follower))
                followers.Remove(follower);
        }

        public bool Follow(Entity entityToFollow)
        {
            if (IsFollowedByEntities || entityToFollow.FollowsEntity)
                return false;

            StopFollowing();

            if (entityToFollow.addFollower(this))
            {
                this.entityToFollow = entityToFollow;
                CancelCurrentTimeBasedAction();
                return true;
            }
            else
            {
                return false;
            }
            
        }

        public void StopFollowing()
        {
            if (entityToFollow != null)
            {
                entityToFollow.removeFollower(this);
                entityToFollow = null;
                CancelCurrentTimeBasedAction();
            }
        }

        public void ReleaseFollowers()
        {
            EntityList tempFollowers = new EntityList();
            
            tempFollowers.AddRange(followers);

            foreach (Entity en in tempFollowers)
                en.StopFollowing();
        }

        /// <summary>
        /// Checks if entity should still follow, and if not, stop following
        /// </summary>
        public void CheckForStopFollowing()
        {
            if (entityToFollow == null)
                return;

            // TODO: Move the unlink checks into external class implementing a check interface.
            if (this.entityToFollow.Location.CurrentMap != this.Location.CurrentMap)
            {
                StopFollowing();
                return;
            }

            int xDiff = Math.Abs(this.entityToFollow.Location.X - this.Location.X);
            int yDiff = Math.Abs(this.entityToFollow.Location.Y - this.Location.Y);

            if ((xDiff > 1) || (yDiff > 1))
            {
                StopFollowing();
                return;
            }

        }

        public IEnumerator<Entity> Followers
        {
            get { return followers.GetEnumerator(); }
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
