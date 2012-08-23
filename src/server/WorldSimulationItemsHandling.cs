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

                pc.InventoryLookAtItem(msgLookAtInventoryItem.Slot);
            }
        }

        private void handleDropItem(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                DropItemIncommingMessage msgDropItem =
                    (DropItemIncommingMessage)msg;

                pc.InventoryDropItemToGround(msgDropItem.Slot, msgDropItem.Quantity);
            }
        }

        private void handleMoveInventoryItem(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                MoveInventoryItemIncommingMessage msgMoveInventoryItem =
                    (MoveInventoryItemIncommingMessage)msg;

                pc.InventoryMoveItemInInventory(msgMoveInventoryItem.Slot, msgMoveInventoryItem.NewSlot);
            }
        }

    }
}