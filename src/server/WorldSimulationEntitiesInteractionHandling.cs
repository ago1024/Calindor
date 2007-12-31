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
        private void handleTouchPlayer(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                TouchPlayerIncommingMessage msgTouchPlayer = (TouchPlayerIncommingMessage)msg;

                Entity en = getEntityByEntityID(msgTouchPlayer.TargetEntityID);

                if ((en != null) && (en is ServerCharacter))
                {
                    // Can talk only to server characters
                    pc.NPCConversationStart(en as ServerCharacter);
                }
            }
        }

        private void handleRespondToNPC(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                RespondToNPCIncommingMessage msgRespondToNPC = (RespondToNPCIncommingMessage)msg;

                Entity en = getEntityByEntityID(msgRespondToNPC.TargetEntityID);

                if ((en != null) && (en is ServerCharacter))
                {
                    // Can talk only to server characters
                    pc.NPCConversationRespond((en as ServerCharacter), msgRespondToNPC.OptionID);
                }
            }
        }

        private void handleAttackSomeone(PlayerCharacter pc, IncommingMessage msg)
        {
            if (pc.LoginState == PlayerCharacterLoginState.LoginSuccesfull)
            {
                AttackSomeoneClientMessage msgAttackSomeone =
                    (AttackSomeoneClientMessage)msg;

                Entity en = getEntityByEntityID((ushort)msgAttackSomeone.TargetObjectID);

                if (en == null)
                    return;

                pc.CombatInitiateAttack(en as EntityImplementation); 
            }
        }
    }
}