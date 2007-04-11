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
using Calindor.Server.Messaging;
using Calindor.Misc.Predefines;
using Calindor.Server.Maps;
using Calindor.Server.TimeBasedActions;
using Calindor.Server.Entities;

namespace Calindor.Server
{
    public partial class WorldSimulation
    {
        private void handleMoveTo(PlayerCharacter pc, IncommingMessage msg)
        { 
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                MoveToIncommingMessage msgMoveTo = (MoveToIncommingMessage)msg;

                // Check if destination tile is walkable
                if (!pc.Location.CurrentMap.IsLocationWalkable(msgMoveTo.X, msgMoveTo.Y))
                    return;

                // Calculate path
                WalkPath path = 
                    pc.Location.CurrentMap.CalculatePath(pc.Location.X, pc.Location.Y, msgMoveTo.X, msgMoveTo.Y);

                if (path.State != WalkPathState.VALID)
                    return;
                
                // Cancel current time based action
                pc.CancelCurrentTimeBasedAction();

                // Add walk time based action
                timeBasedActionsManager.AddAction(new WalkTimeBasedAction(pc, path));

                
                
                // Check followers
                if (pc.IsFollowedByEntities)
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
                }
            }
            
        }

        private void handleSitDown(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                SitDownIncommingMessage msgSitDown = (SitDownIncommingMessage)msg;

                if (msgSitDown.ShouldSit)
                {
                    if (!pc.Location.IsSittingDown)
                    {
                        pc.CancelCurrentTimeBasedAction(); //Cancel current time based action

                        AddActorCommandOutgoingMessage msgAddActorCommand =
                            (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                        msgAddActorCommand.EntityID = pc.EntityID;
                        msgAddActorCommand.Command = PredefinedActorCommand.sit_down;
                        pc.PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                        pc.Location.IsSittingDown = true;
                    }
                }
                else
                {
                    if (pc.Location.IsSittingDown)
                    {
                        pc.CancelCurrentTimeBasedAction(); //Cancel current time based action

                        AddActorCommandOutgoingMessage msgAddActorCommand =
                            (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                        msgAddActorCommand.EntityID = pc.EntityID;
                        msgAddActorCommand.Command = PredefinedActorCommand.stand_up;
                        pc.PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                        pc.Location.IsSittingDown = false;
                    }
                }
            }
        }

        private void handleTurnLeft(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                if (!pc.Location.IsSittingDown)
                {
                    pc.CancelCurrentTimeBasedAction(); //Cancel current time based action

                    AddActorCommandOutgoingMessage msgAddActorCommand =
                        (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                    msgAddActorCommand.EntityID = pc.EntityID;
                    msgAddActorCommand.Command = PredefinedActorCommand.turn_left;
                    pc.PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                    pc.Location.RatateBy(45);
                }
            }
        }

        private void handleTurnRight(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                if (!pc.Location.IsSittingDown)
                {
                    pc.CancelCurrentTimeBasedAction(); //Cancel current time based action

                    AddActorCommandOutgoingMessage msgAddActorCommand =
                        (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                    msgAddActorCommand.EntityID = pc.EntityID;
                    msgAddActorCommand.Command = PredefinedActorCommand.turn_right;
                    pc.PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                    pc.Location.RatateBy(-45);
                }
            }
        }
    }
}