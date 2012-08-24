using System;
using System.Collections;
using System.Text;

namespace Calindor.Server.MapDefinition
{
    public class GenericEntry
    {
        static protected short getShort(IDictionary properties, string key)
        {
            if (properties.Contains(key))
                return Convert.ToInt16(properties[key]);
            throw new Exception(string.Format("Property {0} not set", key));
        }

        static protected short getShortDefault(IDictionary properties, string key, short def)
        {
            if (properties.Contains(key))
                return getShort(properties, key);
            return def;
        }

        static protected int getInt(IDictionary properties, string key)
        {

            if (properties.Contains(key))
                return Convert.ToInt32(properties[key]);
            throw new Exception(string.Format("Property {0} not set", key));
        }

        static protected int getIntDefault(IDictionary properties, string key, int def)
        {
            if (properties.Contains(key))
                return getInt(properties, key);
            return def;
        }

        static protected string getString(IDictionary properties, string key)
        {
            if (properties.Contains(key))
                return Convert.ToString(properties[key]).Trim('`');
            throw new Exception(string.Format("Property {0} not set", key));
        }

        static protected string getStringDefault(IDictionary properties, string key, string def)
        {
            if (properties.Contains(key))
                return getString(properties, key);
            return def;
        }

    }

    public class GenericArea : GenericEntry
    {
        short minX;
        public short MinX
        {
            get { return minX; }
        }
        short minY;
        public short MinY
        {
            get { return minY; }
        }
        short maxX;
        public short MaxX
        {
            get { return maxX; }
        }
        short maxY;
        public short MaxY
        {
            get { return maxY; }
        }

        protected GenericArea(IDictionary properties)
        {
            this.minX = getShort(properties, "min_x");
            this.minY = getShort(properties, "min_y");
            this.maxX = getShort(properties, "max_x");
            this.maxY = getShort(properties, "max_y");
        }

        public bool Contains(short x, short y)
        {
            return x >= this.minX && x < this.maxX && y >= this.minY && y < this.maxY;
        }

        public bool IsClose(short x, short y)
        {
            return x >= this.minX - 30 &&
                x < this.maxX + 30 &&
                y >= this.minY - 30 &&
                y < this.maxY + 30;
        }
    }

    public class GenericPoint : GenericEntry
    {
        short x;
        public short X
        {
            get { return x; }
        }

        short y;
        public short Y
        {
            get { return y; }
        }

        protected GenericPoint(IDictionary properties)
        {
            this.x = getShort(properties, "x");
            this.y = getShort(properties, "y");
        }

        protected GenericPoint(short x, short y)
        {
            this.x = x;
            this.y = y;
        }

    }

    public class TeleportPoint : GenericPoint
    {
        public short SourceX
        {
            get { return X; }
        }
        public short SourceY
        {
            get { return Y; }
        }
        short destX;
        public short DestX
        {
            get { return destX; }
        }
        short destY;
        public short DestY
        {
            get { return destY; }
        }
        short destMap;
        public short DestMap
        {
            get { return destMap; }
        }
        short type;
        public short Type
        {
            get { return type; }
        }

        public TeleportPoint(IDictionary properties) : base(getShort(properties, "teleport_src_x"), getShort(properties, "teleport_src_y"))
        {
            this.destX = getShort(properties, "teleport_dst_x");
            this.destY = getShort(properties, "teleport_dst_y");
            this.destMap = getShort(properties, "teleport_map");
            this.type = getShort(properties, "type");
        }

        public static TeleportPoint Create(IDictionary properties)
        {
            return new TeleportPoint(properties);
        }
    }

    public class TextArea : GenericArea
    {
        string text;
        public string Text
        {
            get { return text; }
        }

        public TextArea(IDictionary properties) : base(properties)
        {
            this.text = getString(properties, "text");
        }

        public static TextArea Create(IDictionary properties)
        {
            return new TextArea(properties);
        }
    }

    public class UseArea : GenericArea
    {
        short teleportX;
        public short TeleportX
        {
            get { return teleportX; }
        }

        short teleportY;
        public short TeleportY
        {
            get { return teleportY; }
        }

        short teleportMap;
        public short TeleportMap
        {
            get { return teleportMap; }
        }

        int mapObjectId;
        public int MapObjectId
        {
            get { return mapObjectId; }
        }

        int invObjectId;
        public int InvObjectId
        {
            get { return invObjectId; }
        }
        string tooFarText;
        public string TooFarText
        {
            get { return tooFarText; }
        }
        string wrongObjectText;
        public string WrongObjectText
        {
            get { return wrongObjectText; }
        }

        string useText;
        public string UseText
        {
            get { return useText; }
        }

        short openBook;
        public short OpenBook
        {
            get { return openBook; }
        }

        bool sendSparks;
        public bool SendSparks
        {
            get { return sendSparks; }
        }

        protected UseArea(IDictionary properties) : base(properties)
        {
            this.teleportX = getShortDefault(properties, "teleport_x", -1);
            this.teleportY = getShortDefault(properties, "teleport_y", -1);
            this.teleportMap = getShortDefault(properties, "teleport_map", -1);
            this.mapObjectId = getInt(properties, "map_object_id");
            this.invObjectId = getIntDefault(properties, "inv_object_id", -1);
            this.sendSparks = getShortDefault(properties, "send_sparks", 0) != 0;
            this.tooFarText = getStringDefault(properties, "too_far_text", "Too far");
            this.wrongObjectText = getStringDefault(properties, "wrong_object_text", "Wrong object");
            this.useText = getStringDefault(properties, "use_text", null);
            this.openBook = getShortDefault(properties, "open_book", -1);
        }

        public static UseArea Create(IDictionary properties)
        {
            UseArea useArea = new UseArea(properties);
            return useArea;
        }
    }

    public class ObjectName : GenericEntry
    {
        int objectId;
        public int ObjectId
        {
            get { return objectId; }
        }

        string objectText;
        public string ObjectText
        {
            get { return objectText; }
        }

        protected ObjectName(IDictionary properties)
        {
            this.objectId = getInt(properties, "object_id");
            this.objectText = getString(properties, "object_name");
            if (this.ObjectText.Length > 48)
                Console.WriteLine("Object_name '{0}' is too long", this.ObjectText);
        }

        public static ObjectName Create(IDictionary properties)
        {
            return new ObjectName(properties);
        }
    }

    public class AttributeArea : GenericArea
    {
        public enum AttributeType
        {
            NeedP2P,
            MinLevel,
            MaxLevel,
            MinCombat,
            MaxCombat,
            AllowRain,
            RainStartChance,
            RainStopChance,
            RainThunderChance,
            DeathLossChance,
            AllowCombat,
            AllowMulticombat,
            AllowBeam,
            AllowTport,
            AllowHarvesting,
            AllowManufacturing,
            AllowSummoning,
            AllowSpellcasting,
            AllowPotions,
            TimedHeat,
            TimedCold,
            TimedPoison,
            TimedCorrosion,
            TimedDamage,
            WalkHeat,
            WalkCold,
            WalkPoison,
            WalkCorrosion,
            TimedHeal,
            TimedMana,
            TimedFood,
            ResearchRate
        }

        private static AttributeType getAttributeType(string typename)
        {
            switch (typename)
            {
                case "need_p2p": return AttributeType.NeedP2P;
                case "min_level": return AttributeType.MinLevel;
                case "max_level": return AttributeType.MaxLevel;
                case "min_combat": return AttributeType.MinCombat;
                case "max_combat": return AttributeType.MaxCombat;
                case "allow_rain": return AttributeType.AllowRain;
                case "rain_start_chance": return AttributeType.RainStartChance;
                case "rain_stop_chance": return AttributeType.RainStopChance;
                case "rain_thunder_chance": return AttributeType.RainThunderChance;
                case "death_loss_chance": return AttributeType.DeathLossChance;
                case "allow_combat": return AttributeType.AllowCombat;
                case "allow_multicombat": return AttributeType.AllowMulticombat;
                case "allow_beam": return AttributeType.AllowBeam;
                case "allow_tport": return AttributeType.AllowTport;
                case "allow_harvesting": return AttributeType.AllowHarvesting;
                case "allow_manufacturing": return AttributeType.AllowManufacturing;
                case "allow_summoning": return AttributeType.AllowSummoning;
                case "allow_spellcasting": return AttributeType.AllowSpellcasting;
                case "allow_potions": return AttributeType.AllowPotions;
                case "timed_heat": return AttributeType.TimedHeat;
                case "timed_cold": return AttributeType.TimedCold;
                case "timed_poison": return AttributeType.TimedPoison;
                case "timed_corrosion": return AttributeType.TimedCorrosion;
                case "timed_damage": return AttributeType.TimedDamage;
                case "walk_heat": return AttributeType.WalkHeat;
                case "walk_cold": return AttributeType.WalkCold;
                case "walk_poison": return AttributeType.WalkPoison;
                case "walk_corrosion": return AttributeType.WalkCorrosion;
                case "timed_heal": return AttributeType.TimedHeal;
                case "timed_mana": return AttributeType.TimedMana;
                case "timed_food": return AttributeType.TimedFood;
                case "research_rate": return AttributeType.ResearchRate;
            }
            throw new Exception("Invalid attribute "+typename);
        }

        int value;
        public int Value
        {
            get { return value; }
        }

        string text;
        public string Text
        {
            get { return text; }
        }

        AttributeType type;
        public AttributeType Type
        {
            get { return type; }
        }

        protected AttributeArea(IDictionary properties, string typename) : base(properties)
        {
            this.value = getInt(properties, "value");
            this.text = getStringDefault(properties, "text", "");
            this.type = getAttributeType(typename);
        }

        public static AttributeArea Create(IDictionary properties, string typename)
        {
            return new AttributeArea(properties, typename);
        }
    }

    public class MapEntry : GenericEntry
    {
        IList useAreas = new ArrayList();
        IList objectNames = new ArrayList();
        IList textAreas = new ArrayList();
        IList teleportPoints = new ArrayList();
        IList attributeAreas = new ArrayList();
        public ICollection AttributeAreas
        {
            get { return attributeAreas; }
        }

        public MapEntry()
        {
        }

        public UseArea getUseArea(short x, short y)
        {
            foreach (UseArea useArea in useAreas)
            {
                if (useArea.Contains(x, y))
                    return useArea;
            }
            return null;
        }

        public UseArea getUseArea(int mapObjectId)
        {
            foreach (UseArea useArea in useAreas)
            {
                if (useArea.MapObjectId == mapObjectId)
                    return useArea;
            }
            return null;
        }

        public void Add(UseArea useArea)
        {
            useAreas.Add(useArea);
        }

        public ICollection getUseAreas()
        {
            return useAreas;
        }

        public ObjectName getObjectName(int mapObjectId)
        {
            foreach (ObjectName objectName in objectNames)
            {
                if (objectName.ObjectId == mapObjectId)
                    return objectName;
            }
            return null;
        }

        public void Add(ObjectName objectName)
        {
            objectNames.Add(objectName);
        }

        public TextArea getTextArea(short x, short y)
        {
            foreach (TextArea textArea in textAreas)
            {
                if (textArea.Contains(x, y))
                    return textArea;
            }
            return null;
        }

        public void Add(TextArea textArea)
        {
            textAreas.Add(textArea);
        }

        public TeleportPoint getTeleportPoint(short x, short y)
        {
            foreach (TeleportPoint teleportPoint in teleportPoints)
            {
                if (teleportPoint.X == x && teleportPoint.Y == y)
                    return teleportPoint;
            }
            return null;
        }

        public void Add(TeleportPoint teleportPoint)
        {
            teleportPoints.Add(teleportPoint);
        }

        public AttributeArea getAttributeArea(short x, short y)
        {
            foreach (AttributeArea attributeArea in attributeAreas)
            {
                if (attributeArea.Contains(x, y))
                    return attributeArea;
            }
            return null;
        }

        public AttributeArea getAttributeArea(short x, short y, AttributeArea.AttributeType attributeType)
        {
            foreach (AttributeArea attributeArea in attributeAreas)
            {
                if (attributeArea.Contains(x, y) && attributeArea.Type == attributeType)
                    return attributeArea;
            }
            return null;
        }

        public void Add(AttributeArea attributeArea)
        {
            attributeAreas.Add(attributeArea);
        }
    }
}
