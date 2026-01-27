using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCNRenderLibrary.Rendering
{
    /// <summary>
    /// Represents a display list for handling GX display commands and parsing GX buffer data.
    /// </summary>
    public partial class GXDisplayList
    {
        enum TexProjection
        {
            ST = 0x00,
            STQ = 0x01,
        };
        enum TexForm
        {
            AB11 = 0x00,
            ABC1 = 0x01,
        };
        enum TexGenType
        {
            REGULAR = 0x00,
            EMBOSS_MAP = 0x01,
            COLOR_STRGBC0 = 0x02,
            COLOR_STRGBC1 = 0x02,
        };
        enum TexSourceRow
        {
            GEOM = 0x00,
            NRM = 0x01,
            CLR = 0x02,
            BNT = 0x03,
            BNB = 0x04,
            TEX0 = 0x05,
            TEX1 = 0x06,
            TEX2 = 0x07,
            TEX3 = 0x08,
            TEX4 = 0x09,
            TEX5 = 0x0A,
            TEX6 = 0x0B,
            TEX7 = 0x0C,
        };

        /// <summary>
        /// Reads texture coordinate gens from a display list given the amount used.
        /// </summary>
        public static TexCoordGen[] ParseTexGens(DisplayListRegisters r, int numTexGens)
        {
            TexCoordGen[] texGens = new TexCoordGen[numTexGens];
            for (int i = 0; i < numTexGens; i++)
            {
                var v = r.xfg(GX.XFRegister.XF_TEX0_ID + i);
                var proj = (TexProjection)((v >> 1) & 0x01);
                var form = (TexForm)((v >> 2) & 0x01);
                var tgType = (TexGenType)((v >> 4) & 0x02);
                var src = (TexSourceRow)((v >> 7) & 0x0F);
                var embossSrc = ((v >> 12) & 0x07);
                var embossLgt = ((v >> 15) & 0x07);

                GX.TexGenType texGenType = GX.TexGenType.MTX2x4;
                GX.TexGenSrc texGenSrc = GX.TexGenSrc.TEX0;

                if (tgType == TexGenType.REGULAR)
                {
                    var srcLookup = new GX.TexGenSrc[]
                    {
                        GX.TexGenSrc.POS,
                        GX.TexGenSrc.NRM,
                        GX.TexGenSrc.COLOR0,
                        GX.TexGenSrc.BINRM,
                        GX.TexGenSrc.TANGENT,
                        GX.TexGenSrc.TEX0,
                        GX.TexGenSrc.TEX1,
                        GX.TexGenSrc.TEX2,
                        GX.TexGenSrc.TEX3,
                        GX.TexGenSrc.TEX4,
                        GX.TexGenSrc.TEX5,
                        GX.TexGenSrc.TEX6,
                        GX.TexGenSrc.TEX7,
                    };
                    texGenType = proj == TexProjection.ST ? GX.TexGenType.MTX2x4 : GX.TexGenType.MTX3x4;
                    texGenSrc = srcLookup[(int)src];
                }
                else if (tgType == TexGenType.EMBOSS_MAP)
                {
                    texGenType = (GX.TexGenType)((int)GX.TexGenType.BUMP0 + embossLgt);
                    texGenSrc = (GX.TexGenSrc)((int)GX.TexGenSrc.TEXCOORD0 + embossSrc);
                }
                else if (tgType == TexGenType.COLOR_STRGBC0)
                {
                    texGenType = GX.TexGenType.SRTG;
                    texGenSrc = GX.TexGenSrc.COLOR0;
                }
                else if (tgType == TexGenType.COLOR_STRGBC1)
                {
                    texGenType = GX.TexGenType.SRTG;
                    texGenSrc = GX.TexGenSrc.COLOR1;
                }

                //GX.TexGenMatrix matrix = (GX.TexGenMatrix)(30 + i * 3);
                GX.TexGenMatrix matrix = GX.TexGenMatrix.IDENTITY;

                var dv = r.xfg(GX.XFRegister.XF_DUALTEX0_ID + i);
                var postMatrix = (GX.PostTexGenMatrix)((int)(dv >> 0) & 0xFF) + (int)GX.PostTexGenMatrix.PTTEXMTX0;
                bool normalize = ((dv >> 8) & 0x01) != 0;

                postMatrix = (GX.PostTexGenMatrix.PTTEXMTX0 + i * 3);

                texGens[i] = new TexCoordGen()
                {
                    Function = texGenType,
                    Matrix = matrix,
                    PostMatrix = postMatrix,
                    SourceParam = texGenSrc,
                    Normalize = normalize,
                };
            }
            return texGens;
        }

        class TevOrder
        {
            public GX.TexMapID texMapID;
            public GX.TexCoordID texCoordID;
            public GX.RasColorChannelID channelID;
        }

        /// <summary>
        /// Reads indirect stages from a display list given the amount of stages used.
        /// </summary>
        public static IndirectStage[] ParseIndStages(DisplayListRegisters r, int numStages)
        {
            var iref = r.bp[(int)GX.BPRegister.RAS1_IREF_ID];

            IndirectStage[] stages = new IndirectStage[numStages];
            for (int i = 0; i < numStages; i++)
            {
                var ss = r.bp[(int)GX.BPRegister.RAS1_SS0_ID + (i >> 2)];
                var scaleS = (GX.IndTexScale)(ss >> ((0x08 * (i & 1)) + 0x00) & 0x0F);
                var scaleT = (GX.IndTexScale)(ss >> ((0x08 * (i & 1)) + 0x04) & 0x0F);
                var texture = (GX.TexMapID)((iref >> (0x06 * i)) & 0x07);
                var texCoordId = (GX.TexCoordID)((iref >> (0x06 * i)) & 0x07);
                stages[i] = new IndirectStage()
                {
                    ScaleS = scaleS,
                    ScaleT = scaleT,
                    TextureMapID = texture,
                    TexCoordId = texCoordId,
                };
            }
            return stages;
        }


        /// <summary>
        /// Reads tev stages from a display list given the amount of stages used.
        /// </summary>
        public static TevStage[] ParseTevStages(DisplayListRegisters r, int numTevs)
        {
            TevStage[] stages = new TevStage[numTevs];

            List<TevOrder> tevOrder = new List<TevOrder>();                ;
            for (int i = 0; i < 8; i++)
            {
                var v = r.bp[(int)GX.BPRegister.RAS1_TREF_0_ID + i];
                var ti0 = (GX.TexMapID)((v >> 0) & 0x07);
                var tc0 = (GX.TexCoordID)((v >> 3) & 0x07);
                var te0 = ((v >> 6) & 0x01) != 0;
                var cc0 = (GX.RasColorChannelID)((v >> 7) & 0x07);
                // 7-10 = pad
                var ti1 = (GX.TexMapID)((v >> 12) & 0x07);
                var tc1 = (GX.TexCoordID)((v >> 15) & 0x07);
                var te1 = ((v >> 18) & 0x01) != 0;
                var cc1 = (GX.RasColorChannelID)((v >> 19) & 0x07);

                if (i * 2 + 0 >= numTevs)
                    break;

                var order0 = new TevOrder()
                {
                    texMapID = te0 ? ti0 : GX.TexMapID.TEXMAP_NULL,
                    texCoordID = tc0,
                    channelID = cc0,
                };
                tevOrder.Add(order0);

                if (i * 2 + 1 >= numTevs)
                    break;

                var order1 = new TevOrder()
                {
                    texMapID = te1 ? ti1 : GX.TexMapID.TEXMAP_NULL,
                    texCoordID = tc1,
                    channelID = cc1,
                };
                tevOrder.Add(order1);
            }
            if (tevOrder.Count != numTevs)
                throw new Exception();

            for (int i = 0; i < tevOrder.Count; i++)
            {
                var color = r.bp[(int)GX.BPRegister.TEV_COLOR_ENV_0_ID + (i * 2)];

                var colorInD    = (GX.CC)((color >> 0) & 0x0F);
                var colorInC    = (GX.CC)((color >> 4) & 0x0F);
                var colorInB    = (GX.CC)((color >> 8) & 0x0F);
                var colorInA    = (GX.CC)((color >> 12) & 0x0F);
                var colorBias   = (GX.TevBias)((color >> 16) & 0x03);
                bool colorSub   = ((color >> 18) & 0x01) != 0;
                bool colorClamp = ((color >> 19) & 0x01) != 0;
                var colorScale  = (GX.TevScale)((color >> 20) & 0x03);
                var colorRegId  = (GX.Register)((color >> 22) & 0x03);

                var colorOp = findTevOp(colorBias, colorScale, colorSub);

                var alpha = r.bp[(int)GX.BPRegister.TEV_ALPHA_ENV_0_ID + (i * 2)];

                var rswap = (alpha >> 0) & 0x03;
                var tswap = (alpha >> 2) & 0x03;

                var alphaInD    = (GX.CA)((alpha >> 4) & 0x07);
                var alphaInC    = (GX.CA)((alpha >> 7) & 0x07);
                var alphaInB    = (GX.CA)((alpha >> 10) & 0x07);
                var alphaInA    = (GX.CA)((alpha >> 13) & 0x07);
                var alphaBias   = (GX.TevBias)((alpha >> 16) & 0x03);
                bool alphaSub   = ((alpha >> 18) & 0x01) != 0;
                bool alphaClamp = ((alpha >> 19) & 0x01) != 0;
                var alphaScale  = (GX.TevScale)((alpha >> 20) & 0x03);
                var alphaRegId  = (GX.Register)((alpha >> 22) & 0x03);

                var alphaOp = findTevOp(alphaBias, alphaScale, alphaSub);

                var ksel = r.bp[(int)GX.BPRegister.TEV_KSEL_0_ID + (i >> 1)];
                var konstColorSel = (GX.KonstColorSel)((((i & 1) == 1) ? (ksel >> 14) : (ksel >> 4)) & 0x1F);
                var konstAlphaSel = (GX.KonstAlphaSel)((((i & 1) == 1) ? (ksel >> 19) : (ksel >> 9)) & 0x1F);

                var indCmd = r.bp[(int)GX.BPRegister.IND_CMD0_ID + i];
                var indTexStage      = (GX.IndTexStageID)((indCmd >> 0) & 0x03);
                var indTexFormat     = (GX.IndTexFormat)((indCmd >> 2) & 0x03);
                var indTexBiasSel    = (GX.IndTexBiasSel)((indCmd >> 4) & 0x03);
                var indTexAlphaSel   = (GX.IndTexAlphaSel)((indCmd >> 7) & 0x03);
                var indTexMatrix     = (GX.IndTexMtxID)((indCmd >> 9) & 0x0F);
                var indTexWrapS      = (GX.IndTexWrap)((indCmd >> 13) & 0x07);
                var indTexWrapT      = (GX.IndTexWrap)((indCmd >> 16) & 0x07);
                var indTexUseOrigLOD = ((indCmd >> 19) & 0x01) != 0;
                var indTexAddPrev    = ((indCmd >> 20) & 0x01) != 0;

                var rasSwapTableRG = r.bp[(int)GX.BPRegister.TEV_KSEL_0_ID + (rswap * 2)];
                var rasSwapTableBA = r.bp[(int)GX.BPRegister.TEV_KSEL_0_ID + (rswap * 2) + 1];

                var rasSwapTable = new SwapTable(
                  (rasSwapTableRG >> 0) & 0x03,
                  (rasSwapTableRG >> 2) & 0x03,
                  (rasSwapTableBA >> 0) & 0x03,
                  (rasSwapTableBA >> 2) & 0x03);

                var texSwapTableRG = r.bp[(int)GX.BPRegister.TEV_KSEL_0_ID + (tswap * 2)];
                var texSwapTableBA = r.bp[(int)GX.BPRegister.TEV_KSEL_0_ID + (tswap * 2) + 1];

                var texSwapTable = new SwapTable(
                    (texSwapTableRG >> 0) & 0x03,
                    (texSwapTableRG >> 2) & 0x03,
                    (texSwapTableBA >> 0) & 0x03,
                    (texSwapTableBA >> 2) & 0x03);

                stages[i] = new TevStage()
                {
                    colorStage = new TevStage.ColorStage()
                    {
                        A = colorInA, B = colorInB, C = colorInC, D = colorInD,
                        Scale = colorScale, Bias = colorBias, Clamp = colorClamp, 
                        Op = colorOp, Output = colorRegId,
                    },
                    alphaStage = new TevStage.AlphaStage()
                    {
                        A = alphaInA,
                        B = alphaInB,
                        C = alphaInC,
                        D = alphaInD,
                        Scale = alphaScale,
                        Bias = alphaBias,
                        Clamp = alphaClamp,
                        Op = alphaOp,
                        Output = alphaRegId,
                    },
                    TexCoordID = tevOrder[i].texCoordID,
                    TexMap = tevOrder[i].texMapID,
                    ChannelID = tevOrder[i].channelID,

                    KonstColorSel = konstColorSel,
                    KonstAlphaSel = konstAlphaSel,
                    RasSwapTable = rasSwapTable,
                    TexSwapTable = texSwapTable,

                    indirectStage = new TevStage.IndirectStage()
                    {
                        Stage = indTexStage,
                        BiasSel = indTexBiasSel,
                        Format = indTexFormat,
                        Matrix = indTexMatrix,
                        AlphaSel = indTexAlphaSel,
                        WrapS = indTexWrapS,
                        WrapT = indTexWrapT,
                        AddPrev = indTexAddPrev,
                        UseOrigLOD = indTexUseOrigLOD,
                    },
                };
            }
            return stages;
        }

        static GX.TevOp findTevOp(GX.TevBias bias, GX.TevScale scale, bool sub)
        {
            if (bias == GX.TevBias.HWB_COMPARE) {
                switch ((int)scale)
                {
                    case 0: return sub ? GX.TevOp.COMP_R8_EQ : GX.TevOp.COMP_R8_GT;
                    case 1: return sub ? GX.TevOp.COMP_GR16_EQ : GX.TevOp.COMP_GR16_GT;
                    case 2: return sub ? GX.TevOp.COMP_BGR24_EQ : GX.TevOp.COMP_BGR24_GT;
                    case 3: return sub ? GX.TevOp.COMP_RGB8_EQ : GX.TevOp.COMP_RGB8_GT;
                    default:
                        return GX.TevOp.ADD;
                }
            } else
            {
                return sub ? GX.TevOp.SUB : GX.TevOp.ADD;
            }
        }


        /// <summary>
        /// Reads raster operation data from a display list.
        /// </summary>
        public static RopInfo ParseRopInfo(DisplayListRegisters r)
        {
            //TODO
            var fogType = GX.FogType.NONE;
            var fogAdjEnabled = false;

            // Blend mode.
            var cm0 = r.bp[(int)GX.BPRegister.PE_CMODE0_ID];
            bool bmboe = ((cm0 >> 0) & 0x01) != 0; //sub/blend
            bool bmloe = ((cm0 >> 1) & 0x01) != 0; //logic
            bool bmbop = ((cm0 >> 11) & 0x0) != 0; //blend

            var blendMode =  bmboe ? (bmbop ? GX.BlendMode.SUBTRACT : GX.BlendMode.BLEND) :
                             bmloe ? GX.BlendMode.LOGIC : GX.BlendMode.NONE; 

            var blendDstFactor = (GX.BlendFactor)((cm0 >> 5) & 0x07);
            var blendSrcFactor = (GX.BlendFactor)((cm0 >> 8) & 0x07);
            var blendLogicOp = (GX.LogicOp)((cm0 >> 12) & 0x0F);

            // Depth state.
            var zm = r.bp[(int)GX.BPRegister.PE_ZMODE_ID];
            var depthTest = ((zm >> 0) & 0x01) != 0;
            var depthFunc = (zm >> 1) & 0x07;
            var depthWrite = ((zm >> 4) & 0x01) != 0;

            var colorUpdate = true; var alphaUpdate = false;

            return new RopInfo()
            {
                FogAdjEnabled = fogAdjEnabled,
                FogType = fogType,
                DepthTest = new DepthTest()
                {
                    Test = depthTest,
                    Write = depthWrite,
                    Function = (GX.CompareType)depthFunc,
                },
                BlendMode = new BlendMode()
                {
                    Dest = blendDstFactor,
                    Source = blendSrcFactor,
                    LogicOp = blendLogicOp,
                    Mode = blendMode,
                },
            };
        }

        /// <summary>
        /// Reads alpha comparison data from a display list.
        /// </summary>
        public static AlphaComparison ParseAlphaTest(DisplayListRegisters r)
        {
            var ap = r.bp[(int)GX.BPRegister.TEV_ALPHAFUNC_ID];
            return new AlphaComparison()
            {
                refLeft   = (byte)((ap >> 0) & 0xFF),
                refRight  = (byte)((ap >> 8) & 0xFF),
                CompLeft  = (GX.CompareType)((ap >> 16) & 0x07),
                compRight = (GX.CompareType)((ap >> 19) & 0x07),
                Op        = (GX.AlphaOp)((ap >> 22) & 0x07),
            }; 
        }
    }
}
