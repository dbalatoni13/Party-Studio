using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core.ViewModels;

namespace GCNRenderLibrary.Rendering
{
    /// <summary>
    /// Represents a scene node used for displaying GX data.
    /// </summary>
    public class SceneNode : ITransformableObject, IRenderNode
    {
        public Action<SceneNode> OnSelected;

        #region ITransformableObject
        public GLTransform Transform { get; set; } = new GLTransform();

        public bool IsHovered { get; set; }

        public bool IsSelected
        {
            get
            {
                return UINode.IsSelected;
            }
            set
            {
                if (value != UINode.IsSelected)
                    UINode.IsSelected = value;

                if (value)
                    OnSelected?.Invoke(this);
            }
        }

        public bool CanSelect { get; set; } = true;

        #endregion

        public MegaState State { get; set; }
        public NodeBase UINode { get; set; }

        public GXMaterial Material;
        public GXMesh Mesh;

        public int Order;

        public List<GLSamplerObject> TextureObjects = new List<GLSamplerObject>();

        //Mesh renderer
        private RenderMeshNonInterleaved RenderMesh;
        private GXShaderCompiler.GXProgram ShaderCompiler;
        private ShaderProgram Shader;
        private ShaderProgram ShaderPicking;

        private GXShaderCompiler.UniformSceneParams SceneParams;

        private GLUniformBlock MaterialParamBlock;
        private GLUniformBlock SceneParamBlock;
        private GLUniformBlock PacketsParamBlock;
        private GLUniformBlock DirectSkinningParamBlock;

        private GXShaderCompiler.UniformMaterialParams materialParams;
        private GXShaderCompiler.PacketParams packetParams;
        private GXShaderCompiler.DirectSkinningParams directSkinningParams;

        //temp use for standard shaders
        private bool HasVertexColors;
        private bool HasNormals;

        //Alpha for intensity
        private readonly Vector4 SelectionColor = new Vector4(1, 1, 0.5f, 0.05f);

        private bool _disposed = false;

        public void RemoveTexture(int index)
        {
            TextureObjects.RemoveAt(index);
        }

        public void ReloadTexture(int index, GLGXTexture tex)
        {
            TextureObjects[index].ID = tex.ID;
        }

        public SceneNode(GXMesh mesh, GXMaterial material, List<GLGXTexture> textures, int order = 0)
        {
            Order = order;
            Mesh = mesh;
            mesh.SceneNode = this;
            material.RenderScene = this;
            Material = material;
            State = new MegaState();
            ReloadMegaStage();
            PrepareGLMesh(mesh);
            PrepareGLMaterial(material, textures);

            UINode = new NodeBase("Mesh");
        }

        public void ReloadMegaStage() {
            GLEnumConverter.translateGfxMegaState(State, Material);
        }

        public void UpdateFog(FogObject fog)
        {
            Material.RopInfo.FogType = fog.FogType;
            materialParams.SetFogUniforms(fog);
        }

        public void UpdateLight(LightObject light, int setID)
        {
            materialParams.SetLightUniforms(light, setID);
        }

        public void ReloadShader()
        {
            Console.WriteLine($"Shader loaded {Material.Name}");

            ShaderCompiler.mMaterial = this.Material;
            if (Mesh.BoneIndices.Count > 0) //Direct skinning instead of packet skinning
                ShaderCompiler.hasDirectSkinning = false;

            for (int i = 0; i < Material.TextureMatrices.Length; i++)
            {
                ShaderCompiler.useTexMtxIdx[i] = false;

                var mapMode = Material.TextureMatrices[i].MappingMethod;
                if (mapMode == TextureMapMode.EnvCamera ||
                    mapMode == TextureMapMode.EnvSpec ||
                    mapMode == TextureMapMode.EnvLight)
                {
                    ShaderCompiler.useTexMtxIdx[i] = Material.UseSkinning;
                }
            }
            var shader = ShaderCompiler.Compile();
            var shaderPicking = ShaderCompiler.CompilePicking();

            Shader?.Dispose();
            Shader = shader;

            ShaderPicking?.Dispose();
            ShaderPicking = shaderPicking;


            materialParams.SetUniformsFromMaterial(Material);
            GLContext.ActiveContext.UpdateViewport = true;
        }

        public virtual void RenderColorPicking(GLContext context, Matrix4[] boneWorldMatrices, Matrix4[] viewWorldMatrices, OpenTK.Vector4 pickingColor)
        {
            if (_disposed || !Mesh.Visible)
                return;

            this.State.SetGLPolygonState();

            if (RenderGlobals.MeshPicking)
                pickingColor = context.ColorPicker.SetPickingColor(this);

            context.CurrentShader = ShaderPicking;

            ShaderPicking.SetVector4("color", pickingColor);

            //Setup the camera view/projection
            SceneParamBlock.BindBlock(context.CurrentShader.program, "ub_SceneParams");
            SceneParamBlock.Update(context.Camera.ProjectionMatrix, 0, 64);
            SceneParamBlock.Update(new Vector4(0, 0, 0, 0), 64, 16);
            SceneParamBlock.Update(Vector4.Zero, 80, 16);

            GL.FrontFace(Mesh.DrawFacesCounterClockWise ? FrontFaceDirection.Ccw : FrontFaceDirection.Cw);

            DrawPackets(context, boneWorldMatrices, viewWorldMatrices);

            GL.FrontFace(FrontFaceDirection.Cw);

            MegaState.SetGLDefaults();
        }

        public void Render(GLContext context, bool isSelected, Matrix4[] boneWorldMatrices, Matrix4[] viewWorldMatrices)
        {
            if (_disposed || !Mesh.Visible)
                return;

            State.SetGLState();
            context.CurrentShader = Shader;

            //Setup the camera view/projection
            SceneParamBlock.BindBlock(context.CurrentShader.program, "ub_SceneParams");
            SceneParamBlock.Update(context.Camera.ProjectionMatrix, 0, 64);
            SceneParamBlock.Update(new Vector4(0, 0, 0, 0), 64, 16);
            if (isSelected || this.IsSelected)
                SceneParamBlock.Update(SelectionColor, 80, 16);
            else
                SceneParamBlock.Update(Vector4.Zero, 80, 16);

            SceneParamBlock.Update(new Vector4((int)RenderGlobals.DebugShadingMode, RenderGlobals.Bightness, 0, 0), 96, 16);

            //Update values during render
            materialParams.UpdatePerFrame(Material);

            //Bind samplers
            int[] samplerIds = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            var location = GL.GetUniformLocation(Shader.program, "u_Texture");
            GL.Uniform1(location, 8, samplerIds);

            for (int i = 0; i < 3; i++)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + samplerIds[i]);
                RenderTools.uvTestPattern.Bind();
            }

            //Bind the texture objects and parameters for samplers
            for (int i = 0; i < TextureObjects.Count; i++)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + samplerIds[i]);
                TextureObjects[i].Render();
            }

            GL.FrontFace(Mesh.DrawFacesCounterClockWise ? FrontFaceDirection.Ccw : FrontFaceDirection.Cw);

            if (isSelected || this.IsSelected)
            {
                GL.Enable(EnableCap.StencilTest);
                GL.Clear(ClearBufferMask.StencilBufferBit);
                GL.ClearStencil(0);
                GL.StencilFunc(StencilFunction.Always, 0x1, 0x1);
                GL.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);
            }

            DrawPackets(context, boneWorldMatrices, viewWorldMatrices);

            if (isSelected || this.IsSelected)
            {
                GL.Disable(EnableCap.Blend);

                SceneParamBlock.BindBlock(context.CurrentShader.program, "ub_SceneParams");
                SceneParamBlock.Update(new Vector4(1), 80, 16);

                GL.LineWidth(2);
                GL.StencilFunc(StencilFunction.Equal, 0x0, 0x1);
                GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                DrawPackets(context, boneWorldMatrices, viewWorldMatrices);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                GL.Disable(EnableCap.StencilTest);
                GL.LineWidth(1);
            }

            GL.FrontFace(FrontFaceDirection.Cw);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            MegaState.SetGLDefaults();
        }

        public void UpdateDirectSkinning(Matrix4[] inverseWorldMatrices)
        {
            for (int i = 0; i < Math.Min(inverseWorldMatrices.Length, 200); i++)
                directSkinningParams.bones[i] = inverseWorldMatrices[i];
        }

        private void DrawPackets(GLContext context, Matrix4[] boneWorldMatrices, Matrix4[] viewWorldMatrices)
        {
            DirectSkinningParamBlock.BindBlock(context.CurrentShader.program, "ub_DirectSkinningParams");
            directSkinningParams.Fill(DirectSkinningParamBlock);

            foreach (var draw in Mesh.DrawCalls)
            {
                for (int i = 0; i < packetParams.posMtx.Length; i++)
                    packetParams.posMtx[i] = new Matrix3x4(viewWorldMatrices[0].Column0, viewWorldMatrices[0].Column1, viewWorldMatrices[0].Column2);

                for (int i = 0; i < draw.PosMatrixTable.Length; i++)
                {
                    var posNrmMatrixID = draw.PosMatrixTable[i];
                    if (posNrmMatrixID == 0xFF)
                        continue;

                    //Setup the skinning indices
                    var skinningMatrix = viewWorldMatrices[posNrmMatrixID];
                    //4x3 matrix push for position matrices
                    packetParams.posMtx[i] = new Matrix3x4(skinningMatrix.Column0, skinningMatrix.Column1, skinningMatrix.Column2);
                }
                for (int i = 0; i < draw.TexMatrixTable.Length; i++)
                {
                    var texMtxIdx = draw.TexMatrixTable[i];
                    if (texMtxIdx == 0xFF)
                        continue;

                    //Setup the skinning indices
                    var skinningMatrix = viewWorldMatrices[texMtxIdx];
                    skinningMatrix = TexMtx.computeNormalMatrix(skinningMatrix);
                    materialParams.TexMtx[i] = new Matrix3x4(skinningMatrix.Column0, skinningMatrix.Column1, skinningMatrix.Column2);
                }
                //Single rigid bodies if bone indices are used directly
                if (!this.Material.UseSkinning && Mesh.BoneIndex != -1)
                {
                    var skinningMatrix = viewWorldMatrices[Mesh.BoneIndex];
                    packetParams.posMtx[0] = new Matrix3x4(skinningMatrix.Column0, skinningMatrix.Column1, skinningMatrix.Column2);

                    var normalMatrix = TexMtx.computeNormalMatrix(skinningMatrix);
                    for (int i = 0; i < 10; i++)
                        materialParams.TexMtx[i] = new Matrix3x4(normalMatrix.Column0, normalMatrix.Column1, normalMatrix.Column2);
                }

                PacketsParamBlock.BindBlock(context.CurrentShader.program, "ub_PacketParams");
                packetParams.Fill(PacketsParamBlock);

                for (int i = 0; i < Material.TextureMatrices.Length; i++)
                {
                    if (Material.TextureMatrices[i] != null)
                    {
                        var mat = Material.TextureMatrices[i].Compute(boneWorldMatrices[0], context.Camera.ProjectionMatrix);
                        materialParams.PostTexMtx[i] = new Matrix3x4(mat.Column0, mat.Column1, mat.Column2);
                    }
                }

                MaterialParamBlock.BindBlock(context.CurrentShader.program, "ub_MaterialParams");
                materialParams.Fill(MaterialParamBlock, Material.HasPostTexMtx);

                RenderMesh.Draw(context.CurrentShader, draw.IndexCount, draw.IndexOffset);
            }
        }

        private void DefaultRender(GLContext context, Matrix4 matrix, bool isSelected)
        {
            StandardMaterial mat = new StandardMaterial();
            mat.hasVertexColors = HasVertexColors;
            mat.HalfLambertShading = HasNormals;
            mat.ModelMatrix = matrix;

            if (DebugShaderRender.DebugRendering != DebugShaderRender.DebugRender.Default)
            {
                context.CurrentShader = GlobalShaders.GetShader("DEBUG");
                context.CurrentShader.SetMatrix4x4(GLConstants.ModelMatrix, ref matrix);
                DebugShaderRender.RenderMaterial(context);
            }
            else
                mat.Render(context);

            RenderMesh.DrawWithSelection(context, isSelected || IsSelected);
        }

        private void PrepareGLMaterial(GXMaterial material, List<GLGXTexture> textures)
        {
            //Attach the renderable textures into texture objects for rendering onto the material
            List<GLSamplerObject> textureObjects = new List<GLSamplerObject>();

            ShaderCompiler = new GXShaderCompiler.GXProgram();
            ReloadShader();

            //Prepare the uniform block GL instances. Important that the binding indices are unique.
            SceneParamBlock = new GLUniformBlock(0, GXShaderCompiler.UniformSceneParams.Size);
            MaterialParamBlock = new GLUniformBlock(1, GXShaderCompiler.UniformMaterialParams.Size);
            PacketsParamBlock = new GLUniformBlock(2, GXShaderCompiler.PacketParams.Size);
            DirectSkinningParamBlock = new GLUniformBlock(3, GXShaderCompiler.DirectSkinningParams.Size);

            //Setup and bind uniforms
            materialParams = new GXShaderCompiler.UniformMaterialParams();
            SceneParams = new GXShaderCompiler.UniformSceneParams();
            packetParams = new GXShaderCompiler.PacketParams();
            SceneParams.projection = Matrix4.Identity;

            //Update the material params with data from the GXMaterial
            materialParams.SetUniformsFromMaterial(material);

            //Direct skinning for non packet cpu skinning to gpu
            directSkinningParams = new GXShaderCompiler.DirectSkinningParams();

            //Prepare the textures
            for (int i = 0; i < material.Textures.Length; i++)
            {
                if (material.Textures[i] == null)
                    continue;

                var sampler = material.Textures[i];
                var target = textures.FirstOrDefault(x => x.Name == sampler.Texture);
                //Map by index instead if used
                if (sampler.TextureIndex > -1 && textures.Count > sampler.TextureIndex)
                    target = textures[sampler.TextureIndex];

                //Failed to find texture, skip
                if (target == null)
                    continue;

                //Create into a renderable GL texture map
                GLSamplerObject obj = new GLSamplerObject(target);
                obj.Name = sampler.Texture;
                obj.WrapU = GLEnumConverter.gxWrapToGL(sampler.WrapX);
                obj.WrapV = GLEnumConverter.gxWrapToGL(sampler.WrapY);
                obj.MagFilter = GLEnumConverter.gxFilterMagToGL(sampler.MagFilter);
                obj.MinFilter = GLEnumConverter.gxFilterMinToGL(sampler.MinFilter);
                TextureObjects.Add(obj);

                //Setup uniforms for the texture.
                materialParams.TexParams[i] = new Vector4(target.Width, target.Height, 0, sampler.LODBias);
            }

            //Init the GL blocks with defaults
            SceneParamBlock.BindBlock(Shader.program, "ub_SceneParams");
            SceneParamBlock.Int();

            MaterialParamBlock.BindBlock(Shader.program, "ub_MaterialParams");
            MaterialParamBlock.Int();

            PacketsParamBlock.BindBlock(Shader.program, "ub_PacketParams");
            PacketsParamBlock.Int();

            DirectSkinningParamBlock.BindBlock(Shader.program, "ub_DirectSkinningParams");
            DirectSkinningParamBlock.Int();

            //Bind with picking shader too. Only needs scene and packet uniform data
            SceneParamBlock.BindBlock(ShaderPicking.program, "ub_SceneParams");
            SceneParamBlock.Int();

            PacketsParamBlock.BindBlock(ShaderPicking.program, "ub_SceneParams");
            PacketsParamBlock.Int();

            DirectSkinningParamBlock.BindBlock(ShaderPicking.program, "ub_DirectSkinningParams");
            DirectSkinningParamBlock.Int();

            //Then fill them with the proper param structs
            packetParams.Fill(PacketsParamBlock);
            materialParams.Fill(MaterialParamBlock, Material.HasPostTexMtx);
            directSkinningParams.Fill(DirectSkinningParamBlock);
        }

        private void PrepareGLMesh(GXMesh mesh)
        {
            if (_disposed || RenderMesh != null)
                return;

            for (int i = 0; i < mesh.Positions.Count; i++)
                mesh.Positions[i] = mesh.Positions[i] * GLContext.PreviewScale;

            int index = 0;
            HasVertexColors = mesh.Color0.Count > 0;
            HasNormals = mesh.Normals.Count > 0;

            //Set attributes
            List<RenderAttribute> attributes = GetGXAttributes(mesh);

            RenderMesh = new RenderMeshNonInterleaved(mesh.Indices, PrimitiveType.Triangles);
            RenderMesh.ClearAttributes();
            RenderMesh.AddAttributes(attributes.ToArray());

            //Put attributes into seperate render buffers for drawing
            index = 0;
            RenderMesh.SetData(mesh.Positions.ToArray(), index++);
            if (mesh.PosMatrixIdx.Count > 0) RenderMesh.SetData(mesh.PosMatrixIdx.ToArray(), index++);
            if (mesh.Normals.Count > 0)   RenderMesh.SetData(mesh.Normals.ToArray(), index++);
            if (mesh.Tangents.Count > 0) RenderMesh.SetData(mesh.Tangents.ToArray(), index++);
            if (mesh.Binormals.Count > 0) RenderMesh.SetData(mesh.Binormals.ToArray(), index++);
            if (mesh.TexCoord0.Count > 0) RenderMesh.SetData(mesh.TexCoord0.ToArray(), index++);
            if (mesh.TexCoord1.Count > 0) RenderMesh.SetData(mesh.TexCoord1.ToArray(), index++);
            if (mesh.TexCoord2.Count > 0) RenderMesh.SetData(mesh.TexCoord2.ToArray(), index++);
            if (mesh.TexCoord3.Count > 0) RenderMesh.SetData(mesh.TexCoord3.ToArray(), index++);
            if (mesh.TexCoord4.Count > 0) RenderMesh.SetData(mesh.TexCoord4.ToArray(), index++);
            if (mesh.TexCoord5.Count > 0) RenderMesh.SetData(mesh.TexCoord5.ToArray(), index++);
            if (mesh.TexCoord6.Count > 0) RenderMesh.SetData(mesh.TexCoord6.ToArray(), index++);
            if (mesh.TexCoord7.Count > 0) RenderMesh.SetData(mesh.TexCoord7.ToArray(), index++);
            if (mesh.Color0.Count > 0)    RenderMesh.SetData(mesh.Color0.ToArray(), index++);
            if (mesh.TexMatrices0123.Count > 0) RenderMesh.SetData(mesh.TexMatrices0123.ToArray(), index++);
            if (mesh.TexMatrices4567.Count > 0) RenderMesh.SetData(mesh.TexMatrices4567.ToArray(), index++);
            if (mesh.BoneIndices.Count > 0) RenderMesh.SetData(mesh.BoneIndices.ToArray(), index++);
            if (mesh.BoneWeights.Count > 0) RenderMesh.SetData(mesh.BoneWeights.ToArray(), index++);
        }

        public void UpdatePositions()
        {
            //RenderMesh.SetData(Mesh.Positions.ToArray(), 0);
            if (Mesh.WeightedPositions.Count > 0)
                RenderMesh.buffers[0].SetData(Mesh.WeightedPositions.ToArray(), BufferUsageHint.DynamicDraw);
            if (Mesh.WeightedNormals.Count > 0)
                RenderMesh.buffers[1].SetData(Mesh.WeightedNormals.ToArray(), BufferUsageHint.DynamicDraw);
        }

        private List<RenderAttribute> GetGXAttributes(GXMesh mesh)
        {
            int index = 0;

            List<RenderAttribute> attributes = new List<RenderAttribute>();
            attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.POS), VertexAttribPointerType.Float, 0, 3) { BufferIndex = index++ });
            if (mesh.PosMatrixIdx.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.PNMTXIDX), VertexAttribPointerType.Float, 0, 1) { BufferIndex = index++ });
            if (mesh.Normals.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.NRM), VertexAttribPointerType.Float, 0, 3) { BufferIndex = index++ });
            if (mesh.Tangents.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TANGENT), VertexAttribPointerType.Float, 0, 3) { BufferIndex = index++ });
            if (mesh.Binormals.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.BINRM), VertexAttribPointerType.Float, 0, 3) { BufferIndex = index++ });
            if (mesh.TexCoord0.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TEX0), VertexAttribPointerType.Float, 0, 2) { BufferIndex = index++ });
            if (mesh.TexCoord1.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TEX1), VertexAttribPointerType.Float, 0, 2) { BufferIndex = index++ });
            if (mesh.TexCoord2.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TEX2), VertexAttribPointerType.Float, 0, 2) { BufferIndex = index++ });
            if (mesh.TexCoord3.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TEX3), VertexAttribPointerType.Float, 0, 2) { BufferIndex = index++ });
            if (mesh.TexCoord4.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TEX4), VertexAttribPointerType.Float, 0, 2) { BufferIndex = index++ });
            if (mesh.TexCoord5.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TEX5), VertexAttribPointerType.Float, 0, 2) { BufferIndex = index++ });
            if (mesh.TexCoord6.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TEX6), VertexAttribPointerType.Float, 0, 2) { BufferIndex = index++ });
            if (mesh.TexCoord7.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TEX7), VertexAttribPointerType.Float, 0, 2) { BufferIndex = index++ });
            if (mesh.Color0.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.CLR0), VertexAttribPointerType.Float, 0, 4) { BufferIndex = index++ });
            if (mesh.TexMatrices0123.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TEX0MTXIDX), VertexAttribPointerType.Float, 0, 4) { BufferIndex = index++ });
            if (mesh.TexMatrices4567.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.TEX4MTXIDX), VertexAttribPointerType.Float, 0, 4) { BufferIndex = index++ });

            if (mesh.BoneIndices.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.BoneIndex), VertexAttribPointerType.Float, 0, 4) { BufferIndex = index++ });
            if (mesh.BoneWeights.Count > 0)
                attributes.Add(new RenderAttribute(GXShaderCompiler.GetAttributeUniform(VertexAttributeInput.BoneWeight), VertexAttribPointerType.Float, 0, 4) { BufferIndex = index++ });

            return attributes;
        }

        private List<RenderAttribute> GetDefaultAttributes(GXMesh mesh)
        {
            int index = 0;

            List<RenderAttribute> attributes = new List<RenderAttribute>();
            attributes.Add(new RenderAttribute(GLConstants.VPosition, VertexAttribPointerType.Float, 0, 3) { BufferIndex = index++ });
            if (mesh.Normals.Count > 0)
                attributes.Add(new RenderAttribute(GLConstants.VNormal, VertexAttribPointerType.Float, 0, 3) { BufferIndex = index++ });
            if (mesh.TexCoord0.Count > 0)
                attributes.Add(new RenderAttribute(GLConstants.VTexCoord0, VertexAttribPointerType.Float, 0, 2) { BufferIndex = index++ });
            if (mesh.Color0.Count > 0)
                attributes.Add(new RenderAttribute(GLConstants.VColor, VertexAttribPointerType.Float, 0, 4) { BufferIndex = index++ });
            return attributes;
        }

        public void Dispose()
        {
            _disposed = true;

            Shader?.Dispose();
            ShaderPicking?.Dispose();
            SceneParamBlock?.Dispose();
            PacketsParamBlock?.Dispose();
            DirectSkinningParamBlock?.Dispose();
            MaterialParamBlock?.Dispose();
            RenderMesh?.Dispose();
            foreach (var tex in this.TextureObjects)
                tex?.Dispose();
        }
    }
}
