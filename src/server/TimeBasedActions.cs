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
        /// Makes action active
        /// </summary>
        void Activate();
        
        /// <summary>
        /// Makes action inactive
        /// </summary>
        void DeActivate();
        

    }

    public class TimeBasedActionList : List<ITimeBasedAction>
    {
    }

    /// <summary>
    /// Default implementation
    /// </summary>
    public abstract class TimeBasedAction : TimeBasedSkippingExecution, 
                                            ITimeBasedAction
    {
        protected EntityImplementation executingEntityImplementation = null;
        protected EntityImplementation affectedEntityImplementation = null;        private bool actionDeactivated = true;
        private bool immediateExecution = false;

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
            if (!actionDeactivated)
            {
                if (immediateExecution)
                    return PreconditionsResult.IMMEDIATE_EXECUTE;
                else
                    return PreconditionsResult.EXECUTE;
            }

            return PreconditionsResult.NO_EXECUTE;
        }
        
        /// <summary>
        /// Code to be execute on activation of action 
        /// </summary>
        protected virtual void onActivation()
        {
        }
        
        /// <summary>
        /// Code to be executed on deactivation of action
        /// </summary>
        protected virtual void onDeActivation()
        {
        }
        
        protected void setImmediateExecution()
        {
            immediateExecution = true;
        }
        protected void resetImmediateExecution()
        {
            immediateExecution = false;

        }
        #region ITimeBasedAction Members



        public bool ShouldContinue
        {
            get { return !actionDeactivated; }
        }

        public void Activate()
        {
            actionDeactivated = false;
            executingEntityImplementation.TimeBasedActionSetExecuted(this);
            if (affectedEntityImplementation != null)
                affectedEntityImplementation.TimeBasedActionAddAffecting(this);
            
            onActivation();
        }
        
        public void DeActivate()
        {
            actionDeactivated = true;
            executingEntityImplementation.TimeBasedActionRemoveExecuted();
            if (affectedEntityImplementation != null)
                affectedEntityImplementation.TimeBasedActionRemoveAffecting(this);
            
            onDeActivation();
        }
        #endregion
    }
    
    /*
     * -------------------------------------------------------------------------
     * IMPLEMENTATION
     * -------------------------------------------------------------------------
     */
    
    public class WalkTimeBasedAction : TimeBasedAction
    {
        private const int WALK_COMMAND_DELAY = 250; // Delay (in milis) of moves

        protected WalkPath walkPath = null;

        public WalkTimeBasedAction(EntityImplementation enImpl, WalkPath walkPath):
            base(enImpl, WALK_COMMAND_DELAY)
        {
           if (walkPath == null)
                throw new ArgumentNullException("walkPath");

            this.walkPath = walkPath;
            // Remove the first item (current location) from path
            this.walkPath.GetNext(); 
            
            setImmediateExecution();
        }
        
        protected override void onActivation ()
        {
            // Stand up
            executingEntityImplementation.LocationStandUp(true); 
        }

        protected override void execute()
        {            resetImmediateExecution();

            WalkPathItem itm = null;

            itm = walkPath.GetNext();

            if (itm == null)
            {
                DeActivate();
                return; //Move finished
            }

            // Check if location is not occupied
            if (executingEntityImplementation.LocationCurrentMap.IsLocationOccupied(itm.X, itm.Y, 
                executingEntityImplementation.LocationDimension))
            {
                DeActivate();
                return; // TODO: Needs to reroute
            }

            // Move and check result
            PredefinedDirection dir = 
                executingEntityImplementation.LocationTakeStepTo(itm.X, itm.Y);

            if (dir == PredefinedDirection.NO_DIRECTION)
            {
                DeActivate();
                return; // Error. Stop.
            }
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

        protected override void onDeActivation()
        {
            executingEntityImplementation.SendLocalChatMessage(
                "You stopped harvesting " + rscDef.HarvestedItem.Name,
                PredefinedColor.Blue1);        }

        protected override void execute()
        {
            if (WorldRNG.NextDouble() <= successRate)
                executingEntityImplementation.HarvestItemHarvested(rscDef);
            
            // After each harvest, recalculate
            calculateParameters();        }
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

            DeActivate();    
        }
    }
    
    // TODO: PROTOTYPE IMPLEMENTATION
    public class AttackTimeBasedAction : TimeBasedAction
    {
            
        public AttackTimeBasedAction(EntityImplementation attacker, EntityImplementation defender):
            base(attacker, defender, 2000) // TODO: Fixed time for now
        {
            // TODO: Maybe add immediate execution for 'backstab' attacks?
        }
            
        protected override void execute()
        {   
            //TODO: Implement
            if (!affectedEntityImplementation.EnergiesIsAlive)
            {
                DeActivate();
                return;
            }

            rotateToFaceDefender();

            if(!executingEntityImplementation.CombatAttack(affectedEntityImplementation))
            {
                DeActivate();
                return;
            }
            
            affectedEntityImplementation.CombatDefend();

            if (!affectedEntityImplementation.EnergiesIsAlive)
            {
                DeActivate();
                return;
            }
        }
        
        protected override void onDeActivation()
        {
            //TODO: Check if this is a last fight performed, if yes, 
            //      send animation frame
            executingEntityImplementation.SendAnimationCommand(
                PredefinedActorCommand.leave_combat);
            
            executingEntityImplementation.SendLocalChatMessage(
                "You stopped attacking.", PredefinedColor.Blue1);
        }
        
        private void rotateToFaceDefender()
        {
            executingEntityImplementation.LocationTurnToFace
                (affectedEntityImplementation.LocationX, affectedEntityImplementation.LocationY);            
        }

        protected override void onActivation()
        {
            // Animation for attacker
            // TODO: Move to EntityImplementation?
            rotateToFaceDefender();

            executingEntityImplementation.SendAnimationCommand(
                PredefinedActorCommand.enter_combat);
        }

    }
}