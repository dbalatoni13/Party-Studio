using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;

namespace GCNRenderLibrary.Rendering
{
    /// <summary>
    /// Represents a mesh used for rendering GX draw call data.
    /// </summary>
    public class GXMesh
    {
        /// <summary>
        /// The draw call list to display polygons.
        /// This list is split based on the matrix tables (a max of 10 matrices per draw)
        /// </summary>
        public List<GXDraw> DrawCalls = new List<GXDraw>();

        /// <summary>
        /// The bone index used for rigid binding a matrix in the matrix table.
        /// </summary>
        public int BoneIndex = -1;

        /// <summary>
        /// Determines to display the gx mesh or not.
        /// </summary>
        public bool Visible = true;

        /// <summary>
        /// Draw the faces counter clockwise instead of clockwise.
        /// </summary>
        public bool DrawFacesCounterClockWise = true;

        /// <summary>
        /// The scene node used to render the mesh data.
        /// </summary>
        public SceneNode SceneNode;

        //Data buffers for GL
        public List<float> PosMatrixIdx = new List<float>();

        /// <summary>
        /// The vertex positions in local space (non weighted).
        /// </summary>
        public List<Vector3> Positions;

        /// <summary>
        /// The vertex normals in local space (non weighted).
        /// </summary>
        public List<Vector3> Normals;

        /// <summary>
        /// The vertex binormals in local space (non weighted).
        /// </summary>
        public List<Vector3> Binormals;

        /// <summary>
        /// The vertex tangents in local space (non weighted).
        /// </summary>
        public List<Vector3> Tangents;

        /// <summary>
        /// The vertex positions in world space (weighted).
        /// </summary>
        public List<Vector3> WeightedPositions;

        /// <summary>
        /// The vertex normals in world space (weighted).
        /// </summary>
        public List<Vector3> WeightedNormals;

        /// <summary>
        /// The texture coordinates (uv layer 0)
        /// </summary>
        public List<Vector2> TexCoord0;

        /// <summary>
        /// The texture coordinates (uv layer 1)
        /// </summary>
        public List<Vector2> TexCoord1;

        /// <summary>
        /// The texture coordinates (uv layer 2)
        /// </summary>
        public List<Vector2> TexCoord2;

        /// <summary>
        /// The texture coordinates (uv layer 3)
        /// </summary>
        public List<Vector2> TexCoord3;

        /// <summary>
        /// The texture coordinates (uv layer 4)
        /// </summary>
        public List<Vector2> TexCoord4;

        /// <summary>
        /// The texture coordinates (uv layer 5)
        /// </summary>
        public List<Vector2> TexCoord5;

        /// <summary>
        /// The texture coordinates (uv layer 6)
        /// </summary>
        public List<Vector2> TexCoord6;

        /// <summary>
        /// The texture coordinates (uv layer 7)
        /// </summary>
        public List<Vector2> TexCoord7;

        /// <summary>
        /// The vertex colors (channel 0)
        /// </summary>
        public List<Vector4> Color0;

        /// <summary>
        /// The vertex colors (channel 1)
        /// </summary>
        public List<Vector4> Color1;

        /// <summary>
        /// 4 indices (0 - 4) used for remapping matrices.
        /// </summary>
        public List<Vector4> TexMatrices0123;

        /// <summary>
        /// 4 indices (4 - 7) used for remapping matrices.
        /// </summary>
        public List<Vector4> TexMatrices4567;

        //CPU -> GPU Skinning

        public List<Vector4> BoneIndices;

        public List<Vector4> BoneWeights;

        /// <summary>
        /// The indices used for displaying and connecting the vertex data as triangles.
        /// </summary>
        public int[] Indices;

        /// <summary>
        /// 
        /// </summary>
        public List<int> BoneIndexTable = new List<int>();

        public GXMesh()
        {
            Positions = new List<Vector3>();
            Normals = new List<Vector3>();
            Binormals = new List<Vector3>();
            Tangents = new List<Vector3>();
            Color0 = new List<Vector4>();
            Color1 = new List<Vector4>();
            TexCoord0 = new List<Vector2>();
            TexCoord1 = new List<Vector2>();
            TexCoord2 = new List<Vector2>();
            TexCoord3 = new List<Vector2>();
            TexCoord4 = new List<Vector2>();
            TexCoord5 = new List<Vector2>();
            TexCoord6 = new List<Vector2>();
            TexCoord7 = new List<Vector2>();

            PosMatrixIdx = new List<float>();
            TexMatrices0123 = new List<Vector4>();
            TexMatrices4567 = new List<Vector4>();

            BoneIndices = new List<Vector4>();
            BoneWeights = new List<Vector4>();

            WeightedNormals = new List<Vector3>();
            WeightedPositions = new List<Vector3>();
        }

        //Todo. Need to support ushorts in renderer
        public void SetIndices(ushort[] faces)
        {
            Indices = new int[faces.Length];
            for (int i = 0; i < faces.Length; i++)
                Indices[i] = faces[i];
        }

        public float GetBoneIndices(int vertexID, int boneID)
        {
            switch (boneID)
            {
                case 0: return this.BoneIndices[vertexID].X;
                case 1: return this.BoneIndices[vertexID].Y;
                case 2: return this.BoneIndices[vertexID].Z;
                case 3: return this.BoneIndices[vertexID].W;
            }
            return 0;
        }

        public float GetWeight(int vertexID, int boneID)
        {
            switch (boneID)
            {
                case 0: return this.BoneWeights[vertexID].X;
                case 1: return this.BoneWeights[vertexID].Y;
                case 2: return this.BoneWeights[vertexID].Z;
                case 3: return this.BoneWeights[vertexID].W;
            }
            return 0;
        }
    }
}
