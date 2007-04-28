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
using Calindor.Server.Messaging;
using Calindor.Server.Items;

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        private void handleLookAtInventoryItem(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                LookAtInventoryItemIncommingMessage msgLookAtInventoryItem =
                    (LookAtInventoryItemIncommingMessage)msg;

                Item itm = pc.Inventory.GetItemAtSlot(msgLookAtInventoryItem.Slot);

                if (itm != null)
                {
                    InventoryItemTextOutgoingMessage msgInventoryItemText =
                        (InventoryItemTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.INVENTORY_ITEM_TEXT);
                    msgInventoryItemText.Text = itm.Definition.Name;
                    pc.PutMessageIntoMyQueue(msgInventoryItemText);
                }
            }
        }

        private void handleDropItem(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                DropItemIncommingMessage msgDropItem =
                    (DropItemIncommingMessage)msg;

                Item itm = pc.Inventory.GetItemAtSlot(msgDropItem.Slot);

                if (itm != null)
                {
                    Item updateItem = new Item(itm.Definition);
                    updateItem.Quantity = -1 * msgDropItem.Quantity;

                    itm = pc.Inventory.UpdateItem(updateItem);

                    if (itm != null)
                    {
                        GetNewInventoryItemOutgoingMessage msgGetNewInventoryItem =
                            (GetNewInventoryItemOutgoingMessage)OutgoingMessagesFactory.Create(
                            OutgoingMessageType.GET_NEW_INVENTORY_ITEM);
                        msgGetNewInventoryItem.FromItem(itm);
                        pc.PutMessageIntoMyQueue(msgGetNewInventoryItem);

                        // TODO: Add putting item to the bad on ground
                    }
                }
            }
        }

        private void handleMoveInventoryItem(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                MoveInventoryItemIncommingMessage msgMoveInventoryItem =
                    (MoveInventoryItemIncommingMessage)msg;
                
                if (msgMoveInventoryItem.Slot > 35)
                    return; //TODO: Add handling for equipment

                if (msgMoveInventoryItem.NewSlot > 35)
                    return; //TODO: Add handling for equipment

                if (pc.Inventory.IsSlotFree(msgMoveInventoryItem.NewSlot))
                {
                    Item itmToRemove = pc.Inventory.RemoveItemAtSlot(msgMoveInventoryItem.Slot);

                    if (itmToRemove != null)
                    {
                        RemoveItemFromInventoryOutgoingMessage msgRemoveItemFromInventory =
                            (RemoveItemFromInventoryOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ITEM_FROM_INVENTORY);
                        msgRemoveItemFromInventory.Slot = itmToRemove.Slot;
                        pc.PutMessageIntoMyQueue(msgRemoveItemFromInventory);

                        Item itmAdded = pc.Inventory.InsertItemToSlot(msgMoveInventoryItem.NewSlot, itmToRemove);

                        GetNewInventoryItemOutgoingMessage msgGetNewInventoryItem =
                            (GetNewInventoryItemOutgoingMessage)OutgoingMessagesFactory.Create(
                            OutgoingMessageType.GET_NEW_INVENTORY_ITEM);
                        msgGetNewInventoryItem.FromItem(itmAdded);
                        pc.PutMessageIntoMyQueue(msgGetNewInventoryItem);
                    }
                }
           }
        }

    }
}