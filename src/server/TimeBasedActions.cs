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
using Calindor.Server.Items;
using Calindor.Server;
using Calindor.Server.Resources;
using Calindor.Misc;

namespace Calindor.Server.TimeBasedActions
{
    public sealed class TimeBasedActionsManager
    {
        private TimeBasedActionList actionsToAdd = new TimeBasedActionList();
        private TimeBasedActionList activeActions = new TimeBasedActionList();
        private TimeBasedActionList actionsToRemove = new TimeBasedActionList();

        public void ExecuteActions()
        {
            // Adding queued actions
            foreach (TimeBasedAction action in actionsToAdd)
                activeActions.Add(action);
            actionsToAdd.Clear();
            
            // Executing actions
            foreach (TimeBasedAction action in activeActions)
            {
                // Execute and check if action finished
                action.Execute();
                if (!action.ShouldContinue)
                    actionsToRemove.Add(action);
            }

            // Removing finished actions
            foreach (TimeBasedAction action in actionsToRemove)
                activeActions.Remove(action);

            actionsToRemove.Clear();
        }

        public void AddAction(ITimeBasedAction action)
        {
            actionsToAdd.Add(action);
        }
    }

    public interface ITimeBasedAction
    {
        /// <summary>
        /// Indicates if action should be executed in next cycle
        /// </summary>
        bool ShouldContinue
        {
            get;
        }

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
    public abstract class TimeBasedAction : TimeBasedExecution, ITimeBasedAction
    {
        protected EntityImplementation targetEntityImplementation = null;
        private bool actionCanceled = false;
        protected bool shouldContinue = true;

        protected TimeBasedAction(EntityImplementation enImpl, uint actionDuration):base(actionDuration)
        {
            if (enImpl == null)
                throw new ArgumentNullException("enImpl");

            targetEntityImplementation = enImpl;
            enImpl.TimeBasedActionSet(this);
        }

        protected override PreconditionsResult checkPreconditions()
        {
            if (actionCanceled)
            {
                shouldContinue = false;
                return PreconditionsResult.NO_EXECUTE;
            }
            else
                return PreconditionsResult.EXECUTE;
        }

        #region ITimeBasedAction Members

        public virtual void Cancel()
        {
            actionCanceled = true;
        }

        public bool ShouldContinue
        {
            get { return shouldContinue; }
        }

        #endregion
    }

    public class WalkTimeBasedAction : TimeBasedAction
    {
        protected WalkPath walkPath = null;
        private const int WALK_COMMAND_DELAY = 250; // Delay (in milis) of sending commands
        private bool firstStep = true; 

        public WalkTimeBasedAction(EntityImplementation enImpl, WalkPath walkPath) : base(enImpl, WALK_COMMAND_DELAY)
        {
           if (walkPath == null)
                throw new ArgumentNullException("walkPath");

            this.walkPath = walkPath;

            updateLastExecutionTime();

            firstStep = true;
        }

        protected override void execute()
        {
            int milisSinceLastExecute = getMilisSinceLastExecution();

            // How many moves should be executed
            int numberOfMoves = milisSinceLastExecute / WALK_COMMAND_DELAY;

            // If this is first step
            if (firstStep)
            {
                numberOfMoves++; // Add one move
                firstStep = false; // No longer first step
                walkPath.GetNext(); // Remove the first item (current location) from path 
                targetEntityImplementation.LocationStandUp(true); // Stand up
            }

            for (int i = 0; i < numberOfMoves; i++)
            {
                WalkPathItem itm = null;

                itm = walkPath.GetNext();

                if (itm == null)
                {
                    shouldContinue = false;
                    return; //Move finished
                }

                // Check if location is not occupied
                if (targetEntityImplementation.LocationCurrentMap.IsLocationOccupied(itm.X, itm.Y, 
                    targetEntityImplementation.LocationDimension))
                {
                    shouldContinue = false;
                    return; // TODO: Needs to reroute
                }


                // Check direction
                int xDiff = targetEntityImplementation.LocationX - itm.X;
                int yDiff = targetEntityImplementation.LocationY - itm.Y;

                if (Math.Abs(xDiff) > 1 || Math.Abs(yDiff) > 1)
                {
                    shouldContinue = false;
                    return; // Error. Stop.
                }

                if (xDiff == 0)
                {
                    if (yDiff == -1)
                        targetEntityImplementation.LocationTakeStep(PredefinedDirection.N);

                    if (yDiff == 1)
                        targetEntityImplementation.LocationTakeStep(PredefinedDirection.S);
                }

                if (xDiff == -1)
                {
                    if (yDiff == -1)
                        targetEntityImplementation.LocationTakeStep(PredefinedDirection.NE);

                    if (yDiff == 0)
                        targetEntityImplementation.LocationTakeStep(PredefinedDirection.E);

                    if (yDiff == 1)
                        targetEntityImplementation.LocationTakeStep(PredefinedDirection.SE);
                }

                if (xDiff == 1)
                {
                    if (yDiff == 1)
                        targetEntityImplementation.LocationTakeStep(PredefinedDirection.SW);

                    if (yDiff == 0)
                        targetEntityImplementation.LocationTakeStep(PredefinedDirection.W);

                    if (yDiff == -1)
                        targetEntityImplementation.LocationTakeStep(PredefinedDirection.NW);

                }
            }

            shouldContinue = true; // Keep on executing
        }

        protected override PreconditionsResult checkPreconditions()
        {
            PreconditionsResult pResult = base.checkPreconditions();

            if (firstStep)
                pResult = PreconditionsResult.IMMEDIATE_EXECUTE;

            return pResult;
        }
    }

    public class HarvestTimeBasedAction : TimeBasedAction
    {
        private HarvestableResourceDescriptor rscDef = null;
        private double successRate = 0.0;

        public HarvestTimeBasedAction(EntityImplementation enImpl, 
            HarvestableResourceDescriptor rscDef): base(enImpl,0)
        {
            if (rscDef == null)
                throw new ArgumentNullException("rscDef");

            this.rscDef = rscDef;
            calculateParameters();
        }

        private void calculateParameters()
        {
            this.successRate = targetEntityImplementation.HarvestGetSuccessRate(rscDef);
            setMilisBetweenExecutions(targetEntityImplementation.HarvestGetActionTime(rscDef));
        }

        public override void Cancel()
        {
            base.Cancel();

            RawTextOutgoingMessage msgRawText =
                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
            msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
            msgRawText.Color = PredefinedColor.Blue1;
            msgRawText.Text = "You stopped harvesting " + rscDef.HarvestedItem.Name;
            targetEntityImplementation.PutMessageIntoMyQueue(msgRawText);
        }

        protected override void execute()
        {
            if (WorldRNG.NextDouble() <= successRate)
                targetEntityImplementation.HarvestItemHarvested(rscDef);
            
            // After each harvest, recalculate
            calculateParameters();

            shouldContinue = true;
        }
    }

    public class RespawnTimeBasedAction : TimeBasedAction
    {
        public RespawnTimeBasedAction(EntityImplementation enIml, uint milisToRespawn):
            base(enIml, milisToRespawn)
        {
        }

        protected override void execute()
        {
            (targetEntityImplementation as ServerCharacter).EnergiesRespawn();

            shouldContinue = false;
        }
    }
    
    // TODO: PROTOTYPE IMPLEMENTATION
    public class AttackTimeBasedAction : TimeBasedAction
    {
        protected EntityImplementation defenderEntityImplementation = null;
        
        public AttackTimeBasedAction(EntityImplementation attacker, EntityImplementation defender):
            base(attacker, 2000) // TODO: Fixed for now
        {
            if (defender == null)
                throw new ArgumentNullException("defender");
            
            defenderEntityImplementation = defender;
            defenderEntityImplementation.TimeBasedActionSet(this);
        }
        
        protected override void execute()
        {
            //TODO: Implement
            targetEntityImplementation.CombatAttack(defenderEntityImplementation);
            defenderEntityImplementation.CombatDefend();
        }
        
        public override void Cancel()
        {
            base.Cancel();
            targetEntityImplementation.CombatStopFighting();
            defenderEntityImplementation.CombatStopFighting();
        }

    }
}