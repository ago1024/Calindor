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
using System.IO;
using System.Xml;

namespace Calindor.Server
{
    public class ServerConfiguration
    {
        private ILogger logger = new DummyLogger();

        public ILogger Logger
        {
            get { return logger; }
            set { logger = value; }
        }

        private string bindIP = "0.0.0.0";

        public string BindIP
        {
            get { return bindIP; }
        }

        private int bindPort = 4242;

        public int BindPort
        {
            get { return bindPort; }
        }

        private int connectionReadBufferSize = 8192;
        public int ConnectionReadBufferSize
        {
            get { return connectionReadBufferSize; }
        }

        private string openingScreen = "Welcome.";
        public string OpeningScreen
        {
            get { return openingScreen; }
        }

        private bool checkProtocolVersion = true;
        public bool CheckProtocolVersion
        {
            get { return checkProtocolVersion; }
        }

        private bool logNormalOperation = false;
        public bool LogNormalOperation
        {
            get { return logNormalOperation; }
        }

        private string dataStoragePath = "";
        public string DataStoragePath
        {
            get { return dataStoragePath; }
        }

        private string mapsPath = "";
        public string MapsPath
        {
            get { return mapsPath; }
        }

        private StartingPointConfigurationInfo startingPoint = new StartingPointConfigurationInfo();
        public StartingPointConfigurationInfo StartingPoint
        {
            get { return startingPoint; }
        }

        private bool enableTestCommands;
        public bool EnableTestCommands
        {
            get { return enableTestCommands; }
        }

        private string[] adminUsers;
        public string[] AdminUsers
        {
            get { return adminUsers; }
        }

        public bool IsAdminUser(string name)
        {
            foreach (string user in adminUsers)
            {
                if (user.ToLower() == name.ToLower())
                    return true;
            }
            return false;
        }

        public bool Load(string path)
        {
            logger.LogProgress(LogSource.Server, "Loading configuration...");

            if (!File.Exists(path))
            {
                logger.LogError(LogSource.Server, "Configuration file not found at path " + path, null);
                return false;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                XmlNode bindIPElement = doc.SelectSingleNode("/configuration/bindIP");
                bindIP = bindIPElement.Attributes["value"].Value;

                XmlNode bindPortElement = doc.SelectSingleNode("/configuration/bindPort");
                bindPort = Convert.ToInt32(bindPortElement.Attributes["value"].Value);

                XmlNode readBufferSizeElement = doc.SelectSingleNode("/configuration/readBufferSize");
                connectionReadBufferSize = Convert.ToInt32(readBufferSizeElement.Attributes["value"].Value);

                XmlNode openingScreenElement = doc.SelectSingleNode("/configuration/openingScreen");
                openingScreen = openingScreenElement.Attributes["value"].Value;

                XmlNode checkProtocolVersionElement = doc.SelectSingleNode("/configuration/checkProtocolVersion");
                checkProtocolVersion = Convert.ToBoolean(checkProtocolVersionElement.Attributes["value"].Value);

                XmlNode logNormalOperationElement = doc.SelectSingleNode("/configuration/logNormalOperation");
                logNormalOperation = Convert.ToBoolean(logNormalOperationElement.Attributes["value"].Value);

                XmlNode dataStoragePathElement = doc.SelectSingleNode("/configuration/dataStoragePath");
                dataStoragePath = dataStoragePathElement.Attributes["value"].Value;

                XmlNode mapsPathElement = doc.SelectSingleNode("/configuration/mapsPath");
                mapsPath = mapsPathElement.Attributes["value"].Value;

                XmlNode startingMapElement = doc.SelectSingleNode("/configuration/startingMap");
                startingPoint.MapName = startingMapElement.Attributes["name"].Value;
                startingPoint.StartX = Convert.ToInt16(startingMapElement.Attributes["startX"].Value);
                startingPoint.StartY = Convert.ToInt16(startingMapElement.Attributes["startY"].Value);
                startingPoint.Deviation = Convert.ToInt16(startingMapElement.Attributes["deviation"].Value);

                XmlNode enableTestCommandsElement = doc.SelectSingleNode("/configuration/enableTestCommands");
                enableTestCommands = Convert.ToBoolean(enableTestCommandsElement.Attributes["value"].Value);

                XmlNode adminUsersElement = doc.SelectSingleNode("/configuration/adminUsers");
                if (adminUsersElement != null)
                    adminUsers = Convert.ToString(adminUsersElement.Attributes["value"].Value).Split(',');
                else
                    adminUsers = new string[0];

            }
            catch (Exception ex)
            {
                logger.LogError(LogSource.Server, "Failed to parse configuration file at path " + path, ex);
                return false;
            }

            logger.LogProgress(LogSource.Server, "Configuration loaded.");

            return true;
        }

    }

    public class StartingPointConfigurationInfo
    {
        private string mapName;
        public string MapName
        {
            get { return mapName; }
            set { mapName = value; }
        }

        private short startX;
        public short StartX
        {
            get { return startX; }
            set { startX = value; }
        }

        private short startY;
        public short StartY
        {
            get { return startY; }
            set { startY = value; }
        }

        private short deviation;

        public short Deviation
        {
            get { return deviation; }
            set
            {
                deviation = value;
                if (deviation < 0)
                    deviation *= -1;
            }
        }
	
	
    }
}
