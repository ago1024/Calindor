/*
 * Copyright (C) 2007-2008 Krzysztof 'DeadwooD' Smiechowicz
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
using System.Threading;
using System.Globalization;
using Calindor.Server;

namespace Calindor.StorageUpdater
{
    public enum Operation
    {
        Nothing,
        Upgrade,
        Downgrade
    }

    public class Program
    {
        private static ServerVersions versions = null;
        private static bool shouldExitAfterParsingCommandLine = false;
        private static string versionFrom = null;
        private static string versionTo = null;
        private static Operation op = Operation.Nothing;

        private static void initializeVersions()
        {
            versions = new ServerVersions();
            versions.AddVersion(new ServerVersion0_3_0());
            versions.AddVersion(new ServerVersion0_4_0_CTP1());
            versions.AddVersion(new ServerVersion0_4_0_CTP2());
            versions.AddVersion(new ServerVersion0_4_0_CTP3());
            versions.AddVersion(new ServerVersion0_6_0());
        }

        private static void displayHelp()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("   /pv - displays informative version");
            Console.WriteLine("   /supports - lists supported server versions");
            Console.WriteLine("   /upgrade {versionFrom} {versionTo} - upgrades from versionFrom to versionTo");
        }

        private static void parseCMDLine(string[] args)
        {
            shouldExitAfterParsingCommandLine = true;

            if (args.Length == 0)
            {
                displayHelp();
                return;
            }

            // Display help
            if ((args[0] == "/h") || (args[0] == "--help") || (args[0] == "/?"))
            {
                displayHelp();
                return;
            }

            // Display informative product version
            if (args[0] == "/pv")
            {
                string version = CSUVersion.GetVersion();
                version = version.Replace(" ", ".");
                version = version.ToLower();
                Console.WriteLine(version);
                return;
            }

            // Display supported versions
            if (args[0] == "/supports")
            {
                IEnumerator<ServerVersion> versionsEnum = versions.Versions;
                
                versionsEnum.MoveNext();
                
                while ((versionsEnum.Current != null))
                {
                    Console.WriteLine(versionsEnum.Current.ServerVersionString);
                    versionsEnum.MoveNext();
                }
                
                return;
            }

            // Upgrade
            if (args[0] == "/upgrade")
            {
                if (args.Length != 3)
                {
                    Console.WriteLine("Correct syntaxt is: /upgrade versionFrom versionTo");
                    return;
                }

                versionFrom = args[1];
                versionTo = args[2];
                op = Operation.Upgrade;
                shouldExitAfterParsingCommandLine = false;
                
                return;
            }

            // Downgrade
            if (args[0] == "/downgrade")
            {
                // TODO: Implement
                Console.WriteLine("Downgrade option is not implemented");
                return;
            }
            
            // Uknown command
            Console.WriteLine("Unknown option!");
            displayHelp();
        }

        public static void Main(string[] args)
        {
            // Setting invariant culture
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;            
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            initializeVersions();

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
            catch (Exception ex)
            {
                // Logger not created... exiting
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Logger not created. Reason: " + ex.ToString());
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.ReadLine();
                return;
            }

            logger.LogProgress(LogSource.Other, "Calindor Storage Updater v" + CSUVersion.GetVersion());

            logger.LogProgress(LogSource.Other, "Starting up...");

            // Loading configuration
            ServerConfiguration conf = new ServerConfiguration();
            conf.Logger = logger;
            if (!conf.Load("./server_config.xml"))
            {
                logger.LogError(LogSource.Other, "Configuration not loaded. Exiting (press ENTER).", null);
                Console.ReadLine();
                return;
            }

            // Start
            logger.LogProgress(LogSource.Other, "BE SURE TO MANUALLY BACKUP STORAGE AT: " + conf.DataStoragePath);
            logger.LogProgress(LogSource.Other, "Press ENTER to continue");
            Console.ReadLine();

            if (op == Operation.Upgrade)
                versions.UpgradeStorage(versionFrom, versionTo, logger, conf.DataStoragePath);

            logger.LogProgress(LogSource.Other, "Finished. (press ENTER)");
            Console.ReadLine();
        }
    }

    public class CSUVersion
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
