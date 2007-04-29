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

namespace Calindor.Server
{
    /// <summary>
    /// Contains implementation of actions that can be performed over the entity
    /// </summary>
    public abstract class EntityImplementation : Entity
    {
        #region Time Based Actions
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

        //TODO: CORRECT
        public void TimeBasedActionCancelCurrent()
        {
            if (tbAction != null)
            {
                tbAction.Cancel();
                tbAction = null;
            }
        }
        #endregion

        #region Map Manager
        protected MapManager mapManager = null;
        public MapManager MapManager
        {
            set { mapManager = value; }
        }
        #endregion

        #region Inventory Handling
        public virtual void InventoryUpdateItem(Item itm)
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

        public virtual void InventoryLookAtItem(byte slot)
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

        public virtual void InventoryDropItemToGround(byte slot, int quantity)
        {
            Item itm = inventory.GetItemAtSlot(slot);

            if (itm != null)
            {
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

        public virtual void InventoryMoveItemInInventory(byte oldSlot, byte newSlot)
        {
            if (oldSlot > 35)
                return; //TODO: Add handling for equipment

            if (newSlot > 35)
                return; //TODO: Add handling for equipment

            if (inventory.IsSlotFree(newSlot))
            {
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
        public virtual void LocationMoveTo(short x, short y)
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


            //TODO: CORRECT
            // Check followers
            /*if (pc.IsFollowedByEntities)
            {
                IEnumerator<Entity> followersEnumerator = pc.Followers;
                followersEnumerator.Reset();

                short xMoveToFollower, yMoveToFollower = 0;

                while (followersEnumerator.MoveNext())
                {
                    // Calculate transition vectors
                    PlayerCharacter follower = followersEnumerator.Current as PlayerCharacter;
                    // TODO: For now only player characters can follow
                    if (follower == null)
                        continue;

                    xMoveToFollower = (short)(msgMoveTo.X + follower.Location.X - pc.Location.X);
                    yMoveToFollower = (short)(msgMoveTo.Y + follower.Location.Y - pc.Location.Y);

                    // Repeat path building actions for followers
                    // If the path cannot be build, the follower will not move and eventually will stop following

                    // Check if destination tile is walkable
                    if (!follower.Location.CurrentMap.IsLocationWalkable(xMoveToFollower, yMoveToFollower))
                        continue;

                    // Calculate path
                    WalkPath followerPath =
                        follower.Location.CurrentMap.CalculatePath(follower.Location.X, follower.Location.Y,
                        xMoveToFollower, yMoveToFollower);

                    if (followerPath.State != WalkPathState.VALID)
                        continue;

                    // Cancel current time based action
                    follower.CancelCurrentTimeBasedAction();

                    // Add walk time based action
                    timeBasedActionsManager.AddAction(new WalkTimeBasedAction(follower, followerPath));

                }
            }*/
        }

        public virtual void LocationStandUp()
        {
            LocationStandUp(false);
        }

        public virtual void LocationStandUp(bool continueWalking)
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

        public virtual void LocationSitDown()
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

        public virtual void LocationTurnLeft()
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

        public virtual void LocationTurnRight()
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

        public virtual void LocationTakeStep(PredefinedDirection dir)
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

        public virtual void LocationChangeMap(string newMapName, short x, short y)
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

        public virtual void LocationChangeLocation(short newX, short newY)
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
        #endregion
        #region Message Queue
        public virtual void PutMessageIntoObserversQueue(OutgoingMessage msg)
        {
            foreach (Entity en in entitiesObservers)
                if (en is EntityImplementation)
                    (en as EntityImplementation).PutMessageIntoMyQueue(msg);
        }

        public virtual void PutMessageIntoMyAndObserversQueue(OutgoingMessage msg)
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
            msg.FromEntity(this);
            msg.FromLocation(location);
        }
        #endregion

    }
}