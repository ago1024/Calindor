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
using System.Globalization;

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
        
        protected bool logNormalOperation = false;
        public bool LogNormalOperation
        {
            get { return logNormalOperation; }
            set { logNormalOperation = value; }
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
            innerThread.CurrentCulture = CultureInfo.InvariantCulture;
            innerThread.CurrentUICulture = CultureInfo.InvariantCulture;
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
                        if (!(ex is ConnectionBrokenException) || LogNormalOperation)
                            Logger.LogError(LogSource.Communication, 
                                string.Format("Failed to perform data read on connection IP: {0}, Port: {1}",
                                    conn.ClientIP, conn.ClientPort), ex);
                    }

                    // Write data
                    try
                    {
                        conn.SerializeAndSendMessages();
                    }
                    catch (Exception ex)
                    {
                        // Error on write.
                        if (!(ex is ConnectionBrokenException) || LogNormalOperation)
                            Logger.LogError(LogSource.Communication, 
                                string.Format("Failed to perform data write on connection IP: {0}, Port: {1}",
                                    conn.ClientIP, conn.ClientPort), ex);                        
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
                            Logger.LogProgress(LogSource.Communication, 
                                string.Format("Shutting down connection for IP: {0}, Port: {1}", 
                                                conn.ClientIP, conn.ClientPort));
                            conn.Shutdown();
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is ConnectionBrokenException) || LogNormalOperation)                          
                                Logger.LogError(LogSource.Communication, 
                                    string.Format("Failed to shutdown connection for IP: {0}, Port: {1}.",
                                        conn.ClientIP, conn.ClientPort), ex);
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
