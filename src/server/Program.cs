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
using System.Reflection;
using Calindor.Server.Maps;

namespace Calindor.Server
{
    public class Program
    {
        private static ServerListeningThread slThread = null;
        private static CommunicationManager commManager = null;
        private static WorldSimulation worldSim = null;

        public static void Main(string[] args)
        {
            // Starting server...
            
            ILogger logger = null;

            try
            {
                logger = new MultiThreadedLogger(System.IO.Directory.GetCurrentDirectory());
            }
            catch
            {
                // Logger not created... exiting
                return;
            }

            logger.LogProgress(LogSource.Server, "Calindor v" + ServerVersion.GetVersion());

            logger.LogProgress(LogSource.Server, "Starting up...");

            // Loading configuration
            ServerConfiguration conf = new ServerConfiguration();
            conf.Logger = logger;
            if (!conf.Load("./server_config.xml"))
            {
                logger.LogProgress(LogSource.Server, "Configuration not loaded. Exiting.");
                return;
            }

            // Loading maps
            MapManager mapManager = new MapManager(conf);
            mapManager.Logger = logger;
            if (!mapManager.LoadMaps())
            {
                logger.LogProgress(LogSource.Server, "Maps not scanned. Exiting.");
                return;
            }
            if (!mapManager.IsStartingMapLoaded())
            {
                logger.LogProgress(LogSource.Server, "Default map not loaded. Exiting.");
                return;
            }


            // Creating world simulation thread
            worldSim = new WorldSimulation(conf, mapManager);
            worldSim.Logger = logger;
            worldSim.StartSimulation();

            // Creating communication manager thread
            commManager = new CommunicationManager();
            commManager.Logger = logger;
            commManager.StartManager();
            

            // Creating server listening thread
            slThread = new ServerListeningThread(conf, commManager, worldSim);
            slThread.Logger = logger;
            slThread.StartListening();
        }
    }

    public class ServerVersion
    {
        public static string GetVersion()
        {
            try
            {
                Assembly thisAssembly = Assembly.GetExecutingAssembly();
                object[] attributes = thisAssembly.GetCustomAttributes(false);
                for (int i = 0; i < attributes.Length; i++)
                {
                    if (attributes[i] is AssemblyInformationalVersionAttribute)
                        return ((AssemblyInformationalVersionAttribute)attributes[i]).InformationalVersion;
                }

                return "NOT_VERSIONED";
            }
            catch
            {
                return "NOT_VERSIONED";
            }
        }
    }
}
