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
using System.Text;
using System.Net.Sockets;
using System.Net;
using Calindor.Server.Messaging;
using System.Threading;



namespace Calindor.Server
{
    public class ServerClientConnection
    {
        protected Socket connectionSocket = null;

        // Read data variables
        protected byte[] readBuffer = null;
        protected int  bytesInBuffer = 0;
        protected int readBufferSize = 0;

        // Connection testing variables
        protected byte[] testBuffer = new byte[1];
        protected long lastCommunicationTick = -1;

        // Connection state managing
        protected bool forcedToCloseConnection = false;
        protected bool connectionBroken = false;

        public bool ConnectionBroken
        {
            get { return connectionBroken; }
        }

        public bool ForcedToCloseConnection
        {
            get { return forcedToCloseConnection; }
        }

        protected bool ConnectionOperational
        {
            get { return ((!forcedToCloseConnection) && (!connectionBroken)); }
        }

        protected IncommingMessagesQueue incommingMessages =
            new IncommingMessagesQueue();

        protected OutgoingMessagesQueue outgoingMessages =
            new OutgoingMessagesQueue();

        protected ILogger logger = new DummyLogger();
        public ILogger Logger
        {
            get { return logger; }
            set { logger = value; }
        }

        protected bool logNornalOperation = false;
        public bool LogNormalOperation
        {
            get { return logNornalOperation; }
            set { logNornalOperation = value; }
        }

        protected string clientIP = "255.255.255.255";
        public string ClientIP
        {
            get { return clientIP; }
        }

        protected int clientPort = 65536;
        public int ClientPort
        {
            get { return clientPort;}
        }

        public ServerClientConnection(Socket toClientSocket) : this(toClientSocket, 8192)
        {
        }

        public ServerClientConnection(Socket toClientSocket, int readBufferSize)
        {
            if (toClientSocket == null)
                throw new ArgumentNullException("toClientSocket");

            if (!IsBufferSizeInRange(readBufferSize))
                throw new ArgumentException("Buffer size must be in range (0, 32768).", "readBufferSize");

            readBuffer = new byte[readBufferSize];
            connectionSocket = toClientSocket;
            bytesInBuffer = 0;
            this.readBufferSize = readBufferSize;
            clientIP = (connectionSocket.RemoteEndPoint as IPEndPoint).Address.ToString();
            clientPort = (connectionSocket.RemoteEndPoint as IPEndPoint).Port;

            updateLastCommunicationTime();
        }

        public static bool IsBufferSizeInRange(int readBufferSize)
        {
            if (readBufferSize < 1)
                return false;

            if (readBufferSize > 32768)
                return false;

            return true;
        }

        public void ForceClose()
        {
            forcedToCloseConnection = true;
        }

        protected void updateLastCommunicationTime()
        {
            lastCommunicationTick = DateTime.Now.Ticks;
        }

        /// <summary>
        /// Checks if connection is still working. If not, sets internal state that block all communication.
        /// </summary>
        public void TestIfNotBroken()
        {
            try
            {
                long diff = DateTime.Now.Ticks - lastCommunicationTick;

                if (diff > 10000000) // Every second from last communication
                {
                    connectionSocket.Send(testBuffer, 0, 0);
                    updateLastCommunicationTime();
                }
            }
            catch (Exception)
            {
                connectionBroken = true;
            }
        }

        public void ReadAndDeserializeMessages()
        {
            if (!ConnectionOperational)
                return; // Connection no longer working

            // Get data into buffer (TODO: what if data larger than buffer.. should deserialize and read again?)
            if (connectionSocket.Available > 0)
            {
                try
                {
                    bytesInBuffer = connectionSocket.Receive(readBuffer);
                    updateLastCommunicationTime();
                }
                catch (SocketException ex)
                {
                    connectionBroken = true;
                    throw new ConnectionBrokenException(ex);
                }
                catch (Exception)
                {
                    connectionBroken = true;
                    throw;
                }
            }
            else
                bytesInBuffer = 0;

            if (bytesInBuffer > 0)
            {
                // Deserialize
                int index = 0;
                UInt16 size = 0;
                IncommingMessage msg = null;
                byte type = 0;

                while (index < bytesInBuffer)
                {
                    // TODO: Error control on not completed data frames. Goes with cyclic buffer mentioned above
                    size = BitConverter.ToUInt16(readBuffer, index + 1);
                    size += 2;

                    if (index + size > bytesInBuffer)
                    {
                        // Error: the read would go beyond buffer
                        Logger.LogError(LogSource.Communication,
                            string.Format("Buffer smaller ({0}) than expected read(({1},{2}) for client {3}",
                            bytesInBuffer, index, size, ClientIP), null);
                        break;
                    }

                    type = 0;

                    try
                    {
                        type = IncommingMessage.GetMessageType(readBuffer, index);
                        msg = IncommingMessagesFactory.Deserialize(readBuffer, index);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(LogSource.Communication,
                            "Exception while deserializing stream for message (" + type + ") from client " + ClientIP, ex);

                        continue;
                    }
                    finally
                    {
                        index += size;
                    }


                    if (msg != null)
                    {
                        if (LogNormalOperation)
                            Logger.LogProgress(LogSource.Communication,
                                string.Format("Received message: {0} from client {1}", msg.ToString(), ClientIP));

                        Monitor.Enter(incommingMessages);

                        try
                        {
                            incommingMessages.Enqueue(msg);
                        }
                        finally
                        {
                            Monitor.Exit(incommingMessages);
                        }
                    }
                    else
                    {
                        Logger.LogWarning(LogSource.Communication,
                            string.Format("Unrecognized message type ({0}) from client {1}", type, ClientIP), null);
                    }

                }
            }
        }

        public void SerializeAndSendMessages()
        {
            if (!ConnectionOperational)
                return; // Connection no longer working

            Monitor.Enter(outgoingMessages);

            try
            {
                foreach (OutgoingMessage msg in outgoingMessages)
                {
                    connectionSocket.Send(msg.Serialize());

                    if (LogNormalOperation)
                        Logger.LogProgress(LogSource.Communication,
                            string.Format("Send message: {0} to client {1}", msg.ToString(), ClientIP));

                    updateLastCommunicationTime();
                }

            }
            catch (SocketException ex)
            {
                connectionBroken = true;
                throw new ConnectionBrokenException(ex);
            }
            catch (Exception)
            {
                connectionBroken = true;
                throw;
            }
            finally
            {
                outgoingMessages.Clear();// Purge the messages regardles of beeing send or exception
                Monitor.Exit(outgoingMessages);
            }
        }

        public bool PutMessageIntoOUTQueue(OutgoingMessage msg)
        {
            if (ConnectionOperational)
            {
                if (Monitor.TryEnter(outgoingMessages, 10))
                {
                    try
                    {
                        outgoingMessages.Enqueue(msg);
                        return true;
                    }
                    finally
                    {
                        Monitor.Exit(outgoingMessages);
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true; // Simulate: All OK
            }
        }

        public IncommingMessage GetMessageFromINQueue()
        {
            if (ConnectionOperational)
            {
                if (Monitor.TryEnter(incommingMessages, 10))
                {
                    try
                    {
                        if (incommingMessages.Count == 0)
                            return null;

                        return incommingMessages.Dequeue();
                    }
                    finally
                    {
                        Monitor.Exit(incommingMessages);
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null; // Simulate: No Messages
            }

        }

        public void Shutdown()
        {
            try
            {
                connectionSocket.Shutdown(SocketShutdown.Both);
            }
            catch(SocketException ex)
            {
                throw new ConnectionBrokenException(ex);
            }
        }
    }

    public class ConnectionBrokenException : ApplicationException
    {
        public ConnectionBrokenException(Exception innerException):
            base("Connection is broken", innerException)
        {
        }
    }

    public class ServerClientConnectionList : List<ServerClientConnection>
    {
    }
}
