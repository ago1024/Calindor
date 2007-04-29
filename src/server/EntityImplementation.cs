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

namespace Calindor.Server
{
    /// <summary>
    /// Contains implementation of actions that can be performed over the entity
    /// </summary>
    public abstract class EntityImplementation : Entity
    {
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
    }
}