using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace GCNRenderLibrary.Rendering
{
    public class GLGXTexture : GLTexture, IRenderableTexture
    {
      //  public int ID { get; set; }

        public TextureTarget TextureTarget { get; set; }

   //     public string Name { get; set; }

      //  public uint Width { get; set; }
      //  public uint Height { get; set; }

        //For loading gcn textures without palettes
        public GLGXTexture(string name, uint width, uint height, uint format, uint mipCount, Memory<byte> rawData) : base()
        {
            Load(name, width, height, format, 0, mipCount, rawData, new byte[0]);
        }

        //For loading textures with byte[] palettes
        public GLGXTexture(string name, uint width, uint height, uint format, uint paletteFormat, uint mipCount, Memory<byte> rawData, Memory<byte> paletteData) : base()
        {
            Load(name, width, height, format, paletteFormat, mipCount, rawData, paletteData);
        }

        //For loading textures with ushort[] palettes
        public GLGXTexture(string name, uint width, uint height, uint format, uint paletteFormat, uint mipCount, Memory<byte> rawData, ushort[] paletteData) : base()
        {
            Load(name, width, height, format, paletteFormat, mipCount, rawData, GetPalette(paletteData));
        }

        public void Reload(string name, uint width, uint height, uint format, uint paletteFormat, uint mipCount, Memory<byte> rawData, ushort[] paletteData)
        {
            Load(name, width, height, format, paletteFormat, mipCount, rawData, GetPalette(paletteData));
        }

        private void Load(string name, uint width, uint height, uint format, uint paletteFormat, uint mipCount, Memory<byte> rawData, Memory<byte> paletteData)
        {
            Name = name;
          //  ID = GL.GenTexture();

            Width = (int)width;
            Height = (int)height;
            TextureTarget = TextureTarget.Texture2D;

            Bind();

         //   GL.TexParameter(TextureTarget, TextureParameterName.TextureMaxLevel, mipCount - 1);
           // GL.TexParameter(TextureTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);

            try
            {
                uint ofs = 0;
                for (int i = 0; i < mipCount; i++)
                {
                    uint mipwidth = (uint)Math.Max(1, width >> i);
                    uint mipheight = (uint)Math.Max(1, height >> i);

                    var size = gctex.ComputeImageSize(format, mipwidth, mipheight);
                    byte[] rgba = new byte[mipwidth * mipheight * 4];
                    if (size <= rawData.Length)
                    {
                        rgba = gctex.Decode(rawData.Slice((int)ofs, (int)size).ToArray(), mipwidth, mipheight,
                            (uint)format, paletteData.ToArray(),
                            (uint)paletteFormat);
                    }

                    GL.TexImage2D(TextureTarget,
                         i,
                         PixelInternalFormat.Rgba,
                         (int)mipwidth,
                         (int)mipheight,
                         0,
                         PixelFormat.Rgba,
                         PixelType.UnsignedByte,
                         rgba);

                    ofs += size;

                    break;
                }

                GL.GenerateTextureMipmap((int)GenerateMipmapTarget.Texture2D);
            }
            catch
            {

            }
            Unbind();
        }

        private byte[] GetPalette(ushort[] paletteData)
        {
            if (paletteData.Length == 0) return new byte[0];

            var mem = new MemoryStream();
            using (var wr = new BinaryWriter(mem))
            {
                for (int i = 0; i < paletteData.Length; i++)
                {
                    wr.Write((byte)(paletteData[i] >> 8));
                    wr.Write((byte)(paletteData[i] & 0xFF));
                }
            }
            return mem.ToArray();
        }

        public void Bind()
        {
            GL.BindTexture(TextureTarget, ID);
        }

        public void Unbind()
        {
            GL.BindTexture(TextureTarget, 0);
        }

        public void Dispose()
        {
            GL.DeleteTexture(ID);
        }
    }
}
