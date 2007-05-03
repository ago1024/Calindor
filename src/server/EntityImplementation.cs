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
using Calindor.Server.Items;
using Calindor.Server.Messaging;
using Calindor.Server.Maps;
using Calindor.Server.TimeBasedActions;
using Calindor.Misc.Predefines;
using Calindor.Server.Resources;
using System;

namespace Calindor.Server
{
    /// <summary>
    /// Contains implementation of actions that can be performed over the entity
    /// </summary>
    public abstract partial class EntityImplementation : Entity
    {
        protected PredefinedEntityImplementationKind kind = PredefinedEntityImplementationKind.ENTITY;
        public PredefinedEntityImplementationKind EntityImplementationKind
        {
            get { return kind; }
        }

        private EntityImplementation()
        {
        }

        public EntityImplementation(PredefinedEntityImplementationKind kind)
        {
            this.kind = kind;
        }

        #region Time Based Actions Handling
        // Time based action
        // TODO: Probably an entity might have more than one time based action
        protected ITimeBasedAction tbAction = null;

        protected TimeBasedActionsManager timeBasedActionsManager = null;
        public void TimeBasedActionSetManager(TimeBasedActionsManager tbaManager)
        {
            timeBasedActionsManager = tbaManager;
        }

        public void TimeBasedActionSet(ITimeBasedAction actionToSet)
        {
            TimeBasedActionCancelCurrent();

            tbAction = actionToSet;
        }

        public void TimeBasedActionCancelCurrent()
        {
            if (tbAction != null)
            {
                tbAction.Cancel();
                tbAction = null;
            }
        }
        #endregion

        #region Inventory Handling
        public  void InventoryUpdateItem(Item itm)
        {
            if (itm != null)
            {
                itm = inventory.UpdateItem(itm);
                if (itm != null)
                {
                    GetNewInventoryItemOutgoingMessage msgGetNewInventoryItem =
                        (GetNewInventoryItemOutgoingMessage)OutgoingMessagesFactory.Create(
                        OutgoingMessageType.GET_NEW_INVENTORY_ITEM);
                    msgGetNewInventoryItem.FromItem(itm);
                    PutMessageIntoMyQueue(msgGetNewInventoryItem);
                }
            }
        }

        public  void InventoryLookAtItem(byte slot)
        {
            Item itm = inventory.GetItemAtSlot(slot);

            if (itm != null)
            {
                InventoryItemTextOutgoingMessage msgInventoryItemText =
                    (InventoryItemTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.INVENTORY_ITEM_TEXT);
                msgInventoryItemText.Text = itm.Definition.Name;
                PutMessageIntoMyQueue(msgInventoryItemText);
            }
        }

        public  void InventoryDropItemToGround(byte slot, int quantity)
        {
            Item itm = inventory.GetItemAtSlot(slot);

            if (itm != null)
            {
                TimeBasedActionCancelCurrent();

                Item updateItem = new Item(itm.Definition);
                updateItem.Quantity = -1 * quantity;

                itm = inventory.UpdateItem(updateItem);

                if (itm != null)
                {
                    GetNewInventoryItemOutgoingMessage msgGetNewInventoryItem =
                        (GetNewInventoryItemOutgoingMessage)OutgoingMessagesFactory.Create(
                        OutgoingMessageType.GET_NEW_INVENTORY_ITEM);
                    msgGetNewInventoryItem.FromItem(itm);
                    PutMessageIntoMyQueue(msgGetNewInventoryItem);

                    // TODO: Add putting item to the bad on ground
                }
            }
        }

        public  void InventoryMoveItemInInventory(byte oldSlot, byte newSlot)
        {
            if (oldSlot > 35)
                return; //TODO: Add handling for equipment

            if (newSlot > 35)
                return; //TODO: Add handling for equipment

            if (inventory.IsSlotFree(newSlot))
            {
                TimeBasedActionCancelCurrent();

                Item itmToRemove = inventory.RemoveItemAtSlot(oldSlot);

                if (itmToRemove != null)
                {
                    RemoveItemFromInventoryOutgoingMessage msgRemoveItemFromInventory =
                        (RemoveItemFromInventoryOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ITEM_FROM_INVENTORY);
                    msgRemoveItemFromInventory.Slot = itmToRemove.Slot;
                    PutMessageIntoMyQueue(msgRemoveItemFromInventory);

                    Item itmAdded = inventory.InsertItemToSlot(newSlot, itmToRemove);

                    GetNewInventoryItemOutgoingMessage msgGetNewInventoryItem =
                        (GetNewInventoryItemOutgoingMessage)OutgoingMessagesFactory.Create(
                        OutgoingMessageType.GET_NEW_INVENTORY_ITEM);
                    msgGetNewInventoryItem.FromItem(itmAdded);
                    PutMessageIntoMyQueue(msgGetNewInventoryItem);
                }
            }
        }
        #endregion

        #region Movement Handling

        protected MapManager mapManager = null;

        public  void LocationMoveTo(short x, short y)
        {
            // Check if destination tile is walkable
            if (!location.CurrentMap.IsLocationWalkable(x, y))
                return;

            // Calculate path
            WalkPath path =
                location.CurrentMap.CalculatePath(location.X, location.Y, x, y);

            if (path.State != WalkPathState.VALID)
                return;

            // Add walk time based action
            timeBasedActionsManager.AddAction(new WalkTimeBasedAction(this, path));


            // Move followers
            if (isFollowedByEntities)
            {
                short xMoveToFollower, yMoveToFollower = 0;

                foreach (Entity en in followers)
                {
                    EntityImplementation follower = en as EntityImplementation;
                    if (follower == null)
                        continue;

                    // Calculate transition vectors
                    xMoveToFollower = (short)(x + follower.LocationX - location.X);
                    yMoveToFollower = (short)(y + follower.LocationY - location.Y);

                    follower.LocationMoveTo(xMoveToFollower, yMoveToFollower);
                }
            }
        }

        public  void LocationStandUp()
        {
            LocationStandUp(false);
        }

        public  void LocationStandUp(bool continueWalking)
        {
            if (location.IsSittingDown)
            {
                if (!continueWalking)
                    TimeBasedActionCancelCurrent(); //Cancel current time based action

                AddActorCommandOutgoingMessage msgAddActorCommand =
                    (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                msgAddActorCommand.EntityID = EntityID;
                msgAddActorCommand.Command = PredefinedActorCommand.stand_up;
                PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                location.IsSittingDown = false;
            }
        }

        public  void LocationSitDown()
        {
            if (!location.IsSittingDown)
            {
                TimeBasedActionCancelCurrent(); //Cancel current time based action

                AddActorCommandOutgoingMessage msgAddActorCommand =
                    (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                msgAddActorCommand.EntityID = EntityID;
                msgAddActorCommand.Command = PredefinedActorCommand.sit_down;
                PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                location.IsSittingDown = true;
            }
        }

        public  void LocationTurnLeft()
        {
            if (!location.IsSittingDown)
            {
                TimeBasedActionCancelCurrent(); //Cancel current time based action

                AddActorCommandOutgoingMessage msgAddActorCommand =
                    (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                msgAddActorCommand.EntityID = EntityID;
                msgAddActorCommand.Command = PredefinedActorCommand.turn_left;
                PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                location.RatateBy(45);
            }
        }

        public  void LocationTurnRight()
        {
            if (!location.IsSittingDown)
            {
                TimeBasedActionCancelCurrent(); //Cancel current time based action

                AddActorCommandOutgoingMessage msgAddActorCommand =
                    (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                msgAddActorCommand.EntityID = EntityID;
                msgAddActorCommand.Command = PredefinedActorCommand.turn_right;
                PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                location.RatateBy(-45);
            }
        }

        public  void LocationTakeStep(PredefinedDirection dir)
        {
            AddActorCommandOutgoingMessage msgAddActorCommand =
                (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
            msgAddActorCommand.EntityID = EntityID;

            switch (dir)
            {
                case(PredefinedDirection.N):
                    msgAddActorCommand.Command = PredefinedActorCommand.move_n;
                    location.Rotation = 0;
                    location.Y++;
                    break;
                case(PredefinedDirection.NE):
                    msgAddActorCommand.Command = PredefinedActorCommand.move_ne;
                    location.Rotation = 45;
                    location.Y++;
                    location.X++;
                    break;
                case (PredefinedDirection.E):
                    msgAddActorCommand.Command = PredefinedActorCommand.move_e;
                    location.Rotation = 90;
                    location.X++;
                    break;
                case (PredefinedDirection.SE):
                    msgAddActorCommand.Command = PredefinedActorCommand.move_se;
                    location.Rotation = 135;
                    location.Y--;
                    location.X++;
                    break;
                case (PredefinedDirection.S):
                    msgAddActorCommand.Command = PredefinedActorCommand.move_s;
                    location.Rotation = 180;
                    location.Y--;
                    break;
                case (PredefinedDirection.SW):
                    msgAddActorCommand.Command = PredefinedActorCommand.move_sw;
                    location.Rotation = 225;
                    location.Y--;
                    location.X--;
                    break;
                case (PredefinedDirection.W):
                    msgAddActorCommand.Command = PredefinedActorCommand.move_w;
                    location.Rotation = 270;
                    location.X--;
                    break;
                case (PredefinedDirection.NW):
                    msgAddActorCommand.Command = PredefinedActorCommand.move_nw;
                    location.Rotation = 315;
                    location.Y++;
                    location.X--;
                    break;
            }

            PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
        }

        public  void LocationChangeMap(string newMapName, short x, short y)
        {
            mapManager.ChangeMapForEntity(this, location, newMapName, x, y);

            // Kill All Actors
            KillAllActorsOutgoingMessage msgKillAllActors =
                (KillAllActorsOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.KILL_ALL_ACTORS);
            PutMessageIntoMyQueue(msgKillAllActors);

            // Change Map
            ChangeMapOutgoingMessage msgChangeMap =
                (ChangeMapOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.CHANGE_MAP);
            msgChangeMap.MapPath = location.CurrentMap.ClientFileName;
            PutMessageIntoMyQueue(msgChangeMap);

            // Add New Enhanced Actor 
            AddNewEnhancedActorOutgoingMessage msgAddNewEnhancedActor =
                (AddNewEnhancedActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR);
            FillOutgoingMessage(msgAddNewEnhancedActor);
            PutMessageIntoMyQueue(msgAddNewEnhancedActor);

        }

        public  void LocationChangeLocation(short newX, short newY)
        {
            // TODO: Should check if new locaiton is walkable?

            RemoveActorOutgoingMessage msgRemoveActor =
                (RemoveActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ACTOR);
            msgRemoveActor.EntityID = EntityID;
            PutMessageIntoMyAndObserversQueue(msgRemoveActor);

            location.X = newX;
            location.Y = newY;

            AddNewEnhancedActorOutgoingMessage msgAddNewEnchangedActor =
                (AddNewEnhancedActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR);
            FillOutgoingMessage(msgAddNewEnchangedActor);
            PutMessageIntoMyAndObserversQueue(msgAddNewEnchangedActor);
        }

        public  void LocationSetMapManager(MapManager mapMngr)
        {
            mapManager = mapMngr;
        }

        public abstract void LocationChangeMapAtEnterWorld();

        public void LocationLeaveMapAtExitWorld()
        {
            if (mapManager != null)
                mapManager.RemoveEntityFromItsMap(this, location);

            // No messages need to be send. Entity will disapear with next round of visibility
        }

        #endregion

        #region Following Handling
        protected EntityList followers = new EntityList();
        protected EntityImplementation entityToFollow = null;
        public  void FollowingStopFollowing()
        {
            if (isFollowingEntity)
            {
                entityToFollow.removeFollower(this);

                TimeBasedActionCancelCurrent();
                
                RawTextOutgoingMessage msgRawTextOut =
                                     (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawTextOut.Color = PredefinedColor.Blue1;
                msgRawTextOut.Text = "You stopped following " + entityToFollow.Name;
                PutMessageIntoMyQueue(msgRawTextOut);

                entityToFollow = null;
            }
        }

        public  void FollowingFollow(EntityImplementation enImpl)
        {
            RawTextOutgoingMessage msgRawTextOutMe =
                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
            msgRawTextOutMe.Channel = PredefinedChannel.CHAT_LOCAL;
            msgRawTextOutMe.Color = PredefinedColor.Blue1;

            // Is it different than 'me'
            if (enImpl == this)
            {
                msgRawTextOutMe.Text = "There is no point following yourself...";
                PutMessageIntoMyQueue(msgRawTextOutMe);
                return;
            }

            // Is is close enough
            int xDiff = location.X - enImpl.LocationX;
            int yDiff = location.Y - enImpl.LocationY;
            if ((Math.Abs(xDiff) > 1) || (Math.Abs(yDiff) > 1) || (location.CurrentMap != enImpl.LocationCurrentMap))
            {
                msgRawTextOutMe.Text = "You need to stand closer...";
                PutMessageIntoMyQueue(msgRawTextOutMe);
                return;
            }

            if (isFollowedByEntities)
            {
                msgRawTextOutMe.Text = "If you want to follow, you can't be followed...";
                PutMessageIntoMyQueue(msgRawTextOutMe);
                return;
            }
                
            if (enImpl.isFollowingEntity)
            {
                msgRawTextOutMe.Text = enImpl.Name + " can't follow anybody if you want to follow him/her...";
                PutMessageIntoMyQueue(msgRawTextOutMe);
                return;
            }

            FollowingStopFollowing();

            TimeBasedActionCancelCurrent();

            if (enImpl.addFollower(this))
            {
                this.entityToFollow = enImpl;
                TimeBasedActionCancelCurrent();
                msgRawTextOutMe.Text = "You start following " + enImpl.Name;
                PutMessageIntoMyQueue(msgRawTextOutMe);

                RawTextOutgoingMessage msgRawTextOutOther = 
                    (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawTextOutOther.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawTextOutOther.Color = PredefinedColor.Blue1;
                msgRawTextOutOther.Text = Name + " follows you...";
                enImpl.PutMessageIntoMyQueue(msgRawTextOutOther);
            }
            else
            {
                msgRawTextOutMe.Color = PredefinedColor.Red2;
                msgRawTextOutMe.Text = "You can't follow " + enImpl.Name;
                PutMessageIntoMyQueue(msgRawTextOutMe);
            }
        }

        public  void FollowingReleaseFollowers()
        {
            if (isFollowedByEntities)
            {
                EntityList tempFollowers = new EntityList();

                tempFollowers.AddRange(followers);

                foreach (Entity en in tempFollowers)
                    if (en is EntityImplementation)
                        (en as EntityImplementation).FollowingStopFollowing();

                RawTextOutgoingMessage msgRawTextOut =
                    (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawTextOut.Color = PredefinedColor.Blue1;
                msgRawTextOut.Text = "You are no longer followed...";
                PutMessageIntoMyQueue(msgRawTextOut);
            }
        }

        public  void FollowingCheckForStopFollowing()
        {
            if (!isFollowingEntity)
                return;

            // TODO: Move the unlink checks into external class implementing a check interface.
            if (this.entityToFollow.LocationCurrentMap != this.location.CurrentMap)
            {
                FollowingStopFollowing();
                return;
            }

            int xDiff = Math.Abs(this.entityToFollow.LocationX - this.location.X);
            int yDiff = Math.Abs(this.entityToFollow.LocationY - this.location.Y);

            if ((xDiff > 1) || (yDiff > 1))
            {
                FollowingStopFollowing();
                return;
            }
        }

        private bool addFollower(EntityImplementation follower)
        {
            if (follower.isFollowedByEntities)
                return false;

            if (!followers.Contains(follower))
                followers.Add(follower);

            return true;
        }

        private bool isFollowedByEntities
        {
            get { return followers.Count > 0; }
        }

        private bool isFollowingEntity
        {
            get { return entityToFollow != null; }
        }


        private void removeFollower(EntityImplementation en)
        {
            if (followers.Contains(en))
                followers.Remove(en);
        }
        #endregion

        #region Message Queue
        public  void PutMessageIntoObserversQueue(OutgoingMessage msg)
        {
            foreach (Entity en in entitiesObservers)
                if (en is EntityImplementation)
                    (en as EntityImplementation).PutMessageIntoMyQueue(msg);
        }

        public  void PutMessageIntoMyAndObserversQueue(OutgoingMessage msg)
        {
            PutMessageIntoMyQueue(msg);

            PutMessageIntoObserversQueue(msg);
        }

        public abstract void PutMessageIntoMyQueue(OutgoingMessage msg);

        #endregion

        #region Fill Messages
        public void FillOutgoingMessage(HereYourInventoryOutgoingMessage msg)
        {
            msg.FromInventory(inventory);
        }
        public void FillOutgoingMessage(AddNewEnhancedActorOutgoingMessage msg)
        {
            msg.FromEntityImplementation(this);
            msg.FromAppearance(appearance);
            msg.FromLocation(location);
        }
        #endregion

        #region Visibility Handling
        public  void VisibilityUpdateVisibleEntities()
        {
            // Remove entities
            calculateRemovedVisibleEntities();
            foreach (Entity en in entitiesVisibleRemoved)
            {
                RemoveActorOutgoingMessage msgRemoveActor =
                    (RemoveActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ACTOR);
                msgRemoveActor.EntityID = en.EntityID;
                PutMessageIntoMyQueue(msgRemoveActor);
            }

            // Added entities
            calculateAddedVisibleEntities();
            foreach (Entity en in entitiesVisibleAdded)
            {
                if (en is EntityImplementation)
                {
                    AddNewEnhancedActorOutgoingMessage msgAddNewEnhancedActor =
                        (AddNewEnhancedActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR);
                    (en as EntityImplementation).FillOutgoingMessage(msgAddNewEnhancedActor);
                    PutMessageIntoMyQueue(msgAddNewEnhancedActor);
                }
            }
        }
        #endregion

        #region EntityImplementation Creation Handling
        
        protected abstract bool isEntityImplementationInCreationPhase();

        public void CreateSetInitialLocation(EntityLocation location)
        {
            if (!isEntityImplementationInCreationPhase())
                throw new InvalidOperationException("This method can only be used during creation!");
            this.location = location;
        }

        public void CreateSetInitialAppearance(EntityAppearance appearance)
        {
            if (!isEntityImplementationInCreationPhase())
                throw new InvalidOperationException("This method can only be used during creation!");
            this.appearance = appearance;
        }

        public void ClearEntityImplementation()
        {
            skills.Clear();
            inventory.Clear();
        }

        #endregion
    }
}