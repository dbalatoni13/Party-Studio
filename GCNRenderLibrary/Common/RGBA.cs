using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GCNRenderLibrary
{
    /// <summary>
    /// Represents color with RGBA in byte format and can convert with uint32 values.
    /// </summary>
    public class RGBA
    {
        public uint Color;

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public RGBA(uint value) {
            Color = value;
            R = (byte)((value >> 24) & 0xFF);
            G = (byte)((value >> 16) & 0xFF);
            B = (byte)((value >> 8) & 0xFF);
            A = (byte)(value & 0xFF);
        }

        public RGBA(float r, float g, float b, float a)
        {
            R = (byte)(r * 255);
            G = (byte)(g * 255);
            B = (byte)(b * 255);
            A = (byte)(a * 255);
        }

        public RGBA(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Vector4 ToVector4()
        {
            return new Vector4(
                R / 255.0f, 
                G / 255.0f, 
                B / 255.0f, 
                A / 255.0f);
        }

        public override string ToString()
        {
            return String.Format("R:{0} G:{1} B:{2} A:{3}", R, G, B, A);
        }
    }
}
