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

namespace Calindor.Server.Serialization
{
    public interface ISerializer
    {
        void WriteValue(short data);
        void WriteValue(ushort data);
        void WriteValue(sbyte data);
        void WriteValue(string data);
        void WriteValue(int data);
        void WriteValue(uint data);
        void WriteValue(byte data);
    }

    public interface IDeserializer
    {
        short ReadShort();
        ushort ReadUShort();
        sbyte ReadSByte();
        int ReadSInt();
        uint ReadUInt();
        string ReadString();
        byte ReadByte();
    }

    public class DeserializationException : ApplicationException
    {
        public DeserializationException(string message):base(message)
        {
            
        }
    }
}