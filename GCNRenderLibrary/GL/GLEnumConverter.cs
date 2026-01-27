using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GCNRenderLibrary.Rendering
{
    public class GLEnumConverter
    {
        static int translateCullMode(GX.CullMode cullMode)
        {
            switch (cullMode)
            {
                case GX.CullMode.FRONT: return (int)CullFaceMode.Front;
                case GX.CullMode.BACK: return (int)CullFaceMode.Back;
                case GX.CullMode.ALL: return (int)CullFaceMode.FrontAndBack;
                case GX.CullMode.NONE: return -1;
                default:
                    return (int)CullFaceMode.Back;
            }
        }

        static BlendingFactor translateBlendFactor(GX.BlendFactor factor)
        {
            switch (factor)
            {
                case GX.BlendFactor.SRCCLR: return BlendingFactor.SrcColor;
                case GX.BlendFactor.INVSRCCLR: return BlendingFactor.OneMinusSrcColor;
                case GX.BlendFactor.ZERO: return BlendingFactor.Zero;
                case GX.BlendFactor.ONE: return BlendingFactor.One;
                case GX.BlendFactor.SRCALPHA: return BlendingFactor.SrcAlpha;
                case GX.BlendFactor.INVSRCALPHA: return BlendingFactor.OneMinusSrcAlpha;
                case GX.BlendFactor.DSTALPHA: return BlendingFactor.DstAlpha;
                case GX.BlendFactor.INVDSTALPHA: return BlendingFactor.OneMinusDstAlpha;
                default:
                    return BlendingFactor.Zero;
            }
        }

        static All translateCompareType(GX.CompareType compareType)
        {
            switch (compareType)
            {
                case GX.CompareType.NEVER: return All.Nearest;
                case GX.CompareType.LESS: return All.Less;
                case GX.CompareType.EQUAL: return All.Equal;
                case GX.CompareType.LEQUAL: return All.Lequal;
                case GX.CompareType.GREATER: return All.Greater;
                case GX.CompareType.NEQUAL: return All.Notequal;
                case GX.CompareType.GEQUAL: return All.Gequal;
                case GX.CompareType.ALWAYS: return All.Always;
                default:
                    return All.Always;
            }
        }

        public static TextureWrapMode gxWrapToGL(GX.WrapMode wrapMode)
        {
            switch (wrapMode)
            {
                case GX.WrapMode.REPEAT: return TextureWrapMode.Repeat;
                case GX.WrapMode.MIRROR: return TextureWrapMode.MirroredRepeat;
                case GX.WrapMode.CLAMP: return TextureWrapMode.ClampToEdge;
            }
            return TextureWrapMode.Repeat;
        }

        public static TextureMinFilter gxFilterMinToGL(GX.TextureFilter filter)
        {
            switch (filter)
            {
                case GX.TextureFilter.Near: return TextureMinFilter.Nearest;
                case GX.TextureFilter.Linear: return TextureMinFilter.Linear;
                case GX.TextureFilter.Linear_Mip_Near: return TextureMinFilter.LinearMipmapNearest;
                case GX.TextureFilter.Linear_Mip_Linear: return TextureMinFilter.LinearMipmapLinear;
                case GX.TextureFilter.Near_Mip_Near: return TextureMinFilter.NearestMipmapNearest;
                case GX.TextureFilter.Near_Mip_Linear: return TextureMinFilter.NearestMipmapLinear;
            }
            return TextureMinFilter.Nearest;
        }

        public static TextureMagFilter gxFilterMagToGL(GX.TextureFilter filter)
        {
            switch (filter)
            {
                case GX.TextureFilter.Near: return TextureMagFilter.Nearest;
                case GX.TextureFilter.Linear: return TextureMagFilter.Linear;
            }
            return TextureMagFilter.Nearest;
        }

        public static void translateGfxMegaState(MegaState state, GXMaterial material)
        {
            var depthTest = material.RopInfo.DepthTest;
            var blendMode = material.RopInfo.BlendMode;

            //Polygon state
            state.CullMode = translateCullMode(material.CullMode);
            state.FontFace = FrontFaceDirection.Cw;

            //Depth state
            state.DepthWrite = depthTest.Write;
            if (depthTest.Test)
                state.DepthCompare = (DepthFunction)translateCompareType(depthTest.Function);
            else
                state.DepthCompare = DepthFunction.Always;

            var mode = blendMode.Mode;
            //Blend state
            if (mode == GX.BlendMode.NONE)
            {
                state.BlendMode = BlendEquationMode.FuncAdd;
                state.BlendSrcFactor = BlendingFactor.One;
                state.BlendDstFactor = BlendingFactor.Zero;
            }
            else if (mode == GX.BlendMode.BLEND)
            {
                state.BlendMode = BlendEquationMode.FuncAdd;
                state.BlendSrcFactor = translateBlendFactor(blendMode.Source);
                state.BlendDstFactor = translateBlendFactor(blendMode.Dest);
            }
            else if (mode == GX.BlendMode.SUBTRACT)
            {
                state.BlendMode = BlendEquationMode.FuncReverseSubtract;
                state.BlendSrcFactor = BlendingFactor.One;
                state.BlendDstFactor = BlendingFactor.One;
            }
            else if (mode == GX.BlendMode.LOGIC)
            {
                Console.WriteLine($"Logic mode unsupported!");
            }
        }
    }
}
