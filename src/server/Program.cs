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
using System.IO;
using System.Globalization;
using System.Threading;
using Calindor.Server.Maps;


namespace Calindor.Server
{
    public class Program
    {
        private static ServerListeningThread slThread = null;
        private static CommunicationManager commManager = null;
        private static WorldSimulation worldSim = null;

        private static bool shouldExitAfterParsingCommandLine = false;

        private static void parseCMDLine(string[] args)
        {
            if (args.Length == 0)
                return;

            // Display informative product version
            if (args[0] == "/pv")
            {
                string version = ServerVersion.GetVersion();
                version = version.Replace(" ", ".");
                version = version.ToLower();
                Console.WriteLine(version);
                shouldExitAfterParsingCommandLine = true;
            }
        }

        public static void Main(string[] args)
        {
            // Starting server...

            // Setting up culture
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        
            // Setting working directory
            try
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Directory.SetCurrentDirectory(workingDirectory);
            }
            catch (Exception ex)
            {
                // Could not set working directory. Exit.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Working directory not set. Reason: " + ex.ToString());
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.ReadLine();
                return;
            }

            // Parsing command line
            parseCMDLine(args);

            if (shouldExitAfterParsingCommandLine)
                return;

            // Creating logger
            ILogger logger = null;

            try
            {
                logger = new MultiThreadedLogger(Directory.GetCurrentDirectory());
            }
            catch(Exception ex)
            {
                // Logger not created... exiting
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Logger not created. Reason: " + ex.ToString());
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.ReadLine();
                return;
            }

            logger.LogProgress(LogSource.Server, "Calindor v" + ServerVersion.GetVersion());

            logger.LogProgress(LogSource.Server, "Starting up...");

            // Loading configuration
            ServerConfiguration conf = new ServerConfiguration();
            conf.Logger = logger;
            if (!conf.Load("./server_config.xml"))
            {
                logger.LogError(LogSource.Server, "Configuration not loaded. Exiting (press ENTER).", null);
                Console.ReadLine();
                return;
            }

            // Loading maps
            MapManager mapManager = new MapManager(conf);
            mapManager.Logger = logger;
            if (!mapManager.LoadMaps())
            {
                logger.LogError(LogSource.Server, "Maps not scanned. Exiting (press ENTER).", null);
                Console.ReadLine();
                return;
            }
            if (!mapManager.IsStartingMapLoaded())
            {
                logger.LogError(LogSource.Server, "Default map not loaded. Exiting (press ENTER).", null);
                Console.ReadLine();
                return;
            }
            if (!mapManager.IsStartPointWalkable())
            {
                logger.LogError(LogSource.Server, "Default start point not walkable. Exiting (press ENTER).", null);
                Console.ReadLine();
                return;
            }


            // Creating world simulation thread
            worldSim = new WorldSimulation(conf, mapManager);
            worldSim.Logger = logger;
            if (!worldSim.StartSimulation())
            {
                logger.LogError(LogSource.Server, "WorldSimuation not started. Exiting  (press ENTER).", null);
                Console.ReadLine();
                return;
            }

            // Creating communication manager thread
            commManager = new CommunicationManager();
            commManager.Logger = logger;
            commManager.LogNormalOperation = conf.LogNormalOperation;
            commManager.StartManager();
            

            // Creating server listening thread
            slThread = new ServerListeningThread(conf, commManager, worldSim);
            slThread.Logger = logger;
            if (!slThread.StartListening())
            {
                logger.LogError(LogSource.Server, "Listener not started. Exiting  (press ENTER).", null);
                worldSim.StopSimulation();
                commManager.StopManager();
                Console.ReadLine();
                return;
            }

            logger.LogProgress(LogSource.Server, "Server started.");

            while (worldSim.IsRunning)
            {
                Thread.Sleep(100);
            }

            worldSim.StopSimulation();
            commManager.StopManager();
            slThread.StopListening();
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
