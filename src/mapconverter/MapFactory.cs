using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using Calindor.Mapconverter.MapObjects;
using Calindor.Mapconverter.ElmMapObjects;
using Calindor.Mapconverter.XmlMapObjects;

namespace Calindor.Mapconverter
{
    public class MapFactory
    {
        static private Stream EnsureStream(string filename, Stream stream)
        {
            if (stream != null)
                return stream;
            else
                return new FileStream(filename, FileMode.Open);
        }

        static private Map LoadMapData(Map map, Stream stream)
        {
            if (map.LoadMapData(stream))
                return map;
            else
                return null;
        }

        static Map CreateMap(string filename, Stream stream)
        {
            string lcname = filename.ToLower();
            if (lcname.EndsWith(".gz"))
            {
                string fname = filename.Substring(0, filename.Length - 3);
                Stream gzstream = new GZipStream(EnsureStream(filename, stream), CompressionMode.Decompress);
                return CreateMap(fname, gzstream);
            }
            else if (lcname.EndsWith(".xml"))
                return LoadMapData(new XmlMap(filename), EnsureStream(filename, stream));
            else if (lcname.EndsWith(".elm"))
                return LoadMapData(new ElmMap(filename), EnsureStream(filename, stream));
            else
                return null;
        }

        public static Map CreateMap(string filename)
        {
            return CreateMap(filename, null);
        }

    }

    public class SerializerFactory
    {
        static private Stream EnsureStream(string filename, Stream stream)
        {
            if (stream != null)
                return stream;
            else
                return new FileStream(filename, FileMode.Create);
        }

        static IMapSerializer CreateSerializer(string filename, Stream stream)
        {
            string lcname = filename.ToLower();
            if (lcname.EndsWith(".gz"))
            {
                string fname = filename.Substring(0, filename.Length - 3);
                Stream gzstream = new GZipStream(EnsureStream(filename, stream), CompressionMode.Compress);
                return CreateSerializer(fname, gzstream);
            }
            else if (lcname.EndsWith(".xml"))
                return new XmlMapSerializer(EnsureStream(filename, stream));
            else if (lcname.EndsWith(".elm"))
                return new ElmMapSerializer(EnsureStream(filename, stream));
            else if (lcname.EndsWith(".bmp"))
                return new BMPSerializer(EnsureStream(filename, stream));
            else
                return null;
        }

        public static IMapSerializer CreateSerializer(string filename)
        {
            return CreateSerializer(filename, null);
        }
    }
}
