using UnityEngine;
using System.Collections;
using System.IO;

namespace PCFFileFormat
{
    //All blocks must adhere to this protocol.
    public abstract class DataBlockBase
    {
        protected static readonly int ChunkByteLength = 4;
        protected static readonly int SimpleByteLength = 1;
        protected static readonly int BlockTypeLength = 4;
        protected static readonly int ResourceTypeLength = 4;
        protected static readonly int ObjectCountLength = 4;
        protected static readonly int REFERENCE_ID_LENGTH = 36;

        protected int objectCount;

        public abstract int GetLength();
        public abstract byte[] GetBytes();
        public abstract void SetBytes(Stream file, int chunkLength, long offsetPos, bool assemblyMode);

        public int GetObjectCount()
        {
            return this.objectCount;
        }
    }
}
