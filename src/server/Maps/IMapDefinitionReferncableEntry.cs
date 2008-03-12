using System;
using System.Collections.Generic;
using System.Text;

namespace Calindor.Server.Maps
{
    /// <summary>
    /// Reflect entry which could be referenced. If you need make property
    /// that is reference to another object this property type must be
    /// inherited for this class
    /// </summary>
    public abstract class IMapDefinitionReferencableEntry : IMapDefinitonEntry
    {
        private String id;

        public IMapDefinitionReferencableEntry()
            : base()
        {
        }

        public IMapDefinitionReferencableEntry(String id)
            : this()
        {
            this.id = id;
        }

        [PropertyNameAttribute("id")]
        public String Id
        {
            get { return this.id; }
            set { this.id = value; }
        }
    }
}
