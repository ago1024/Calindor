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
using Calindor.Server.Maps;

namespace Calindor.Server.Entities
{
    public class EntityLocation
    {
        private short[] innerData = new short[5];
        
        public short X
        {
            get { return innerData[0]; }
            set { innerData[0] = value; }
        }

        public short Y
        {
            get { return innerData[1]; }
            set { innerData[1] = value; }
        }

        public short Z
        {
            get { return innerData[2]; }
            set { innerData[2] = value; }
        }

        public short Rotation
        {
            get { return innerData[3]; }
            set { innerData[3] = value; }
        }

        public bool IsSittingDown
        {
            get { if (innerData[4] == 1) return true; else return false; }
            set { if (value) innerData[4] = 1; else innerData[4] = 0; }
        }

        /// <summary>
        /// Name of the map deserialized from file
        /// </summary>
        private string loadedMapName;

        /// <summary>
        /// Should only be used at login time
        /// </summary>
        public string LoadedMapMame
        {
            get { return loadedMapName; }
        }

        public string CurrentMapName
        {
            get 
            {
                if (CurrentMap == null)
                    return "__NULL__";
                else
                    return CurrentMap.Name;
            }
        }

        private Map currentMap = null;
        public Map CurrentMap
        {
            get { return currentMap; }
            set { currentMap = value; }
        }

        public virtual void Serialize(ISerializer sr)
        {
            for (int i = 0; i < innerData.Length; i++)
                sr.WriteValue(innerData[i]);
            sr.WriteValue(CurrentMapName);
        }

        public virtual void Deserialize(IDeserializer dsr)
        {
            for (int i = 0; i < innerData.Length; i++)
                innerData[i] = dsr.ReadShort();
            loadedMapName = dsr.ReadString();
        }

        public void RatateBy(short additionalRotation)
        {
            innerData[3] = (short)((int)(innerData[3] + additionalRotation) % 360);
            if (innerData[3] < 0)
                innerData[3] += 360;
        }
    }
}