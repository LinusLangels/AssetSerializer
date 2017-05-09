using UnityEngine;
using System.Collections;

namespace PCFFileFormat
{
    public abstract class ResourceBase
    {
        protected static readonly int ChunkByteLength = 4;
        protected static readonly int BlockTypeLength = 4;
        protected static readonly int REFERENCE_ID_LENGTH = 36;

        public abstract int GetLength();
        public abstract byte[] GetBytes();
    }
}
