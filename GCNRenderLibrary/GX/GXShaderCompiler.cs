using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using GLFrameworkEngine;
using OpenTK;

namespace GCNRenderLibrary.Rendering
{
    internal class GXShaderCompiler
    {
        public GXShaderCompiler()
        {
            if (UniformMaterialParams.Size != 1536)
                throw new Exception("Bad sized UParam!");
        }

        static Vector4 colorConvert(RGBA color)
        {
            return new Vector4(
                color.R / 255.0f,
                color.G / 255.0f,
                color.B / 255.0f,
                color.A / 255.0f);
        }

        public class VertexAttributeGenDef
        {
            public VertexAttributeInput Attribute;
            public string Name;
            public string Type;
            public uint Count;

            public VertexAttributeGenDef(VertexAttributeInput attr, string name, string type, uint count)
            {
                Attribute = attr;
                Name = name;
                Type = type;
                Count = count;
            }
        }

        public static VertexAttributeGenDef[] vtxAttributeGenDefs = new VertexAttributeGenDef[19]
        {
            new VertexAttributeGenDef(VertexAttributeInput.POS, "Position", "Float", 3),
            new VertexAttributeGenDef(VertexAttributeInput.PNMTXIDX, "PnMtxIdx", "Float", 1),
            new VertexAttributeGenDef(VertexAttributeInput.TEX0MTXIDX, "TexMtx0123Idx", "Float", 4),
            new VertexAttributeGenDef(VertexAttributeInput.TEX4MTXIDX, "TexMtx4567Idx", "Float", 4),
            new VertexAttributeGenDef(VertexAttributeInput.NRM, "Normal", "Float", 3),
            new VertexAttributeGenDef(VertexAttributeInput.TANGENT, "Tangent", "Float", 3),
            new VertexAttributeGenDef(VertexAttributeInput.BINRM, "Binormal", "Float", 3),
            new VertexAttributeGenDef(VertexAttributeInput.BoneIndex, "BoneIndex", "Float", 4),
            new VertexAttributeGenDef(VertexAttributeInput.BoneWeight, "BoneWeight", "Float", 4),
            new VertexAttributeGenDef(VertexAttributeInput.CLR0, "Color0", "Float", 4),
            new VertexAttributeGenDef(VertexAttributeInput.CLR1, "Color1", "Float", 4),
            new VertexAttributeGenDef(VertexAttributeInput.TEX0, "Tex0", "Float", 2),
            new VertexAttributeGenDef(VertexAttributeInput.TEX1, "Tex1", "Float", 2),
            new VertexAttributeGenDef(VertexAttributeInput.TEX2, "Tex2", "Float", 2),
            new VertexAttributeGenDef(VertexAttributeInput.TEX3, "Tex3", "Float", 2),
            new VertexAttributeGenDef(VertexAttributeInput.TEX4, "Tex4", "Float", 2),
            new VertexAttributeGenDef(VertexAttributeInput.TEX5, "Tex5", "Float", 2),
            new VertexAttributeGenDef(VertexAttributeInput.TEX6, "Tex6", "Float", 2),
            new VertexAttributeGenDef(VertexAttributeInput.TEX7, "Tex7", "Float", 2),
        };

        public static string GetAttributeUniform(VertexAttributeInput attr) {
            return $"a_{vtxAttributeGenDefs.FirstOrDefault(x => x.Attribute == attr).Name}";
        }

        static string generateDirectSkinning()
        {
            return @"
vec3 skin(vec3 pos, ivec4 index)
{
    vec4 newPosition = vec4(pos.xyz, 1.0);

    newPosition = bones[index.x] * vec4(pos, 1.0) * a_BoneWeight.x;
    newPosition += bones[index.y] * vec4(pos, 1.0) * a_BoneWeight.y;
    newPosition += bones[index.z] * vec4(pos, 1.0) * a_BoneWeight.z;
    if (a_BoneWeight.w < 1)
		newPosition += bones[index.w] * vec4(pos, 1.0) * a_BoneWeight.w;

    return newPosition.xyz;
}

vec3 skinNRM(vec3 nr, ivec4 index)
{
    vec3 newNormal = vec3(0);
	newNormal =  mat3(bones[index.x]) * nr * a_BoneWeight.x;
	newNormal += mat3(bones[index.y]) * nr * a_BoneWeight.y;
	newNormal += mat3(bones[index.z]) * nr * a_BoneWeight.z;
	newNormal += mat3(bones[index.w]) * nr * a_BoneWeight.w;
    return newNormal;
}";
        }

        static string generateBindingsDefinition(bool postTexMtxBlock, bool lightsBlock, bool fogBlock, bool useDirectSkinning = false)
        {
return (@"
#version 400

// Expected to be constant across the entire scene.
layout(std140) uniform ub_SceneParams
{
        mat4x4 u_Projection;
        vec4 u_Misc0;
        vec4 u_SelectionParams;
        vec4 u_DebugParams;
};
#define u_SceneTextureLODBias u_Misc0[0]
struct Light
{
    vec4 Color;
    vec4 Position;
    vec4 Direction;
    vec4 DistAtten;
    vec4 CosAtten;
};

struct FogBlock {
    // A, B, C, Center
    vec4 Param;
    // 10 items
    vec4 AdjTable[3];
    // Fog color is RGB
    vec4 Color;
};

// Expected to change with each material.
layout(std140, row_major) uniform ub_MaterialParams
{
    vec4 u_ColorMatReg [2];
    vec4 u_ColorAmbReg [2];
    vec4 u_KonstColor [4];
    vec4 u_Color [4];
    mat4x3 u_TexMtx [10]; //4x3
    // SizeX, SizeY, 0, Bias
    vec4 u_TextureParams [8];
    mat4x2 u_IndTexMtx [3]; // 4x2
    // Optional parameters.)") +
    "\n" + (postTexMtxBlock? "    mat4x3 u_PostTexMtx[20];\n" : "") + // 4x3
    (lightsBlock ? "Light u_LightParams[8];\n" : "") +
    (fogBlock ? "FogBlock u_FogBlock;\n" : "") +
    "};\n" +
    "// Expected to change with each shape packet.\n" +
    "layout(std140, row_major) uniform ub_PacketParams {\n" +
    "    mat4x3 u_PosMtx[10];\n" + // 4x3
    "};\n" +

    //Uses direct bone indices instead of limiting 10 per packet
    //This is used for cpu to gpu skinning
    (useDirectSkinning ? "layout(std140, row_major) uniform ub_DirectSkinningParams {\n" +
    "    mat4 bones[170];\n" + // 4x4
    "};\n" : "") +

    "uniform sampler2D u_Texture[8];\n";
        }

        public struct UniformMaterialParams
        {
            public Vector4[] ColorMatRegs;

            public Vector4[] ColorAmbRegs;

            public Vector4[] KonstColor;

            public Vector4[] Color;

            public Matrix3x4[] TexMtx;

            public Matrix3x4[] PostTexMtx;

            // sizex, sizey, 0, bias
            public Vector4[] TexParams;

            public Matrix2x4[] IndTexMtx;

            public Light[] u_LightParams;

            public FogBlock u_FogBlock;

            public static readonly int Size = 2576;

            public void UpdatePerFrame(GXMaterial mat)
            {
                for (int i = 0; i < 4; i++)
                {
                    KonstColor[i] = colorConvert(mat.TevKonstColors[i]);
                    Color[i] = colorConvert(mat.TevColors[i]);
                }
                for (int i = 0; i < 2; i++)
                {
                    ColorMatRegs[i] = colorConvert(mat.MaterialColor[i]);
                    ColorAmbRegs[i] = colorConvert(mat.AmbientColor[i]);
                }
            }

            public void SetFogUniforms(FogObject fog)
            {
                u_FogBlock = new FogBlock()
                {
                    Param = new Vector4(fog.A, fog.B, fog.C, fog.AdjustCenter),
                    AdjTable0 = new Vector4(fog.AdjTable[0], fog.AdjTable[1], fog.AdjTable[2], fog.AdjTable[3]),
                    AdjTable1 = new Vector4(fog.AdjTable[4], fog.AdjTable[5], fog.AdjTable[6], fog.AdjTable[7]),
                    AdjTable2 = new Vector4(fog.AdjTable[8], fog.AdjTable[9], 0, 0),
                    Color = new Vector4(fog.Color.X, fog.Color.Y, fog.Color.Z, 1.0f),
                };
            }

            public void SetLightUniforms(LightObject light, int setID)
            {
                u_LightParams[setID] = new Light()
                {
                    Position = new Vector4(light.Position.X, light.Position.Y, light.Position.Z, 0),
                    Direction = new Vector4(light.Direction.X, light.Direction.Y, light.Direction.Z, 0),
                    CosAtten = new Vector4(light.CosAtten.X, light.CosAtten.Y, light.CosAtten.Z, 0),
                    DistAtten = new Vector4(light.DistAtten.X, light.DistAtten.Y, light.DistAtten.Z, 0),
                    Color = new Vector4(light.Color.X, light.Color.Y, light.Color.Z, light.Color.W),
                };
            }

            public void SetUniformsFromMaterial(GXMaterial mat)
            {
                ColorMatRegs = new Vector4[2];
                ColorAmbRegs = new Vector4[2];
                KonstColor = new Vector4[4];
                Color = new Vector4[4];
                IndTexMtx = new Matrix2x4[3];
                u_LightParams = new Light[8];
                TexMtx = new Matrix3x4[10];
                PostTexMtx = new Matrix3x4[20];

                if (TexParams?.Length != 8)
                    TexParams = new Vector4[8];

                for (int i = 0; i < TexMtx.Length; i++)
                    TexMtx[i] = new Matrix3x4(
                        new Vector4(1, 0, 0, 0),
                        new Vector4(0, 1, 0, 0),
                        new Vector4(0, 0, 1, 0));

                for (int i = 0; i < PostTexMtx.Length; i++)
                    PostTexMtx[i] = new Matrix3x4(
                        new Vector4(1, 0, 0, 0),
                        new Vector4(0, 1, 0, 0),
                        new Vector4(0, 0, 1, 0));

                for (int i = 0; i < 2; i++)
                {
                    //TODO
                    ColorMatRegs[i] = new Vector4(1.0f);
                    ColorAmbRegs[i] = new Vector4(1.0f);
                }
                for (int i = 0; i < 4; i++)
                {
                    KonstColor[i] = colorConvert(mat.TevKonstColors[i]);
                    Color[i] = colorConvert(mat.TevColors[i]);
                }

                for (int i = 0; i < mat.IndirectMatrices.Length; i++)
                    IndTexMtx[i] = mat.IndirectMatrices[i].compute();

                for (int i = 0; i < 8; i++)
                    u_LightParams[i] = new Light()
                    {
                        Direction = new Vector4(0, 0, -1, 0),
                    };
            }

            //Sets the uniform block with data from this
            public void Fill(GLUniformBlock block, bool hasPostTexMtx)
            {
                //Need to use sub buffers for arrays
                int offset = 0;
                block.Update(ColorMatRegs, offset, 16); offset += ColorMatRegs.Length * 16;
                block.Update(ColorAmbRegs, offset, 16); offset += ColorAmbRegs.Length * 16;
                block.Update(KonstColor, offset, 16); offset += KonstColor.Length * 16;
                block.Update(Color, offset, 16); offset += Color.Length * 16;
                block.Update(TexMtx, offset, 48); offset += TexMtx.Length * 48;
                block.Update(TexParams, offset, 16); offset += TexParams.Length * 16;
                block.Update(IndTexMtx, offset, 32); offset += IndTexMtx.Length * 32;

                if (hasPostTexMtx)
                {
                    block.Update(PostTexMtx, offset, 48); offset += PostTexMtx.Length * 48;
                }
                block.Update(u_LightParams, offset, Light.Size); offset += u_LightParams.Length * Light.Size;
                block.Update(u_FogBlock, offset, FogBlock.Size); offset += FogBlock.Size;
            }
        }

        public struct Light
        {
            public Vector4 Color;
            public Vector4 Position;
            public Vector4 Direction;
            public Vector4 DistAtten;
            public Vector4 CosAtten;

            public static readonly int Size = 5 * 16;
        }

        public struct FogBlock
        {
            // A, B, C, Center
            public Vector4 Param;
            // 10 items
            public Vector4 AdjTable0;
            public Vector4 AdjTable1;
            public Vector4 AdjTable2;
            // Fog color is RGB
            public Vector4 Color;

            public static readonly int Size = 16 * 5;
        }

        public struct UniformSceneParams
        {
            public Matrix4 projection;
            public Vector4 Misc0;

            public static readonly int Size = 64 + 16 + 16 + 16;
        }

        public class DirectSkinningParams
        {
            public Matrix4[] bones = new Matrix4[200];

            public DirectSkinningParams()
            {
                //Default to identity matrices
                for (int i = 0; i < 200; i++)
                    bones[i] = Matrix4.Identity;
            }

            //Sets the uniform block with data from this
            public void Fill(GLUniformBlock block)
            {
                //Need to use sub buffers for arrays
                block.Update(bones, 0, 64);
            }

            public static readonly int Size = 64 * 170;
        }

        public class PacketParams
        {
            public Matrix3x4[] posMtx = new Matrix3x4[10];

            public PacketParams()
            {
                //Default to identity matrices
                for (int i = 0; i < 10; i++)
                    posMtx[i] = new Matrix3x4(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0));
            }

            //Sets the uniform block with data from this
            public void Fill(GLUniformBlock block)
            {
                //Need to use sub buffers for arrays
                block.Update(posMtx, 0, 48);
            }

            public static readonly int Size = 48 * 10;
        }

        public class GXProgram
        {
            public GXMaterial mMaterial;

            public bool usePnMtxIdx = true;
            public bool[] useTexMtxIdx = new bool[16];
            public bool hasLightsBlock = true;
            public bool hasFogBlock = true;
            public bool hasDirectSkinning = false;

            string generateBoth() => generateBindingsDefinition(mMaterial.HasPostTexMtx, hasLightsBlock, hasFogBlock, hasDirectSkinning);

            public uint calcParamsBlockSize()
            {
                uint size = 4 * 2 + 4 * 2 + 4 * 4 + 4 * 4 + 4 * 3 * 10 + 4 * 2 * 3 + 4 * 8;
                if (mMaterial.HasPostTexMtx)
                    size += 4 * 3 * 20;
                if (hasLightsBlock)
                    size += 4 * 5 * 8;
                if (hasFogBlock)
                    size += 4 * 5;
                return size;
            }

            static string generateTexMtxIdxAttr(int index)
            {
                switch (index)
                {
                    case 0: return "a_TexMtx0123Idx.x";
                    case 1: return "a_TexMtx0123Idx.y";
                    case 2: return "a_TexMtx0123Idx.z";
                    case 3: return "a_TexMtx0123Idx.w";
                    case 4: return "a_TexMtx4567Idx.x";
                    case 5: return "a_TexMtx4567Idx.y";
                    case 6: return "a_TexMtx4567Idx.z";
                    case 7: return "a_TexMtx4567Idx.w";
                    default:
                        return "INVALID_TEX_MTX_ID";
                }
            }

            static string generateTexGenSource(GX.TexGenSrc src)
            {
                switch (src)
                {
                    case GX.TexGenSrc.POS: return "vec4(a_Position, 1.0)";
                    case GX.TexGenSrc.NRM: return "vec4(a_Normal, 1.0)";
                    case GX.TexGenSrc.BINRM: return "vec4(a_Binormal, 1.0)";
                    case GX.TexGenSrc.TANGENT: return "vec4(a_Tangent, 1.0)";
                    case GX.TexGenSrc.COLOR0: return "v_Color0";
                    case GX.TexGenSrc.COLOR1: return "v_Color1";
                    case GX.TexGenSrc.TEX0: return "vec4(a_Tex0, 1.0, 1.0)";
                    case GX.TexGenSrc.TEX1: return "vec4(a_Tex1, 1.0, 1.0)";
                    case GX.TexGenSrc.TEX2: return "vec4(a_Tex2, 1.0, 1.0)";
                    case GX.TexGenSrc.TEX3: return "vec4(a_Tex3, 1.0, 1.0)";
                    case GX.TexGenSrc.TEX4: return "vec4(a_Tex4, 1.0, 1.0)";
                    case GX.TexGenSrc.TEX5: return "vec4(a_Tex5, 1.0, 1.0)";
                    case GX.TexGenSrc.TEX6: return "vec4(a_Tex6, 1.0, 1.0)";
                    case GX.TexGenSrc.TEX7: return "vec4(a_Tex7, 1.0, 1.0)";
                    case GX.TexGenSrc.TEXCOORD0: return "vec4(v_TexCoord0, 1.0)";
                    case GX.TexGenSrc.TEXCOORD1: return "vec4(v_TexCoord1, 1.0)";
                    case GX.TexGenSrc.TEXCOORD2: return "vec4(v_TexCoord2, 1.0)";
                    case GX.TexGenSrc.TEXCOORD3: return "vec4(v_TexCoord3, 1.0)";
                    case GX.TexGenSrc.TEXCOORD4: return "vec4(v_TexCoord4, 1.0)";
                    case GX.TexGenSrc.TEXCOORD5: return "vec4(v_TexCoord5, 1.0)";
                    case GX.TexGenSrc.TEXCOORD6: return "vec4(v_TexCoord6, 1.0)";
                    default:
                        return "INVALID_TEX_GEN_SRC";
                }
            }

            private string generateTexGenType(TexCoordGen texCoordGen, int id, string src)
            {
                switch (texCoordGen.Function)
                {
                    case GX.TexGenType.SRTG:
                        return $"vec3({src}.xy, 1.0)";
                    case GX.TexGenType.MTX2x4:
                        return $"vec3({generateTexGenMatrixMult(texCoordGen, id, src)}.xy, 1.0)";
                    case GX.TexGenType.MTX3x4:
                        return $"{generateTexGenMatrixMult(texCoordGen, id, src)}";
                    case GX.TexGenType.BUMP0:
                    case GX.TexGenType.BUMP1:
                    case GX.TexGenType.BUMP2:
                    case GX.TexGenType.BUMP3:
                    case GX.TexGenType.BUMP4:
                    case GX.TexGenType.BUMP5:
                    case GX.TexGenType.BUMP6:
                    case GX.TexGenType.BUMP7:
                        return $"{generateTexGenBump(texCoordGen, id, src)}";
                    default:
                        return "INVALID_TEX_GEN_TYPE";
                }
            }

            private string generateTexGenBump(TexCoordGen texCoordGen, int id, string src)
            {
                var lightIdx = (int)(texCoordGen.Function - GX.TexGenType.BUMP0);
                var lightDir = $"normalize(u_LightParams[{lightIdx}].Position.xyz - v_Position.xyz)";
                var b = this.generateMulNrm(1);
                var t = this.generateMulNrm(2);
                return $"{src}.xyz";

                return $"{src}.xyz + vec3(dot({lightDir}, {b}.xyz), dot({lightDir}, {t}.xyz), 0.0)";
            }

            private string generatePostTexGenMatrixMult(TexCoordGen texCoordGen, int id, string src)
            {
                //TODO
                if (texCoordGen.PostMatrix == GX.PostTexGenMatrix.PTIDENTITY)
                {
                    return src + ".xyz";
                }
                else if (texCoordGen.PostMatrix >= GX.PostTexGenMatrix.PTTEXMTX0)
                {
                    uint texMtxIdx = (uint)(texCoordGen.PostMatrix - GX.PostTexGenMatrix.PTTEXMTX0) / 3;
                    return $"u_PostTexMtx[{texMtxIdx}] * {src}";
                }
                else
                    return "INVALID_POST_TEX_COORD";
            }

            private string generateTexGenMatrixMult(TexCoordGen texCoordGen, int id, string src)
            {
                // TODO: Will ID ever be different from index?

                // Dynamic TexMtxIdx is off by default.
                if (useTexMtxIdx[id])
                {
                    string attrStr = generateTexMtxIdxAttr(id);
                    return generateMulPntMatrixDynamic($"uint({attrStr})", src);
                }
                else
                    return generateMulPntMatrixStatic(texCoordGen.Matrix, src);
            }

            private string generateTexGenNrm(TexCoordGen texCoordGen, int id)
            {
                var src = generateTexGenSource(texCoordGen.SourceParam);
                var type = generateTexGenType(texCoordGen, id, src);
                if (texCoordGen.Normalize)
                    return "normalize(" + type + ")";
                else
                    return type;
            }

            private string generateTexGenPost(TexCoordGen texCoordGen, int id)
            {
                var src = generateTexGenNrm(texCoordGen, id);
                if (!mMaterial.HasPostTexMtx || texCoordGen.PostMatrix == GX.PostTexGenMatrix.PTIDENTITY)
                    return src;
                else
                    return generatePostTexGenMatrixMult(texCoordGen, id, $"vec4({src}, 1.0)");
            }

            private string generateTexGen(TexCoordGen texCoordGen, int id)
            {
                return $"v_TexCoord{id} = {generateTexGenPost(texCoordGen, id)};\n";
            }

            private string generateTexGens()
            {
                string output = "";

                var texGens = mMaterial.TexGens;
                for (int i = 0; i < texGens.Length; i++)
                    output += generateTexGen(texGens[i], i);

                return output;
            }

            private string generateTexCoordGetters()
            {
                string output = "";
                for (int i = 0; i < mMaterial.TexGens.Length; i++)
                    output += "vec2 ReadTexCoord" + i + "() { return v_TexCoord" + i + ".xy / v_TexCoord" + i + ".z; }\n";
                return output;
            }

            private string generateIndTexStageScaleN(GX.IndTexScale scale)
            {
                switch (scale)
                {
                    case GX.IndTexScale._1: return "1.0";
                    case GX.IndTexScale._2: return "1.0/2.0";
                    case GX.IndTexScale._4: return "1.0/4.0";
                    case GX.IndTexScale._8: return "1.0/8.0";
                    case GX.IndTexScale._16: return "1.0/16.0";
                    case GX.IndTexScale._32: return "1.0/32.0";
                    case GX.IndTexScale._64: return "1.0/64.0";
                    case GX.IndTexScale._128: return "1.0/128.0";
                    case GX.IndTexScale._256: return "1.0/256.0";
                }
                return "INVALID";
            }

            private string generateIndTexStageScale(IndirectStage stage)
            {
                string baseCoord = $"ReadTexCoord{(int)stage.TexCoordId}()";
                if (stage.ScaleS == GX.IndTexScale._1 && stage.ScaleT == GX.IndTexScale._1)
                    return baseCoord;
                else
                    return $"{baseCoord } * vec2({generateIndTexStageScaleN(stage.ScaleS)}, {generateIndTexStageScaleN(stage.ScaleT)})";
            }

            private string generateTextureSample(uint index, string coord) {
                return $"texture(u_Texture[{index}], {coord}, TextureLODBias({index}))";
            }

            private void generateIndTexStage(StringBuilder builder, uint indTexStageIndex)
            {
                var stage = mMaterial.IndirectStages[indTexStageIndex];

                builder.Append($"    vec3 t_IndTexCoord{indTexStageIndex} = 255.0 * ");
                builder.Append(generateTextureSample((uint)stage.TexCoordId,
                               generateIndTexStageScale(stage)));

                builder.Append(".abg;\n");
            }

            private string generateIndTexStages()
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < mMaterial.IndirectStages.Length; i++)
                {
                    if ((int)mMaterial.IndirectStages[i].TexCoordId >= mMaterial.TexGens.Length)
                        continue;

                    generateIndTexStage(builder, (uint)i);
                }
                return builder.ToString();
            }

            private int generateMaterialSource(StringBuilder builder, ChannelControl channel, int i)
            {
                switch (channel.MatColorSource)
                {
                    case GX.ColorSrc.VTX: builder.Append($"a_Color{i}"); break;
                    case GX.ColorSrc.REG: builder.Append($"u_ColorMatReg[{i}]"); break;
                }
                return 1;
            }

            private int generateAmbientSource(StringBuilder builder, ChannelControl channel, int i)
            {
                switch (channel.MatColorSource)
                {
                    case GX.ColorSrc.VTX: builder.Append($"a_Color{i}"); break;
                    case GX.ColorSrc.REG: builder.Append($"u_ColorAmbReg[{i}]"); break;
                }
                return 1;
            }

            private int generateLightDiffFn(StringBuilder builder, ChannelControl channel, string lightName)
            {
                var NdotL = "dot(t_Normal, t_LightDeltaDir)";
                switch (channel.DiffuseFunction)
                {
                    case GX.DiffuseFn.NONE: builder.Append("1.0"); break;
                    case GX.DiffuseFn.SIGN: builder.Append(NdotL); break;
                    case GX.DiffuseFn.CLAMP: builder.Append($"max({NdotL}, 0.0f)"); break;
                }
                return 1;
            }

            private int generateLightAttnFn(StringBuilder builder, ChannelControl channel, string lightName)
            {
                builder.Append($"\n//AttenuationFunction {channel.AttenuationFunction}\n");

                switch (channel.AttenuationFunction)
                {
                    case GX.AttenFn.NONE:
                        builder.Append($"t_Attenuation =  1.0;\n");
                        break;
                    case GX.AttenFn.SPOT:
                        {
                            string attn = $"max(0.0, dot(t_LightDeltaDir, {lightName}.Direction.xyz))";
                            string cosAttn = $"max(0.0, ApplyAttenuation({lightName}.CosAtten.xyz, {attn}))";
                            string distAttn = $"dot({lightName}.DistAtten.xyz, vec3(1.0, t_LightDeltaDist, t_LightDeltaDist2))";
                            builder.Append($"t_Attenuation = {cosAttn} / {distAttn};\n");
                        }
                        break;
                    case GX.AttenFn.SPEC:
                        {
                            string attn = $"(dot(t_Normal, t_LightDeltaDir) >= 0.0) ? max(0.0, dot(t_Normal, {lightName}.Direction.xyz)) : 0.0";
                            string cosAttn = $"ApplyAttenuation({lightName}.CosAtten.xyz, t_Attenuation)";
                            string distAttn = $"ApplyAttenuation({lightName}.DistAtten.xyz, t_Attenuation)";
                            builder.Append($"t_Attenuation  = {attn};\n");
                            builder.Append($"t_Attenuation = {cosAttn} / {distAttn};\n");
                        }
                        break;
                    default:
                        Console.WriteLine($"Invalid attenuation function {channel.AttenuationFunction}!");
                        return 0;
                }
                return 1;
            }

            private int generateColorChannel(StringBuilder builder, ChannelControl chan, string outputName, int i)
            {
                if (chan.LightingEnabled)
                {
                    builder.Append("t_LightAccum = ");
                    generateAmbientSource(builder, chan, i);
                    builder.Append(";\n");

                    for (int j = 0; j < 8; j++)
                    {
                        //if (((uint)chan.LitMask & (1 << j)) != 1)
                        //    continue;

                        if (!chan.LightsToggle[j])
                            continue;

                        string lightName = $"u_LightParams[{j}]";
                        builder.Append($"    t_LightDelta = {lightName}.Position.xyz - v_Position.xyz;\n");
                        builder.Append("    t_LightDeltaDist2 = dot(t_LightDelta, t_LightDelta);\n");
                        builder.Append("    t_LightDeltaDist = sqrt(t_LightDeltaDist2);\n");
                        builder.Append("    t_LightDeltaDir = t_LightDelta / t_LightDeltaDist;\n");
                        int error = generateLightAttnFn(builder, chan, lightName);
                        if (error == 0)
                            return 0;

                        builder.Append($"\n//DiffuseFunction {chan.DiffuseFunction}\n");

                        builder.Append("    t_LightAccum += ");
                        error = generateLightDiffFn(builder, chan, lightName);
                        if (error == 0)
                            return 0;

                        builder.Append($" * t_Attenuation * {lightName}.Color;\n");
                    }
                }
                else
                {
                    // Without lighting, everything is full-bright.
                    builder.Append("    t_LightAccum = vec4(1.0);\n");
                }

                builder.Append($"    {outputName} = ");
                generateMaterialSource(builder, chan, i);
                builder.Append(" * clamp(t_LightAccum, 0.0, 1.0);\n");
                return 1;
            }

            private int generateLightChannel(StringBuilder builder, LightingChannelControl lightChannel, string outputName, int i)
            {
                if (lightChannel.ColorChannel == lightChannel.AlphaChannel)
                {
                    //TODO
                    builder.Append("    ");
                    generateColorChannel(builder, lightChannel.ColorChannel, outputName, i);
                }
                else
                {
                    generateColorChannel(builder, lightChannel.ColorChannel, "t_ColorChanTemp", i);
                    builder.Append($"\n    {outputName}.rgb = t_ColorChanTemp.rgb;\n");

                    generateColorChannel(builder, lightChannel.AlphaChannel, "t_ColorChanTemp", i);
                    builder.Append($"\n    {outputName}.a = t_ColorChanTemp.a;\n");
                }
                return 1;
            }

            private int generateLightChannels(StringBuilder builder)
            {
                var channels = this.mMaterial.ColorChannelControls;

                LightingChannelControl[] ctrl = new LightingChannelControl[2];
                ctrl[0] = new LightingChannelControl();
                ctrl[1] = new LightingChannelControl();

                //Color0/Alpha0
                if (channels.Length > 0) ctrl[0].ColorChannel = channels[0];
                if (channels.Length > 1) ctrl[0].AlphaChannel = channels[1];
                //Color1/Alpha1
                if (channels.Length > 2) ctrl[1].ColorChannel = channels[2];
                if (channels.Length > 3) ctrl[1].AlphaChannel = channels[3];

                for (int i = 0; i < ctrl.Length; i++)
                {
                    generateLightChannel(builder, ctrl[i], $"v_Color{i}", i);
                    builder.Append("\n");
                }
                return 1;
            }

            private int generateAttributeStorageType(StringBuilder builder, string format, uint count)
            {
                switch (count)
                {
                    case 1: builder.Append("float"); break;
                    case 2: builder.Append("vec2"); break;
                    case 3: builder.Append("vec3"); break;
                    case 4: builder.Append("vec4"); break;
                    default:
                        throw new Exception($"Invalid count {count}!");
                }
                return 1;
            }

            private string generateKonstColorSel(GX.KonstColorSel konstColor)
            {
                switch (konstColor)
                {
                    case GX.KonstColorSel.KCSEL_1: return "vec3(8.0/8.0)";
                    case GX.KonstColorSel.KCSEL_7_8: return "vec3(7.0/8.0)";
                    case GX.KonstColorSel.KCSEL_6_8: return "vec3(6.0/8.0)";
                    case GX.KonstColorSel.KCSEL_5_8: return "vec3(5.0/8.0)";
                    case GX.KonstColorSel.KCSEL_4_8: return "vec3(4.0/8.0)";
                    case GX.KonstColorSel.KCSEL_3_8: return "vec3(3.0/8.0)";
                    case GX.KonstColorSel.KCSEL_2_8: return "vec3(2.0/8.0)";
                    case GX.KonstColorSel.KCSEL_1_8: return "vec3(1.0/8.0)";
                    case GX.KonstColorSel.KCSEL_K0: return "s_kColor0.rgb";
                    case GX.KonstColorSel.KCSEL_K0_R: return "s_kColor0.rrr";
                    case GX.KonstColorSel.KCSEL_K0_G: return "s_kColor0.ggg";
                    case GX.KonstColorSel.KCSEL_K0_B: return "s_kColor0.bbb";
                    case GX.KonstColorSel.KCSEL_K0_A: return "s_kColor0.aaa";
                    case GX.KonstColorSel.KCSEL_K1: return "s_kColor1.rgb";
                    case GX.KonstColorSel.KCSEL_K1_R: return "s_kColor1.rrr";
                    case GX.KonstColorSel.KCSEL_K1_G: return "s_kColor1.ggg";
                    case GX.KonstColorSel.KCSEL_K1_B: return "s_kColor1.bbb";
                    case GX.KonstColorSel.KCSEL_K1_A: return "s_kColor1.aaa";
                    case GX.KonstColorSel.KCSEL_K2: return "s_kColor2.rgb";
                    case GX.KonstColorSel.KCSEL_K2_R: return "s_kColor2.rrr";
                    case GX.KonstColorSel.KCSEL_K2_G: return "s_kColor2.ggg";
                    case GX.KonstColorSel.KCSEL_K2_B: return "s_kColor2.bbb";
                    case GX.KonstColorSel.KCSEL_K2_A: return "s_kColor2.aaa";
                    case GX.KonstColorSel.KCSEL_K3: return "s_kColor3.rgb";
                    case GX.KonstColorSel.KCSEL_K3_R: return "s_kColor3.rrr";
                    case GX.KonstColorSel.KCSEL_K3_G: return "s_kColor3.ggg";
                    case GX.KonstColorSel.KCSEL_K3_B: return "s_kColor3.bbb";
                    case GX.KonstColorSel.KCSEL_K3_A: return "s_kColor3.aaa";
                }
                return "";
            }

            private string generateKonstAlphaSel(GX.KonstAlphaSel konstAlpha)
            {
                switch (konstAlpha)
                {
                    case GX.KonstAlphaSel.KASEL_1: return "(8.0/8.0)";
                    case GX.KonstAlphaSel.KASEL_7_8: return "(7.0/8.0)";
                    case GX.KonstAlphaSel.KASEL_6_8: return "(6.0/8.0)";
                    case GX.KonstAlphaSel.KASEL_5_8: return "(5.0/8.0)";
                    case GX.KonstAlphaSel.KASEL_4_8: return "(4.0/8.0)";
                    case GX.KonstAlphaSel.KASEL_3_8: return "(3.0/8.0)";
                    case GX.KonstAlphaSel.KASEL_2_8: return "(2.0/8.0)";
                    case GX.KonstAlphaSel.KASEL_1_8: return "(1.0/8.0)";
                    case GX.KonstAlphaSel.KASEL_K0_R: return "s_kColor0.r";
                    case GX.KonstAlphaSel.KASEL_K0_G: return "s_kColor0.g";
                    case GX.KonstAlphaSel.KASEL_K0_B: return "s_kColor0.b";
                    case GX.KonstAlphaSel.KASEL_K0_A: return "s_kColor0.a";
                    case GX.KonstAlphaSel.KASEL_K1_R: return "s_kColor1.r";
                    case GX.KonstAlphaSel.KASEL_K1_G: return "s_kColor1.g";
                    case GX.KonstAlphaSel.KASEL_K1_B: return "s_kColor1.b";
                    case GX.KonstAlphaSel.KASEL_K1_A: return "s_kColor1.a";
                    case GX.KonstAlphaSel.KASEL_K2_R: return "s_kColor2.r";
                    case GX.KonstAlphaSel.KASEL_K2_G: return "s_kColor2.g";
                    case GX.KonstAlphaSel.KASEL_K2_B: return "s_kColor2.b";
                    case GX.KonstAlphaSel.KASEL_K2_A: return "s_kColor2.a";
                    case GX.KonstAlphaSel.KASEL_K3_R: return "s_kColor3.r";
                    case GX.KonstAlphaSel.KASEL_K3_G: return "s_kColor3.g";
                    case GX.KonstAlphaSel.KASEL_K3_B: return "s_kColor3.b";
                    case GX.KonstAlphaSel.KASEL_K3_A: return "s_kColor3.a";
                }

                return "";
            }

            private string generateRas(TevStage stage)
            {
                switch (stage.ChannelID)
                {
                    case GX.RasColorChannelID.COLOR0A0:
                        return "v_Color0";
                    case GX.RasColorChannelID.COLOR1A1:
                        return "v_Color1";
                    case GX.RasColorChannelID.COLOR_ZERO:
                        return "vec4(0, 0, 0, 0)";
                    default:
                        return "v_Color0";
                }
            }

            private bool stageUsesSimpleCoords(TevStage stage)
            {
                return stage.indirectStage.Matrix == GX.IndTexMtxID.OFF && !stage.indirectStage.AddPrev;
            }

            private string generateTexAccess(TevStage stage)
            {
                if (stage.TexMap == GX.TexMapID.TEXMAP_NULL)
                    return "vec4(1.0, 1.0, 1.0, 1.0)";

                var texScale = this.stageUsesSimpleCoords(stage) ? "" : $" * TextureInvScale({(int)stage.TexMap})";
                return this.generateTextureSample((uint)stage.TexMap, $"t_TexCoord{texScale}");
            }

            private string generateComponentSwizzle(SwapTable swapTable, GX.TevColorChan channel)
            {
                string[] suffixes = new string[] { "r", "g", "b", "a" };
                if (swapTable != null)
                    channel = swapTable.Get((int)channel);
                return suffixes[(int)channel];
            }

            private string generateColorSwizzle(SwapTable swapTable, GX.CC colorIn)
            {
                var swapR = this.generateComponentSwizzle(swapTable, GX.TevColorChan.R);
                var swapG = this.generateComponentSwizzle(swapTable, GX.TevColorChan.G);
                var swapB = this.generateComponentSwizzle(swapTable, GX.TevColorChan.B);
                var swapA = this.generateComponentSwizzle(swapTable, GX.TevColorChan.A);
                switch (colorIn)
                {
                    case GX.CC.TEXC: 
                    case GX.CC.RASC:
                        return $"{swapR}{swapG}{swapB}";
                    case GX.CC.TEXA:
                    case GX.CC.RASA:
                        return $"{swapA}{swapA}{swapA}";
                }
                throw new Exception();
            }

            private string generateColorIn(TevStage stage, GX.CC colorIn)
            {
                switch (colorIn)
                {
                    case GX.CC.CPREV: return "t_ColorPrev.rgb";
                    case GX.CC.APREV: return "t_ColorPrev.aaa";
                    case GX.CC.C0: return "t_Color0.rgb";
                    case GX.CC.A0: return "t_Color0.aaa";
                    case GX.CC.C1: return "t_Color1.rgb";
                    case GX.CC.A1: return "t_Color1.aaa";
                    case GX.CC.C2: return "t_Color2.rgb";
                    case GX.CC.A2: return "t_Color2.aaa";
                    case GX.CC.TEXC: return $"{ this.generateTexAccess(stage)}.{ this.generateColorSwizzle(stage.TexSwapTable, colorIn)}";
                    case GX.CC.TEXA: return $"{ this.generateTexAccess(stage)}.{ this.generateColorSwizzle(stage.TexSwapTable, colorIn)}";
                    case GX.CC.RASC: return $"TevSaturate({ this.generateRas(stage)}.{ this.generateColorSwizzle(stage.RasSwapTable, colorIn)})";
                    case GX.CC.RASA: return $"TevSaturate({ this.generateRas(stage)}.{ this.generateColorSwizzle(stage.RasSwapTable, colorIn)})";
                    case GX.CC.ONE: return "vec3(1)";
                    case GX.CC.HALF: return "vec3(1.0 / 2.0)";
                    case GX.CC.KONST: return $"{this.generateKonstColorSel(stage.KonstColorSel)}";
                    case GX.CC.ZERO: return "vec3(0)";
                    default:
                        return "vec3(0)"; 
                }
            }

            private string generateAlphaIn(TevStage stage, GX.CA alphaIn)
            {
                switch (alphaIn)
                {
                    case GX.CA.APREV: return "t_ColorPrev.a";
                    case GX.CA.A0: return "t_Color0.a";
                    case GX.CA.A1: return "t_Color1.a";
                    case GX.CA.A2: return "t_Color2.a";
                    case GX.CA.TEXA: return $"{ this.generateTexAccess(stage)}.{ this.generateComponentSwizzle(stage.TexSwapTable, GX.TevColorChan.A)}";
                    case GX.CA.RASA: return $"TevSaturate({ this.generateRas(stage)}.{ this.generateComponentSwizzle(stage.RasSwapTable, GX.TevColorChan.A)})";
                    case GX.CA.KONST: return $"{this.generateKonstAlphaSel(stage.KonstAlphaSel)}";
                    case GX.CA.ZERO: return "0.0";
                    default:
                        return "0.0";
                }
            }

            private string generateTevInputs(TevStage stage)
            {
                string str = "";
                str += $"    t_TevA = TevOverflow(vec4({ this.generateColorIn(stage, stage.colorStage.A)}, { this.generateAlphaIn(stage, stage.alphaStage.A)}));\n";
                str += $"    t_TevB = TevOverflow(vec4({ this.generateColorIn(stage, stage.colorStage.B)}, { this.generateAlphaIn(stage, stage.alphaStage.B)}));\n";
                str += $"    t_TevC = TevOverflow(vec4({ this.generateColorIn(stage, stage.colorStage.C)}, { this.generateAlphaIn(stage, stage.alphaStage.C)}));\n";
                str += $"    t_TevD = vec4({ this.generateColorIn(stage, stage.colorStage.D)}, { this.generateAlphaIn(stage, stage.alphaStage.D)});\n";
                return str.Trim();
            }

            private string generateTevRegister(GX.Register reg)
            {
                switch (reg)
                {
                    case GX.Register.PREV: return "t_ColorPrev";
                    case GX.Register.REG0: return "t_Color0";
                    case GX.Register.REG1: return "t_Color1";
                    case GX.Register.REG2: return "t_Color2";
                }
                throw new Exception();
            }

            private string generateTevOpBiasScaleClamp(string value, GX.TevBias bias, GX.TevScale scale)
            {
                string v = value;
                if (bias == GX.TevBias.ADDHALF)
                    v = $"TevBias({v}, 0.5)";
                if (bias == GX.TevBias.SUBHALF)
                    v = $"TevBias({v}, -0.5)";

                if (scale == GX.TevScale.SCALE_2)
                    v = $"({v}) * 2.0";
                if (scale == GX.TevScale.SCALE_4)
                    v = $"({v}) * 4.0";
                if (scale == GX.TevScale.DIVIDE_2)
                    v = $"({v}) * 0.5";

                return v;
            }

            private string generateTevOp(GX.TevOp op, GX.TevBias bias, GX.TevScale scale, string a, string b, string c, string d, string zero)
            {
                switch (op)
                {
                    case GX.TevOp.ADD:
                    case GX.TevOp.SUB:
                        string neg = (op == GX.TevOp.SUB) ? "-" : "";
                        string v = $"{neg}mix({a}, {b}, {c}) + {d}";
                        return this.generateTevOpBiasScaleClamp(v, bias, scale);
                    case GX.TevOp.COMP_R8_GT: return $"((t_TevA.r >  t_TevB.r) ? {c} : {zero} + {d}";
                    case GX.TevOp.COMP_R8_EQ: return $"((t_TevA.r ==  t_TevB.r) ? {c} : {zero} + {d}";
                    case GX.TevOp.COMP_GR16_GT: return $"((TevPack16(t_TevA.rg) > TevPack16(t_TevB.rg)) ? {c} : {zero} + {d}";
                    case GX.TevOp.COMP_GR16_EQ: return $"((TevPack16(t_TevA.rg) == TevPack16(t_TevB.rg)) ? {c} : {zero} + {d}";
                    case GX.TevOp.COMP_BGR24_GT: return $"((TevPack24(t_TevA.rgb) >  TevPack24(t_TevB.rgb)) ? {c} : {zero} + {d}";
                    case GX.TevOp.COMP_BGR24_EQ: return $"((TevPack24(t_TevA.rgb) ==  TevPack24(t_TevB.rgb)) ? {c} : {zero} + {d}";
                    case GX.TevOp.COMP_RGB8_GT: return $"(TevPerCompGT({a}, {b}) * {c}) + {d}";
                    case GX.TevOp.COMP_RGB8_EQ: return $"(TevPerCompEQ({a}, {b}) * {c}) + {d}";
                }
                throw new Exception();
                return "";
            }

            private string generateTevOpValue(GX.TevOp op, GX.TevBias bias, GX.TevScale scale, bool clamp, string a, string b, string c, string d, string zero)
            {
                var expr = this.generateTevOp(op, bias, scale, a, b, c, d, zero);
                if (clamp)
                    return $"TevSaturate({expr})";
                return $"clamp({expr}, -4.0, 4.0)";
            }

            private string generateColorOp(TevStage stage)
            {
                string a = "t_TevA.rgb"; string b = "t_TevB.rgb"; string c = "t_TevC.rgb"; string d = "t_TevD.rgb"; string zero = "vec3(0)";
                string value = generateTevOpValue(stage.colorStage.Op, stage.colorStage.Bias, stage.colorStage.Scale, stage.colorStage.Clamp, a, b, c, d, zero);
                return $"\n    {this.generateTevRegister(stage.colorStage.Output)}.rgb = {value};";
            }

            private string generateAlphaOp(TevStage stage)
            {
                string a = "t_TevA.a"; string b = "t_TevB.a"; string c = "t_TevC.a"; string d = "t_TevD.a"; string zero = "0.0";
                string value = this.generateTevOpValue(stage.alphaStage.Op, stage.alphaStage.Bias, stage.alphaStage.Scale, stage.alphaStage.Clamp, a, b, c, d, zero);
                return $"\n    {this.generateTevRegister(stage.alphaStage.Output)}.a = {value};";
            }

            private string generateTevTexCoordWrapN(string texCoord, GX.IndTexWrap wrap)
            {
                switch (wrap)
                {
                    case GX.IndTexWrap.OFF: return texCoord;
                    case GX.IndTexWrap._0: return "0.0";
                    case GX.IndTexWrap._256: return $"mod({texCoord}), 256.0";
                    case GX.IndTexWrap._128: return $"mod({texCoord}), 256.0";
                    case GX.IndTexWrap._64: return $"mod({texCoord}), 64.0";
                    case GX.IndTexWrap._32: return $"mod({texCoord}), 32.0";
                    case GX.IndTexWrap._16: return $"mod({texCoord}), 16.0";
                }
                return texCoord;
            }

            private string generateTevTexCoordWrap(TevStage stage)
            {
                int lastTexGenId = mMaterial.TexGens.Length - 1;
                int texGenId = (int)stage.TexCoordID;

                if (texGenId >= lastTexGenId)
                    texGenId = lastTexGenId;
                if (texGenId < 0)
                    return "vec2(0.0, 0.0)";

                var texScale = this.stageUsesSimpleCoords(stage) ? "" : $" * TextureScale({(int)stage.TexMap})";
                var baseCoord = $"ReadTexCoord{texGenId}(){texScale}";
                if (stage.indirectStage.WrapS == GX.IndTexWrap.OFF &&
                    stage.indirectStage.WrapT == GX.IndTexWrap.OFF)
                    return baseCoord;
                else
                    return $"vec2({generateTevTexCoordWrapN(baseCoord + ".x", stage.indirectStage.WrapS)}, {generateTevTexCoordWrapN(baseCoord + ".x", stage.indirectStage.WrapS)})";
            }

            private string generateTevTexCoordIndTexCoordBias(TevStage stage)
            {
                string bias = stage.indirectStage.Format == GX.IndTexFormat._8 ? "-128.0" : "1.0";
                switch (stage.indirectStage.BiasSel)
                {
                    case GX.IndTexBiasSel.NONE:return "";
                    case GX.IndTexBiasSel.S:   return $" + vec3({bias}, 0.0, 0.0)";
                    case GX.IndTexBiasSel.ST:  return $" + vec3({bias},{bias}, 0.0)";
                    case GX.IndTexBiasSel.SU:  return $" + vec3({bias}, 0.0, {bias})";
                    case GX.IndTexBiasSel.T:   return $" + vec3(0.0,{bias}, 0.0)";
                    case GX.IndTexBiasSel.TU:  return $" + vec3(0.0,{bias}, {bias})";
                    case GX.IndTexBiasSel.U:   return $" + vec3(0.0, 0.0, {bias})";
                    case GX.IndTexBiasSel.STU: return $" + vec3({bias})";
                }
                return "";
            }

            private string generateTevTexCoordIndTexCoord(TevStage stage)
            {
                var baseCoord = $"(t_IndTexCoord{(int)stage.indirectStage.Stage})";
                switch (stage.indirectStage.Format)
                {
                    case GX.IndTexFormat._8:
                        return baseCoord;
                    default:
                        Console.WriteLine($"Unsupported indirect format! {stage.indirectStage.Format}");
                        break;
                }
                return baseCoord;
            }

            private string generateTevTexCoordIndirectMtx(TevStage stage)
            {
                var indTevCoord = "(" + generateTevTexCoordIndTexCoord(stage) +
                                        generateTevTexCoordIndTexCoordBias(stage) + ")";

                switch (stage.indirectStage.Matrix)
                {
                    case GX.IndTexMtxID._0: return $"(u_IndTexMtx[0] * vec4({indTevCoord}, 0.0))";
                    case GX.IndTexMtxID._1: return $"(u_IndTexMtx[1] * vec4({indTevCoord}, 0.0))";
                    case GX.IndTexMtxID._2: return $"(u_IndTexMtx[2] * vec4({indTevCoord}, 0.0))";
                    default:
                        Console.WriteLine($"Unsupported indirect matrix mode! {stage.indirectStage.Matrix}");
                        break;
                }

                return indTevCoord + ".xy";
            }

            private string generateTevTexCoordIndirectTranslation(TevStage stage)
            {
                return "(" + generateTevTexCoordIndirectMtx(stage) + $" * TextureInvScale({(int)stage.TexMap}))";            }

            private string generateTevTexCoordIndirect(TevStage stage)
            {
                var baseCoord = generateTevTexCoordWrap(stage);

                if (stage.indirectStage.Matrix != GX.IndTexMtxID.OFF &&
                    (int)stage.indirectStage.Stage < mMaterial.Stages.Length)
                    return baseCoord + " + " + generateTevTexCoordIndirectTranslation(stage);
                else
                    return baseCoord;
            }

            private string generateTevTexCoord(TevStage stage)
            {
                if (stage.TexCoordID == GX.TexCoordID.TEXCOORD_NULL)
                    return "";

                var finalCoord = generateTevTexCoordIndirect(stage);
                if (stage.indirectStage.AddPrev)
                    return $"    t_TexCoord += {finalCoord};\n";
                else
                    return $"    t_TexCoord = {finalCoord};\n";
            }

            private int generateTevStage(StringBuilder builder, uint tevStageIndex)
            {
                var stage = mMaterial.Stages[tevStageIndex];

                builder.Append($"\n\n    //\n    // TEV Stage {tevStageIndex}\n   //\n");
                builder.Append(generateTevTexCoord(stage));
                builder.Append(generateTevInputs(stage));
                builder.Append(generateColorOp(stage));
                builder.Append(generateAlphaOp(stage));
                return 1;
            }

            private int generateTevStages(StringBuilder builder)
            {
                for (uint i = 0; i < mMaterial.Stages.Length; i++)
                {
                    int error = generateTevStage(builder, i);
                    if (error == 0)
                        return error;
                }
                return 1;
            }

            private int generateTevStagesLastMinuteFixup(StringBuilder builder)
            {
                //temp. Todo
                var tevStages = this.mMaterial.Stages;
                var lastTevStage = tevStages[tevStages.Length - 1];
                var colorReg = this.generateTevRegister(lastTevStage.colorStage.Output);
                var alphaReg = this.generateTevRegister(lastTevStage.alphaStage.Output);
                if (colorReg == alphaReg)
                    builder.AppendLine($"\n    vec4 t_TevOutput = {colorReg};");
                else
                    builder.AppendLine($"\n    vec4 t_TevOutput = vec4({colorReg}.rgb, {alphaReg}.a);");
                return 1;
            }

            private int generateAlphaTestCompare(StringBuilder builder, GX.CompareType compare, float reference)
            {
                switch (compare)
                {
                    case GX.CompareType.NEVER:   builder.Append($"false"); break;
                    case GX.CompareType.LESS:    builder.Append($"t_PixelOut.a < {reference}"); break;
                    case GX.CompareType.EQUAL:   builder.Append($"t_PixelOut.a == {reference}");break;
                    case GX.CompareType.LEQUAL:  builder.Append($"t_PixelOut.a <= {reference}"); break;
                    case GX.CompareType.GREATER: builder.Append($"t_PixelOut.a > {reference}"); break;
                    case GX.CompareType.NEQUAL:  builder.Append($"t_PixelOut.a != {reference}"); break;
                    case GX.CompareType.GEQUAL:  builder.Append($"t_PixelOut.a >= {reference}"); break;
                    case GX.CompareType.ALWAYS:  builder.Append($"true"); break;
                }
                return 1;
            }

            private int generateAlphaTestOp(StringBuilder builder, GX.AlphaOp op)
            {
                switch (op)
                {
                    case GX.AlphaOp.AND:  builder.Append("t_AlphaTestA && t_AlphaTestB"); break;
                    case GX.AlphaOp.OR:   builder.Append("t_AlphaTestA || t_AlphaTestB"); break;
                    case GX.AlphaOp.XOR : builder.Append("t_AlphaTestA != t_AlphaTestB"); break;
                    case GX.AlphaOp.XNOR: builder.Append("t_AlphaTestA == t_AlphaTestB"); break;
                }
                return 1;
            }

            private int generateAlphaTest(StringBuilder builder)
            {
                var alphaTest = mMaterial.AlphaCompare;

                builder.Append("\n	bool t_AlphaTestA = ");
                generateAlphaTestCompare(builder, alphaTest.CompLeft, alphaTest.refLeft / 255.0f);
                builder.Append(";\n");

                builder.Append("\n	bool t_AlphaTestB = ");
                generateAlphaTestCompare(builder, alphaTest.compRight, alphaTest.refRight / 255.0f);
                builder.Append(";\n");
                builder.Append("	if (!(");
                generateAlphaTestOp(builder, alphaTest.Op);
                builder.Append("))\n");
                builder.Append("		discard; \n");

                return 1;
            }

            private string generateFogZCoord()
            {
                return "gl_FragCoord.z";
            }

            private string generateFogBase()
            {
                string isProjection = "(u_FogBlock.Param.y != 0.0)";

                string A = "u_FogBlock.Param.x";
                string B = "u_FogBlock.Param.y";
                string z = this.generateFogZCoord();
                return $"({isProjection}) ? ({A} / ({B} - {z})) : ({A} * { z})";
            }

            private string generateFogAdj(string src) => "";

            private string generateFogFunc(string src)
            {
                mMaterial.RopInfo.FogType = GX.FogType.PERSP_REVEXP2;

                string func = "";
                switch (mMaterial.RopInfo.FogType)
                {
                    case GX.FogType.PERSP_EXP:
                    case GX.FogType.ORTHO_EXP:
                        func = $"    {src} = 1.0 - exp2(-8.0 * {src});";
                        break;
                    case GX.FogType.PERSP_EXP2:
                    case GX.FogType.ORTHO_EXP2:
                        func = $"    {src} = 1.0 - exp2(-8.0 * pow({src}, 2.0));";
                        break;
                    case GX.FogType.PERSP_REVEXP:
                    case GX.FogType.ORTHO_REVEXP:
                        func = $"    {src} = exp2(-8.0 * (1.0 - {src}));";
                        break;
                    case GX.FogType.PERSP_REVEXP2:
                    case GX.FogType.ORTHO_REVEXP2:
                        func = $"    {src} = exp2(-8.0 * pow((1.0 - {src}), 2.0));";
                        break;
                }
                return func;
            }

            private void generateFog(StringBuilder builder)
            {
             //   if (mMaterial.RopInfo.FogType == GX.FogType.NONE)
                 //   return;

                string C = "u_FogBlock.Param.z";
                builder.AppendLine($"    float t_FogBase = {this.generateFogBase()};");
                builder.AppendLine($"{this.generateFogAdj("t_FogBase")}");
                builder.AppendLine($"    float t_Fog = TevSaturate(t_FogBase - {C});");
                builder.AppendLine($"{this.generateFogFunc("t_Fog")}");
                builder.AppendLine($"    t_PixelOut.rgb = mix(t_PixelOut.rgb, u_FogBlock.Color.rgb, t_Fog);");
            }

            private int generateVertAttributeDefs(StringBuilder builder)
            {
                int i = 0;
                foreach (var attr in vtxAttributeGenDefs)
                {
                    builder.Append($"layout(location = {i}) in ");
                    generateAttributeStorageType(builder, attr.Type, attr.Count);
                    builder.Append($" a_{attr.Name};\n");
                    i++;
                }
                return 1;
            }

            public string generateMulPntMatrixStatic(GX.TexGenMatrix pnt, string src)
            {
                if (pnt == GX.TexGenMatrix.IDENTITY)
                    return $"{src}.xyz";
                else if (pnt >= GX.TexGenMatrix.TEXMTX0)
                {
                    int texMtxIdx = (((int)pnt - (int)GX.TexGenMatrix.TEXMTX0)) / 3;
                    return $"(u_TexMtx[{texMtxIdx}] * {src})";
                }
                else if (pnt >= GX.TexGenMatrix.PNMTX0)
                {
                    int pnMtxIdx = (((int)pnt - (int)GX.TexGenMatrix.PNMTX0)) / 3;
                    return $"(u_PosMtx[{pnMtxIdx}] * {src})";
                }
                Console.WriteLine($"Invalid posttexmatrix! {pnt}");
                return "";
            }

            private string generateMulPntMatrixDynamic(string attrStr, string src) {
                return $"(GetPosTexMatrix({attrStr}) * {src})";
            }

            private string generateMulPos()
            {
                var src = "vec4(a_Position, 1.0)";
                if (hasDirectSkinning)
                {
                    //Apply direct skinning to vertex position and normal
                    src = "vec4(skin(a_Position.xyz, ivec4(a_BoneIndex)), 1.0)";
                }

                if (usePnMtxIdx)
                    return generateMulPntMatrixDynamic("uint(a_PnMtxIdx)", src);
                else
                    return generateMulPntMatrixStatic(GX.TexGenMatrix.PNMTX0, src);
            }



            private string generateMulNrm(int type)
            {
                var src = "vec4(a_Normal, 0.0)";
                if (hasDirectSkinning)
                {
                    //Apply direct skinning to vertex position and normal
                   // src = "vec4(skinNRM(a_Normal.xyz, ivec4(a_BoneIndex)), 1.0)";
                }

                if (type == 1) src = "vec4(a_Binormal.xyz, 0.0)";
                if (type == 2) src = "vec4(a_Tangent.xyz, 0.0)";

                if (usePnMtxIdx)
                   return $"normalize({generateMulPntMatrixDynamic("uint(a_PnMtxIdx)", src)})";
                else
                    return $"normalize({generateMulPntMatrixStatic(GX.TexGenMatrix.PNMTX0, src)})";
            }

            private string generateVert()
            {
                string varying_vert =
                        @"
out vec3 v_Position;
out vec4 v_Color0;
out vec4 v_Color1;
out vec4 v_VertexColor;
out vec3 v_Normal;
out vec3 v_Binormal;
out vec3 v_Tangent;
out vec3 v_TexCoord0;
out vec3 v_TexCoord1;
out vec3 v_TexCoord2;
out vec3 v_TexCoord3;
out vec3 v_TexCoord4;
out vec3 v_TexCoord5;
out vec3 v_TexCoord6;
out vec3 v_TexCoord7;
out vec4 v_BoneIndices;
out vec4 v_BoneWeights;" + "\n";

                StringBuilder builder = new StringBuilder(1024 * 64);
                builder.AppendLine("");

                builder.Append(varying_vert);
                builder.AppendLine("");
                var error = generateVertAttributeDefs(builder);
                if (error == 0)
                    return "";

                if (hasDirectSkinning)
                    builder.AppendLine(generateDirectSkinning());

                builder.AppendLine("");

                builder.Append("mat4x3 GetPosTexMatrix(uint t_MtxIdx) {\n" +
                    "    if (t_MtxIdx == " + ((int)GX.TexGenMatrix.IDENTITY) + "u)\n" +
                    "        return mat4x3(1.0);\n" +
                    "    else if (t_MtxIdx >= " + ((int)GX.TexGenMatrix.TEXMTX0) + "u)\n" +
                    "        return u_TexMtx[(t_MtxIdx - " + ((int)GX.TexGenMatrix.TEXMTX0) + "u) / 3u];\n" +
                    "    else\n" +
                    "        return u_PosMtx[t_MtxIdx / 3u];\n" +
                    "}\n" +
                    "float ApplyAttenuation(vec3 t_Coeff, float t_Value) {\n" +
                    "    return dot(t_Coeff, vec3(1.0, t_Value, t_Value*t_Value));\n" +
                    "}\n");

                builder.Append("void main() {\n");
                builder.Append($"    vec3 t_Position = {generateMulPos()}");
                builder.Append(";\n");

                builder.Append("    v_Position = t_Position;\n");
                builder.Append($"    vec3 t_Normal = {generateMulNrm(0)}");
                builder.Append(";\n");

                builder.Append("    vec4 t_LightAccum;\n");
                builder.Append("    vec3 t_LightDelta, t_LightDeltaDir;\n");
                builder.Append("    float t_LightDeltaDist2, t_LightDeltaDist, t_Attenuation;\n");
                builder.Append("    vec4 t_ColorChanTemp;\n");
                builder.Append("    v_Color0 = a_Color0;\n");
                builder.Append("    v_Normal = t_Normal;\n");
                builder.Append("    v_VertexColor = a_Color0;\n");
                builder.Append("    v_Tangent = a_Tangent;\n");
                builder.Append("    v_Binormal = a_Binormal;\n");

                generateLightChannels(builder);

                builder.Append(generateTexGens());
                builder.Append("gl_Position = (u_Projection * vec4(t_Position, 1.0));\n" + "}\n");
                
                return builder.ToString();
            }

            private string generateFrag()
            {
                string varying_frag =
                    @"
in vec3 v_Position;
in vec4 v_Color0;
in vec4 v_Color1;
in vec4 v_VertexColor;
in vec3 v_TexCoord0;
in vec3 v_TexCoord1;
in vec3 v_TexCoord2;
in vec3 v_TexCoord3;
in vec3 v_TexCoord4;
in vec3 v_TexCoord5;
in vec3 v_TexCoord6;
in vec3 v_TexCoord7;
in vec3 v_Normal;
in vec3 v_Binormal;
in vec3 v_Tangent;
in vec4 v_BoneIndices;
in vec4 v_BoneWeights;

out vec4 fragOut;" + "\n";

                StringBuilder builder = new StringBuilder(1024 * 64);
               // if (mMaterial.EarlyZComparison)
              //      builder.Append("layout(early_fragment_tests) in;\n");

                builder.Append(varying_frag);
                builder.Append(generateTexCoordGetters());

                builder.Append(@"
float TextureLODBias(int index) { return u_SceneTextureLODBias + u_TextureParams[index].w; }
vec2 TextureInvScale(int index) { return 1.0 / u_TextureParams[index].xy; }
vec2 TextureScale(int index) { return u_TextureParams[index].xy; }
vec3 TevBias(vec3 a, float b) { return a + vec3(b); }
float TevBias(float a, float b) { return a + b; }
vec3 TevSaturate(vec3 a) { return clamp(a, vec3(0), vec3(1)); }
float TevSaturate(float a) { return clamp(a, 0.0, 1.0); }
float TevOverflow(float a) { return float(int(a * 255.0) & 255) / 255.0; }
vec4 TevOverflow(vec4 a) { return vec4(TevOverflow(a.r), TevOverflow(a.g), TevOverflow(a.b), TevOverflow(a.a)); }
float TevPack16(vec2 a) { return dot(a, vec2(1.0, 256.0)); }
float TevPack24(vec3 a) { return dot(a, vec3(1.0, 256.0, 256.0 * 256.0)); }
float TevPerCompGT(float a, float b) { return float(a > b); }
float TevPerCompEQ(float a, float b) { return float(a == b); }
vec3 TevPerCompGT(vec3 a, vec3 b) { return vec3(greaterThan(a, b)); }
vec3 TevPerCompEQ(vec3 a, vec3 b) { return vec3(greaterThan(a, b)); }

void main() {
    vec4 s_kColor0   = u_KonstColor[0];
    vec4 s_kColor1   = u_KonstColor[1];
    vec4 s_kColor2   = u_KonstColor[2];
    vec4 s_kColor3   = u_KonstColor[3];
    vec4 t_ColorPrev = u_Color[0];
    vec4 t_Color0    = u_Color[1];
    vec4 t_Color1    = u_Color[2];
    vec4 t_Color2    = u_Color[3];" + "\n");

                builder.Append(generateIndTexStages());
                builder.Append(@"
    vec2 t_TexCoord = vec2(0.0, 0.0);
    vec4 t_TevA, t_TevB, t_TevC, t_TevD;");

                generateTevStages(builder);
                generateTevStagesLastMinuteFixup(builder);
                builder.Append("    vec4 t_PixelOut = TevOverflow(t_TevOutput);\n");
                generateAlphaTest(builder);
                generateFog(builder);
                builder.Append("    fragOut = t_PixelOut;\n");
                builder.AppendLine("    if (u_SelectionParams.w > 0.0)");
                builder.AppendLine("    {");
                builder.AppendLine("        float hc_a = u_SelectionParams.w;");
                builder.AppendLine("        fragOut = vec4(fragOut.rgb * (1 - hc_a) + u_SelectionParams.rgb * hc_a, fragOut.a);");
                builder.AppendLine("    }");

                //Debug shading modes
                generateDebugShading(builder);

                builder.Append("}\n");

                return builder.ToString();
            }

            private void generateDebugShading(StringBuilder builder)
            {
                builder.AppendLine($"    if (u_DebugParams.x == {(float)GXDebugShading.Normals}) //Normals");
                builder.AppendLine("    {");
                builder.AppendLine("        fragOut.rgb = (v_Normal * 0.5) + 0.5;");
                builder.AppendLine("    }");

                builder.AppendLine($"    if (u_DebugParams.x == {(float)GXDebugShading.Texture0}) //Texture0");
                builder.AppendLine("    {");

                //Map texture 0 if exists
                var texStage0 = this.mMaterial.Stages.FirstOrDefault(x => x.TexMap == GX.TexMapID.TEXMAP0);
                if (texStage0 != null)
                {
                    var diffuseTex = generateTexAccess(texStage0);
                    var finalCoord = generateTevTexCoord(texStage0);
                    builder.AppendLine($"        t_TexCoord = {finalCoord};");
                    builder.AppendLine($"        fragOut.rgb = {diffuseTex}.rgb;");
                }
                else
                    builder.AppendLine("        fragOut.rgb = vec3(1.0, 1.0, 1.0);");
                builder.AppendLine("    }");

                
                builder.AppendLine($"    if (u_DebugParams.x == {(float)GXDebugShading.VertexColor}) //Color0");
                builder.AppendLine("    {");
                builder.AppendLine("        fragOut.rgb = v_VertexColor.rgb;");
                builder.AppendLine("    }");

                builder.AppendLine($"    if (u_DebugParams.x == {(float)GXDebugShading.VertexAlpha}) //Color0");
                builder.AppendLine("    {");
                builder.AppendLine("        fragOut.rgb = v_VertexColor.aaa;");
                builder.AppendLine("    }");

                builder.AppendLine($"    if (u_DebugParams.x == {(float)GXDebugShading.RasterColor0}) //Color0");
                builder.AppendLine("    {");
                builder.AppendLine("        fragOut.rgb = v_Color0.rgb;");
                builder.AppendLine("    }");

                builder.AppendLine($"    if (u_DebugParams.x == {(float)GXDebugShading.RasterColor1}) //Color1");
                builder.AppendLine("    {");
                builder.AppendLine("        fragOut.rgb = v_Color1.rgb;");
                builder.AppendLine("    }");

                builder.AppendLine($"    if (u_DebugParams.x == {(float)GXDebugShading.RasterAlpha0}) //Alpha0");
                builder.AppendLine("    {");
                builder.AppendLine("        fragOut.rgb = v_Color0.aaa;");
                builder.AppendLine("    }");

                builder.AppendLine($"    if (u_DebugParams.x == {(float)GXDebugShading.RasterAlpha1}) //Alpha1");
                builder.AppendLine("    {");
                builder.AppendLine("        fragOut.rgb = v_Color1.aaa;");
                builder.AppendLine("    }");


                builder.AppendLine($"    if (u_DebugParams.x == {(float)GXDebugShading.Tangent}) //Color0");
                builder.AppendLine("    {");
                builder.AppendLine("        fragOut.rgb = (v_Tangent.rgb * 0.5) + 0.5;");
                builder.AppendLine("    }");

                builder.AppendLine($"    if (u_DebugParams.x == {(float)GXDebugShading.Binormal}) //Color0");
                builder.AppendLine("    {");
                builder.AppendLine("        fragOut.rgb = (v_Binormal.rgb * 0.5) + 0.5;");
                builder.AppendLine("    }");

                builder.AppendLine($"    if (u_DebugParams.x != {(float)GXDebugShading.Default}) //Alpha1");
                builder.AppendLine("    {");
                builder.AppendLine("        fragOut.a = 1.0;");
                builder.AppendLine("    }");
                builder.AppendLine("    fragOut.rgb *= vec3(u_DebugParams.y);");
            }

            public string GetFragment() => generateBoth() + this.generateFrag();
            public string GetVertex() => generateBoth() + this.generateVert();

            public ShaderProgram Compile()
            {
                var both = generateBoth();
                this.mMaterial.Vert = both + generateVert();
                this.mMaterial.Frag = both + generateFrag();

                var shader = new ShaderProgram(
                     new VertexShader(this.mMaterial.Vert),
                     new FragmentShader(this.mMaterial.Frag));

                return shader;
            }

            public ShaderProgram CompilePicking()
            {
                var both = generateBoth();
                string vert = both + generateVert();
                string frag = @"
#version 330

uniform vec4 color;

layout (location = 0) out vec4 fragOut;

void main(){
    fragOut = color;
}"; 

                var shader = new ShaderProgram(
                     new VertexShader(vert),
                     new FragmentShader(frag));

                return shader;
            }
        }
    }
}
