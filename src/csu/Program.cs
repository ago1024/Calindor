using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
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
        }

        private static void parseCMDLine(string[] args)
        {
            shouldExitAfterParsingCommandLine = true;

            if (args.Length == 0)
                return;

            // Display informative product version
            if (args[0] == "/pv")
            {
                string version = CSUVersion.GetVersion();
                version = version.Replace(" ", ".");
                version = version.ToLower();
                Console.WriteLine(version);
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
            }

            // Downgrade
            if (args[0] == "/downgrade")
            {
            }
        }

        public static void Main(string[] args)
        {
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

            if (op == Operation.Upgrade)
                versions.UpgradeStorage(versionFrom, versionTo, logger, conf.DataStoragePath);
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
