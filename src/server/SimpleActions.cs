using System;
using System.Collections.Generic;
using System.Text;

using Calindor.Server.Messaging;
using Calindor.Misc.Predefines;
using Calindor.Server.Entities;


namespace Calindor.Server.SimpleActions
{
    public interface ISimpleAction
    {
        void Execute();
    }

    public class SimpleActionImpl : ISimpleAction
    {
        protected EntityImplementation executingEntity;
        protected string useMessage;

        public SimpleActionImpl(EntityImplementation enImpl, string useMessage)
        {
            this.executingEntity = enImpl;
            this.useMessage = useMessage;
        }

        public virtual void Execute()
        {
            if (useMessage != null && useMessage.Length > 0)
            {
                RawTextOutgoingMessage msgToSender = (RawTextOutgoingMessage)OutgoingMessagesFactory.Create(OutgoingMessageType.RAW_TEXT);
                msgToSender.Color = PredefinedColor.Blue1;
                msgToSender.Channel = PredefinedChannel.CHAT_LOCAL;
                msgToSender.Text = useMessage;
                executingEntity.PutMessageIntoMyQueue(msgToSender);
            }
        }
    }

    public class TeleportAction : SimpleActionImpl
    {
        protected short destX;
        protected short destY;
        protected string destMap;

        public TeleportAction(EntityImplementation enImpl, short destX, short destY, string destMap, string useMessage) :
            base(enImpl, useMessage)
        {
            this.destX = destX;
            this.destY = destY;
            this.destMap = destMap;
        }

        public override void Execute()
        {
            base.Execute();
            if (destMap == null)
                executingEntity.LocationChangeLocation(destX, destY);
            else
                executingEntity.LocationChangeMap(destMap, destX, destY);
        }
    }
}
