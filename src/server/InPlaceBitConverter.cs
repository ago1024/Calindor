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

namespace Calindor.Misc
{
    // TODO: Use bit operations instead of System.BitConverter for speed
    public sealed class InPlaceBitConverter
    {
        
        public static void GetBytes(UInt16 value, byte[] outputBuffer, int startIndex)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            outputBuffer[startIndex] = bytes[0];
            outputBuffer[startIndex + 1] = bytes[1];
        }

        public static void GetBytes(Int16 value, byte[] outputBuffer, int startIndex)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            outputBuffer[startIndex] = bytes[0];
            outputBuffer[startIndex + 1] = bytes[1];
        }

        public static void GetBytes(string value, byte[] outputBuffer, int startIndex)
        {
            for (int i = 0; i < value.Length; i++)
                outputBuffer[startIndex + i] = (byte)value[i];
        }
    }
}