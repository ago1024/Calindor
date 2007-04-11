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
using Calindor.Server.Entities;
using Calindor.Server.Messaging;

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        /// <summary>
        /// Adds/removes entities according to visibility
        /// </summary>
        /// <param name="pc"></param>
        public void handleVisibilityChangeEvents(PlayerCharacter pc)
        { 
            // Remove entities
            EntityList entitiesToRemove = pc.GetRemovedVisibleEntities();
            foreach (Entity en in entitiesToRemove)
            {
                //TODO: For now only players, all entities later (when they are actually displayed)
                if (en is PlayerCharacter)
                {
                    RemoveActorOutgoingMessage msgRemoveActor =
                        (RemoveActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.REMOVE_ACTOR);
                    msgRemoveActor.EntityID = en.EntityID;
                    pc.PutMessageIntoMyQueue(msgRemoveActor);
                }
            }

            // Added entities
            EntityList entitiesToAdd = pc.GetAddedVisibleEntities();
            foreach (Entity en in entitiesToAdd)
            {
                //TODO: For now only players, all entities later (when they are actually displayed)
                if (en is PlayerCharacter)
                {
                    AddNewEnhancedActorOutgoingMessage msgAddNewEnhancedActor =
                        (AddNewEnhancedActorOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_NEW_ENHANCED_ACTOR);
                    msgAddNewEnhancedActor.FromPlayerCharacter(en as PlayerCharacter);
                    pc.PutMessageIntoMyQueue(msgAddNewEnhancedActor);
                }
            }

        }
    }
}