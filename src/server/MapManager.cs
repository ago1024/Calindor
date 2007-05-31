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
using Calindor.Server;
using System.IO;
using Calindor.Server.Entities;

namespace Calindor.Server.Maps
{
    public class MapManager
    {
        ServerConfiguration serverConfiguration = null;

        private ILogger logger = new DummyLogger();

        public ILogger Logger
        {
            get { return logger; }
            set { logger = value; }
        }

        private MapList availableMaps = new MapList();
        private MapNameMapDictionary mapsByName = new MapNameMapDictionary();

        private MapManager()
        {
        }

        public MapManager(ServerConfiguration conf)
        {
            if (conf == null)
                throw new ArgumentNullException("conf");

            serverConfiguration = conf;
        }

        public void AddMap(Map m)
        {
            if (!mapsByName.ContainsKey(m.Name))
            {
                availableMaps.Add(m);
                mapsByName.Add(m.Name, m);
            }
        }

        public Map GetMapByName(string name)
        {
            if (mapsByName.ContainsKey(name.ToLower()))
                return mapsByName[name.ToLower()];
            else
                return null;
        }

        public bool LoadMaps()
        {
            logger.LogProgress(LogSource.Server, "Loading maps...");
            
            availableMaps.Clear();
            mapsByName.Clear();

            if (!Directory.Exists(serverConfiguration.MapsPath))
            {
                logger.LogError(LogSource.Server, "Maps path (" + serverConfiguration.MapsPath + ") does not exist", null);
                return false;
            }

            try
            {
                string[] elmFiles = Directory.GetFiles(serverConfiguration.MapsPath, "*.elm");
                foreach (string elmFile in elmFiles)
                {
                    Map m = new Map(elmFile);
                    m.LoadMapData();
                    AddMap(m);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(LogSource.Server, "Error while loading maps from " + serverConfiguration.MapsPath, ex);
                return false;
            }

            logger.LogProgress(LogSource.Server, "Maps loaded.");

            return true;
        }

        public bool IsStartingMapLoaded()
        {
            if (StartPointMap == null)
                return false;
            else
                return true;
        }

        public bool IsStartPointWalkable()
        {
            if (!IsStartingMapLoaded())
                return false;

            return StartPointMap.IsLocationWalkable(StartPointX, StartPointY);
        }

        public string StartPointMapName
        {
            get { return serverConfiguration.StartingPoint.MapName; }
        }

        public Map StartPointMap
        {
            get { return GetMapByName(StartPointMapName); }
        }
        public short StartPointX
        {
            get { return serverConfiguration.StartingPoint.StartX; }
        }

        public short StartPointY
        {
            get { return serverConfiguration.StartingPoint.StartY; }
        }

        public short StartPointDeviation
        {
            get { return serverConfiguration.StartingPoint.Deviation; }
        }

        private void addEntityToNewMap(Entity en, EntityLocation currentLocation, string newMapName, short newX, short newY)
        {
            Map newMap = GetMapByName(newMapName);

            if (newMap == null) /*No such map: Should not happen!*/
            {
                logger.LogWarning(LogSource.World, "Move " + en.Name + " to map (" + newMapName + ") failed. Map not found. Moving to start map!", null);
                newMap = StartPointMap;
                newX = StartPointX;
                newY = StartPointY;
            }

            if (!newMap.IsLocationWalkable(newX, newY)) /*Destination location not walkable: Should not happen!*/
            {
                logger.LogWarning(LogSource.World, "Move " + en.Name + " to map (" + newMap.Name + "(" 
                    + newX + ", " + newY +")) failed. Destination location not walkable. Moving to start map!", null);
                newMap = StartPointMap;
                newX = StartPointX;
                newY = StartPointY;
            }


            newMap.AddEntity(en);

            // Change player location
            currentLocation.CurrentMap = newMap;
            currentLocation.X = newX;
            currentLocation.Y = newY;
        }

        public void ChangeMapForEntity(Entity en, EntityLocation currentLocation, string newMapName, short newX, short newY)
        {
            ChangeMapForEntity(en, currentLocation, newMapName, false, newX, newY);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="newMapName"></param>
        /// <param name="isLogIn"></param>
        /// <param name="newX">Location on new map</param>
        /// <param name="newY">Location on new map</param>
        public void ChangeMapForEntity(Entity en, EntityLocation currentLocation, string newMapName, bool isLogIn, short newX, short newY)
        {
            if ((isLogIn) && currentLocation.CurrentMap == null)
            {
                // Login, only add (to avoid Warning from RemovePlayerFromHisMap)
                addEntityToNewMap(en, currentLocation, newMapName, newX, newY);
            }
            else
            {
                // Remove from old
                RemoveEntityFromItsMap(en, currentLocation);

                // Add to new
                addEntityToNewMap(en, currentLocation, newMapName, newX, newY);
            }
                
        }


        /// <summary>
        /// Removes entity from his current map
        /// </summary>
        /// <param name="?"></param>
        ///
        public void RemoveEntityFromItsMap(Entity en, EntityLocation currentLocation)
        {
            Map oldMap = currentLocation.CurrentMap;
                       
            if (oldMap != null)
                oldMap.RemoveEntity(en);
            else
                logger.LogWarning(LogSource.World, "Player (" + en.Name+ ") not connected with map.", null);

            currentLocation.CurrentMap = null;
        }
    }

    public class Map
    {
        private string pathToMap;
        
        // Map data
        private int sizeX = 0;
        private int sizeY = 0;
        private byte[,] mapData = null;

        // Pathfinder
        Pathfinder pathfinder = null;
        PathfinderParameters pathfinderParams = null;
        

        public string Name
        {
            get { return Path.GetFileName(pathToMap).ToLower() ; }
        }

        public string ServerFileName
        {
            get { return Path.GetFileName(pathToMap); }
        }

        public string ClientFileName
        {
            get { return "./maps/" + Path.GetFileName(pathToMap) ; }
        }

        private EntityList entitiesOnMap = new EntityList();

        private Map()
        {
        }

        public Map(string pathToMap)
        {
            this.pathToMap = pathToMap;
        }

        public bool LoadMapData()
        {
            BinaryReader br = new BinaryReader(new FileStream(pathToMap, FileMode.Open, FileAccess.Read));
            
            try
            {
                // Reading header
                byte[] header = br.ReadBytes(124);

                // Checking name
                if ((header[0] != 'e') || (header[1] != 'l') || (header[2] != 'm') || (header[3] != 'f'))
                {
                    throw new InvalidDataException("Not an 'elmf' format");
                }

                // Getting map size
                int xTileCount = BitConverter.ToInt32(header, 4);
                int yTileCount = BitConverter.ToInt32(header, 8);

                sizeX = xTileCount * 6;
                sizeY = yTileCount * 6;

                int tileMapOffeset = BitConverter.ToInt32(header, 12);
                int heightMapOffset = BitConverter.ToInt32(header, 16);
                
                byte[] temp = null;
                temp = br.ReadBytes(xTileCount * yTileCount); // read tile map
                temp = br.ReadBytes(sizeX * sizeY); // read height map

                mapData = new byte[sizeX, sizeY];

                // Copy map data to final array
                for (int y = 0; y < sizeY; y++)
                    for (int x = 0; x < sizeX; x++)
                        mapData[x, y] = temp[y * sizeX + x];

                pathfinder = new Pathfinder(mapData);
                pathfinderParams = new PathfinderParameters();
            }
            finally
            {
                if (br != null)
                    br.Close();
            }


            return true;
        }

        public bool IsLocationWalkable(short x, short y)
        {
            if (pathfinder == null)
                throw new InvalidOperationException("Pathfinder not created");
            
            return pathfinder.IsLocationWalkable(x, y);
            
        }

        public bool IsLocationOccupied(short x, short y, int dimension)
        {
            foreach (Entity en in entitiesOnMap)
            {
                if (en.LocationDimension != dimension)
                    continue;

                if ((en.LocationX == x) && (en.LocationY == y))
                    return true;
            }

            return false;
        }

        public WalkPath CalculatePath(short startX, short startY, short endX, short endY)
        {
            if (pathfinder == null)
                throw new InvalidOperationException("Pathfinder not created");

            pathfinderParams.StartX = startX;
            pathfinderParams.StartY = startY;
            pathfinderParams.EndX = endX;
            pathfinderParams.EndY = endY;
            pathfinderParams.MaxIterations = 1000;

            return pathfinder.CalculatePath(pathfinderParams);

        }

        public void AddEntity(Entity en)
        {
            if (!entitiesOnMap.Contains(en))
                entitiesOnMap.Add(en);
        }

        public void RemoveEntity(Entity en)
        {
            if (entitiesOnMap.Contains(en))
                entitiesOnMap.Remove(en);
        }

        public IEnumerator<Entity> EntitiesOnMap
        {
            get { return entitiesOnMap.GetEnumerator(); }
        }
    }

    public class MapList : List<Map>
    { 
    }

    public class MapNameMapDictionary : Dictionary<string, Map>
    {
    }
}