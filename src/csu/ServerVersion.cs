using System;
using System.Collections.Generic;
using Calindor.Server;

namespace Calindor.StorageUpdater
{
    public class ServerVersions
    {
        ServerVersionList versions = new ServerVersionList();

        public IEnumerator<ServerVersion> Versions
        {
            get { return versions.GetEnumerator(); }
        }

        public void AddVersion(ServerVersion ver)
        {
            versions.Add(ver);
            ver.Order = versions.Count - 1;
        }

        public ServerVersion GetVersion(string version)
        {
            foreach (ServerVersion sv in versions)
                if (sv.ServerVersionString == version)
                    return sv;

            return null;
        }

        public ServerVersion GetVersion(int order)
        {
            if (order < 0 || order >= versions.Count)
                return null;

            return versions[order];
        }

        public void UpgradeStorage(string versionFrom, string versionTo, ILogger logger, string dataStoragePath)
        {
            logger.LogProgress(LogSource.Other, "Upgrade starts...");

            // Checks
            ServerVersion verFrom = GetVersion(versionFrom);
            if (verFrom == null)
            {
                logger.LogError(LogSource.Other, "VersionFrom(" + versionFrom + ") is not supported.", null);
                return;
            }

            ServerVersion verTo = GetVersion(versionTo);
            if (verTo == null)
            {
                logger.LogError(LogSource.Other, "VersionTo(" + versionTo + ") is not supported.", null);
                return;
            }

            if (verFrom.Order >= verTo.Order)
            {
                logger.LogError(LogSource.Other, "VersionFrom(" + verFrom.ServerVersionString 
                    + ") must be lesser than VersionTo(" + verTo.ServerVersionString + ")", null);
                return;
            }

            // Checks ok


            PlayerCharacterDataStoreIterator it = new PlayerCharacterDataStoreIterator();
            it.Initialize(dataStoragePath);

            string playerName = null;

            while ((playerName = it.GetNextPlayerCharacterName()) != null)
            {
                /*
                 * Foreach character:
                 * 1. make backup
                 * 2. upgrade through all versions
                 * 3. remove backup
                 */

                //TODO: Make backup

                try
                {
                    // Iterate through versions and upgrade
                    for (int i = verFrom.Order + 1; i <= verTo.Order; i++)
                        GetVersion(i).UpgradeToThisVersion();

                    // TODO: Delete backup
                }
                catch (Exception ex)
                {
                    logger.LogError(LogSource.Other, "Failed to update player " + playerName, ex);

                    // TODO: Restore backup
                }

            }

            logger.LogProgress(LogSource.Other, "Upgrade completed.");
        }

        public void DowngradeStorage(string versionFrom, string versionTo)
        {
        }
    }

    public abstract class ServerVersion
    {
        private int order;

        public int Order
        {
            get { return order; }
            set { order = value; }
        }
	
        public abstract string ServerVersionString
        {
            get;
        }

        public abstract void UpgradeToThisVersion();

        public abstract void DowngradeToPreviousVersion();
	
    }

    public class ServerVersionList : List<ServerVersion>
    {
    }

    /*
     * 
     * Implementations
     *
     */

    public class ServerVersion0_3_0 : ServerVersion
    {
        public override string ServerVersionString
        {
            get { return "0.3.0"; }
        }

        public override void UpgradeToThisVersion()
        {
            return; //Nothing
        }

        public override void DowngradeToPreviousVersion()
        {
            return; //Nothing
        }
    }

    public class ServerVersion0_4_0_CTP1 : ServerVersion
    {
        public override string ServerVersionString
        {
            get { return "0.4.0CTP1"; }
        }

        public override void UpgradeToThisVersion()
        {
            //TODO: Implement
            Console.WriteLine("Upgraded to 0.4.0CTP1");
        }

        public override void DowngradeToPreviousVersion()
        {
            //TODO: Implement
        }
    }
}