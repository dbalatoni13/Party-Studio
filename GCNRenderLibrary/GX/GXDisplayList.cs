using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Numerics;

namespace GCNRenderLibrary.Rendering
{
    /// <summary>
    /// Represents a display list for handling GX display commands and parsing GX buffer data.
    /// </summary>
    public partial class GXDisplayList
    {
        class QBPCommand
        {
            public uint Reg;
            public uint Val;
        }

        public static void Run(Stream stream, DisplayListRegisters r)
        {
            Run(new Toolbox.Core.IO.FileReader(stream), r, (int)stream.Length);
        }

        public static void Run(Toolbox.Core.IO.FileReader reader, DisplayListRegisters r, int dlSize)
        {
            reader.ByteOrder = Syroot.BinaryData.ByteOrder.BigEndian;

            var startPos = reader.Position;
            while (reader.Position < startPos + dlSize)
            {
                GX.Command tag = (GX.Command)reader.ReadByte();
                switch (tag)
                {
                    case GX.Command.LOAD_BP_REG:
                        r.bps(reader.ReadUInt32());
                        break;
                    case GX.Command.LOAD_CP_REG:
                        {
                            byte regAddr = reader.ReadByte();
                            r.cp[regAddr] = reader.ReadUInt32();
                        }
                        break;
                    case GX.Command.NOOP:
                        break;
                    case GX.Command.LOAD_XF_REG:
                        {
                            var len = reader.ReadUInt16() + 1;
                            ushort regAddr = reader.ReadUInt16();
                            for (int i = 0; i < len; i++)
                                r.xfs((GX.XFRegister)regAddr, i, reader.ReadUInt32());

                            // Clear out the other values.
                            for (int j = len; j < 16; j++) {
                                r.xfs((GX.XFRegister)regAddr, j, 0);
                            }
                        }
                        break;
                    case GX.Command.LOAD_INDX_A:
                    case GX.Command.LOAD_INDX_B:
                    case GX.Command.LOAD_INDX_C:
                    case GX.Command.LOAD_INDX_D:
                        reader.Seek(5);
                        break;
                    default:
                        if (((uint)tag & 0x80) != 0)
                        {

                        }
                        break;
                }
            }
        }   
    }

    public class VertexLayoutHelper
    {
        public GX.AttrType[] vcd;
        public AttributeFormat[] vat;
        public int VertexSize;

        public VertexLayoutHelper(DisplayListRegisters r)
        {
            var vcdL = r.cp[(int)GX.CPRegister.VCD_LO_ID];
            var vcdH = r.cp[(int)GX.CPRegister.VCD_HI_ID];

            //Define types for all attributes for parsing the display list data

            //VCD
            vcd = new GX.AttrType[(int)GX.Attr.TEX7 + 1];
            vcd[(int)GX.Attr.PNMTXIDX] = (GX.AttrType)((vcdL >> 0) & 0x01);
            vcd[(int)GX.Attr.TEX0MTXIDX] = (GX.AttrType)((vcdL >> 1) & 0x01);
            vcd[(int)GX.Attr.TEX1MTXIDX] = (GX.AttrType)((vcdL >> 2) & 0x01);
            vcd[(int)GX.Attr.TEX2MTXIDX] = (GX.AttrType)((vcdL >> 3) & 0x01);
            vcd[(int)GX.Attr.TEX3MTXIDX] = (GX.AttrType)((vcdL >> 4) & 0x01);
            vcd[(int)GX.Attr.TEX4MTXIDX] = (GX.AttrType)((vcdL >> 5) & 0x01);
            vcd[(int)GX.Attr.TEX5MTXIDX] = (GX.AttrType)((vcdL >> 6) & 0x01);
            vcd[(int)GX.Attr.TEX6MTXIDX] = (GX.AttrType)((vcdL >> 7) & 0x01);
            vcd[(int)GX.Attr.TEX7MTXIDX] = (GX.AttrType)((vcdL >> 8) & 0x01);
            vcd[(int)GX.Attr.POS] = (GX.AttrType)((vcdL >> 9) & 0x03);
            vcd[(int)GX.Attr.NRM] = (GX.AttrType)((vcdL >> 11) & 0x03);
            vcd[(int)GX.Attr.CLR0] = (GX.AttrType)((vcdL >> 13) & 0x03);
            vcd[(int)GX.Attr.CLR1] = (GX.AttrType)((vcdL >> 15) & 0x03);
            vcd[(int)GX.Attr.TEX0] = (GX.AttrType)((vcdH >> 0) & 0x03);
            vcd[(int)GX.Attr.TEX1] = (GX.AttrType)((vcdH >> 2) & 0x03);
            vcd[(int)GX.Attr.TEX2] = (GX.AttrType)((vcdH >> 4) & 0x03);
            vcd[(int)GX.Attr.TEX3] = (GX.AttrType)((vcdH >> 6) & 0x03);
            vcd[(int)GX.Attr.TEX4] = (GX.AttrType)((vcdH >> 8) & 0x03);
            vcd[(int)GX.Attr.TEX5] = (GX.AttrType)((vcdH >> 10) & 0x03);
            vcd[(int)GX.Attr.TEX4] = (GX.AttrType)((vcdH >> 12) & 0x03);
            vcd[(int)GX.Attr.TEX7] = (GX.AttrType)((vcdH >> 14) & 0x03);

            // VAT. Describes attribute formats.

            //compCnt
            var vatA = r.cp[(int)GX.CPRegister.VAT_A_ID + (int)GX.VtxFmt.VTXFMT0];
            //compType 
            var vatB = r.cp[(int)GX.CPRegister.VAT_B_ID + (int)GX.VtxFmt.VTXFMT0];
            //compShift
            var vatC = r.cp[(int)GX.CPRegister.VAT_C_ID + (int)GX.VtxFmt.VTXFMT0];

            AttributeFormat vatFmt(uint compCount, uint type, uint shift)
            {
                return new AttributeFormat((GX.CompCnt)compCount, (GX.CompType)type, shift);
            }

            vat = new AttributeFormat[(int)GX.Attr.TEX7 + 1];
            vat[(int)GX.Attr.POS] = vatFmt((vatA >> 0) & 0x01, (vatA >> 1) & 0x07, (vatA >> 4) & 0x1F);
            var nrm3 = (vatA >> 31) != 0;
            var nrmCnt = nrm3 ? (int)GX.CompCnt.NRM_NBT3 : (vatA >> 9) & 0x01;
            vat[(int)GX.Attr.NRM] = vatFmt(nrmCnt, (vatA >> 10) & 0x07, 0);
            vat[(int)GX.Attr.CLR0] = vatFmt((vatA >> 13) & 0x01, (vatA >> 14) & 0x07, 0);
            vat[(int)GX.Attr.CLR1] = vatFmt((vatA >> 17) & 0x01, (vatA >> 18) & 0x07, 0);
            vat[(int)GX.Attr.TEX0] = vatFmt((vatA >> 21) & 0x01, (vatA >> 22) & 0x07, (vatA >> 25) & 0x1F);
            vat[(int)GX.Attr.TEX1] = vatFmt((vatB >> 0) & 0x01, (vatB >> 1) & 0x07, (vatB >> 4) & 0x1F);
            vat[(int)GX.Attr.TEX2] = vatFmt((vatB >> 9) & 0x01, (vatB >> 10) & 0x07, (vatB >> 13) & 0x1F);
            vat[(int)GX.Attr.TEX3] = vatFmt((vatB >> 18) & 0x01, (vatB >> 19) & 0x07, (vatB >> 22) & 0x1F);
            vat[(int)GX.Attr.TEX4] = vatFmt((vatB >> 27) & 0x01, (vatB >> 28) & 0x07, (vatC >> 0) & 0x1F);
            vat[(int)GX.Attr.TEX5] = vatFmt((vatC >> 5) & 0x01, (vatC >> 6) & 0x07, (vatC >> 9) & 0x1F);
            vat[(int)GX.Attr.TEX6] = vatFmt((vatC >> 14) & 0x01, (vatC >> 15) & 0x07, (vatC >> 18) & 0x1F);
            vat[(int)GX.Attr.TEX7] = vatFmt((vatC >> 23) & 0x01, (vatC >> 24) & 0x07, (vatC >> 27) & 0x1F);

            VertexSize = GetVertexSize();
        }

        private int GetVertexSize()
        {
            int vertexSize = 0;
            for (int i = 0; i < vcd.Length; i++)
            {
                if (vat[i] == null || vcd[i] == GX.AttrType.NONE)
                    continue;

                if (vcd[i] == GX.AttrType.DIRECT)
                    vertexSize += getAttributeByteSizeRaw((GX.Attr)i, vat[i]);
                else if (vcd[i] == GX.AttrType.INDEX8)
                    vertexSize += 1;
                else if (vcd[i] == GX.AttrType.INDEX16)
                    vertexSize += 2;
            }
            return vertexSize;
        }

        private int getAttributeByteSizeRaw(GX.Attr attr, AttributeFormat format)
        {
            if (IsColor(attr, format))
                return format.GetColorStride();

            return 3 * format.GetComponentStride();
        }

        static bool IsColor(GX.Attr attr, AttributeFormat format)
        {
            switch (format.Type)
            {
                case GX.CompType.RGBA4:
                case GX.CompType.RGBA8:
                case GX.CompType.RGBX8:
                case GX.CompType.RGB565:
                case GX.CompType.RGB8:
                    return attr == GX.Attr.CLR0 ||
                           attr == GX.Attr.CLR1;
                default:
                    return false;
            }
        }
    }

    public class VtxLoader
    {
        public VertexLayoutHelper Layout;
        public byte[] BoneMatrixTable = new byte[21];

        public VtxLoader(VertexLayoutHelper layout) {
            Layout = layout;
        }


        public GXMesh RunVertices(GXBuffer[] buffers, Stream DLBuffer)
        {
            using (var reader = new Toolbox.Core.IO.FileReader(DLBuffer)) {
               return parseDisplayList(reader, buffers);
            }
        }

        private GXMesh parseDisplayList(Toolbox.Core.IO.FileReader reader, GXBuffer[] buffers)
        {
            GXMesh mesh = new GXMesh();
            GXDraw currentDraw = null;
            GXDraw currentXfmem = null;

            List<float[]> TexMatrices0123 = new List<float[]>();
            List<float[]> TexMatrices4567 = new List<float[]>();

            List<Vector3> Positons = new List<Vector3>();
            List<Vector3> Normals = new List<Vector3>();

            List<Vector2> TexCoord0 = new List<Vector2>();
            List<Vector2> TexCoord1 = new List<Vector2>();
            List<Vector2> TexCoord2 = new List<Vector2>();
            List<Vector2> TexCoord3 = new List<Vector2>();
            List<Vector2> TexCoord4 = new List<Vector2>();
            List<Vector2> TexCoord5 = new List<Vector2>();
            List<Vector2> TexCoord6 = new List<Vector2>();
            List<Vector2> TexCoord7 = new List<Vector2>();
            List<Vector4> Color0 = new List<Vector4>();
            List<Vector4> Color1 = new List<Vector4>();

            int totalIndexCount = 0;

            List<DrawCall> drawCalls = new List<DrawCall>();
            while (!reader.EndOfStream)
            {
                reader.ByteOrder = Syroot.BinaryData.ByteOrder.BigEndian;

                byte cmd = reader.ReadByte();
                if (cmd == 0)
                    break;

                switch ((GX.Command)cmd)
                {
                    case GX.Command.LOAD_INDX_A: //Position Matrices
                        {
                            currentDraw = null;
                            if (currentXfmem == null)
                                currentXfmem = new GXDraw(totalIndexCount);

                            var memoryElemSize = 3 * 4;
                            var memoryBaseAddr = 0x0000;
                            var table = currentXfmem.PosMatrixTable;

                            ushort index = reader.ReadUInt16();
                            int addrLen = reader.ReadUInt16();
                            var len = (addrLen >> 12) + 1;
                            int addr = addrLen & 0x0FFF;
                            //Divide by element size (4x3 matrix)
                            int tableIndex = ((addr - memoryBaseAddr) / memoryElemSize) | 0;
                            if (len != memoryElemSize)
                                throw new Exception();

                            table[tableIndex] = (byte)index;
                        }
                        continue;
                    case GX.Command.LOAD_INDX_C: //Texture Matrices
                        {
                            currentDraw = null;
                            if (currentXfmem == null)
                                currentXfmem = new GXDraw(totalIndexCount);

                            var memoryElemSize = 3 * 4;
                            var memoryBaseAddr = 0x0078;
                            var table = currentXfmem.TexMatrixTable;

                            ushort index = reader.ReadUInt16();
                            int addrLen = reader.ReadUInt16();
                            var len = (addrLen >> 12) + 1;
                            int addr = addrLen & 0x0FFF;
                            //Divide by element size (4x3 matrix)
                            int tableIndex = ((addr - memoryBaseAddr) / memoryElemSize) | 0;
                            if (len != memoryElemSize)
                                throw new Exception();

                            table[tableIndex] = (byte)index;
                        }
                        continue;
                    case GX.Command.LOAD_INDX_B: //Normal Matrices
                    case GX.Command.LOAD_INDX_D: //Light  Objects
                        reader.ReadInt32();
                        continue;
                }

                if (currentDraw == null)
                {
                    if (currentXfmem != null)
                    {
                        currentDraw = currentXfmem;
                        currentXfmem = null;
                    }
                    else
                    {
                        currentDraw = new GXDraw(totalIndexCount);
                    }
                    mesh.DrawCalls.Add(currentDraw);
                }

                var primType = cmd & 0xF8;
                var vertexFormat = cmd & 0x07;
                ushort vertexCount = reader.ReadUInt16();

                int indexCount = 0;
                switch ((GX.Command)primType)
                {
                    case GX.Command.DRAW_TRIANGLES:
                        indexCount = vertexCount;
                        break;
                    case GX.Command.DRAW_TRIANGLE_FAN:
                    case GX.Command.DRAW_TRIANGLE_STRIP:
                        indexCount = (vertexCount - 2) * 3;
                        break;
                    case GX.Command.DRAW_QUADS:
                    case GX.Command.DRAW_QUADS_2:
                        indexCount = ((vertexCount * 6) / 4) * 3;
                        break;
                }

                for (int v = 0; v < vertexCount; v++)
                {
                    bool hasTexMtx = false;

                    //Create vertex buffers for each attribute
                    for (int i = 0; i < (int)GX.Attr.MAX; i++)
                    {
                        if (Layout.vcd[i] == GX.AttrType.NONE)
                            continue;

                        ushort index = 0;
                        switch (Layout.vcd[i])
                        {
                            case GX.AttrType.INDEX8:
                                index = reader.ReadByte();
                                break;
                            case GX.AttrType.INDEX16:
                                index = reader.ReadUInt16();
                                break;
                            case GX.AttrType.DIRECT:
                                if (isVtxAttribMtxIdx((GX.Attr)i))
                                {
                                    byte matrixIdx = reader.ReadByte();
                                    switch ((GX.Attr)i)
                                    {
                                        case GX.Attr.PNMTXIDX:
                                            mesh.PosMatrixIdx.Add(matrixIdx);
                                            break;
                                        case GX.Attr.TEX0MTXIDX:
                                        case GX.Attr.TEX1MTXIDX:
                                        case GX.Attr.TEX2MTXIDX:
                                        case GX.Attr.TEX3MTXIDX:
                                            {
                                                //Pack into a uint32
                                                int texindex = i - (int)GX.Attr.TEX0MTXIDX;
                                                if (!hasTexMtx)
                                                {
                                                    TexMatrices0123.Add(new float[4] { 0, 0, 0, 0 });
                                                    hasTexMtx = true;
                                                }
                                                //last element
                                                int ind = TexMatrices0123.Count - 1;
                                                TexMatrices0123[ind][texindex] = matrixIdx;
                                            }
                                            break;
                                        case GX.Attr.TEX4MTXIDX:
                                        case GX.Attr.TEX5MTXIDX:
                                        case GX.Attr.TEX6MTXIDX:
                                        case GX.Attr.TEX7MTXIDX:
                                            {
                                                //Pack into a uint32
                                                int texindex = i - (int)GX.Attr.TEX4MTXIDX;
                                                if (texindex == 0)
                                                    TexMatrices4567.Add(new float[4]);
                                                TexMatrices4567[v][texindex] = matrixIdx;
                                            }
                                            break;
                                    }
                                }
                                else //TODO
                                    throw new Exception($"Direct type not supported!");
                                break;
                        }

                        switch ((GX.Attr)i)
                        {
                            case GX.Attr.POS:  Positons.Add(buffers[i].Data3[index]); break;
                            case GX.Attr.NRM:  Normals.Add(buffers[i].Data3[index]); break;
                            case GX.Attr.TEX0: TexCoord0.Add(buffers[i].Data2[index]); break;
                            case GX.Attr.TEX1: TexCoord1.Add(buffers[i].Data2[index]); break;
                            case GX.Attr.TEX2: TexCoord2.Add(buffers[i].Data2[index]); break;
                            case GX.Attr.TEX3: TexCoord3.Add(buffers[i].Data2[index]); break;
                            case GX.Attr.TEX4: TexCoord4.Add(buffers[i].Data2[index]); break;
                            case GX.Attr.TEX5: TexCoord5.Add(buffers[i].Data2[index]); break;
                            case GX.Attr.TEX6: TexCoord6.Add(buffers[i].Data2[index]); break;
                            case GX.Attr.TEX7: TexCoord7.Add(buffers[i].Data2[index]); break;
                            case GX.Attr.CLR0: Color0.Add(buffers[i].Data4[index]); break;
                            case GX.Attr.CLR1: Color1.Add(buffers[i].Data4[index]); break;
                        }
                    }
                }
                drawCalls.Add(new DrawCall() { primType = (GX.Command)primType, vertexCount = vertexCount, });
                currentDraw.IndexCount += indexCount;
                totalIndexCount += indexCount;
            }

            var indices = GenerateIndices(drawCalls, totalIndexCount, 0);
            mesh.SetIndices(indices);

            if (mesh.PosMatrixIdx.Count == 0)
                mesh.PosMatrixIdx = new float[Positons.Count].ToList();
            mesh.Positions = Positons;
            mesh.Normals = Normals;
            mesh.TexCoord0 = TexCoord0;
            mesh.TexCoord1 = TexCoord1;
            mesh.TexCoord2 = TexCoord2;
            mesh.TexCoord3 = TexCoord3;
            mesh.TexCoord4 = TexCoord4;
            mesh.TexCoord5 = TexCoord5;
            mesh.TexCoord6 = TexCoord6;
            mesh.TexCoord7 = TexCoord7;
            mesh.Color0 = Color0;
            mesh.Color1 = Color1;

            List<Vector4> texIndices = new List<Vector4>();
            foreach (var ind in TexMatrices0123)
                texIndices.Add(new Vector4(ind[0], ind[1], ind[2], ind[3]));

            mesh.TexMatrices0123 = texIndices;
            mesh.TexMatrices4567 = new List<Vector4>();

            return mesh;
        }

        private bool isVtxAttribMtxIdx(GX.Attr attr)
        {
            return attr == GX.Attr.PNMTXIDX || isVtxAttribTexMtxIdx(attr);
        }

        private bool isVtxAttribTexMtxIdx(GX.Attr attr)
        {
            switch (attr)
            {
                case GX.Attr.TEX0MTXIDX:
                case GX.Attr.TEX1MTXIDX:
                case GX.Attr.TEX2MTXIDX:
                case GX.Attr.TEX3MTXIDX:
                case GX.Attr.TEX4MTXIDX:
                case GX.Attr.TEX5MTXIDX:
                case GX.Attr.TEX6MTXIDX:
                case GX.Attr.TEX7MTXIDX:
                    return true;
                default:
                    return false;
            }
        }

        //Generates indices into usable triangles for opengl
        public static ushort[] GenerateIndices(List<DrawCall> drawCalls, int totalIndexCount, ushort firstVertexId)
        {
            //Generate the indices
            int indexDataIdx = 0;
            ushort[] dstIndexData = new ushort[totalIndexCount];
            ushort vertexId = firstVertexId;

            for (int z = 0; z < drawCalls.Count; z++)
            {
                var drawCall = drawCalls[z];

                // Convert topology to triangles.
                switch (drawCall.primType)
                {
                    case GX.Command.DRAW_TRIANGLES:
                        // Copy vertices.
                        for (int i = 0; i < drawCall.vertexCount; i++)
                        {
                            dstIndexData[indexDataIdx++] = vertexId++;
                        }
                        break;
                    case GX.Command.DRAW_TRIANGLE_STRIP:
                        // First vertex defines original triangle.
                        for (int i = 0; i < 3; i++)
                        {
                            dstIndexData[indexDataIdx++] = vertexId++;
                        }

                        for (int i = 3; i < drawCall.vertexCount; i++)
                        {
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId - ((i & 1) == 1 ? 1 : 2));
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId - ((i & 1) == 1 ? 2 : 1));
                            dstIndexData[indexDataIdx++] = vertexId++;
                        }
                        break;
                    case GX.Command.DRAW_TRIANGLE_FAN:
                        // First vertex defines original triangle.
                        ushort firstVertex = vertexId;

                        for (int i = 0; i < 3; i++)
                        {
                            dstIndexData[indexDataIdx++] = vertexId++;
                        }

                        for (int i = 3; i < drawCall.vertexCount; i++)
                        {
                            dstIndexData[indexDataIdx++] = firstVertex;
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId - 1);
                            dstIndexData[indexDataIdx++] = vertexId++;
                        }
                        break;
                    case GX.Command.DRAW_QUADS:
                    case GX.Command.DRAW_QUADS_2:
                        // Each quad (4 vertices) is split into 2 triangles (6 vertices)
                        for (int i = 0; i < drawCall.vertexCount; i += 4)
                        {
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 0);
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 1);
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 2);

                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 0);
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 2);
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 3);
                            vertexId += 4;
                        }
                        break;
                }
            }
            return dstIndexData;
        }

        public class DrawCall
        {
            public GX.Command primType;
            public int vertexCount;
        }
    }

    public class AttributeFormat
    {
        public GX.CompCnt ComponentCount;
        public GX.CompType Type;
        public int Divisor;

        public AttributeFormat(GX.CompCnt compCount, GX.CompType type, uint div)
        {
            ComponentCount = compCount;
            Type = type;
            Divisor = (int)div;
        }

        public int GetComponentStride()
        {
            switch (Type)
            {
                case GX.CompType.U8:
                case GX.CompType.S8:
                case GX.CompType.RGBA8:
                    return 1;
                case GX.CompType.U16:
                case GX.CompType.S16:
                    return 2;
                case GX.CompType.F32:
                    return 4;
            }
            return 4;
        }

        public int GetColorStride()
        {
            switch (Type)
            {
                case GX.CompType.RGB565:
                    return 2;
                case GX.CompType.RGB8:
                    return 3;
                case GX.CompType.RGBX8:
                    return 4;
                case GX.CompType.RGBA4:
                    return 2;
                case GX.CompType.RGBA6:
                    return 3;
                case GX.CompType.RGBA8:
                    return 4;
            }
            return 4;
        }
    }

    public class DisplayListRegisters
    {
        public uint[] bp = new uint[0x100];
        public uint[] cp = new uint[0x100];

        public uint[] xf = new uint[0x1000];

        public uint[] kc = new uint[4 * 2 * 2];

        public DisplayListRegisters()
        {
            this.bp[(int)GX.BPRegister.SS_MASK] = 0x00FFFFFF;
        }

        public void bps(uint regBag)
        {
            // First byte has register address, other 3 have value.
            var regAddr = regBag >> 24;

            var regWMask = this.bp[(int)GX.BPRegister.SS_MASK];
            // Retrieve existing value, overwrite w/ mask.
            var regValue = (this.bp[regAddr] & ~regWMask) | (regBag & regWMask);
            // The mask resets after use.
            if (regAddr != (int)GX.BPRegister.SS_MASK)
                this.bp[(int)GX.BPRegister.SS_MASK] = 0x00FFFFFF;
            // Set new value.
            this.bp[regAddr] = regValue;

            // Copy TEV colors internally.
            if (regAddr >= (int)GX.BPRegister.TEV_REGISTERL_0_ID && regAddr <= (int)GX.BPRegister.TEV_REGISTERL_0_ID + 4 * 2)
            {
                var kci = (int)regAddr - GX.BPRegister.TEV_REGISTERL_0_ID;
                var bank = (regValue >> 23) & 0x01;
                this.kc[(int)(bank * 4 * 2 + (int)kci)] = regValue;
            }
        }

        public void xfs(GX.XFRegister idx, int sub, uint value)
        {
            idx -= 0x1000;
            this.xf[(int)idx * 0x10 + sub] = value;
        }

        public uint xfg(GX.XFRegister idx , int sub = 0)
        {
            idx -= 0x1000;
            return this.xf[(int)idx * 0x10 + sub];
        }
    }
}
