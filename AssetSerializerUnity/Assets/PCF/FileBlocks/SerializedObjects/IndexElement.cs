using UnityEngine;
using System.Collections;
using System;

namespace PCFFileFormat
{
    //Summary: A resource element is a datapoint inside each datablock, multiple resource elements can exists in each datablock.
    internal class IndexElement
    {
        private byte[] elementType;        // 4 Bytes UINT32
        private byte[] byteOffset;           // 4 Bytes UINT32

        public IndexElement(PCFResourceType elementType, int byteOffset)
        {
            this.elementType = BitConverter.GetBytes((int)elementType);
            this.byteOffset = BitConverter.GetBytes(byteOffset);
        }

        public int GetLength()
        {
            return elementType.Length + byteOffset.Length;
        }

        //Summary: Get a bytestream representation of this element.
        public byte[] GetBytes()
        {
            int arrayLength = GetLength();
            byte[] bytes = new byte[arrayLength];

            //Copy the first 4 bytes to the start of the stream. this is the length of the data chunk.
            int offset = 0;
            Buffer.BlockCopy(elementType, 0, bytes, offset, elementType.Length);

            //Copy the id into the byte representation, offset it from the length block
            offset += elementType.Length;
            Buffer.BlockCopy(byteOffset, 0, bytes, offset, byteOffset.Length);

            return bytes;
        }
    }
}
