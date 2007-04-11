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
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Calindor.Server
{
    public class ServerListeningThread
    {
        protected Thread innerThread = null;
       
        protected Socket serverSocket =
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
       
        protected ILogger logger = new DummyLogger();

        public ILogger Logger
        {
            get { return logger; }
            set { logger = value; }
        }

        //
        protected ServerConfiguration serverConfiguration = null;

        //
        protected CommunicationManager serverCommunicationManager = null;

        //
        protected WorldSimulation serverWorldSimulation = null;


        public ServerListeningThread(ServerConfiguration conf, CommunicationManager commManager,
            WorldSimulation worldSim)
        {
            if (conf == null)
                throw new ArgumentNullException("conf");

            serverConfiguration = conf;

            if (commManager == null)
                throw new ArgumentNullException("commManager");

            serverCommunicationManager = commManager;

            if (worldSim == null)
                throw new ArgumentNullException("worldSim");

            serverWorldSimulation = worldSim;
        }

        public void StartListening()
        {
            Logger.LogProgress(LogSource.Listener, "ListeningThread starting");

            IPAddress address = IPAddress.Parse(serverConfiguration.BindIP);
            EndPoint ep = new IPEndPoint(address, serverConfiguration.BindPort);
            serverSocket.Bind(ep);
            serverSocket.Listen(100);

            ThreadStart ts = new ThreadStart(this.threadMain);
            innerThread = new Thread(ts);
            innerThread.Start();
        }

        protected void threadMain()
        {
            while (true)
            {
                Socket newClientSocket = null;

                try
                {
                    newClientSocket = serverSocket.Accept();

                    Logger.LogProgress(LogSource.Listener, "Accepted connection from IP: " +
                        (newClientSocket.RemoteEndPoint as IPEndPoint).Address + ", Port: " +
                        (newClientSocket.RemoteEndPoint as IPEndPoint).Port);
                }
                catch (Exception ex)
                {
                    Logger.LogError(LogSource.Listener, "Failed to accept connection.", ex);
                    continue;
                }

                try
                {
                    // Creating a new connection
                    ServerClientConnection conn =
                        new ServerClientConnection(newClientSocket, serverConfiguration.ConnectionReadBufferSize);
                    conn.Logger = Logger;

                    // Creating a new player
                    PlayerCharacter pc = new PlayerCharacter(conn);

                    // Adding a new connection
                    serverCommunicationManager.AddNewConnection(conn);

                    // Adding a new player
                    serverWorldSimulation.AddNewPlayer(pc);
                }
                catch (Exception ex)
                {
                    Logger.LogError(LogSource.Listener, "Failed to add new connection or player", ex);
                }

            }
        }
    }
}