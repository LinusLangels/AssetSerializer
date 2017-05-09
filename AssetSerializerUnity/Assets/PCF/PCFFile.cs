using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

namespace PCFFileFormat
{
    public enum PCFfileType             // FileType defined in the header
    {
        Unknown,
        GameAsset,
        SoundAsset,
		TextureAsset,
        AnimationAsset,
        MetaData,
    }

    [Flags]
    public enum PCFResourceType : uint     // DataBlocks
    {
        NONE = 0,
        OBJECT = 1,
        ROOT = 2 << 0,
        NODE = 2 << 1,
        INDEX = 2 << 2,
        SCRIPT = 2 << 3,
        MESH = 2 << 4,
        SKINNEDMESH = 2 << 5,
        TRANSFORM = 2 << 6,
        ANIMATOR = 2 << 7,
        AVATAR = 2 << 8,
        ANIMATION = 2 << 9,
        MATERIAL = 2 << 10,
        MATERIALPOINTER = 2 << 11,
        TRANSFORMPOINTER = 2 << 12,
        TEXTURE = 2 << 13,
        LIGHT = 2 << 14,
        LIGHTPROBES = 2 << 15,
        INTERNALBUNDLE = 2 << 16,
        CAMERA = 2 << 17,
        COLLIDER = 2 << 18,
        AUDIO = 2 << 19,
        PRIMITIVE = 2 << 20,
        COLLECTION = 2 << 21,
        POINTERCOLLECTION = 2 << 22,
        CLASS = 2 << 23,
        GRADIENT = 2 << 24,
        ANIMATIONCLIP = 2 << 25,
        ANIMATIONCLIPREFERENCE = 2 << 26,
        READMETADATA = ROOT | OBJECT | SCRIPT | CLASS | PRIMITIVE | COLLECTION | POINTERCOLLECTION,
        READTEXTURES = ROOT | OBJECT | TRANSFORM | MESH | SKINNEDMESH | MATERIAL | TEXTURE,
        ALL = OBJECT | ROOT | NODE | INDEX | SCRIPT | MESH | SKINNEDMESH | 
            TRANSFORM | ANIMATOR | AVATAR | ANIMATION | MATERIAL | MATERIALPOINTER |
            TRANSFORMPOINTER | TEXTURE | LIGHT | LIGHTPROBES | INTERNALBUNDLE | CAMERA | COLLIDER |
            AUDIO | PRIMITIVE | COLLECTION | POINTERCOLLECTION | CLASS | GRADIENT | ANIMATIONCLIP | ANIMATIONCLIPREFERENCE,
    }

    public enum PCFBlockTypes
    {
        RESOURCEBLOCK,
        INDEXBLOCK,
        NODEBLOCK,
    }

    public class PCFFile
    {
        public struct FileHeader
        {
            private byte[] version;     //Always 4 bytes long. UINT32.
            private byte[] length;      //UINT32.
            private byte[] fileType;    //UINT32.

            public FileHeader(int version, int length, PCFfileType type)
            {
                this.version = BitConverter.GetBytes(version);
                this.length = BitConverter.GetBytes(length);
                this.fileType = BitConverter.GetBytes((int)type);
            }

            public int GetVersion()
            {
                return BitConverter.ToInt32(this.version, 0);
            }

            public int GetFileLength()
            {
                return BitConverter.ToInt32(this.length, 0);
            }

            public int GetFileType()
            {
                return BitConverter.ToInt32(this.fileType, 0);
            }

            public void SetFileLength(int length)
            {
                this.length = BitConverter.GetBytes(length);
            }

            public int GetLength()
            {
                return this.version.Length + this.length.Length + this.fileType.Length;
            }

            //Summary: Get a bytestream representation of this element.
            public byte[] GetBytes()
            {
                int arrayLength = GetLength();
                byte[] bytes = new byte[arrayLength];

                int offset = 0;

                //Copy the first 4 bytes to the start of the stream
                Buffer.BlockCopy(this.version, 0, bytes, offset, this.version.Length);

                //Copy the four length bytes starting 4 indexes in. the first four being occupied by the Version.
                offset += this.version.Length;
                Buffer.BlockCopy(this.length, 0, bytes, offset, this.length.Length);

                //Copy the four type bytes starting 8 indexes in.
                offset += this.length.Length;
                Buffer.BlockCopy(this.fileType, 0, bytes, offset, this.fileType.Length);

                return bytes;
            }

            //Summary: Break apart the bytestream into its individual components.
            public void Deserialize(byte[] bytes)
            {
                //Copy file length into the UINT32.
                this.version = new byte[4];
                Buffer.BlockCopy(bytes, 0, this.version, 0, 4);

                //Copy file length into the UINT32.
                this.length = new byte[4];
                Buffer.BlockCopy(bytes, this.version.Length, this.length, 0, 4);

                //Copy file length into the UINT32.
                this.fileType = new byte[4];
                Buffer.BlockCopy(bytes, this.version.Length + this.length.Length, this.fileType, 0, 4);
            }
        }

        public static readonly string FILE_EXTENSION = ".pcf";

        static byte[] nullElement;
        static byte[] INT_BUFFER;

        static readonly int FileVersion = 1;        //Version 1.0
        static readonly int HeaderByteLength = 12;
        static readonly int ChunkByteLength = 4;
        static readonly int BlockTypeLength = 4;
        

        //Internal variables
        private IFileHandle path;
        private FileHeader fileHeader;
        private Dictionary<PCFResourceType, DataBlockBase> blockData;
        private IndexBlock indexBlock;
        private PCFfileType fileType;
        private bool memStream;

        //This static constructor sets the chunk/data order of this file format
        static PCFFile()
        {
            nullElement = BitConverter.GetBytes(ChunkByteLength);
            INT_BUFFER = new byte[ChunkByteLength];
        }

		public PCFFile(IFileHandle path, bool memStream, PCFfileType fileType = PCFfileType.Unknown)
        {
            this.memStream = memStream;
            this.path = path;
            this.fileType = fileType;
            this.blockData = new Dictionary<PCFResourceType, DataBlockBase>();

            //This must exist in order for pcf file to work properly.
            this.indexBlock = new IndexBlock();
        }

        public void SetSavePath(IFileHandle path)
        {
            this.path = path;
        }

		public void LoadFile(PCFResourceType loadFlags, bool streamResources, bool assemblyMode = false)
        {
			LoadDataFromDisk(loadFlags, streamResources, assemblyMode);
        }

        public FileHeader GetHeader()
        {
            if (this.path != null && this.path.Exists)
            {
                Stream file = this.path.GetFileStream(FileMode.Open);

                this.fileHeader = LoadHeader(file);

                if (file != null)
                {
                    file.Close();
                }
            }

            return this.fileHeader;
        }

        public void SaveFile()
        {
            if (this.path != null)
            {
                //Iterate all blocks and save to disk.
                SaveToDisk();
            }
            else
            {
                Debug.LogError("No Filepath specified!");
            }
        }

        public void Unload()
        {
            this.blockData.Clear();
            this.blockData = null;
            this.indexBlock = null;
        }

        public void AddDataBlocks(Dictionary<PCFResourceType, DataBlockBase> dataBlocks)
        {
            this.blockData = dataBlocks;
        }

        public Dictionary<PCFResourceType, DataBlockBase> GetDataBlocks()
        {
            return this.blockData;
        }

        FileHeader GetHeaderPrototype()
        {
            FileHeader header = new FileHeader(FileVersion, 0, this.fileType);

            return header;
        }

        void SaveToDisk()
        {
            //Open file for reading.
            FileStream file = new FileStream(this.path.FullName, FileMode.Create, FileAccess.Write);
            FileHeader header = GetHeaderPrototype();

            if (this.indexBlock == null)
            {
                Debug.LogError("No Index block specified!");
                return;
            }

            int indexBlockSize = (this.blockData.Count * IndexBlock.BYTES_PER_ELEMENT) + ChunkByteLength + BlockTypeLength;

            //Calculate number of bytes to offset from start, make room for header and index block.
            int fileOffset = indexBlockSize + header.GetLength();

            file.Seek((long)fileOffset, SeekOrigin.Begin);

            foreach (KeyValuePair<PCFResourceType, DataBlockBase> pair in this.blockData)
            {
                //Add index for each block.
                IndexElement indexElement = new IndexElement(pair.Key, fileOffset);
                this.indexBlock.AddIndex(indexElement);

                //All byte data for a block.
                byte[] blockData = pair.Value.GetBytes();
                
                //Make sure we dont end up with empty blocks.
                if (blockData == null || blockData.Length == 0)
                {
                    blockData = nullElement;
                }

                //Cannot use GetLength() here because we need to factor in all bytes including chunklength bytes.
                int blockLength = blockData.Length;

                file.Write(blockData, 0, blockLength);

                //Move file position to next block.
                fileOffset += blockLength;
            }

            header.SetFileLength(fileOffset - header.GetLength());

            //Seek back to file header (aka index 0) and write header + index block to disk.
            file.Seek(0, SeekOrigin.Begin);

            byte[] headerBytes = header.GetBytes();
            byte[] indexBytes = this.indexBlock.GetBytes();

            file.Write(headerBytes, 0, headerBytes.Length);
            file.Write(indexBytes, 0, indexBytes.Length);

            if (file != null)
            {
                //Stop reading pcf file.
                file.Dispose();
                file.Close();
            }
        }

        FileHeader LoadHeader(Stream file)
        {
            byte[] headerBytes = new byte[HeaderByteLength];
            file.Read(headerBytes, 0, HeaderByteLength);

            FileHeader header = new FileHeader();
            header.Deserialize(headerBytes);

            return header;
        }

		void LoadDataFromDisk(PCFResourceType loadFlags, bool streamResources, bool assemblyMode)
        {
            Stream file = this.path.GetFileStream(FileMode.Open);

            //Load header first.
            this.fileHeader = LoadHeader(file);

            int bytesRead = HeaderByteLength;
            int fileLength = (int)file.Length;

            while (bytesRead < fileLength)
            {
                int chunkLength = GetNextSegmentLength(file);
                PCFBlockTypes blockType = GetNextBlockType(file);
                DataBlockBase block = null;
                PCFResourceType resourceType = PCFResourceType.NONE;

                if (blockType == PCFBlockTypes.INDEXBLOCK)
                {
                    block = new IndexBlock();

                    this.blockData.Add(PCFResourceType.INDEX, block);
                }
                else if (blockType == PCFBlockTypes.NODEBLOCK)
                {
                    block = new NodeBlock(loadFlags);

                    this.blockData.Add(PCFResourceType.NODE, block);
                }
                else if (blockType == PCFBlockTypes.RESOURCEBLOCK)
                {
                    //Read 4 bytes to determine resourcetype.
                    file.Read(INT_BUFFER, 0, ChunkByteLength);
                    int rawResourceTypeValue = BitConverter.ToInt32(INT_BUFFER, 0);
                    resourceType = (PCFResourceType)Enum.ToObject(typeof(PCFResourceType), rawResourceTypeValue);

                    //Allows us to mask what blocks to read in from file.
					if ((resourceType & loadFlags) != 0)
                    {
						block = new ResourceBlock(resourceType, streamResources);
                        this.blockData.Add(resourceType, block);
                    }
                }
                else
                {
                    Debug.LogError("Unknown block type");
                }

                //Increment file position, make sure to count the chunk size bytes.
                bytesRead += chunkLength + ChunkByteLength;

                if (block != null)
                {
                    if (memStream)
                    {
                        int bytsOffset = ChunkByteLength;
                        if (blockType == PCFBlockTypes.RESOURCEBLOCK)
                            bytsOffset += ChunkByteLength;

                        long fileStreamPos = file.Position;
                        byte[] streamBytes = new byte[chunkLength - bytsOffset];
                        file.Read(streamBytes, 0, chunkLength - bytsOffset);

                        Stream stream = new MemoryStream(streamBytes);

                        block.SetBytes(stream, chunkLength, fileStreamPos, assemblyMode);
                    }
                    else
                    {
                        //Will internally read from file and increment its position.
                        block.SetBytes(file, chunkLength, 0, assemblyMode);
                    }

                }
                else
                {
                    //Manually seek to next chunk incase SetBytes was never called.
                    file.Seek(bytesRead, SeekOrigin.Begin);
                }
            }

            if (file != null)
            {
                //Stop reading pcf file.
                file.Close();
            }
        }

        int GetNextSegmentLength(Stream fileStream)
        {
            int bytesRead = fileStream.Read(INT_BUFFER, 0, ChunkByteLength);

            if (bytesRead != 0)
            {
                return BitConverter.ToInt32(INT_BUFFER, 0);
            }
            else
            {
                return 0;
            } 
        }

        PCFBlockTypes GetNextBlockType(Stream file)
        {
            file.Read(INT_BUFFER, 0, ChunkByteLength);
            int rawEnumValue = BitConverter.ToInt32(INT_BUFFER, 0);

            return (PCFBlockTypes)Enum.ToObject(typeof(PCFBlockTypes), rawEnumValue);
        }
    }
}