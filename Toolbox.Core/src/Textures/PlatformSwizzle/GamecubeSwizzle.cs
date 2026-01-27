using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;

namespace Toolbox.Core.Imaging
{
    public class GamecubeSwizzle : IPlatformSwizzle
    {
        public TexFormat OutputFormat { get; set; } = TexFormat.RGBA8_UNORM;

        public Decode_Gamecube.TextureFormats Format = Decode_Gamecube.TextureFormats.RGBA32;
        public Decode_Gamecube.PaletteFormats PaletteFormat = Decode_Gamecube.PaletteFormats.RGB565;

        public override string ToString() {
            return $"{Format}" + (PaletteData.Length > 0 ? $"_p_{PaletteFormat}" : "");
        }

        public ushort[] PaletteData { get; set; } = new ushort[0];

        public void SetPalette(byte[] palette, Decode_Gamecube.PaletteFormats format) {
            PaletteFormat = format;
            using (var reader = new Toolbox.Core.IO.FileReader(palette)) {
                PaletteData = reader.ReadUInt16s(palette.Length / 2);
            }
        }

        public void SetPalette(ushort[] palette, Decode_Gamecube.PaletteFormats format) {
            PaletteFormat = format;
            PaletteData = palette;
        }

        public GamecubeSwizzle() { }

        public GamecubeSwizzle(Decode_Gamecube.TextureFormats format)
        {
            Format = format;
        }

        public GamecubeSwizzle(Decode_Gamecube.TextureFormats format, Decode_Gamecube.PaletteFormats paletteFormat) {
            Format = format;
            PaletteFormat = paletteFormat;
        }

        public byte[] DecodeImage(byte[] data, uint width, uint height, uint arrayCount, uint mipCount, int array, int mip) {

            byte[] GetPalette(ushort[] paletteData)
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

            return ImageUtility.ConvertBgraToRgba(gctex_tc.Decode(data, (uint)width, (uint)height,
             (uint)Format, GetPalette(PaletteData),
             (uint)PaletteFormat));
        }

        public byte[] EncodeImage(byte[] data, uint width, uint height, uint arrayCount, uint mipCount, int array, int mip) {
            var encoded = Decode_Gamecube.EncodeData(data, Format, PaletteFormat, (int)width, (int)height );
            PaletteData = encoded.Item2;
            return encoded.Item1;
        }
    }
}
