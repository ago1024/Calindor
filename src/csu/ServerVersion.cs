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
using System.IO;
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
                try
                {
                    if (!verTo.CheckFileVersions(playerName))
                    {
                        // UPGRADE
                        // Iterate through versions and upgrade
                        for (int i = verFrom.Order + 1; i <= verTo.Order; i++)
                            GetVersion(i).UpgradeToThisVersion(playerName);

                        logger.LogProgress(LogSource.Other, "UPDATE DONE: " + playerName);
                    }
                    else
                    {
                        // DO NOTHING
                        logger.LogProgress(LogSource.Other, "UP TO DATE: " + playerName);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(LogSource.Other, "UPDATE FAILED: " + playerName, null);
                    logger.LogError(LogSource.Other, "", ex);
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

        /// <summary>
        /// Checks if storage for player is in this version
        /// </summary>
        /// <param name="playerName">Player name to check</param>
        /// <returns>True if storage is in this version</returns>
        public bool CheckFileVersions(string playerName)
        {
            foreach (PlayerCharacterFileVersion pcFV in thisVersionFileVersions)
            {
                try
                {
                    pcDsr.Start(playerName, pcFV.Type, pcFV.Version); // Will throw if wrong file version
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
                catch (FileNotFoundException)
                {
                    return false;
                }
                finally
                {
                    pcDsr.End();
                }
            }

            return true;
        }

        #region Upgrade
        public virtual void UpgradeToThisVersion(string playerName)
        {
            if (previousVersion == null)
                throw new ArgumentException("previousVersion is null");

            // Check if not already in this version
            if (CheckFileVersions(playerName))
                return; // Nothing to do

            // Check if in previous version
            if (!previousVersion.CheckFileVersions(playerName))
                throw new InvalidOperationException("Storage for player " + playerName + " is not in correct state to upgrade from " 
                    + previousVersion.ServerVersionString + " to " + ServerVersionString);
            
            // All ok
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

        private FileVersion version;
        public FileVersion Version
        {
            get { return version; }
        }
	

        private PlayerCharacterFileVersion()
        {
        }

        public PlayerCharacterFileVersion(PlayerCharacterDataType type, FileVersion version)
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
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCAppearance, FileVersion.VER_1_0_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCLocation, FileVersion.VER_1_1_0));
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
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCAppearance, FileVersion.VER_1_0_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCLocation, FileVersion.VER_1_1_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCEnergies, FileVersion.VER_1_0_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCInventory, FileVersion.VER_1_0_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCSkills, FileVersion.VER_1_0_0));
        }

        protected override void upgradeToThisVersionImplementation(string playerName)
        {
            // Appearance - no changes            
            // Location - no changes
            
            // Energies - create
            try
            {
                pcSer.Start(playerName, PlayerCharacterDataType.PCEnergies, FileVersion.VER_1_0_0);
                pcSer.WriteValue((short)1);
            }
            finally
            {
                pcSer.End();
            }

            // Inventory - create
            try
            {
                pcSer.Start(playerName, PlayerCharacterDataType.PCInventory, FileVersion.VER_1_0_0);
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
                pcSer.Start(playerName, PlayerCharacterDataType.PCSkills, FileVersion.VER_1_0_0);
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

    public class ServerVersion0_4_0_CTP2 : ServerVersion
    {
        public override string ServerVersionString
        {
            get { return "0.4.0CTP2"; }
        }

        public ServerVersion0_4_0_CTP2()
        {
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCAppearance, FileVersion.VER_1_1_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCLocation, FileVersion.VER_1_2_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCEnergies, FileVersion.VER_1_0_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCInventory, FileVersion.VER_1_0_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCSkills, FileVersion.VER_1_0_0));
        }

        protected override void upgradeToThisVersionImplementation(string playerName)
        {
            string string_data = "";
            byte[] byteBuffer = new byte[10];
            
            // Appearance - add buffs byte
            try
            {
                pcDsr.Start(playerName, PlayerCharacterDataType.PCAppearance, FileVersion.VER_1_0_0);
                // Read name
                string_data = pcDsr.ReadString();
                // Read binary
                for (int i = 0; i < 7; i++)
                   byteBuffer[i] = pcDsr.ReadByte();
            }
            finally
            {
                pcDsr.End();
            }

            try
            {
                pcSer.Start(playerName, PlayerCharacterDataType.PCAppearance, FileVersion.VER_1_1_0);
                pcSer.WriteValue(string_data);
                for (int i = 0; i < 7; i++)
                    pcSer.WriteValue(byteBuffer[i]);
                pcSer.WriteValue((byte)0); //NEW VALUE
            }
            finally
            {
                pcSer.End();
            }
          
            // Location - add dimensions int
            try
            {
                pcDsr.Start(playerName, PlayerCharacterDataType.PCLocation, FileVersion.VER_1_1_0);
                // Read binary
                for (int i = 0; i < 10; i++)
                    byteBuffer[i] = pcDsr.ReadByte();
                // Read map name
                string_data = pcDsr.ReadString();

            }
            finally
            {
                pcDsr.End();
            }

            try
            {
                pcSer.Start(playerName, PlayerCharacterDataType.PCLocation, FileVersion.VER_1_2_0);
                for (int i = 0; i < 10; i++)
                    pcSer.WriteValue(byteBuffer[i]);
                pcSer.WriteValue(string_data);
                pcSer.WriteValue((int)1); // NEW VALUE
            }
            finally
            {
                pcSer.End();
            }

            // Energies - no changes
            // Inventory - no changes
            // Skills - no changes
        }

        protected override void downgradeToPreviousVersionImplementation(string playerName)
        {
            //TODO: Implement
        }
    }

    public class ServerVersion0_4_0_CTP3 : ServerVersion
    {
        public override string ServerVersionString
        {
            get { return "0.4.0CTP3"; }
        }

        public ServerVersion0_4_0_CTP3()
        {
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCAppearance, FileVersion.VER_1_1_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCLocation, FileVersion.VER_1_2_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCEnergies, FileVersion.VER_1_0_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCInventory, FileVersion.VER_1_0_0));
            thisVersionFileVersions.Add(new PlayerCharacterFileVersion(PlayerCharacterDataType.PCSkills, FileVersion.VER_1_0_0));
        }

        protected override void upgradeToThisVersionImplementation(string playerName)
        {
            // Appearance - no changes
            // Location - no changes
            // Energies - no changes
            // Inventory - no changes
            // Skills - no changes
        }

        protected override void downgradeToPreviousVersionImplementation(string playerName)
        {
            // Appearance - no changes
            // Location - no changes
            // Energies - no changes
            // Inventory - no changes
            // Skills - no changes
        }
    }

}