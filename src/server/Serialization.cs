/*
 * Copyright (C) 2007 Krzysztof 'DeadwooD' Smiechowicz
 * Original project page: http://sourceforge.net/projects/calindor/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 */

namespace Calindor.Server.Serialization
{
    public interface ISerializer
    {
        void WriteValue(short data);
        void WriteValue(sbyte data);
        void WriteValue(string data);
    }

    public interface IDeserializer
    {
        short ReadShort();
        sbyte ReadSByte();
        string ReadString();
    }
}