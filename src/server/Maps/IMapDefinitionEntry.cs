using System;
using System.Collections.Generic;
using System.Text;

namespace Calindor.Server.Maps
{
    /// <summary>
    /// Base class for every entry in file
    /// </summary>
    public abstract class IMapDefinitonEntry
    {
        private List<IMapDefinitonEntry> children;
        private IMapDefinitonEntry father;
        public IMapDefinitonEntry()
        {
            this.children = new List<IMapDefinitonEntry>();
        }
        public abstract String GetTagName();
        public void add(IMapDefinitonEntry entry)
        {
            this.children.Add(entry);
        }
        public List<IMapDefinitonEntry> Children
        {
            get { return this.children; }
        }

        /// <summary>
        /// Tag in which this element was declared (not refereced)
        /// </summary>
        public IMapDefinitonEntry Father
        {
            get { return this.father; }
            set
            {
                this.father = value;
                if (!this.father.ContainsChild(this))
                    this.father.Children.Add(this);
            }
        }
        public bool ContainsChild(IMapDefinitonEntry entry)
        {
            foreach (IMapDefinitonEntry e in this.children)
                if (e == entry)
                    return true;
            return false;
        }

    }

    /// <summary>
    /// Every property in object with this attribute
    /// will be reflected to xml attribute
    /// </summary>
    public class PropertyNameAttribute : Attribute
    {
        private String name;
        public PropertyNameAttribute(String name)
        {
            this.name = name;
        }

        public String Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
    }
}
