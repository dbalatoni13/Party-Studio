using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace GCNRenderLibrary.Rendering
{
    public class GXMaterial
    {
        public string Frag;
        public string Vert;

        public string Name;

        public bool Updated = false;

        public bool HasPostTexMtx = false;

        //The rendered scene attached to the material.
        //Use to reload shader data
        public SceneNode RenderScene;

        //Polygon state
        public GX.CullMode CullMode = GX.CullMode.BACK;

        //Vertex state

        public TexCoordGen[] TexGens = new TexCoordGen[8];
        //Color0, Alpha0, Color1, Alpha1

        public RGBA[] AmbientColor = new RGBA[2];
        public RGBA[] MaterialColor = new RGBA[2];

        public ChannelControl[] ColorChannelControls = new ChannelControl[4];

        //TEV State
        public TevStage[] Stages = new TevStage[1];
        // Indirect TEV state
        public IndirectStage[] IndirectStages = new IndirectStage[4];

        public AlphaComparison AlphaCompare = new AlphaComparison();

        // Raster / blend state. 
        public RopInfo RopInfo = new RopInfo();

        public RGBA[] TevKonstColors = new RGBA[4];
        public RGBA[] TevColors = new RGBA[4];

        public bool EarlyZComparison = true;

        public bool Dither = false;
        public bool XLU = false;

        public bool UseSkinning = false;

        public IndirectMatrix[] IndirectMatrices = new IndirectMatrix[3];

        public GXSampler[] Textures = new GXSampler[8];

        public GXTextureMatrix[] TextureMatrices = new GXTextureMatrix[8];


        public GXMaterial()
        {
            for (int i = 0; i < AmbientColor.Length; i++)
                AmbientColor[i] = new RGBA(255, 255, 255, 255);
            for (int i = 0; i < MaterialColor.Length; i++)
                MaterialColor[i] = new RGBA(255, 255, 255, 255);

            for (int i = 0; i < ColorChannelControls.Length; i++)
                ColorChannelControls[i] = new ChannelControl();

            for (int i = 0; i < TextureMatrices.Length; i++)
                TextureMatrices[i] = new GXTextureMatrix();

            for (int i = 0; i < TexGens.Length; i++)
                TexGens[i] = new TexCoordGen();

            for (int i = 0; i < IndirectStages.Length; i++)
                IndirectStages[i] = new IndirectStage();
            for (int i = 0; i < IndirectMatrices.Length; i++)
                IndirectMatrices[i] = new IndirectMatrix();

            Stages[0] = new TevStage();
            Stages[0].colorStage.A = GX.CC.RASC;
            Stages[0].colorStage.B = GX.CC.ZERO;
            Stages[0].colorStage.C = GX.CC.ZERO;
            Stages[0].colorStage.D = GX.CC.ZERO;
            Stages[0].colorStage.Op = GX.TevOp.ADD;
            Stages[0].colorStage.Clamp = true;
            Stages[0].colorStage.Output = GX.Register.PREV;

            Stages[0].alphaStage.A = GX.CA.RASA;
            Stages[0].alphaStage.B = GX.CA.ZERO;
            Stages[0].alphaStage.C = GX.CA.ZERO;
            Stages[0].alphaStage.D = GX.CA.ZERO;
            Stages[0].alphaStage.Op = GX.TevOp.ADD;
            Stages[0].alphaStage.Clamp = true;
            Stages[0].alphaStage.Output = GX.Register.PREV;

            for (int i = 0; i < TevKonstColors.Length; i++)
                TevKonstColors[i] = new RGBA(0, 0, 0, 0);
            for (int i = 0; i < TevColors.Length; i++)
                TevColors[i] = new RGBA(0, 0, 0, 0);
        }

        public void SetTevKColorSel(int stageID, GX.KonstColorSel sel) => this.Stages[stageID].KonstColorSel = sel;
        public void SetTevKAlphaSel(int stageID, GX.KonstAlphaSel sel) => this.Stages[stageID].KonstAlphaSel = sel;

        public void SetTexCoordGen2(int index, GX.TexGenType func, GX.TexGenSrc src,
            GX.TexGenMatrix matrix, bool normalize, GX.PostTexGenMatrix postMatrix)
        {
            this.TexGens[index].SourceParam = src;
            this.TexGens[index].Function = func;
            this.TexGens[index].Normalize = normalize;
            this.TexGens[index].Matrix = matrix;
            this.TexGens[index].PostMatrix = postMatrix;

            //Hack for now as this tool uses the wrong coords, oops
            this.TexGens[index].Matrix = GX.TexGenMatrix.IDENTITY;
            this.TexGens[index].PostMatrix = GX.PostTexGenMatrix.PTIDENTITY;

            if (matrix == GX.TexGenMatrix.TEXMTX0) this.TexGens[index].PostMatrix = GX.PostTexGenMatrix.PTTEXMTX0;
            if (matrix == GX.TexGenMatrix.TEXMTX1) this.TexGens[index].PostMatrix = GX.PostTexGenMatrix.PTTEXMTX1;
            if (matrix == GX.TexGenMatrix.TEXMTX2) this.TexGens[index].PostMatrix = GX.PostTexGenMatrix.PTTEXMTX2;
            if (matrix == GX.TexGenMatrix.TEXMTX3) this.TexGens[index].PostMatrix = GX.PostTexGenMatrix.PTTEXMTX3;
            if (matrix == GX.TexGenMatrix.TEXMTX4) this.TexGens[index].PostMatrix = GX.PostTexGenMatrix.PTTEXMTX4;
            if (matrix == GX.TexGenMatrix.TEXMTX5) this.TexGens[index].PostMatrix = GX.PostTexGenMatrix.PTTEXMTX5;
            if (matrix == GX.TexGenMatrix.TEXMTX6) this.TexGens[index].PostMatrix = GX.PostTexGenMatrix.PTTEXMTX6;
            if (matrix == GX.TexGenMatrix.TEXMTX7) this.TexGens[index].PostMatrix = GX.PostTexGenMatrix.PTTEXMTX7;
        }

        public void GXSetTevColor(int index, RGBA color) => TevColors[index] = color;

        public void GXSetTevOrder(int stage, GX.TexCoordID texCoord, GX.TexMapID texMap, GX.ChannelID rasterChannel)
        {
            Stages[stage].TexCoordID = texCoord;
            Stages[stage].TexMap = texMap;
            //Here the channels are just combined. 
            if (rasterChannel == GX.ChannelID.COLOR0A0 || rasterChannel == GX.ChannelID.COLOR0 || rasterChannel == GX.ChannelID.ALPHA0)
                Stages[stage].ChannelID = GX.RasColorChannelID.COLOR0A0;
            if (rasterChannel == GX.ChannelID.COLOR1A1 || rasterChannel == GX.ChannelID.COLOR1 || rasterChannel == GX.ChannelID.ALPHA1)
                Stages[stage].ChannelID = GX.RasColorChannelID.COLOR1A1;
            //Zero or null output
            if (rasterChannel == GX.ChannelID.COLOR_ZERO || rasterChannel == GX.ChannelID.COLOR_NULL)
                Stages[stage].ChannelID = GX.RasColorChannelID.COLOR_ZERO;
        }

        public void GXSetTevOrder(int stage, GX.TexCoordID texCoord, GX.TexMapID texMap, GX.RasColorChannelID rasterChannel)
        {
            Stages[stage].TexCoordID = texCoord;
            Stages[stage].TexMap = texMap;
            Stages[stage].ChannelID = rasterChannel;
        }

        public void SetBlend(GX.BlendMode mode, GX.BlendFactor src, GX.BlendFactor dst)
        {
            this.RopInfo.BlendMode.Mode = mode;
            this.RopInfo.BlendMode.Source = src;
            this.RopInfo.BlendMode.Dest = dst;
        }

        public void SetBlend(GX.BlendMode mode, GX.BlendFactor src, GX.BlendFactor dst, GX.LogicOp logicOp)
        {
            this.RopInfo.BlendMode.Mode = mode;
            this.RopInfo.BlendMode.Source = src;
            this.RopInfo.BlendMode.Dest = dst;
            this.RopInfo.BlendMode.LogicOp = logicOp;
        }

        public void GXSetNumChans(int count)
        {
            if (count < 0 || count > 2)
                throw new Exception("Invalid channel count! Must be 0 or 1 or 2!");

            this.ColorChannelControls = new ChannelControl[count * 2]; //2 channels each (color/alpha)
            for (int i = 0; i < ColorChannelControls.Length; i++)
                ColorChannelControls[i] = new ChannelControl();
        }

        public void GXSetChanCtrl(GX.ChannelID channel, bool useLights,
            GX.ColorSrc ambSrc, GX.ColorSrc matSrc, uint lightState,
          GX.AttenFn atten, GX.DiffuseFn diffn)
        {
            if (!(channel >= GX.ChannelID.COLOR0 && channel <= GX.ChannelID.ALPHA1))
                throw new Exception($"Invalid channel ID! {channel}");

            int[] indices = new int[] { 0, 2, 1, 3 };
            int index = indices[(int)channel];

            this.ColorChannelControls[index].LightingEnabled = useLights;
            this.ColorChannelControls[index].LightsToggle[0] = useLights;
            this.ColorChannelControls[index].AttenuationFunction = atten;
            this.ColorChannelControls[index].DiffuseFunction = diffn;
            this.ColorChannelControls[index].AmbColorSource = ambSrc;
            this.ColorChannelControls[index].MatColorSource = matSrc;
        }

        public void SetNumTevStages(int num)
        {
            this.Stages = new TevStage[num];
            for (int i = 0; i < num; i++)
                this.Stages[i] = new TevStage();
        }

        public void SetTevTexture(int stage, GX.TexMapID tex, GX.TexCoordID coordID)
        {
            this.Stages[stage].TexMap = tex;
            this.Stages[stage].TexCoordID = coordID;
        }

        public void SetTevColorIn(int stage, GX.CC A, GX.CC B, GX.CC C, GX.CC D)
        {
            Stages[stage].colorStage.A = A;
            Stages[stage].colorStage.B = B;
            Stages[stage].colorStage.C = C;
            Stages[stage].colorStage.D = D;
        }

        public void SetTexSwapChannel(int stage, GX.TevColorChan r, GX.TevColorChan g, GX.TevColorChan b)
        {
            this.Stages[stage].TexSwapTable.R = r;
            this.Stages[stage].TexSwapTable.G = g;
            this.Stages[stage].TexSwapTable.B = b;
        }

        public void SetTexSwapChannel(int stage, GX.TevColorChan r, GX.TevColorChan g, GX.TevColorChan b, GX.TevColorChan a)
        {
            this.Stages[stage].TexSwapTable.R = r;
            this.Stages[stage].TexSwapTable.G = g;
            this.Stages[stage].TexSwapTable.B = b;
            this.Stages[stage].TexSwapTable.A = a;
        }

        public void SetRasSwapChannel(int stage, GX.TevColorChan r, GX.TevColorChan g, GX.TevColorChan b, GX.TevColorChan a)
        {
            this.Stages[stage].RasSwapTable.R = r;
            this.Stages[stage].RasSwapTable.G = g;
            this.Stages[stage].RasSwapTable.B = b;
            this.Stages[stage].RasSwapTable.A = a;
        }

        public void SetTevAlphaIn(int stage, GX.CA A, GX.CA B, GX.CA C, GX.CA D)
        {
            Stages[stage].alphaStage.A = A;
            Stages[stage].alphaStage.B = B;
            Stages[stage].alphaStage.C = C;
            Stages[stage].alphaStage.D = D;
        }

        public void SetTevColorOp(int stage, GX.TevOp op, GX.TevBias bias, GX.TevScale scale, bool clamp, GX.Register output)
        {
            Stages[stage].colorStage.Op = op;
            Stages[stage].colorStage.Bias = bias;
            Stages[stage].colorStage.Scale = scale;
            Stages[stage].colorStage.Clamp = clamp;
            Stages[stage].colorStage.Output = output;
        }

        public void SetTevAlphaOp(int stage, GX.TevOp op, GX.TevBias bias, GX.TevScale scale, bool clamp, GX.Register output)
        {
            Stages[stage].alphaStage.Op = op;
            Stages[stage].alphaStage.Bias = bias;
            Stages[stage].alphaStage.Scale = scale;
            Stages[stage].alphaStage.Clamp = clamp;
            Stages[stage].alphaStage.Output = output;
        }

        public void SetZMode(bool enable, GX.CompareType func, bool write)
        {
            RopInfo.DepthTest.Test = enable;
            RopInfo.DepthTest.Function = func;
            RopInfo.DepthTest.Write = write;
        }

        public void SetAlphaCompare(GX.CompareType leftFunc, float ref0, GX.AlphaOp op, GX.CompareType rightFunc, float ref1)
        {
            AlphaCompare.CompLeft = leftFunc;
            AlphaCompare.compRight = rightFunc;
            AlphaCompare.Op = op;
            AlphaCompare.refLeft = (byte)(ref0 * 255);
            AlphaCompare.refRight = (byte)(ref1 * 255);
        }

        public void SetUsesSkinning()
        {

        }

        public void ResetAnimation()
        {
            foreach (var tex in this.Textures)
                tex?.ResetAnimation();
            foreach (var mat in this.TextureMatrices)
                mat?.ResetAnimation();
        }
    }

    public class LightObject
    {
        public Vector3 Position;
        public Vector3 Direction;
        public Vector3 DistAtten;
        public Vector3 CosAtten;
        public Vector4 Color;

        public LightObject() { Reset(); }

        public void Reset()
        {
            Position = new Vector3();
            Direction = new Vector3(0, 0, -1);
            DistAtten = new Vector3(1, 0, 0);
            CosAtten = new Vector3(1, 0, 0);
            Color = new Vector4(0, 0, 0, 1);
        }

        public void SetSpot(float cutoff, GX.SpotFunction spotFunc)
        {
            if (cutoff <= 0 || cutoff >= 90)
                spotFunc = GX.SpotFunction.OFF;

            var cr = MathF.Cos(OpenTK.MathHelper.DegreesToRadians(cutoff));
            if (spotFunc == GX.SpotFunction.FLAT)
            {
                this.CosAtten = new Vector3(-1000.0f * cr, 1000.0f, 0.0f);
            }
            else if (spotFunc == GX.SpotFunction.COS)
            {
                this.CosAtten = new Vector3(-cr / (1.0f - cr), 1.0f / (1.0f - cr), 0.0f);
            }
            else if (spotFunc == GX.SpotFunction.COS2)
            {
                this.CosAtten = new Vector3(0.0f, -cr / (1.0f - cr), 1.0f / (1.0f - cr));
            }
            else if (spotFunc == GX.SpotFunction.SHARP)
            {
                float d = (1.0f - cr) * (1.0f - cr);
                this.CosAtten = new Vector3(cr * (cr - 2.0f) / d, 2.0f / d, -1.0f / d);
            }
            else if (spotFunc == GX.SpotFunction.RING1)
            {
                float d = (1.0f - cr) * (1.0f - cr);
                this.CosAtten = new Vector3(-4.0f * cr / d, 4.0f * (1.0f + cr) / d, -4.0f / d);
            }
            else if (spotFunc == GX.SpotFunction.RING2)
            {
                float d = (1.0f - cr) * (1.0f - cr);
                this.CosAtten = new Vector3(1.0f - 2.0f * cr * cr / d, 4.0f * cr / d, -2.0f / d);
            }
            else if (spotFunc == GX.SpotFunction.OFF)
            {
                this.CosAtten = new Vector3(1.0f, 0.0f, 0.0f);
            }
        }

        public void SetDistAttn(float refDist, float refBrightness, GX.DistAttnFunction distFunc)
        {
            if (distFunc == GX.DistAttnFunction.GENTLE)
                this.DistAtten = new Vector3(1.0f, (1.0f - refBrightness) / (refBrightness * refDist), 0.0f);
            else if (distFunc == GX.DistAttnFunction.MEDIUM)
                this.DistAtten = new Vector3(1.0f, 0.5f* (1.0f - refBrightness) / (refBrightness * refDist), 0.5f * (1.0f - refBrightness) / (refBrightness * refDist * refDist));
            else if (distFunc == GX.DistAttnFunction.STEEP)
                this.DistAtten = new Vector3(1.0f, 0.0f, (1.0f - refBrightness) / (refBrightness * refDist * refDist));
            else if (distFunc == GX.DistAttnFunction.OFF)
                this.DistAtten = new Vector3(1.0f, 0.0f, 0.0f);
        }
    }

    public class FogObject
    {
        public float A;
        public float B;
        public float C;
        public ushort[] AdjTable = new ushort[10];
        public ushort AdjustCenter = 0;
        public Vector4 Color;

        public GX.FogType FogType = GX.FogType.PERSP_LIN;

        public FogObject() { Reset(); }

        public void SetFog(GX.FogType type, float startZ, float endZ, float nearZ, float farZ)
        {
            this.FogType = type;

            var proj = ((int)type >> 3) != 0;
            if (proj) //ortho
            {
                A = (farZ - nearZ) / (endZ - startZ);
                B = 0.0f;
                C = (startZ - nearZ) / (endZ - startZ);
            }
            else
            {
                A = (farZ * nearZ) / ((farZ - nearZ) * (endZ - startZ));
                B = (farZ) / (farZ - nearZ);
                C = (startZ) / (endZ - startZ);
            }
        }

        public void Reset()
        {
            A = 0;
            B = 0;
            C = 0;
            AdjTable = new ushort[10];
            AdjustCenter = 0;
            Color = new Vector4(0);
        }
    }

    public class RopInfo
    {      
        public GX.FogType FogType = GX.FogType.NONE;
        public bool FogAdjEnabled = false;

        public BlendMode BlendMode = new BlendMode();
        public DepthTest DepthTest = new DepthTest();
    }

    public class GXTextureMatrix
    {
        private Dictionary<Track, float> Animations = new Dictionary<Track, float>();

        public Vector2 Scale = new Vector2(1, 1);
        public float Rotation = 0;
        public Vector2 Position = new Vector2(0, 0);

        public sbyte SceneCameraIndex = -1;
        public sbyte SceneLightIndex = -1;
        public MatrixMode MatrixMode = MatrixMode.Maya;
        public TextureMapMode MappingMethod = TextureMapMode.TextureCoordinates;
        public bool IdentityMatrix = true;
        public Matrix4x4 EffectMatrix = Matrix4x4.Identity;
        public CommonMappingOption Option = 0;

        public OpenTK.Matrix4 GetSRTMatrix()
        {
            var texSRT = TexMtx.computeTexSrt(
                   new OpenTK.Vector2(GetValue(Track.ScaleU, Scale.X), GetValue(Track.ScaleV, Scale.Y)),
                       GetValue(Track.Rotate, Rotation),
                   new OpenTK.Vector2(GetValue(Track.TransU, Position.X), GetValue(Track.TransV, Position.Y)),
                   MatrixMode);

            return texSRT;
        }

        public OpenTK.Matrix4 Compute(OpenTK.Matrix4 model, OpenTK.Matrix4 mtxProj)
        {
            var effectMatrix = new OpenTK.Matrix4(
                EffectMatrix.M11, EffectMatrix.M12, EffectMatrix.M13, EffectMatrix.M14,
                EffectMatrix.M21, EffectMatrix.M22, EffectMatrix.M23, EffectMatrix.M24,
                EffectMatrix.M31, EffectMatrix.M32, EffectMatrix.M33, EffectMatrix.M34,
                EffectMatrix.M41, EffectMatrix.M42, EffectMatrix.M43, EffectMatrix.M44);

            var texSRT = TexMtx.computeTexSrt(
                new OpenTK.Vector2(GetValue(Track.ScaleU, Scale.X), GetValue(Track.ScaleV, Scale.Y)),
                    GetValue(Track.Rotate, Rotation), 
                new OpenTK.Vector2(GetValue(Track.TransU, Position.X), GetValue(Track.TransV, Position.Y)),
                MatrixMode);

            return TexMtx.computeTexMtx(model, mtxProj, texSRT, effectMatrix, MappingMethod, Option);
        }

        private float GetValue(Track track, float defaultValue)
        {
            if (Animations.ContainsKey(track))
                return Animations[track];
            return defaultValue;
        }

        public void SetAnimation(Track track, float value)
        {
            if (!Animations.ContainsKey(track))
                Animations.Add(track, 0);

            Animations[track] = value;
        }

        public void ResetAnimation()
        {
            Animations.Clear();
        }

        public enum Track
        {
            TransU,
            TransV,
            Rotate,
            ScaleU,
            ScaleV,
        }
    }
    public class GXSampler
    {
        internal long StartAddress = 0;

        public string Texture { get; set; }
        public string Palette { get; set; }
        public int TextureIndex { get; set; } = -1;
        public int PaletteIndex { get; set; }
        public GX.WrapMode WrapX;
        public GX.WrapMode WrapY;
        public GX.TextureFilter MinFilter;
        public GX.TextureFilter MagFilter;
        public float LODBias;
        public AnisotropyLevel MaxAnisotropy;
        public bool ClampBias;
        public bool EdgeLOD;

        public string AnimatedTexture;

        public void ResetAnimation()
        {
            AnimatedTexture = null;
        }
    }

    public class BlendMode
    {
        public GX.BlendMode Mode = GX.BlendMode.NONE;

        public GX.BlendFactor Source = GX.BlendFactor.SRCALPHA;
        public GX.BlendFactor Dest = GX.BlendFactor.INVSRCALPHA;

        public GX.LogicOp LogicOp = GX.LogicOp.COPY;
    }

    public class DepthTest
    {
        public bool Test = true;
        public bool Write = true;
        public GX.CompareType Function = GX.CompareType.LEQUAL;
    }

    public class AlphaComparison
    {
        public GX.CompareType CompLeft = GX.CompareType.ALWAYS;
        public byte refLeft;

        public GX.AlphaOp Op = GX.AlphaOp.AND;

        public GX.CompareType compRight = GX.CompareType.ALWAYS;
        public byte refRight;
    }

    public class TevStage
    {
        public GX.TexCoordID TexCoordID;
        public GX.TexMapID TexMap;
        public GX.RasColorChannelID ChannelID;

        public GX.KonstColorSel KonstColorSel;
        public GX.KonstAlphaSel KonstAlphaSel = GX.KonstAlphaSel.KASEL_1;

        public SwapTable RasSwapTable = new SwapTable();
        public SwapTable TexSwapTable = new SwapTable();

        public ColorStage colorStage = new ColorStage();
        public AlphaStage alphaStage = new AlphaStage();
        public IndirectStage indirectStage = new IndirectStage();

        public class ColorStage
        {
            public GX.CC A = GX.CC.ZERO;
            public GX.CC B = GX.CC.ZERO;
            public GX.CC C = GX.CC.ZERO;
            public GX.CC D = GX.CC.CPREV;
            public GX.TevOp Op = GX.TevOp.ADD;
            public GX.TevBias Bias = GX.TevBias.ZERO;
            public GX.TevScale Scale = GX.TevScale.SCALE_1;
            public bool Clamp = true;
            public GX.Register Output = GX.Register.PREV;
        }

        public class AlphaStage
        {
            public GX.CA A = GX.CA.ZERO;
            public GX.CA B = GX.CA.ZERO;
            public GX.CA C = GX.CA.ZERO;
            public GX.CA D = GX.CA.APREV;
            public GX.TevOp Op = GX.TevOp.ADD;
            public GX.TevBias Bias = GX.TevBias.ZERO;
            public GX.TevScale Scale = GX.TevScale.SCALE_1;
            public bool Clamp = true;
            public GX.Register Output = GX.Register.PREV;
        }

        public class IndirectStage
        {
            public GX.IndTexStageID Stage;
            public GX.IndTexFormat Format;
            public GX.IndTexBiasSel BiasSel;
            public GX.IndTexAlphaSel AlphaSel;
            public GX.IndTexMtxID Matrix;
            public GX.IndTexWrap WrapS;
            public GX.IndTexWrap WrapT;
            public bool AddPrev;
            public bool UseOrigLOD;
        }
    }

    public class SwapTable
    {
        public GX.TevColorChan R = GX.TevColorChan.R;
        public GX.TevColorChan G = GX.TevColorChan.G;
        public GX.TevColorChan B = GX.TevColorChan.B;
        public GX.TevColorChan A = GX.TevColorChan.A;

        public SwapTable() { }

        public SwapTable(uint r, uint g, uint b, uint a)
        {
            R = (GX.TevColorChan)r;
            G = (GX.TevColorChan)g;
            B = (GX.TevColorChan)b;
            A = (GX.TevColorChan)a;
        }

        public GX.TevColorChan Get(int index)
        {
            if (index == 0) return R;
            if (index == 1) return G;
            if (index == 2) return B;
            if (index == 3) return A;
            return R;
        }
    }

    public class IndirectMatrix
    {
        public Vector2 Scale = new Vector2(0.5f, 0.5f);
        public float Rotate;
        public Vector2 Translate = new Vector2();

        public OpenTK.Matrix2x4 compute()
        {
            //construct a 2x3 matrix
            float theta = Rotate / 180.0f * 3.141592f;
            float sinR = MathF.Sin(theta);
            float cosR = MathF.Cos(theta);
            float center = 0.0f;

            var dst =  new Vector3[2];
            dst[0].X = Scale.X * cosR;
            dst[0].Y = Scale.X * -sinR;
            dst[0].Z = Translate.X + center + Scale.X * (sinR * center - cosR * center);

            dst[1].X = Scale.Y * cosR;
            dst[1].Y = Scale.Y * sinR;
            dst[1].Z = Translate.Y + center + -Scale.Y * (-sinR * center + cosR * center);

            return new OpenTK.Matrix2x4(
                new OpenTK.Vector4(dst[0].X, dst[0].Y, dst[0].Z, 0),
                new OpenTK.Vector4(dst[1].X, dst[1].Y, dst[1].Z, 0));
        }
    }

    public struct IndirectTextureScalePair
    {
        public GX.IndTexScale U;
        public GX.IndTexScale V;
    }

    public class IndirectStage
    {
        public GX.IndTexScale ScaleS;
        public GX.IndTexScale ScaleT;

        public GX.TexMapID TextureMapID;
        public GX.TexCoordID TexCoordId;
    }

    public class TexCoordGen
    {
        public GX.TexGenType Function = GX.TexGenType.MTX2x4;

        public GX.TexGenSrc SourceParam = GX.TexGenSrc.TEX0;

        public GX.TexGenMatrix Matrix = GX.TexGenMatrix.IDENTITY;

        public GX.PostTexGenMatrix PostMatrix = GX.PostTexGenMatrix.PTIDENTITY;

        public bool Normalize = false;
    }

    public class LightingChannelControl
    {
        public ChannelControl ColorChannel = new ChannelControl();
        public ChannelControl AlphaChannel = new ChannelControl();
    }

    public class ChannelControl
    {
        internal uint _channelCtrl;

        public uint ToUint32() => _channelCtrl;

        public bool MaterialVertexColors
        {
            get { return MatColorSource == GX.ColorSrc.VTX; }
            set
            {
                if (value)
                    MatColorSource = GX.ColorSrc.VTX;
                else
                    MatColorSource = GX.ColorSrc.REG;
            }
        }

        public bool AmbientVertexColors
        {
            get { return AmbColorSource == GX.ColorSrc.VTX; }
            set
            {
                if (value)
                    AmbColorSource = GX.ColorSrc.VTX;
                else
                    AmbColorSource = GX.ColorSrc.REG;
            }
        }

        public GX.ColorSrc MatColorSource;

        public GX.ColorSrc AmbColorSource;

        public GX.DiffuseFn DiffuseFunction;

        public bool LightingEnabled;

        public int LitMaskL;
        public int LitMaskH;
        public int LitMask;

        public bool[] LightsToggle = new bool[8];

        private bool AttnEn;
        public bool AttnSelect;

        public GX.AttenFn AttenuationFunction;

        public ChannelControl() { }

        public ChannelControl(uint chanCtrl) {
            _channelCtrl = chanCtrl;
            Setup((int)chanCtrl);
        }

        private void Setup(int chanCtrl)
        {
            MatColorSource = (GX.ColorSrc)((chanCtrl >> 0) & 0x01);
            LightingEnabled = ((chanCtrl >> 1) & 0x01) != 0;
            LitMaskL = (chanCtrl >> 2) & 0x0F;
            LitMaskH = (chanCtrl >> 11 & 0x0F);
            LitMask = (LitMaskH << 4) | LitMaskL;
            AmbColorSource = (GX.ColorSrc)((chanCtrl >> 6 & 0x01));
            DiffuseFunction = (GX.DiffuseFn)((chanCtrl >> 7 & 0x03));
            AttnEn = (chanCtrl >> 9 & 0x01) != 0;
            AttnSelect = (chanCtrl >> 10 & 0x01) != 0;
            AttenuationFunction = AttnEn ? (AttnSelect ? GX.AttenFn.SPOT : GX.AttenFn.SPEC) : GX.AttenFn.NONE;
        }
    }
}
