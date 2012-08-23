using System;
using System.Collections.Generic;
using System.Text;
using Calindor.Mapconverter.MapObjects;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Calindor.Mapconverter
{
    public abstract class BitmapSerializer : IMapSerializer
    {
        Stream outstream;
        ImageFormat imageFormat;
        public BitmapSerializer(string filename, ImageFormat imageFormat)
        {
            this.outstream = new FileStream(filename, FileMode.Create) ;
            this.imageFormat = imageFormat;
        }

        public BitmapSerializer(Stream outstream, ImageFormat imageFormat)
        {
            this.outstream = outstream;
            this.imageFormat = imageFormat;
        }

        public abstract IMapSerializer Create(string filename);

        public abstract IMapSerializer Create(Stream outstream);

        public void Serialize(Map map)
        {
            Bitmap bitmap = new Bitmap(map.SizeX, map.SizeY, PixelFormat.Format32bppArgb);
            byte[,] heightData = map.HeightData;
            short[,] clusterData = map.ClusterData;

            for (int y = 0; y < map.SizeY; y++)
            {
                for (int x = 0; x < map.SizeX; x++)
                {
                    int color = 255 << 16;
                    if (clusterData != null)
                        color += clusterData[x, y];
                    color <<= 8;
                    color += heightData[x, y];

                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(color));
                }
            }

            bitmap.Save(outstream, imageFormat);
        }
    }

    public class BMPSerializer : BitmapSerializer
    {
        public BMPSerializer(string filename)
            : base(filename, ImageFormat.Bmp)
        {
        }

        public BMPSerializer(Stream outstream)
            : base(outstream, ImageFormat.Bmp)
        {
        }

        public override IMapSerializer Create(string filename)
        {
            return new BMPSerializer(filename);
        }

        public override IMapSerializer Create(Stream outstream)
        {
            return new BMPSerializer(outstream);
        }

    }

}
