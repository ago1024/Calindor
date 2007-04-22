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
using Calindor.Misc.Predefines;
using Calindor.Server.Serialization;

namespace Calindor.Server.Entities
{
    public class EntityAppearance
    {
        private sbyte[] innerData = new sbyte[7];

        public PredefinedModelHead Head
        {
            get { return (PredefinedModelHead)innerData[6]; }
            set { innerData[6] = (sbyte)value; }
        }

        public PredefinedEntityType Type
        {
            get { return (PredefinedEntityType)innerData[5]; }
            set { innerData[5] = (sbyte)value; }
        }

        public PredefinedModelSkin Skin
        {
            get { return (PredefinedModelSkin)innerData[0]; }
            set { innerData[0] = (sbyte)value; }
        }

        public PredefinedModelHair Hair
        {
            get { return (PredefinedModelHair)innerData[1]; }
            set { innerData[1] = (sbyte)value; }
        }

        public PredefinedModelShirt Shirt
        {
            get { return (PredefinedModelShirt)innerData[2]; }
            set { innerData[2] = (sbyte)value; }
        }

        public PredefinedModelPants Pants
        {
            get { return (PredefinedModelPants)innerData[3]; }
            set { innerData[3] = (sbyte)value; }
        }

        public PredefinedModelBoots Boots
        {
            get { return (PredefinedModelBoots)innerData[4]; }
            set { innerData[4] = (sbyte)value; }
        }

        public virtual void Serialize(ISerializer sr)
        {
            for (int i = 0; i <innerData.Length; i++)
                sr.WriteValue(innerData[i]);
        }

        public virtual void Deserialize(IDeserializer dsr)
        {
            for (int i = 0; i < innerData.Length; i++)
                innerData[i] = dsr.ReadSByte();
        }
    }
}