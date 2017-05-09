//#define VERBOSE_LOG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace PCFFileFormat
{
    //Data blocks contains elements, they act as the parent data container.
    public class ResourceBlock : DataBlockBase
    {
        //Reuse this buffer, optimization.
        static byte[] CHUNK_BYTE_BUFFER = new byte[ChunkByteLength];
        static byte[] SIMPLE_BYTE_BUFFER = new byte[SimpleByteLength];

        private byte[] chunkLength; //Always 4 bytes long. UINT32. Describes how many bytes the data element contains.
        private byte[] blockType; //4 bytes.
        private byte[] resourceType; //4 bytes.
        private Dictionary<UInt32, AssetResource> resourceDatabase;
		private bool streamResources;

		public ResourceBlock(PCFResourceType resourceType, bool streamResources = false)
        {
            this.blockType = BitConverter.GetBytes((int)PCFBlockTypes.RESOURCEBLOCK);
            this.resourceType = BitConverter.GetBytes((int)resourceType);
			this.streamResources = streamResources;
            this.objectCount = 0;
        }

        public override int GetLength()
        {
            int resourcesLength = 0;

            for (int i = 0; i < this.resourceDatabase.Count; i++)
            {
                var item = this.resourceDatabase.ElementAt(i);

                //Factor in the 4 bytes that tell the size of the chunk.
                resourcesLength += item.Value.GetLength() + ChunkByteLength;
            }

            return resourcesLength + this.blockType.Length + this.resourceType.Length + ChunkByteLength;
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
            Buffer.BlockCopy(this.chunkLength, 0, bytes, 0, this.chunkLength.Length);

            //Copy the data block starting 4 bytes in, offset from the length block.
            offset += this.chunkLength.Length;
            Buffer.BlockCopy(this.blockType, 0, bytes, offset, this.blockType.Length);

            offset += this.chunkLength.Length;
            Buffer.BlockCopy(this.resourceType, 0, bytes, offset, this.resourceType.Length);

            //Tracks how many resources are added to this block.
            offset += this.resourceType.Length;
            byte[] serializedObjectCount = BitConverter.GetBytes(this.objectCount);
            Buffer.BlockCopy(serializedObjectCount, 0, bytes, offset, serializedObjectCount.Length);

            offset += serializedObjectCount.Length;
            for (int i = 0; i < this.resourceDatabase.Count; i++)
            {
                var item = this.resourceDatabase.ElementAt(i);
                byte[] resourceData = item.Value.GetBytes();

                Buffer.BlockCopy(resourceData, 0, bytes, offset, resourceData.Length);

                offset += resourceData.Length;
            }

            return bytes;
        }

        public override void SetBytes(Stream file, int chunkLength, long offsetPos, bool assemblyMode)
        {
            //Factor in the bytes we have already read elsewhere.
            int bytesToRead = chunkLength - (BlockTypeLength + ResourceTypeLength);
            int bytesRead = 0;

            //Number of resources in this block.
            file.Read(CHUNK_BYTE_BUFFER, 0, ChunkByteLength);
            this.objectCount = BitConverter.ToInt32(CHUNK_BYTE_BUFFER, 0);

            //Preallocate size of collection to avoid resize calls (memory optimization)
            this.resourceDatabase = new Dictionary<UInt32, AssetResource>(this.objectCount);

            bytesRead += ChunkByteLength;

            //Loop and create index elements.
            while (bytesRead < bytesToRead)
            {
                file.Read(CHUNK_BYTE_BUFFER, 0, ChunkByteLength);
                int resourceChunkLength = BitConverter.ToInt32(CHUNK_BYTE_BUFFER, 0);

                int bytesLeftToRead = resourceChunkLength;

                file.Read(CHUNK_BYTE_BUFFER, 0, ChunkByteLength);
                UInt32 referenceID = BitConverter.ToUInt32(CHUNK_BYTE_BUFFER, 0);

                bytesLeftToRead -= ChunkByteLength;

                file.Read(SIMPLE_BYTE_BUFFER, 0, SimpleByteLength);
                bool streamed = BitConverter.ToBoolean(SIMPLE_BYTE_BUFFER, 0);

                bytesLeftToRead -= SimpleByteLength;

                file.Read(CHUNK_BYTE_BUFFER, 0, ChunkByteLength);
                int metaDataType = BitConverter.ToInt32(CHUNK_BYTE_BUFFER, 0);

                bytesLeftToRead -= ChunkByteLength;

                file.Read(CHUNK_BYTE_BUFFER, 0, ChunkByteLength);
                int metadataLength = BitConverter.ToInt32(CHUNK_BYTE_BUFFER, 0);

                bytesLeftToRead -= ChunkByteLength;

                //Optmization, some nodes lack these fields.
                byte[] metaData = null;
                if (metadataLength > 0)
                {
                    metaData = new byte[metadataLength];
                    file.Read(metaData, 0, metadataLength);

                    bytesLeftToRead -= metadataLength;
                }
               
                byte[] dataBuffer = null;

                //The are used for streamed nodes.
                UInt32 streamPosition = 0;
                UInt32 streamLength = 0;

                //Optmization, some nodes lack these fields.
                if (bytesLeftToRead != 0)
                {
                    streamPosition = (UInt32)(file.Position + offsetPos);
                    streamLength = (UInt32)bytesLeftToRead;

                    if (assemblyMode)
                    {
                        streamPosition = (UInt32)(file.Position + offsetPos);
                        streamLength = (UInt32)bytesLeftToRead;
                        dataBuffer = new byte[bytesLeftToRead];

                        file.Read(dataBuffer, 0, bytesLeftToRead);
                    }
                    else
                    {
                        //Streamed nodes dont load the byte data into memory, only save the location of where it is in the file.
                        //We also allow the caller to stream all data so that we can create the resources without putting pressure om memory.
                        if (streamed || this.streamResources)
                        {
                            file.Seek(bytesLeftToRead, SeekOrigin.Current);
                        }
                        else
                        {
                            dataBuffer = new byte[bytesLeftToRead];
                            file.Read(dataBuffer, 0, bytesLeftToRead);
                        }
                    }
                }

                AssetResource resource = new AssetResource(streamed, streamPosition, streamLength);
                resource.Deserialize(referenceID, metaDataType, metaData, dataBuffer, assemblyMode);

                //Use hashmap indexing, faster to lookup.
                this.resourceDatabase.Add(referenceID, resource);

                bytesRead += resourceChunkLength + ChunkByteLength;
            }

            #if VERBOSE_LOG
            Debug.Log("Resourceblock length: " + chunkLength);
            #endif
        }

        public void AddResource(UInt32 referenceID, AssetResource resource)
        {
            if (this.resourceDatabase == null)
                this.resourceDatabase = new Dictionary<UInt32, AssetResource>();

            this.resourceDatabase.Add(referenceID, resource);

            //Tracks how many resources is added to this block.
            //that way we can anticipate collection size when deserializing. (Optimization)
            this.objectCount++;
        }

        public AssetResource GetResource(UInt32 referenceID)
        {
            if (this.resourceDatabase.ContainsKey(referenceID))
            {
                return this.resourceDatabase[referenceID];
            }

            return null;
        }

        public void ReplaceResource(UInt32 referenceID, AssetResource newResource)
        {
            if (this.resourceDatabase.ContainsKey(referenceID))
            {
                this.resourceDatabase.Remove(referenceID);
                this.resourceDatabase.Add(referenceID, newResource);
            }
        }
    }
}