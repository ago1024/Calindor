/*
 * Copyright (C) 2007 Krzysztof 'DeadwooD' Smiechowicz
 * Original project page: http://sourceforge.net/projects/calindor/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 */
using System.IO;
using System;

namespace Calindor.StorageUpdater
{
    public class PlayerCharacterDataStoreIterator
    {
        private DirectoryInfo[] topLevelDirs = null;
        private DirectoryInfo[] playerDirs = null;
        private int topLevelDirsIndex = 0;
        private int playerDirsIndex = 0;

        public PlayerCharacterDataStoreIterator()
        {
        }

        public void Initialize(string storagePath)
        {
            if (!Directory.Exists(storagePath))
                throw new ArgumentException(storagePath + " does not exist");

            DirectoryInfo storageDir = new DirectoryInfo(storagePath);

            topLevelDirs = storageDir.GetDirectories();
            topLevelDirsIndex = 0;
        }

        public string GetNextPlayerCharacterName()
        {
            if (topLevelDirs == null)
                throw new NullReferenceException("topLevelDirs");

            if (playerDirs == null || playerDirs.Length == playerDirsIndex)
            {
                // Move to next top level directory
                if (topLevelDirs.Length == topLevelDirsIndex)
                    return null;

                playerDirs = topLevelDirs[topLevelDirsIndex++].GetDirectories();
                playerDirsIndex = 0;
            }

            return playerDirs[playerDirsIndex++].Name;
        }
    }
}