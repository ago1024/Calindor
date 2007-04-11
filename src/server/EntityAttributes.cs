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

namespace Calindor.Server.Entities
{
/*    public class PersistentShortAttributes
    {
        protected short[] innerData = null;
        protected int maxNumberOfItems = -1;

        public virtual void Serialize(ISerializer sr)
        {
            for (int i = 0; i < maxNumberOfItems; i++)
            {
                if (i < innerData.Length)
                    sr.WriteValue(innerData[i]);
                else
                    sr.WriteValue((short)0);
            }
        }

        public virtual void Deserialize(IDeserializer dsr)
        {
            for (int i = 0; i < maxNumberOfItems; i++)
            {
                if (i < innerData.Length)
                    innerData[i] = dsr.ReadShort();
                else
                    dsr.ReadShort();
            }
        }
    }

    public class MiscAttributes : PersistentShortAttributes
    {
        public MiscAttributes()
        {
            innerData = new short[3];
            maxNumberOfItems = 20;
        }

        public short OverallLevel
        {
            get { return innerData[0]; }
            set { innerData[0] = value; }
        }

        public short MaterialPoints
        {
            get { return innerData[1]; }
            set { innerData[1] = value; }
        }

        public short EtherealPoints
        {
            get { return innerData[2]; }
            set { innerData[2] = value; }
        }
    }

    public class BasicAttributes : PersistentShortAttributes
    {
        public BasicAttributes()
        {
            innerData = new short[6];
            maxNumberOfItems = 20;
        }


        public short Physique
        {
            get { return innerData[0]; }
            set { innerData[0] = value; }
        }

        public short Coordination
        {
            get { return innerData[1]; }
            set { innerData[1] = value; }
        }

        public short Reasoning
        {
            get { return innerData[2]; }
            set { innerData[2] = value; }
        }

        public short Will
        {
            get { return innerData[3]; }
            set { innerData[3] = value; }
        }

        public short Instinct
        {
            get { return innerData[4]; }
            set { innerData[4] = value; }
        }

        public short Vitality
        {
            get { return innerData[5]; }
            set { innerData[5] = value; }
        }
    }

    public class CrossAttributes : PersistentShortAttributes
    {
        public CrossAttributes()
        {
            innerData = new short[9];
            maxNumberOfItems = 30;
        }

        public short Might
        {
            get { return innerData[0]; }
            set { innerData[0] = value; }
        }

        public short Matter
        {
            get { return innerData[1]; }
            set { innerData[1] = value; }
        }

        public short Toughness
        {
            get { return innerData[2]; }
            set { innerData[2] = value; }
        }

        public short Charm
        {
            get { return innerData[3]; }
            set { innerData[3] = value; }
        }

        public short Reaction
        {
            get { return innerData[4]; }
            set { innerData[4] = value; }
        }

        public short Perception
        {
            get { return innerData[5]; }
            set { innerData[5] = value; }
        }

        public short Rationality
        {
            get { return innerData[6]; }
            set { innerData[6] = value; }
        }

        public short Dexterity
        {
            get { return innerData[7]; }
            set { innerData[7] = value; }
        }

        public short Etherality
        {
            get { return innerData[8]; }
            set { innerData[8] = value; }
        }
    }

    public class Nexuses : PersistentShortAttributes
    {
        public Nexuses()
        {
            innerData = new short[6];
            maxNumberOfItems = 20;
        }

        public short Human
        {
            get { return innerData[0]; }
            set { innerData[0] = value; }
        }

        public short Animal
        {
            get { return innerData[1]; }
            set { innerData[1] = value; }
        }

        public short Vegetal
        {
            get { return innerData[2]; }
            set { innerData[2] = value; }
        }

        public short Inorganic
        {
            get { return innerData[3]; }
            set { innerData[3] = value; }
        }

        public short Artificial
        {
            get { return innerData[4]; }
            set { innerData[4] = value; }
        }

        public short Magic
        {
            get { return innerData[5]; }
            set { innerData[5] = value; }
        }
    }

    public class Skills : PersistentShortAttributes
    {
        public Skills()
        {
            innerData = new short[9];
            maxNumberOfItems = 50;
        }

        public short Attack
        {
            get { return innerData[0]; }
            set { innerData[0] = value; }
        }

        public short Defense
        {
            get { return innerData[1]; }
            set { innerData[1] = value; }
        }

        public short Harvest
        {
            get { return innerData[2]; }
            set { innerData[2] = value; }
        }

        public short Alchemy
        {
            get { return innerData[3]; }
            set { innerData[3] = value; }
        }

        public short Magic
        {
            get { return innerData[4]; }
            set { innerData[4] = value; }
        }

        public short Potion
        {
            get { return innerData[5]; }
            set { innerData[5] = value; }
        }

        public short Summoning
        {
            get { return innerData[6]; }
            set { innerData[6] = value; }
        }

        public short Manufacturing
        {
            get { return innerData[7]; }
            set { innerData[7] = value; }
        }

        public short Crafting
        {
            get { return innerData[8]; }
            set { innerData[8] = value; }
        }
    }*/
}