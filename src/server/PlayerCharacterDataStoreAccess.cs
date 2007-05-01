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
using System.IO;
using Calindor.Server.Entities;
using Calindor.Server.Serialization;

namespace Calindor.Server
{
    public class PlayerCharacterDataStoreAccess
    {
        protected FileStream fs = null;
        protected string storagePath = "";

        protected bool exists(string playerName)
        {
            playerName = playerName.ToLower();
            if (Directory.Exists(buildPlayerPath(playerName)))
                return true;
            else
                return false;
        }

        protected string buildPlayerPath(string playerName)
        {
            playerName = playerName.ToLower();
            string firstLetter = playerName.Substring(0, 1);
            string withNameLetterDirectory = Path.Combine(storagePath, firstLetter.ToLower());
            string withNameDirectory = Path.Combine(withNameLetterDirectory, playerName);
            return withNameDirectory;
        }
    }

    public class PlayerCharacterAuthentication : PlayerCharacterDataStoreAccess
    {
        private PlayerCharacterAuthentication()
        { 
        }
        
        public PlayerCharacterAuthentication(string storagePath)
        {
            this.storagePath = storagePath;
        }

        // TODO: Not safe. May throw an exception
        public bool Exists(string playerName)
        {
            return exists(playerName);
        }

        public bool IsStorageAccessible()
        {
            try
            {
                if (storagePath == null)
                    return false;

                if (!Directory.Exists(storagePath))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsAcceptablePlayerName(string playerName)
        {
            if ((playerName == null) || (playerName == ""))
                return false;

            if (playerName.Length < 3)
                return false;

            if (playerName.Length > 15)
                return false;

            if (!Char.IsLetter(playerName, 0))
                return false;

            for (int i = 0; i < playerName.Length; i++)
                if (!(Char.IsLetterOrDigit(playerName, i) || playerName[i] == '_'))
                    return false;

            return true;
        }

        protected string readPassword(string playerName)
        {
            playerName = playerName.ToLower();

            FileStream fs = null;
            BinaryReader br = null;

            try
            {
                fs = new FileStream(
                    Path.Combine(buildPlayerPath(playerName), playerName + ".auth"),
                    FileMode.Open, FileAccess.Read);
                br = new BinaryReader(fs);
                return br.ReadString();
            }
            finally
            {
                if (br != null)
                    br.Close();
                else
                    if (fs != null)
                    {
                        fs.Close();
                        fs.Dispose();
                    }
            }
        }

        protected void writePassword(string playerName, string password)
        {
            playerName = playerName.ToLower();

            FileStream fs = null;
            BinaryWriter bw = null;

            try
            {
                fs = new FileStream(
                    Path.Combine(buildPlayerPath(playerName), playerName + ".auth"),
                    FileMode.Create, FileAccess.Write);
                bw = new BinaryWriter(fs);
                bw.Write(password);
            }
            finally
            {
                if (bw != null)
                    bw.Close();
                else
                    if (fs != null)
                    {
                        fs.Close();
                        fs.Dispose();
                    }
            }
        }

        public void Create(string playerName, string password)
        {
            if (!Exists(playerName))
                Directory.CreateDirectory(buildPlayerPath(playerName));

            writePassword(playerName, password);

        }

        public bool Authenticate(string playerName, string password)
        {
            if (!exists(playerName))
                throw new PlayerCharacterDoesNotExistException(playerName);

            if (readPassword(playerName) != password)
                return false;
            else
                return true;
        }

        public bool ChangePassword(string playerName, string currentPassword, string newPassword)
        {
            if (!exists(playerName))
                throw new PlayerCharacterDoesNotExistException(playerName);

            if (readPassword(playerName) == currentPassword)
            {
                writePassword(playerName, newPassword);
                return true;
            }
            else
                return false;
        }
    }

    public enum PlayerCharacterDataType
    {
        PCAppearance    =   0,
        PCAttributes    =   1,
        PCLocation      =   2,
        PCInventory     =   3,
        PCSkills        =   4,
    }

    public class PlayerCharacterDoesNotExistException : ApplicationException
    {
        private string playerName = "";

        public PlayerCharacterDoesNotExistException(string playerName)
        {
            this.playerName = playerName;
        }

        public override string Message
        {
            get
            {
                return "Player " + playerName + " does not exist.";
            }
        }
    }
    public class PlayerCharacterSerializer : PlayerCharacterDataStoreAccess, ISerializer
    {
        private BinaryWriter bw = null;

        public PlayerCharacterSerializer(string storagePath)
        {
            this.storagePath = storagePath;
        }

        public void Start(string playerName, PlayerCharacterDataType type, string fileVer)
        {
            playerName = playerName.ToLower();

            if (!exists(playerName))
                throw new PlayerCharacterDoesNotExistException(playerName);

            End(); // Just in case

            string withNameDirectory = buildPlayerPath(playerName);

            string extention = ".null";
            switch (type)
            {
                case (PlayerCharacterDataType.PCAppearance):
                    extention = ".appearance";
                    break;
                case (PlayerCharacterDataType.PCAttributes):
                    extention = ".attributes";
                    break;
                case(PlayerCharacterDataType.PCLocation):
                    extention = ".location";
                    break;
                case(PlayerCharacterDataType.PCInventory):
                    extention = ".inventory";
                    break;
                case (PlayerCharacterDataType.PCSkills):
                    extention = ".skills";
                    break;
                default:
                    throw new ArgumentException("Unrecognized type of player data");
            }

            // Open
            string filePath = Path.Combine(withNameDirectory, playerName + extention);
            fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            bw = new BinaryWriter(fs);

            // Write version
            WriteValue(fileVer);
        }

        public void WriteValue(short data)
        {
            bw.Write(data);
        }

        public void WriteValue(ushort data)
        {
            bw.Write(data);
        }
        
        public void WriteValue(sbyte data)
        {
            bw.Write(data);
        }

        public void WriteValue(string data)
        {
            bw.Write(data);
        }

        public void WriteValue(int data)
        {
            bw.Write(data);
        }

        public void WriteValue(uint data)
        {
            bw.Write(data);
        }

        public void WriteValue(byte data)
        {
            bw.Write(data);
        }

        public void End()
        {
            if (bw != null)
            {
                bw.Close();
                bw = null;
            }
        }

    }

    public class PlayerCharacterDeserializer : PlayerCharacterDataStoreAccess, IDeserializer
    {

        private BinaryReader br = null;

        public PlayerCharacterDeserializer(string storagePath)
        {
            this.storagePath = storagePath;
        }

        public void Start(string playerName, PlayerCharacterDataType type, string fileVer)
        {
            playerName = playerName.ToLower();

            if (!exists(playerName))
                throw new PlayerCharacterDoesNotExistException(playerName);

            End(); // Just in case

            string withNameDirectory = buildPlayerPath(playerName);

            string extention = ".null";
            switch (type)
            {
                case (PlayerCharacterDataType.PCAppearance):
                    extention = ".appearance";
                    break;
                case (PlayerCharacterDataType.PCAttributes):
                    extention = ".attributes";
                    break;
                case(PlayerCharacterDataType.PCLocation):
                    extention = ".location";
                    break;
                case(PlayerCharacterDataType.PCInventory):
                    extention = ".inventory";
                    break;
                case (PlayerCharacterDataType.PCSkills):
                    extention = ".skills";
                    break;
                default:
                    throw new ArgumentException("Unrecognized type of player data");
            }

            // Open
            string filePath = Path.Combine(withNameDirectory, playerName + extention);
            fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            br = new BinaryReader(fs);

            // Read version
            string ver = ReadString();
            if (ver != fileVer)
                throw new InvalidOperationException("File version is wrong for this deserializer");
        }

        public short ReadShort()
        {
            return br.ReadInt16();
        }

        public ushort ReadUShort()
        {
            return br.ReadUInt16();
        }

        public sbyte ReadSByte()
        {
            return br.ReadSByte();
        }

        public string ReadString()
        {
            return br.ReadString();
        }

        public int ReadSInt()
        {
            return br.ReadInt32();
        }
        
        public uint ReadUInt()
        {
            return br.ReadUInt32();
        }

        public byte ReadByte()
        {
            return br.ReadByte();
        }

        public void End()
        {
            if (br != null)
            {
                br.Close();
                br = null;
            }
        }
    }

}