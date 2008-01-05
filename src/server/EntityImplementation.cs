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
        // Time based action that is currently executed
        private ITimeBasedAction executedTimeBasedAction = null;
        // Time based action that are executed by other entities, but affect this one
        private TimeBasedActionList affectingTimeBasedActions = new TimeBasedActionList();

        private TimeBasedActionsManager timeBasedActionsManager = null;
        protected void timeBasedActionAddActionToManager(ITimeBasedAction actionToAdd)
        {
            if (timeBasedActionsManager == null)
                throw new NullReferenceException("timeBasedActionsManager");
            
            timeBasedActionsManager.AddAction(actionToAdd);
        }
        
        public void TimeBasedActionConnectToManager(TimeBasedActionsManager tbaManager)
        {
            timeBasedActionsManager = tbaManager;
        }
        
        public void TimeBasedActionAddAffecting(ITimeBasedAction actionToAdd)
        {
            affectingTimeBasedActions.Add(actionToAdd);
            
            // Additional handling for attack action
            if (actionToAdd is AttackTimeBasedAction)
                attackersCount++;
        }
        
        public void TimeBasedActionRemoveAffecting(ITimeBasedAction actionToRemove)
        {
            bool actionRemoved = affectingTimeBasedActions.Remove(actionToRemove);
            
            // Additional handling for attack action
            if (actionRemoved && (actionToRemove is AttackTimeBasedAction))
                attackersCount--;
            
            // Failsafe
            if (attackersCount < 0)
                throw new InvalidOperationException(
                    "Attacker count less than 0 for " + Name + "(" + EntityID + ")");
            
        }
        
        public void TimeBasedActionSetExecuted(ITimeBasedAction actionToExecute)
        {
            // Only one action can be executed at any time
            TimeBasedActionCancelExecuted();
            
            executedTimeBasedAction = actionToExecute;
            
            // Add action to manager (manager takes care of duplicated adds)
            timeBasedActionAddActionToManager(actionToExecute);
        }
        
        public void TimeBasedActionRemoveExecuted()
        {
            executedTimeBasedAction = null;
        }

        public void TimeBasedActionCancelExecuted()
        {
            if (executedTimeBasedAction != null)
                executedTimeBasedAction.DeActivate();
        }
        #endregion

        #region Inventory Handling
        public void InventoryUpdateItem(Item itm)
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

        public void InventoryLookAtItem(byte slot)
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

        public void InventoryDropItemToGround(byte slot, int quantity)
        {
            Item itm = inventory.GetItemAtSlot(slot);

            if (itm != null)
            {
                TimeBasedActionCancelExecuted();

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

        public void InventoryMoveItemInInventory(byte oldSlot, byte newSlot)
        {
            if (oldSlot > 35)
                return; //TODO: Add handling for equipment

            if (newSlot > 35)
                return; //TODO: Add handling for equipment

            if (inventory.IsSlotFree(newSlot))
            {
                TimeBasedActionCancelExecuted();

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

        /// <summary>
        /// Returns COPY of stored item. Use this copy to update inventory.
        /// </summary>
        /// <param name="itmDef"></param>
        /// <returns></returns>
        public Item InventoryGetItemByDefinition(ItemDefinition itmDef)
        {
            Item _return = inventory.FindItemByDefinitionID(itmDef.ID);

            if (_return != null)
                return _return.Clone();

            return null;
        }
        #endregion

        #region Movement Handling

        protected MapManager mapManager = null;

        public void LocationMoveTo(short x, short y)
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
            WalkTimeBasedAction walk = new WalkTimeBasedAction(this, path);
            walk.Activate();


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

        public void LocationStandUp()
        {
            LocationStandUp(false);
        }

        public void LocationStandUp(bool continueWalking)
        {
            if (location.IsSittingDown)
            {
                if (!continueWalking)
                    TimeBasedActionCancelExecuted(); //Cancel current time based action

                AddActorCommandOutgoingMessage msgAddActorCommand =
                    (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                msgAddActorCommand.EntityID = EntityID;
                msgAddActorCommand.Command = PredefinedActorCommand.stand_up;
                PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                location.IsSittingDown = false;
            }
        }

        public void LocationSitDown()
        {
            if (!location.IsSittingDown)
            {
                TimeBasedActionCancelExecuted(); //Cancel current time based action

                AddActorCommandOutgoingMessage msgAddActorCommand =
                    (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                msgAddActorCommand.EntityID = EntityID;
                msgAddActorCommand.Command = PredefinedActorCommand.sit_down;
                PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                location.IsSittingDown = true;
            }
        }

        public void LocationTurnLeft()
        {
            if (!location.IsSittingDown)
            {
                TimeBasedActionCancelExecuted(); //Cancel current time based action

                AddActorCommandOutgoingMessage msgAddActorCommand =
                    (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                msgAddActorCommand.EntityID = EntityID;
                msgAddActorCommand.Command = PredefinedActorCommand.turn_left;
                PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                location.RotateBy(45);
            }
        }

        public void LocationTurnRight()
        {
            if (!location.IsSittingDown)
            {
                TimeBasedActionCancelExecuted(); //Cancel current time based action

                AddActorCommandOutgoingMessage msgAddActorCommand =
                    (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                msgAddActorCommand.EntityID = EntityID;
                msgAddActorCommand.Command = PredefinedActorCommand.turn_right;
                PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                location.RotateBy(-45);
            }
        }
        
        public PredefinedDirection LocationTurnToFace(short x, short y)
        {
            if (!location.IsSittingDown)
            {
                PredefinedDirection dir = locationCalculateDirectionTo(x, y);
                
                if (dir != PredefinedDirection.NO_DIRECTION)
                    LocationTurnTo(dir);
                
                return dir;
            }
            
            return PredefinedDirection.NO_DIRECTION;
        }
        
        public void LocationTurnTo(PredefinedDirection dir)
        {
            if (!location.IsSittingDown)
            {
                if (dir == PredefinedDirection.NO_DIRECTION)
                    return;
                
                PredefinedActorCommand command = PredefinedActorCommand.turn_n;
                short rotation = 0;
                
                switch (dir)
                {
                    case(PredefinedDirection.N):
                        command = PredefinedActorCommand.turn_n;
                        rotation = 0;
                        break;
                    case(PredefinedDirection.NE):
                        command = PredefinedActorCommand.turn_ne;
                        rotation = 45;
                        break;
                    case (PredefinedDirection.E):
                        command = PredefinedActorCommand.turn_e;
                        rotation = 90;
                        break;
                    case (PredefinedDirection.SE):
                        command = PredefinedActorCommand.turn_se;
                        rotation = 135;
                        break;
                    case (PredefinedDirection.S):
                        command = PredefinedActorCommand.turn_s;
                        rotation = 180;
                        break;
                    case (PredefinedDirection.SW):
                        command = PredefinedActorCommand.turn_sw;
                        rotation = 225;
                        break;
                    case (PredefinedDirection.W):
                        command = PredefinedActorCommand.turn_w;
                        rotation = 270;
                        break;
                    case (PredefinedDirection.NW):
                        command = PredefinedActorCommand.turn_nw;
                        rotation = 315;
                        break;
                }

                if (location.Rotation != rotation)
                {
                    // Rotate only if new rotation different
                    location.Rotation = rotation;
                    SendAnimationCommand(command);
                }
            }
        }
        
        protected PredefinedDirection locationCalculateDirectionTo(short x, short y)
        {
            int xDiff = LocationX - x;
            int yDiff = LocationY - y;
            
            if (xDiff != 0)
                xDiff /= Math.Abs(xDiff);
            
            if (yDiff != 0)
                yDiff /= Math.Abs(yDiff);
            
            if (xDiff == 0)
            {
                if (yDiff == -1)
                    return PredefinedDirection.N;

                if (yDiff == 1)
                    return PredefinedDirection.S;
            }

            if (xDiff == -1)
            {
                if (yDiff == -1)
                    return PredefinedDirection.NE;

                if (yDiff == 0)
                    return PredefinedDirection.E;

                if (yDiff == 1)
                    return PredefinedDirection.SE;
            }

            if (xDiff == 1)
            {
                if (yDiff == 1)
                    return PredefinedDirection.SW;

                if (yDiff == 0)
                    return PredefinedDirection.W;

                if (yDiff == -1)
                    return PredefinedDirection.NW;

            }
            
            // x == 0 && y == 0
            return PredefinedDirection.NO_DIRECTION;
        }
        public PredefinedDirection LocationTakeStepTo(short x, short y)
        {
            if (!location.IsSittingDown)
            {
                int xDiff = LocationX - x;
                int yDiff = LocationY - y;

                if (Math.Abs(xDiff) > 1 || Math.Abs(yDiff) > 1)
                    return PredefinedDirection.NO_DIRECTION;

                PredefinedDirection dir = locationCalculateDirectionTo(x, y);
                
                if (dir != PredefinedDirection.NO_DIRECTION)
                    LocationTakeStep(dir);
                
                return dir;
            }
            
            return PredefinedDirection.NO_DIRECTION;
        }
        
        public void LocationTakeStep(PredefinedDirection dir)
        {
            if (!location.IsSittingDown)
            {
                if (dir == PredefinedDirection.NO_DIRECTION)
                    return;
                
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
        }

        public void LocationChangeMap(string newMapName, short x, short y)
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

            // Display
            PutMessageIntoMyQueue(visibilityDisplayEntityImplementation());

        }

        public void LocationChangeLocation(short newX, short newY)
        {
            // TODO: Should check if new locaiton is walkable?

            RemoveActorOutgoingMessage msgRemoveActor =
                (RemoveActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ACTOR);
            msgRemoveActor.EntityID = EntityID;
            PutMessageIntoMyAndObserversQueue(msgRemoveActor);

            location.X = newX;
            location.Y = newY;

            // Display
            PutMessageIntoMyAndObserversQueue(visibilityDisplayEntityImplementation());
        }

        public void LocationSetMapManager(MapManager mapMngr)
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

        public void LocationChangeDimension(PredefinedDimension dimension)
        {
            switch(dimension)
            {
                case(PredefinedDimension.LIFE):
                    appearanceSetTransparent(false);
                    break;
                case(PredefinedDimension.SHADOWS):
                    appearanceSetTransparent(true);
                    break;
                default:
                    throw new ArgumentException("No handling for dimension " + dimension.ToString());
            }

            location.Dimension = (int)dimension;

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

                TimeBasedActionCancelExecuted();
                
                RawTextOutgoingMessage msgRawTextOut =
                                     (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgRawTextOut.Channel = PredefinedChannel.CHAT_LOCAL;
                msgRawTextOut.Color = PredefinedColor.Blue1;
                msgRawTextOut.Text = "You stopped following " + entityToFollow.Name;
                PutMessageIntoMyQueue(msgRawTextOut);

                entityToFollow = null;
            }
        }

        public void FollowingFollow(EntityImplementation enImpl)
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
            double distance = Double.MaxValue;
            DistanceCalculationResult result = getDistanceToEntity(enImpl, out distance);
            if (result != DistanceCalculationResult.CALC_OK)
            {
                msgRawTextOutMe.Text = "The one you seek is not here...";
                PutMessageIntoMyQueue(msgRawTextOutMe);
                return;
            }

            if (distance > 3.0)
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

            TimeBasedActionCancelExecuted();

            if (enImpl.addFollower(this))
            {
                this.entityToFollow = enImpl;
                TimeBasedActionCancelExecuted();
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

        public void FollowingReleaseFollowers()
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

        public void FollowingCheckForStopFollowing()
        {
            if (!isFollowingEntity)
                return;

            double distance = Double.MaxValue;
            DistanceCalculationResult result = getDistanceToEntity(entityToFollow, out distance);

            // TODO: Move the unlink checks into external class implementing a check interface.
            if (result != DistanceCalculationResult.CALC_OK)
            {
                FollowingStopFollowing();
                return;
            }

            if (distance > 3.0)
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
        public void PutMessageIntoObserversQueue(OutgoingMessage msg)
        {
            foreach (Entity en in entitiesObservers)
                if (en is EntityImplementation)
                    (en as EntityImplementation).PutMessageIntoMyQueue(msg);
        }

        public void PutMessageIntoMyAndObserversQueue(OutgoingMessage msg)
        {
            PutMessageIntoMyQueue(msg);

            PutMessageIntoObserversQueue(msg);
        }

        public abstract void PutMessageIntoMyQueue(OutgoingMessage msg);
        
        /// <summary>
        /// Sends a local chat message to the entity
        /// </summary>
        /// <param name="message">
        /// A <see cref="System.String"/>
        /// </param>
        /// <param name="color">
        /// A <see cref="PredefinedColor"/>
        /// </param>
        public abstract void SendLocalChatMessage(string message, PredefinedColor color);
        
        /// <summary>
        /// Sends AddActorCommand to entity and all observers
        /// </summary>
        /// <param name="command">
        /// A <see cref="PredefinedActorCommand"/>
        /// </param>
        public void SendAnimationCommand(PredefinedActorCommand command)
        {
            AddActorCommandOutgoingMessage msgAddActorCommand =
                (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
            msgAddActorCommand.EntityID = EntityID;
            msgAddActorCommand.Command = command;
            PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
        }

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
            msg.FromEnergies(energies);
        }
        public void FillOutgoingMessage(AddNewActorOutgoingMessage msg)
        {
            msg.FromEntityImplementation(this);
            msg.FromAppearance(appearance);
            msg.FromLocation(location);
            msg.FromEnergies(energies);
        }
        #endregion

        #region Visibility Handling
        public void VisibilityUpdateVisibleEntities()
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
                    PutMessageIntoMyQueue((en as EntityImplementation).visibilityDisplayEntityImplementation());
                }
            }
        }

        protected virtual OutgoingMessage visibilityDisplayEntityImplementation()
        {
            if (appearance.IsEnhancedModel)
            {
                AddNewEnhancedActorOutgoingMessage msgAddNewEnhancedActor =
                            (AddNewEnhancedActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR);
                FillOutgoingMessage(msgAddNewEnhancedActor);
                return msgAddNewEnhancedActor;
            }
            else
            {
                AddNewActorOutgoingMessage msgAddNewActor =
                    (AddNewActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ACTOR);
                FillOutgoingMessage(msgAddNewActor);
                return msgAddNewActor;
            }
        }

        #endregion

        #region Creation Handling
        
        protected abstract bool isEntityImplementationInCreationPhase();

        public virtual void CreateSetInitialLocation(EntityLocation location)
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

        public virtual void CreateRecalculateInitialEnergies()
        {
            if (!isEntityImplementationInCreationPhase())
                throw new InvalidOperationException("This method can only be used during creation!");
            
            // TODO: Recalculate based on attributes/perks/items
            energies.SetMaxHealth(50);


            energies.UpdateCurrentHealth(energies.GetHealthDifference());
        }

        public void ClearEntityImplementation()
        {
            skills.Clear();
            inventory.Clear();
        }

        #endregion

        #region Energies Handling
        
        protected virtual void energiesEntityDied()
        {
            // Cancel current action
            TimeBasedActionCancelExecuted();
        }

        public void EnergiesUpdateHealth(short updValue)
        {
            if (!energies.IsAlive)
                return;

            short actualHealthChanged = energies.UpdateCurrentHealth(updValue);

            if (energies.CurrentHealth <= 0)
            {
                energiesEntityDied();
            }

            // Send messages
            if (actualHealthChanged < 0)
            {
                GetActorDamageOutgoingMessage msgGetActorDamage =
                    (GetActorDamageOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.GET_ACTOR_DAMAGE);
                msgGetActorDamage.EntityID = EntityID;
                msgGetActorDamage.Damage = (ushort)(-1.0 * actualHealthChanged); // actual health changed is less than 0
                PutMessageIntoMyAndObserversQueue(msgGetActorDamage);
            }

            if (actualHealthChanged >= 0)
            {
                GetActorHealOutgoingMessage msgGetActorHeal =
                    (GetActorHealOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.GET_ACTOR_HEAL);
                msgGetActorHeal.EntityID = EntityID;
                msgGetActorHeal.Heal = (ushort)(actualHealthChanged); // actual health changed is more than 0
                PutMessageIntoMyAndObserversQueue(msgGetActorHeal);
            }

            SendPartialStatOutgoingMessage msgSendPartialStat =
                (SendPartialStatOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.SEND_PARTIAL_STAT);
            msgSendPartialStat.StatType = PredefinedPartialStatType.MAT_POINT_CUR;
            msgSendPartialStat.Value = energies.CurrentHealth;
            PutMessageIntoMyQueue(msgSendPartialStat);

        }

        public void EnergiesRestoreAllHealth()
        {
            EnergiesUpdateHealth(energies.GetHealthDifference());
        }

        public void EnergiesResurrect()
        {
            if (energies.IsAlive)
                return;

            short actualHealthChanged = energies.UpdateCurrentHealth(
                (short)((energies.CurrentHealth * -1) + (energies.MaxHealth / 4)));

            // Send messages
            if (actualHealthChanged >= 0)
            {
                GetActorHealOutgoingMessage msgGetActorHeal =
                    (GetActorHealOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.GET_ACTOR_HEAL);
                msgGetActorHeal.EntityID = EntityID;
                msgGetActorHeal.Heal = (ushort)(actualHealthChanged); // actual health changed is more than 0
                PutMessageIntoMyAndObserversQueue(msgGetActorHeal);
            }
            else
                throw new InvalidOperationException("Less than 0 health change during resurrection!");

            SendPartialStatOutgoingMessage msgSendPartialStat =
                (SendPartialStatOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.SEND_PARTIAL_STAT);
            msgSendPartialStat.StatType = PredefinedPartialStatType.MAT_POINT_CUR;
            msgSendPartialStat.Value = energies.CurrentHealth;
            PutMessageIntoMyQueue(msgSendPartialStat);

            RawTextOutgoingMessage msgRawText =
                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
            msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
            msgRawText.Color = PredefinedColor.Green2;
            msgRawText.Text = "Warm beams of sun touch your face again... and you feel you are alive...";
            PutMessageIntoMyQueue(msgRawText);

            // Change dimension
            LocationChangeDimension(PredefinedDimension.LIFE);
        }

        public bool EnergiesIsAlive
        {
            get { return energies.IsAlive; }
        }
        #endregion

        #region Appearance Handling
        protected void appearanceSetTransparent(bool _value)
        {
            appearance.IsTransparent = _value;

            SendBuffsOutgoingMessage msgSendBuff =
                (SendBuffsOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.SEND_BUFFS);
            msgSendBuff.EntityID = EntityID;
            msgSendBuff.IsTransparent = appearance.IsTransparent;
            PutMessageIntoMyAndObserversQueue(msgSendBuff);
        }
        #endregion
        
        #region Calendar Events Handling
        public virtual void CalendarNewMinute(ushort minuteOfTheDay)
        {
            // New minute event
            NewMinuteOutgoingMessage msg =
                    (NewMinuteOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.NEW_MINUTE);
            msg.MinuteOfTheDay = minuteOfTheDay;
            
            PutMessageIntoMyQueue(msg);
            
            // Heal a bit
            if (energies.GetHealthDifference() != 0)
            {
                short healedHealth = (short)WorldRNG.Next(1,4);
                EnergiesUpdateHealth(healedHealth);
            }
        }
        #endregion
        
    }
}