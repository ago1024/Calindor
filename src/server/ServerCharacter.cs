/*
 * Copyright (C) 2007 Krzysztof 'DeadwooD' Smiechowicz
 * Original project page: http://sourceforge.net/projects/calindor/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 */
using Calindor.Misc.Predefines;

namespace Calindor.Server
{

    public class ServerCharacter : EntityImplementation
    {
        public ServerCharacter(PredefinedEntityImplementationKind kind) : base(kind)
        {

        }

        #region Message Exchange
        public override void PutMessageIntoMyQueue(Calindor.Server.Messaging.OutgoingMessage msg)
        {
            return; // There is no queue for server character
        }
        #endregion

        #region Movement Handling
        public override void LocationChangeMapAtEnterWorld()
        {
            mapManager.ChangeMapForEntity(this, location, location.CurrentMapName, true, location.X, location.Y);
        }
        #endregion

        protected override bool isEntityImplementationInCreationPhase()
        {
            return true;
        }
    }
}
