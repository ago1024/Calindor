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

namespace Calindor.Server
{
    public class ProtocolVersion
    {
        public static UInt16 FirstDigit
        {
            get { return 10; }
        }

        public static UInt16 SecondDigit
        {
            get { return 19; }
        }
    }
}
