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
using System.Threading;

namespace Calindor.Server
{

    public class CommunicationManager
    {
        protected Thread innerThread = null;
        protected bool isWorking = false;

        protected ILogger logger = new DummyLogger();
        public ILogger Logger
        {
            get { return logger; }
            set { logger = value; }
        }

        // List of active connections
        protected ServerClientConnectionList activeConnections =
            new ServerClientConnectionList();

        // List of new connections to be added to active ones
        protected ServerClientConnectionList newConnections =
            new ServerClientConnectionList();
        
        // List of connections to be removed from active ones
        protected ServerClientConnectionList toBeRemovedConnections =
            new ServerClientConnectionList();

        public CommunicationManager()
        {
        }

        public void StartManager()
        {
            Logger.LogProgress(LogSource.Communication, "CommunicationManager starting");

            // Creating thread
            ThreadStart ts = new ThreadStart(threadMain);
            innerThread = new Thread(ts);
            isWorking = true;
            innerThread.Start();
        }

        public void StopManager()
        {
            isWorking = false;
        }

        protected void threadMain()
        {


            while (isWorking)
            {
                // Process messages from existing connections
                foreach (ServerClientConnection conn in activeConnections)
                {
                    // Check if working
                    conn.TestIfNotBroken();

                    // Read data
                    try
                    {
                        conn.ReadAndDeserializeMessages();
                    }
                    catch (Exception ex)
                    {
                        // Error on read.
                        Logger.LogError(LogSource.Communication, "Failed to perform data read on connection.", ex);
                    }

                    // Write data
                    try
                    {
                        conn.SerializeAndSendMessages();
                    }
                    catch (Exception ex)
                    {
                        // Error on write.
                        Logger.LogError(LogSource.Communication, "Failed to perform data write on connection.", ex);
                    }

                    // Check if connection needs to be closed
                    if (conn.ForcedToCloseConnection)
                        toBeRemovedConnections.Add(conn);
                }

                // Remove closed connections
                if (toBeRemovedConnections.Count > 0)
                {
                    foreach (ServerClientConnection conn in toBeRemovedConnections)
                    {
                        activeConnections.Remove(conn);
                        
                        try
                        {
                            Logger.LogProgress(LogSource.Communication, "Shutting down connection for " + conn.ClientIP);
                            conn.Shutdown();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(LogSource.Communication, "Failed to shutdown connection.", ex);
                        }
                    }
                    toBeRemovedConnections.Clear();
                }


                // Add new connections to the list
                Monitor.TryEnter(newConnections, 10);

                try
                {
                    activeConnections.AddRange(newConnections);
                    newConnections.Clear();
                }
                finally
                {

                    Monitor.Exit(newConnections);
                }

                // Sleep
              Thread.Sleep(100);
            }

            Logger.LogProgress(LogSource.Communication, "CommunicationManager stopping");
        }

        public bool AddNewConnection(ServerClientConnection conn)
        {
            Monitor.Enter(newConnections);

            try
            {
                newConnections.Add(conn);
                return true;
            }
            finally
            {
                Monitor.Exit(newConnections);
            }
        }
    }
}
