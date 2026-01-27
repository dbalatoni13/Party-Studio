using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCNRenderLibrary.Rendering
{
    /// <summary>
    /// Represents a single draw call of a GXMesh used for drawing indices.
    /// </summary>
    public class GXDraw
    {
        /// <summary>
        /// The start offset in bytes where to read the indices.
        /// </summary>
        public int IndexOffset;

        /// <summary>
        /// The total amount of indices to load in draw.
        /// </summary>
        public int IndexCount;

        /// <summary>
        /// The position index table used to map the vertex index to the node matrix table.
        /// </summary>
        public byte[] PosMatrixTable = new byte[21];

        /// <summary>
        /// The tex index table used to map the vertex index to the tex matrix table.
        /// </summary>
        public byte[] TexMatrixTable = new byte[21];

        public GXDraw(int indexStart)
        {
            IndexOffset = indexStart * sizeof(int);

            for (int i = 0; i < PosMatrixTable.Length; i++)
                PosMatrixTable[i] = 0xFF;
            for (int i = 0; i < TexMatrixTable.Length; i++)
                TexMatrixTable[i] = 0xFF;
        }
    }
}
