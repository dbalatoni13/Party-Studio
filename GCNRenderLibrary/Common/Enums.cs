using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCNRenderLibrary
{
    public enum GXDebugShading
    {
        Default = 0,
        Normals = 1,
        VertexColor = 2,
        VertexAlpha = 3,
        RasterColor0 = 4,
        RasterColor1 = 5,
        RasterAlpha0 = 6,
        RasterAlpha1 = 7,
        Texture0 = 8,

        Tangent = 10,
        Binormal = 11,
    }

    public enum BillboardMode
    {
        NONE = 0,
        BILLBOARD,
        PERSP_BILLBOARD,
        ROT,
        PERSP_ROT,
        Y,
        PERSP_Y,
    }

    public enum VertexAttributeInput
    {
        PNMTXIDX = 0,
        TEX0MTXIDX = 1,
        TEX1MTXIDX = 2,
        TEX2MTXIDX = 3,
        TEX3MTXIDX = 4,
        TEX4MTXIDX = 5,
        TEX5MTXIDX = 6,
        TEX6MTXIDX = 7,
        TEX7MTXIDX = 8,
        POS = 9,
        NRM = 10,
        CLR0 = 11,
        CLR1 = 12,
        TEX0 = 13,
        TEX1 = 14,
        TEX2 = 15,
        TEX3 = 16,
        TEX4 = 17,
        TEX5 = 18,
        TEX6 = 19,
        TEX7 = 20,
        BINRM = 21,
        TANGENT = 22,

        BoneIndex,
        BoneWeight,
    }

    public enum AnisotropyLevel
    {
        One,
        Two,
        Four,
    }

    public enum MatrixMode
    {
        Default,
        Maya,
        Max,
        XSI,
    }

    public enum ScalingMode
    {
        MatrixMaya,
        MatrixXSI,
        Matrix3dsMax,
    }

    public enum SrtAttribute
    {
        ScaleU,
        ScaleV,
        Rotate,
        TransU,
        TransV,

        _Max
    }

    public enum TextureMapMode
    {
        TextureCoordinates,
        EnvCamera,
        Projection,
        EnvLight,
        EnvSpec,
    }

    public enum CommonMappingOption
    {
        NoSelection,
        DontRemapTextureSpace, // -1 -> 1 (J3D "basic")
        KeepTranslation        // Don't reset translation column
    };

    public enum DataFormat
    {
        Byte, SByte, Ushort, Short, Float,
    }

    public enum ColorDataFormat
    {
        RGB565, RGB8, RGBX8, RGBA4, RGBA6, RGBA8,
    }

    public enum RenderCommand
    {
        NoOp,
        Return,

        NodeDescendence,
        NodeMixing,

        Draw,

        EnvelopeMatrix,
        MatrixCopy
    };
}
