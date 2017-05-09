//#define DESERIALIZE_ALL_NODE_NAMES

using UnityEngine;
using System.Collections;
using System;
using System.IO;

namespace PCFFileFormat
{
    public class NodeBlock : DataBlockBase
    {
        private byte[] chunkLength; //Always 4 bytes long. UINT32. Describes how many bytes the data element contains.
        private byte[] blockType;  //4 bytes.
        private NodeResource rootNodeResource; //We are using a list cause we dont know final array size until everything is done.
        private PCFResourceType loadFlags;

        public NodeBlock(NodeResource resource)
        {
            this.blockType = BitConverter.GetBytes((int)PCFBlockTypes.NODEBLOCK);
            this.rootNodeResource = resource;
        }

        public NodeBlock(PCFResourceType loadFlags)
        {
            this.blockType = BitConverter.GetBytes((int)PCFBlockTypes.NODEBLOCK);
            this.rootNodeResource = null;
            this.loadFlags = loadFlags;
        }

        public NodeBlock()
        {
            this.blockType = BitConverter.GetBytes((int)PCFBlockTypes.NODEBLOCK);
            this.rootNodeResource = null;
        }

        public virtual T RecreateNodeGraph<T>(NodeFactory nodeFactory) where T : UnityNodeBase
        {
            if (this.rootNodeResource != null)
            {
                NodeResource.SetNodeFactory(nodeFactory);

                T rootNode = this.rootNodeResource.Recreate<T>(null, this.loadFlags);

                return rootNode;
            }

            return default(T);
        }

        public override int GetLength()
        {
            int length = 0;
            GetLengthRecursivly(this.rootNodeResource, ref length);

            return length + this.blockType.Length;
        }

        void GetLengthRecursivly(NodeResource node, ref int length)
        {
            //Should be 68 or we have seriously fucked up.
            length += node.GetLength();

            foreach (NodeResource child in node.GetChildren())
            {
                GetLengthRecursivly(child, ref length);
            }
        }

        public NodeResource GetRootNodeResource()
        {
            return rootNodeResource;
        }

        public override byte[] GetBytes()
        {
            int arrayLength = GetLength();
            this.chunkLength = BitConverter.GetBytes(arrayLength);
            byte[] bytes = new byte[arrayLength + ChunkByteLength];

            //Copy the first 4 bytes to the start of the stream. this is the length of the data chunk.
            int offset = 0;
            Buffer.BlockCopy(this.chunkLength, 0, bytes, offset, this.chunkLength.Length);

            offset += this.chunkLength.Length;
            Buffer.BlockCopy(this.blockType, 0, bytes, offset, this.blockType.Length);

            offset += this.blockType.Length;

            //Recursivly serialize nodedata tree.
            SerializeNodeTree(bytes, ref offset, this.rootNodeResource);

            return bytes;
        }

        void SerializeNodeTree(byte[] byteArray, ref int offset, NodeResource node)
        {
            byte[] nodeBytes = node.GetBytes();

            Buffer.BlockCopy(nodeBytes, 0, byteArray, offset, nodeBytes.Length);

            offset += nodeBytes.Length;

            foreach (NodeResource child in node.GetChildren())
            {
                SerializeNodeTree(byteArray, ref offset, child);
            }
        }

        public override void SetBytes(Stream file, int chunkLength, long offsetPos, bool assemblyMode)
        {
            int bytesToRead = chunkLength - BlockTypeLength;
            int bytesRead = 0;

            #if DESERIALIZE_ALL_NODE_NAMES
            bool readNames = true;
            #else
            bool readNames = false;
            #endif

            this.rootNodeResource = NodeResource.DeserializeNode(file, ref bytesRead, readNames, assemblyMode);
            DeserializeNodeTree(file, this.rootNodeResource, ref bytesRead, readNames, assemblyMode);

            if (bytesToRead != bytesRead)
            {
                Debug.LogError("Malformed node tree");
            }
        }

        void DeserializeNodeTree(Stream file, NodeResource deserializedParentNode, ref int bytesRead, bool debug, bool assemblyMode)
        {
            int childCount = deserializedParentNode.GetNumberOfChildren();

            for (int i = 0; i < childCount; i++)
            {
                NodeResource node = NodeResource.DeserializeNode(file, ref bytesRead, debug, assemblyMode);

                deserializedParentNode.AddChildNode(node);

                DeserializeNodeTree(file, node, ref bytesRead, debug, assemblyMode);
            }
        }
    }
}