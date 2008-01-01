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
        private Dictionary<int, object> actionsSignatures = new Dictionary<int,object>();
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
            {
                activeActions.Remove(action);
                actionsSignatures.Remove(action.GetHashCode());
            }

            actionsToRemove.Clear();
        }

        public void AddAction(ITimeBasedAction action)
        {
            // Don't add existing action
            if (actionsSignatures.ContainsKey(action.GetHashCode()))
                return;
                
            actionsToAdd.Add(action);
            actionsSignatures.Add(action.GetHashCode(), null);
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
        
        /// <summary>
        /// Call this method, to 'activate' the action
        /// </summary>
        void Activate();
    }

    public class TimeBasedActionList : List<ITimeBasedAction>
    {
    }

    /// <summary>
    /// Default implementation
    /// </summary>
    public abstract class TimeBasedAction : TimeBasedExecution, ITimeBasedAction
    {
        protected EntityImplementation executingEntityImplementation = null;
        protected EntityImplementation affectedEntityImplementation = null;
        private bool actionCanceled = false;
        protected bool shouldContinue = true;

        protected TimeBasedAction(EntityImplementation executing, uint actionDuration):
            base(actionDuration)
        {
            if (executing == null)
                throw new ArgumentNullException("executing");

            executingEntityImplementation = executing;
        }
        
        // TODO: Maybe pass array of affected entities?
        protected TimeBasedAction(EntityImplementation executing,
            EntityImplementation affected, uint actionDuration):
            this(executing, actionDuration)
        {
            if (affected == null)
                throw new NullReferenceException("affected");
            affectedEntityImplementation = affected;
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
            executingEntityImplementation.TimeBasedActionRemoveExecuted();
            if (affectedEntityImplementation != null)
                affectedEntityImplementation.TimeBasedActionRemoveAffecting(this);
        }

        public bool ShouldContinue
        {
            get { return shouldContinue; }
        }

        public void Activate()
        {
            executingEntityImplementation.TimeBasedActionSetExecuted(this);
            if (affectedEntityImplementation != null)
                affectedEntityImplementation.TimeBasedActionAddAffecting(this);
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
                executingEntityImplementation.LocationStandUp(true); // Stand up
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
                if (executingEntityImplementation.LocationCurrentMap.IsLocationOccupied(itm.X, itm.Y, 
                    executingEntityImplementation.LocationDimension))
                {
                    shouldContinue = false;
                    return; // TODO: Needs to reroute
                }


                // Check direction
                int xDiff = executingEntityImplementation.LocationX - itm.X;
                int yDiff = executingEntityImplementation.LocationY - itm.Y;

                if (Math.Abs(xDiff) > 1 || Math.Abs(yDiff) > 1)
                {
                    shouldContinue = false;
                    return; // Error. Stop.
                }

                if (xDiff == 0)
                {
                    if (yDiff == -1)
                        executingEntityImplementation.LocationTakeStep(PredefinedDirection.N);

                    if (yDiff == 1)
                        executingEntityImplementation.LocationTakeStep(PredefinedDirection.S);
                }

                if (xDiff == -1)
                {
                    if (yDiff == -1)
                        executingEntityImplementation.LocationTakeStep(PredefinedDirection.NE);

                    if (yDiff == 0)
                        executingEntityImplementation.LocationTakeStep(PredefinedDirection.E);

                    if (yDiff == 1)
                        executingEntityImplementation.LocationTakeStep(PredefinedDirection.SE);
                }

                if (xDiff == 1)
                {
                    if (yDiff == 1)
                        executingEntityImplementation.LocationTakeStep(PredefinedDirection.SW);

                    if (yDiff == 0)
                        executingEntityImplementation.LocationTakeStep(PredefinedDirection.W);

                    if (yDiff == -1)
                        executingEntityImplementation.LocationTakeStep(PredefinedDirection.NW);

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
            this.successRate = executingEntityImplementation.HarvestGetSuccessRate(rscDef);
            setMilisBetweenExecutions(executingEntityImplementation.HarvestGetActionTime(rscDef));
        }

        public override void Cancel()
        {
            base.Cancel();

            RawTextOutgoingMessage msgRawText =
                (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
            msgRawText.Channel = PredefinedChannel.CHAT_LOCAL;
            msgRawText.Color = PredefinedColor.Blue1;
            msgRawText.Text = "You stopped harvesting " + rscDef.HarvestedItem.Name;
            executingEntityImplementation.PutMessageIntoMyQueue(msgRawText);
        }

        protected override void execute()
        {
            if (WorldRNG.NextDouble() <= successRate)
                executingEntityImplementation.HarvestItemHarvested(rscDef);
            
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
            (executingEntityImplementation as ServerCharacter).EnergiesRespawn();

            shouldContinue = false;
        }
    }
    
    // TODO: PROTOTYPE IMPLEMENTATION
    public class AttackTimeBasedAction : TimeBasedAction
    {
         
        public AttackTimeBasedAction(EntityImplementation attacker, EntityImplementation defender):
            base(attacker, defender, 2000) // TODO: Fixed for now
        {
        }
        
        protected override void execute()
        {
            //TODO: Implement
            executingEntityImplementation.CombatAttack(affectedEntityImplementation);
            affectedEntityImplementation.CombatDefend();
            if (!affectedEntityImplementation.EnergiesIsAlive)
                Cancel();
        }
        
        public override void Cancel()
        {
            base.Cancel();
            executingEntityImplementation.CombatStopFighting();
        }

    }
}