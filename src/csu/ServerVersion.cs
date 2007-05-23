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

                //TODO: Make backup

                try
                {
                    // Iterate through versions and upgrade
                    for (int i = verFrom.Order + 1; i <= verTo.Order; i++)
                        GetVersion(i).UpgradeToThisVersion(playerName);

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

        protected string storagePath = null;

        public string StoragePath
        {
            set { storagePath = value; }
        }

        public abstract void UpgradeToThisVersion(string playerName);

        public abstract void DowngradeToPreviousVersion(string playerName);
	
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

        public override void UpgradeToThisVersion(string playerName)
        {
            return; //Nothing
        }

        public override void DowngradeToPreviousVersion(string playerName)
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

        private string playerNameAppearance0_3_0 = "";
        private byte[] bArrayAppearance0_3_0 = new byte[7];
        private short[] sArrayLocation0_3_0 = new short[5];
        private string mapNameLocation0_3_0 = "";

        public override void UpgradeToThisVersion(string playerName)
        {
            // Create serializer / deserializer
            PlayerCharacterSerializer pcSer = new PlayerCharacterSerializer(storagePath);
            PlayerCharacterDeserializer pcDSer = new PlayerCharacterDeserializer(storagePath);

            // Load appearance 0.3.0
            pcDSer.Start(playerName, PlayerCharacterDataType.PCAppearance, "VER.1.0.0");
            playerNameAppearance0_3_0 = pcDSer.ReadString();
            for (int i = 0; i < 7; i++)
                bArrayAppearance0_3_0[i] = pcDSer.ReadByte();
            pcDSer.End();

            // Save appearance 0.4.0CTP1
            pcSer.Start(playerName, PlayerCharacterDataType.PCAppearance, "VER.1.0.0");
            pcSer.WriteValue(playerNameAppearance0_3_0);
            for (int i = 0; i < 7; i++)
                pcSer.WriteValue(bArrayAppearance0_3_0[i]);
            pcSer.End();

            
            // Load location 0.3.0
            pcDSer.Start(playerName, PlayerCharacterDataType.PCLocation, "VER.1.1.0");
            for (int i = 0; i < 5; i++)
                sArrayLocation0_3_0[i] = pcDSer.ReadShort();
            mapNameLocation0_3_0 = pcDSer.ReadString();
            pcDSer.End();

            // Save location 0.4.0CTP1
            pcSer.Start(playerName, PlayerCharacterDataType.PCLocation, "VER.1.1.0");
            for (int i = 0; i < 5; i++)
                pcSer.WriteValue(sArrayLocation0_3_0[i]);
            pcSer.WriteValue(mapNameLocation0_3_0);
            pcSer.End();
            
            // Load energies 0.3.0
            // Nothing - does not exist

            // Save energies 0.4.0CTP1
            pcSer.Start(playerName, PlayerCharacterDataType.PCEnergies, "VER.1.0.0");
            pcSer.WriteValue((short)5);
            pcSer.End();


            // Load inventory 0.3.0
            // Nothing - does not exist

            // Save inventory 0.4.0CTP1
            pcSer.Start(playerName, PlayerCharacterDataType.PCInventory, "VER.1.0.0");
            pcSer.WriteValue((byte)36);
            pcSer.WriteValue((byte)0);
            pcSer.End();


            // Load skills 0.3.0
            // Nothing - does not exist

            // Save skills 0.4.0CTP1
            pcSer.Start(playerName, PlayerCharacterDataType.PCSkills, "VER.1.0.0");
            for (int i = 0; i < 6; i++)
            {
                pcSer.WriteValue((byte)i);
                pcSer.WriteValue((uint)0);
            }
            pcSer.End();

        }

        public override void DowngradeToPreviousVersion(string playerName)
        {
            //TODO: Implement
        }
    }
}