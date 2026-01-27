using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GCNRenderLibrary.Rendering
{
    /// <summary>
    /// Represents a display builder for writing display commands.
    /// </summary>
    internal class GXDisplayListBuilder
    {
        private BinaryWriter _writer;

        public GXDisplayListBuilder(BinaryWriter writer)
        {
            _writer = writer;
        }

        public void WriteBP(byte reg, uint val)
        {
            val &= ((1 << 24) - 1); // 24-bit
            val |= (uint)(reg & 0xff) << 24;
            _writer.Write((byte)GX.Command.LOAD_BP_REG);
            _writer.Write(val);
        }

        public void WriteXF(ushort reg, uint c)
        {
            _writer.Write((byte)GX.Command.LOAD_XF_REG);
            _writer.Write((ushort)0);
            _writer.Write(reg);
            _writer.Write(reg);
        }

        public void WriteCP(byte reg, uint c)
        {
            _writer.Write((byte)GX.Command.LOAD_CP_REG);
            _writer.Write(reg);
            _writer.Write(reg);
        }
    }
}
