
using UnityEngine;
using System.Collections;
using System;

namespace PCFFileFormat
{
    public enum MetaDataType
    {
        UNKOWN = 0,
        JSON = 1,
        BINARY = 2,
        PROTOBUF = 3,
    }

    //This is the basic data representation for all types.
    public class AssetResource : ResourceBase
    {
        private byte[] chunkLength;          // 4 Bytes
        private byte[] resourceID;           // 4 Bytes
        private byte[] metaDataType;         // 4 Bytes
        private byte[] metaDataLength;       // 4 Bytes
        private byte[] metaData;             // UTF8 text string with metadata
        private byte[] resourceData;         // The remaining bytes
        private byte[] streamed;

        private UInt32 deserializedID;
        private int deserializedMetaDataType;

        private bool isStreamed;
        private UInt32 streamPosition;
        private UInt32 streamLength;

        public AssetResource(bool streamed)
        {
            this.streamed = BitConverter.GetBytes(streamed);
        }

        public AssetResource(bool streamed, UInt32 streamPosition, UInt32 streamLength)
        {
            this.isStreamed = streamed;
            this.streamPosition = streamPosition;
            this.streamLength = streamLength;
        }

        public void Serialize(UInt32 resourceID, MetaDataType metaDataType, byte[] metaData, byte[] data)
        {
            this.resourceID = BitConverter.GetBytes(resourceID);
            this.metaDataType = BitConverter.GetBytes((int)metaDataType);
            this.resourceData = data;
            
            if (metaData != null)
            {
                this.metaData = metaData;
                this.metaDataLength = BitConverter.GetBytes(metaData.Length);
            }
            else
            {
                this.metaDataLength = BitConverter.GetBytes(0);
            }
        }
        
        public void Deserialize(UInt32 resourceID, int metaDataType, byte[] metaData, byte[] data, bool assemblyMode)
        {
            if (assemblyMode)
            {
                this.resourceID = BitConverter.GetBytes(resourceID);
                this.metaDataType = BitConverter.GetBytes(metaDataType);
                this.streamed = BitConverter.GetBytes(isStreamed);

                if (metaData != null)
                {
                    this.metaDataLength = BitConverter.GetBytes(metaData.Length);
                }
                else
                {
                    this.metaDataLength = BitConverter.GetBytes(0);
                }
            }

            this.deserializedID = resourceID;
            this.deserializedMetaDataType = metaDataType;
            this.metaData = metaData;
            this.resourceData = data;
        }

        public override int GetLength()
        {
            int dataLength = this.resourceData != null ? resourceData.Length : 0;
            int metaLength = this.metaData != null ? this.metaData.Length : 0;

            return resourceID.Length +
                   streamed.Length +
                   metaDataType.Length +
                   metaDataLength.Length +
                   metaLength +
                   dataLength;
        }

        public UInt32 GetResourceID()
        {
            return this.deserializedID;
        }

        public MetaDataType GetMetaDataType()
        {
            MetaDataType type = (MetaDataType)Enum.ToObject(typeof(MetaDataType), this.metaDataType);

            return type;
        }

        public bool IsStreamed()
        {
            return this.isStreamed;
        }

        public UInt32 GetStreamPosition()
        {
            return this.streamPosition;
        }

        public UInt32 GetStreamLength()
        {
            return this.streamLength;
        }

        public byte[] GetMetaData()
        {
            return this.metaData;
        }

        public byte[] GetResourceData()
        {
            return this.resourceData;
        }

        //Summary: Get a bytestream representation of this element.
        public override byte[] GetBytes()
        {
            int dataLength = GetLength();
            this.chunkLength = BitConverter.GetBytes(dataLength);

            //We exclude the 4 bytes that tell us the length of the chunk.
            //We do however make room for the bytes into the buffer so we can copy the data.
            byte[] bytes = new byte[dataLength + ChunkByteLength];

            //Copy the first 4 bytes to the start of the stream. this is the length of the data chunk.
            int offset = 0;
            Buffer.BlockCopy(this.chunkLength, 0, bytes, offset, this.chunkLength.Length);

            //Copy the hash ID for the resource into buffer.
            offset += this.chunkLength.Length;
            Buffer.BlockCopy(this.resourceID, 0, bytes, offset, this.resourceID.Length);

            offset += this.resourceID.Length;
            Buffer.BlockCopy(this.streamed, 0, bytes, offset, this.streamed.Length);

            //Copy the hash ID for the resource into buffer.
            offset += this.streamed.Length;
            Buffer.BlockCopy(this.metaDataType, 0, bytes, offset, this.metaDataType.Length);

            //Copy the length of the metadata text.
            offset += this.metaDataType.Length;
            Buffer.BlockCopy(this.metaDataLength, 0, bytes, offset, this.metaDataLength.Length);

            offset += this.metaDataLength.Length;

            if (this.metaData != null)
            {
                //Copy the metadata
                Buffer.BlockCopy(this.metaData, 0, bytes, offset, this.metaData.Length);

                //Copy the data block.
                offset += this.metaData.Length;
            }

            if (this.resourceData != null && this.resourceData.Length > 0)
            {
                Buffer.BlockCopy(this.resourceData, 0, bytes, offset, this.resourceData.Length);
            }

            return bytes;
        }
    }
}