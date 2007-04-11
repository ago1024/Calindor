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
using Calindor.Server.Maps;
using Calindor.Server.Messaging;
using Calindor.Misc.Predefines;

namespace Calindor.Server.TimeBasedActions
{
    public class TimeBasedActionsManager
    {
        private TimeBasedActionList activeActions = new TimeBasedActionList();
        private TimeBasedActionList actionsToRemove = new TimeBasedActionList();

        public void ExecuteActions()
        {
            // Executing actions
            foreach (TimeBasedAction action in activeActions)
            {
                // Execute and check if action finished
                if (!action.Execute())
                    actionsToRemove.Add(action);
            }

            // Removing finished actions
            foreach (TimeBasedAction action in actionsToRemove)
                activeActions.Remove(action);

            actionsToRemove.Clear();
        }

        public void AddAction(ITimeBasedAction action)
        {
            activeActions.Add(action);
        }
    }

    public interface ITimeBasedAction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>False is action finished</returns>
        bool Execute();

        /// <summary>
        /// Causes the action to be canceled
        /// </summary>
        void Cancel();
    }

    public class TimeBasedActionList : List<ITimeBasedAction>
    {
    }

    /// <summary>
    /// Default implementation
    /// </summary>
    public abstract class TimeBasedAction : ITimeBasedAction
    {
        protected PlayerCharacter targetPlayerCharacter = null;
        protected long lastExecutedTick = DateTime.Now.Ticks;
        protected bool actionCanceled = false;

        protected int getMilisSinceLastExecution()
        {
            return (int)((DateTime.Now.Ticks - lastExecutedTick)) / 10000;
        }

        protected void updateLastExecutionTime()
        {
            lastExecutedTick = DateTime.Now.Ticks;
        }

        #region ITimeBasedAction Members

        public abstract bool Execute();

        public void Cancel()
        {
            actionCanceled = true;
        }

        #endregion
    }

    public class WalkTimeBasedAction : TimeBasedAction
    {
        protected WalkPath walkPath = null;
        private const int WALK_COMMAND_DELAY = 250; // Delay (in milis) of sending commands
        private bool firstStep = true; 

        public WalkTimeBasedAction(PlayerCharacter pc, WalkPath walkPath)
        {
            if (pc == null)
                throw new ArgumentNullException("pc");

            if (walkPath == null)
                throw new ArgumentNullException("walkPath");

            targetPlayerCharacter = pc;
            pc.SetTimeBasedAction(this);
            this.walkPath = walkPath;

            updateLastExecutionTime();
            firstStep = true;
        }

        public override bool Execute()
        {
            if (actionCanceled)
                return false; // Action canceled. Nothing to do.

            int milisSinceLastExecute = getMilisSinceLastExecution();

            if ((milisSinceLastExecute > WALK_COMMAND_DELAY) || (firstStep))
            {
                // How many moves should be executed
                int numberOfMoves = milisSinceLastExecute / WALK_COMMAND_DELAY;

                // If this is first step
                if (firstStep)
                {
                    numberOfMoves++; // Add one move
                    firstStep = false; // No longer first step
                    walkPath.GetNext(); // Remove the first item (current location) from path 
                }

                // If sitting down, stand up
                if (targetPlayerCharacter.Location.IsSittingDown)
                {
                    AddActorCommandOutgoingMessage msgAddActorCommand =
                        (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                    msgAddActorCommand.EntityID = targetPlayerCharacter.EntityID;
                    msgAddActorCommand.Command = PredefinedActorCommand.stand_up;
                    targetPlayerCharacter.PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                    targetPlayerCharacter.Location.IsSittingDown = false;
                }

                for (int i = 0; i < numberOfMoves; i++)
                {
                    WalkPathItem itm = null;

                    itm = walkPath.GetNext();

                    if (itm == null)
                        return false; //Move finished

                    // Check if location is not occupied
                    if (targetPlayerCharacter.Location.CurrentMap.IsLocationOccupied(itm.X, itm.Y))
                        return false; // TODO: Needs to reroute

                    AddActorCommandOutgoingMessage msgAddActorCommand =
                        (AddActorCommandOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.ADD_ACTOR_COMMAND);
                    msgAddActorCommand.EntityID = targetPlayerCharacter.EntityID;

                    // Check direction
                    int xDiff = targetPlayerCharacter.Location.X - itm.X;
                    int yDiff = targetPlayerCharacter.Location.Y - itm.Y;

                    if (Math.Abs(xDiff) > 1 || Math.Abs(yDiff) > 1)
                        return false; // Error. Stop.

                    if (xDiff == 0)
                    {
                        if (yDiff == -1)
                        {
                            msgAddActorCommand.Command = PredefinedActorCommand.move_n;
                            targetPlayerCharacter.Location.Rotation = 0;
                        }
                    
                        if (yDiff == 1)
                        {
                            msgAddActorCommand.Command = PredefinedActorCommand.move_s;
                            targetPlayerCharacter.Location.Rotation = 180;
                        }
                    }

                    if (xDiff == -1)
                    {
                        if (yDiff == -1)
                        {
                            msgAddActorCommand.Command = PredefinedActorCommand.move_ne;
                            targetPlayerCharacter.Location.Rotation = 45;
                        }

                        if (yDiff == 0)
                        {
                            msgAddActorCommand.Command = PredefinedActorCommand.move_e;
                            targetPlayerCharacter.Location.Rotation = 90;
                        }
                        
                        if (yDiff == 1)
                        {
                            msgAddActorCommand.Command = PredefinedActorCommand.move_se;
                            targetPlayerCharacter.Location.Rotation = 135;
                        }
                    }

                    if (xDiff == 1)
                    {
                        if (yDiff == 1)
                        {
                            msgAddActorCommand.Command = PredefinedActorCommand.move_sw;
                            targetPlayerCharacter.Location.Rotation = 225;
                        }

                        if (yDiff == 0)
                        {
                            msgAddActorCommand.Command = PredefinedActorCommand.move_w;
                            targetPlayerCharacter.Location.Rotation = 270;
                        }

                        if (yDiff == -1)
                        {
                            msgAddActorCommand.Command = PredefinedActorCommand.move_nw;
                            targetPlayerCharacter.Location.Rotation = 315;
                        }

                    }

                    targetPlayerCharacter.PutMessageIntoMyAndObserversQueue(msgAddActorCommand);
                    targetPlayerCharacter.Location.X = itm.X;
                    targetPlayerCharacter.Location.Y = itm.Y;
                }

                updateLastExecutionTime();
            }

            return true; // Keep on executing
        }
    }
}