using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace GCNRenderLibrary.Rendering
{
    public class GXBuffer
    {
        //Todo I will redo the way these are handled. Just something quick to get the data
        public List<Vector2> Data2;

        public List<Vector3> Data3;

        public List<Vector4> Data4;

        /// <summary>
        /// The size of the buffer stride in bytes
        /// </summary>
        public int Stride;

        public GXBuffer(List<Vector2> vector, int stride)
        {
            Data2 = vector;
            Stride = stride;
        }

        public GXBuffer(List<Vector3> vector, int stride)
        {
            Data3 = vector;
            Stride = stride;
        }

        public GXBuffer(List<Vector4> vector, int stride)
        {
            Data4 = vector;
            Stride = stride;
        }
    }
}
