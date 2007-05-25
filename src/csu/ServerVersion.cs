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
            if (versions.Count > 0)
                ver.PreviousVersion = versions[versions.Count - 1];

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

            // Set data storage path to all version
            foreach (ServerVersion sv in versions)
                sv.StoragePath = dataStoragePath;

            string playerName = null;

            while ((playerName = it.GetNextPlayerCharacterName()) != null)
            {
                /*
                 * Foreach character:
                 * 1. make backup
                 * 2. upgrade through all versions
                 * 3. remove backup
                 */

                try
                {
                    // Iterate through versions and upgrade
                    for (int i = verFrom.Order + 1; i <= verTo.Order; i++)
                        GetVersion(i).UpgradeToThisVersion(playerName);
                }
                catch (Exception ex)
                {
                    logger.LogError(LogSource.Other, "Failed to update player " + playerName, ex);
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

        protected string storagePath = null;

        protected PlayerCharacterDeserializer pcDsr = null;
        protected PlayerCharacterSerializer pcSer = null;

        public string StoragePath
        {
            set 
            { 
                storagePath = value;
                pcDsr = new PlayerCharacterDeserializer(storagePath);
                pcSer = new PlayerCharacterSerializer(storagePath);
            }
        }

        protected ServerVersion previousVersion = null;
        public ServerVersion PreviousVersion
        {
            set { previousVersion = value; }
        }

        protected PlayerCharacterFileVersionList thisVersionFileVersions =
            new PlayerCharacterFileVersionList();

        private void checkFileVersions(string playerName)
        {
            foreach (PlayerCharacterFileVersion pcFV in thisVersionFileVersions)
            {
                try
                {
                    pcDsr.Start(playerName, pcFV.Type, pcFV.Version); // Will throw if wrong file version
                }
                finally
                {
                    pcDsr.End();
                }
            }
        }

        #region Upgrade
        public virtual void UpgradeToThisVersion(string playerName)
        {
            if (previousVersion == null)
                throw new ArgumentException("previousVersion is null");

            // TODO: check if not already in this version, if() return

            // Check if in previous version
            // TODO: add if () throw
            previousVersion.checkFileVersions(playerName);

            upgradeToThisVersionImplementation(playerName);
        }

        protected abstract void upgradeToThisVersionImplementation(string playerName);
        #endregion

        #region Downgrade
        // TODO: implement
        public virtual void DowngradeToPreviousVersion(string playerName)
        {
        }

        protected abstract void downgradeToPreviousVersionImplementation(string playerName);
        #endregion

    }

    public class ServerVersionList : List<ServerVersion>
    {
    }

    public class PlayerCharacterFileVersion
    {
        private PlayerCharacterDataType type;

        public PlayerCharacterDataType Type
        {
            get { return type; }
        }

        private string version;

        public string Version
        {
            get { return version; }
        }

        private PlayerCharacterFileVersion()
        {
        }

        public PlayerCharacterFileVersion(PlayerCharacterDataType type, string version)
        {
            this.type = type;
            this.version = version;
        }
	
    }

    public class PlayerCharacterFileVersionList : List<PlayerCharacterFileVersion>
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
        
        public ServerVersion0_3_0()
        {
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCAppearance, "VER.1.0.0"));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCLocation, "VER.1.1.0"));
        }

        protected override void upgradeToThisVersionImplementation(string playerName)
        {
            return; //Nothing
        }

        protected override void downgradeToPreviousVersionImplementation(string playerName)
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

        public ServerVersion0_4_0_CTP1()
        {
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCAppearance, "VER.1.0.0"));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCLocation, "VER.1.1.0"));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCEnergies, "VER.1.0.0"));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCInventory, "VER.1.0.0"));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCSkills, "VER.1.0.0"));
        }

        protected override void upgradeToThisVersionImplementation(string playerName)
        {
            // Appearance - no changes            
            // Location - no changes
            
            // Energies - create
            try
            {
                pcSer.Start(playerName, PlayerCharacterDataType.PCEnergies, "VER.1.0.0");
                pcSer.WriteValue((short)1);
            }
            finally
            {
                pcSer.End();
            }

            // Inventory - create
            try
            {
                pcSer.Start(playerName, PlayerCharacterDataType.PCInventory, "VER.1.0.0");
                pcSer.WriteValue((byte)36);
                pcSer.WriteValue((byte)0);
            }
            finally
            {
                pcSer.End();
            }

            // Skills - create
            try
            {
                pcSer.Start(playerName, PlayerCharacterDataType.PCSkills, "VER.1.0.0");
                for (int i = 0; i < 6; i++)
                {
                    pcSer.WriteValue((byte)i);
                    pcSer.WriteValue((uint)0);
                }
            }
            finally
            {
                pcSer.End();
            }

        }

        protected override void downgradeToPreviousVersionImplementation(string playerName)
        {
            //TODO: Implement
        }
    }
}