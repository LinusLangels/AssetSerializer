//#define VERBOSE_LOG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace PCFFileFormat
{
    //Summary: Index over what blocks are present in the file and where to find them.
    class IndexBlock : DataBlockBase
    {
        public static readonly int BYTES_PER_ELEMENT = 8;

        private byte[] chunkLength; //Always 4 bytes long. UINT32. Describes how many bytes the data element contains.
        private byte[] blockType; //4 bytes.
        private List<IndexElement> indexElements;

        public IndexBlock()
        {
            this.blockType = BitConverter.GetBytes((int)PCFBlockTypes.INDEXBLOCK);
            this.indexElements = new List<IndexElement>();
        }

        public void AddIndex(IndexElement index)
        {
            //We cant add to it, if it doesn't exist now...can we.
            if (this.indexElements == null)
                this.indexElements = new List<IndexElement>();

            indexElements.Add(index);
        }

        public override int GetLength()
        {
            int elementsLength = 0;

            //Index elements have a fixed length of 8.
            for (int i = 0; i < indexElements.Count; i++)
                elementsLength += indexElements[i].GetLength();

            return elementsLength + blockType.Length;
        }

        //Summary: Get a bytestream representation of this block.
        public override byte[] GetBytes()
        {
            int dataLength = GetLength();
            this.chunkLength = BitConverter.GetBytes(dataLength);

            //We exclude the 4 bytes that tell us the length of the chunk.
            //We do however make room for the bytes into the buffer so we can copy the data.
            byte[] bytes = new byte[dataLength + ChunkByteLength];

            //Copy the first 4 bytes to the start of the stream. this is the length of the data chunk.
            int offset = 0;
            Buffer.BlockCopy(this.chunkLength, 0, bytes, 0, chunkLength.Length);

            //Offset to chunk length bytes.
            offset += this.chunkLength.Length;
            Buffer.BlockCopy(this.blockType, 0, bytes, offset, this.blockType.Length);

            //Offset from blocktype bytes.
            offset += this.blockType.Length;

            for (int i = 0; i < this.indexElements.Count; i++)
            {
                int byteLength = this.indexElements[i].GetLength();
                Buffer.BlockCopy(this.indexElements[i].GetBytes(), 0, bytes, offset, byteLength);

                offset += byteLength;
            }

            return bytes;
        }

        public override void SetBytes(Stream file, int chunkLength, long offsetPos, bool assemblyMode)
        {
            int bytesToRead = chunkLength - BlockTypeLength;
            int bytesRead = 0;

            //Loop and create index elements.
            while (bytesRead < bytesToRead)
            {
                byte[] elementTypeBuffer = new byte[ChunkByteLength];
                file.Read(elementTypeBuffer, 0, ChunkByteLength);
                int rawElementTypeValue = BitConverter.ToInt32(elementTypeBuffer, 0);
                PCFResourceType resourceType = (PCFResourceType)Enum.ToObject(typeof(PCFResourceType), rawElementTypeValue);

                byte[] byteOffsetBuffer = new byte[ChunkByteLength];
                file.Read(byteOffsetBuffer, 0, ChunkByteLength);
                int rawByteOffsetValue = BitConverter.ToInt32(byteOffsetBuffer, 0);

                IndexElement element = new IndexElement(resourceType, rawByteOffsetValue);
                this.indexElements.Add(element);

                bytesRead += BYTES_PER_ELEMENT;
            }

            #if VERBOSE_LOG
            Debug.Log("Indexblock length: " + bytesToRead);
            #endif
        }
    }
}
