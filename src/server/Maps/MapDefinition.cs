/*
 * Copyright (C) 2008 Wojciech Duda
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
using System.Reflection;
using System.Xml;

namespace Calindor.Server.Maps
{
    public class MapDefinition : List<IMapDefinitonEntry>
    {

       
        private IMapDefinitonEntry GetElement(String name)
        {
            //TODO 

            if (name == "root")
                return new Root();
            if(name=="test")
                return new TestElement();
            if (name == "subItem")
                return new TestRefElement();
            return null;
        }

        public void Fill()
        {
            this.objectsToSet.Fill(this.objectsCache);
            this.objectsToSet.Clear();
            this.objectsCache.Clear();
        }

        #region helpers

        private Stack<IMapDefinitonEntry> tmpRoot = new Stack<IMapDefinitonEntry>();

        private class ObjectsToSetCache : Dictionary<String, ToSet>
        {
            public ObjectsToSetCache()
                : base()
            {
            }

            public void Fill(ObjectsCache objects)
            {
                foreach (KeyValuePair<String, ToSet> entry in this)
                {
                    if (objects.ContainsKey(entry.Key))
                    {
                        IMapDefinitionReferencableEntry e = objects[entry.Key];
                        entry.Value.SetValue(e);
                    }
                    else
                    {
                        //TODO Fill Error
                    }
                }
            }
        }

        private class ObjectsCache : Dictionary<String, IMapDefinitionReferencableEntry>
        {
            public ObjectsCache()
                : base()
            {
            }
        }

        private ObjectsToSetCache objectsToSet = new ObjectsToSetCache();

        private ObjectsCache objectsCache = new ObjectsCache();

        private class ToSet{
            private PropertyInfo propertyInfo;
            private IMapDefinitonEntry objectToSet;
            public ToSet(PropertyInfo pi, IMapDefinitonEntry objectToSet){
                this.propertyInfo = pi;
                this.objectToSet = objectToSet;
            }
            public void SetValue(IMapDefinitionReferencableEntry value)
            {
                this.propertyInfo.SetValue(this.objectToSet, value, null);
            }
        }

        #endregion

        private void NewElement(String name, Dictionary<String, String> attributes)
        {
            IMapDefinitonEntry e = this.GetElement(name);

            
            if (this.tmpRoot.Count != 0){
                e.Father = this.tmpRoot.Peek();
                //TODO - find and fill property in this.tmpRoot
            }
            this.tmpRoot.Push(e);

            System.Reflection.PropertyInfo[] ps = e.GetType().GetProperties();
            foreach (System.Reflection.PropertyInfo pi in ps)
            {
                foreach (Object a in pi.GetCustomAttributes(typeof(PropertyNameAttribute), true))
                {
                    String pName = ((PropertyNameAttribute)a).Name.ToLower();
                    if (attributes.ContainsKey(pName)){
                        if (pi.PropertyType.IsSubclassOf(typeof(IMapDefinitionReferencableEntry))){
                            this.objectsToSet.Add(attributes[pName], new ToSet(pi, e));
                        }else{
                            pi.SetValue(e, Convert.ChangeType(attributes[pName].Trim(), pi.PropertyType), null);
                        }
                    }else{
                        //TODO parse error
                    }
                }
            }

            if (e is IMapDefinitionReferencableEntry){
                IMapDefinitionReferencableEntry re = (IMapDefinitionReferencableEntry)e;
                this.objectsCache.Add(re.Id, re);
            }


        }

        private void EndElement()
        {
            if(this.tmpRoot.Count>0)
                this.tmpRoot.Pop();
        }

        private Dictionary<String, String> ParseAttributes(XmlTextReader reader)
        {
            Dictionary<String, String> att = new Dictionary<String,String>();
            if(!reader.MoveToFirstAttribute())
                return att;
            do{
                att.Add(reader.Name.ToLower(), reader.Value);
            }while(reader.MoveToNextAttribute());
            return att;
        }

        public void Read(System.IO.StreamReader input)
        {
            XmlTextReader r = new XmlTextReader(input);
            while (r.Read())
            {
                switch (r.NodeType)
                {
                    case XmlNodeType.Element: 
                        this.NewElement(r.Name, this.ParseAttributes(r));
                    break;
                    case XmlNodeType.EndElement: 
                        this.EndElement(); 
                    break;
                }
            }
        }
    }


    /*
    public class UseAreaEntry : IMapDefinitonEntry
    {
        private static readonly String tagName = "use_area";
        private int minX;
        private int minY;
        private int maxX;
        private int maxY;
        private int teleportX;
        private int teleportY;
        private int teleportMap;
        private int mapObjectId = -1;
        private int invObjectId = -1;
        private bool sendSparks;
        private String tooFarText;
        private String wrongObjectText;
        private String useText;
        private int openBook;

        public override String GetTagName()
        {
            return UseAreaEntry.tagName;
        }

        [MapDefinitionEntryAttribute("min_x")]
        public int MinX
        {
            get { return this.minX; }
            set { this.minX = value; }
        }

        [MapDefinitionEntryAttribute("max_x")]
        public int MaxX
        {
            get { return this.maxX; }
            set { this.maxX = value; }
        }

        [MapDefinitionEntryAttribute("min_y")]
        public int MinY
        {
            get { return this.minY; }
            set { this.minY = value; }
        }

        [MapDefinitionEntryAttribute("max_y")]
        public int MaxY
        {
            get { return this.maxY; }
            set { this.maxY = value; }
        }

        [MapDefinitionEntryAttribute("teleport_x")]
        public int TeleportX
        {
            get { return this.teleportX; }
            set { this.teleportX = value; }
        }

        [MapDefinitionEntryAttribute("teleport_y")]
        public int TeleportY
        {
            get { return this.teleportY; }
            set { this.teleportY = value; }
        }

        [MapDefinitionEntryAttribute("map_object_id")]
        public int MapObjectId
        {
            get { return this.mapObjectId; }
            set { this.mapObjectId = value; }
        }

        [MapDefinitionEntryAttribute("too_far_text")]
        public String TooFarText
        {
            get { return this.tooFarText; }
            set { this.tooFarText = value; }
        }

        [MapDefinitionEntryAttribute("inv_object_id")]
        public int InvObjectId
        {
            get { return this.invObjectId; }
            set { this.invObjectId = value; }
        }

        [MapDefinitionEntryAttribute("wrong_object_text")]
        public String WrongObjectText
        {
            get { return this.wrongObjectText; }
            set { this.wrongObjectText = value; }
        }

        [MapDefinitionEntryAttribute("use_text")]
        public String UseText
        {
            get { return this.useText; }
            set { this.useText = value; }
        }
    }*/

    #region test
    public class Root : IMapDefinitonEntry
    {
        public override String GetTagName()
        {
            return "root";
        }
    }

    public class TestRefElement : IMapDefinitionReferencableEntry
    {
        private String value;
        public override string GetTagName()
        {
            return "subItem";
        }

        [PropertyName("value")]
        public String Value
        {
            get { return this.value; }
            set { this.value = value; }
        }
        public override string ToString()
        {
            if (this.value != null)
                return this.value;
            return "{unset}";
        }
    }

    public class TestElement : IMapDefinitonEntry
    {
        private String value;
        private TestRefElement subItem;

        public override string GetTagName()
        {
            return "test";
        }

        [PropertyName("value")]
        public String Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        [PropertyName("refId")]
        public TestRefElement SubItem{
            get { return this.subItem; }
            set { this.subItem = value; }
        }

        public override string ToString()
        {
            if (this.value != null)
                return this.value;
            return "{unset}";
        }
    }
    #endregion
}
