using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

namespace PCFFileFormat
{
    //A node regardless of type is always 68 bytes in length.
    public class NodeResource : ResourceBase
    {
        static NodeFactory nodeFactory;

        public static void SetNodeFactory(NodeFactory nodeFactoryImpl)
        {
            nodeFactory = nodeFactoryImpl;
        }

        //Reuse this buffer, optimization.
        static byte[] CHUNK_BYTE_BUFFER = new byte[ChunkByteLength];
        static byte[] NODE_NAME_BUFFER = new byte[128];
        static string DEFAULT_NODE_NAME = "none";

        private int numberOfChildren;
        private PCFResourceType resourceType;
        private UInt32 referenceID;
        private string nodeName;
        private List<NodeResource> childNodes;

        private byte[] serializedChildCount;            // 4 bytes
        private byte[] serializedResourceType;          // 4 Bytes
        private byte[] serializedReferenceID;           // 36 Bytes
        private byte[] serializedNodeNameLength;        // 4 bytes
        private byte[] serializedNodeName;              // 20 bytes

        public NodeResource()
        {

        }

        public NodeResource(PCFResourceType resourceType, UInt32 referenceID, string name)
        {
            this.serializedResourceType = BitConverter.GetBytes((int)resourceType);
            this.serializedReferenceID = BitConverter.GetBytes(referenceID);
            this.serializedNodeName = System.Text.Encoding.UTF8.GetBytes(name);
            this.serializedNodeNameLength = BitConverter.GetBytes(this.serializedNodeName.Length);

            this.childNodes = new List<NodeResource>();
            this.serializedChildCount = BitConverter.GetBytes(childNodes.Count);
        }

        public NodeResource(PCFResourceType resourceType, UInt32 referenceID, string name, int childCount, bool assemblyMode)
        {
            if (assemblyMode)
            {
                this.serializedResourceType = BitConverter.GetBytes((int)resourceType);
                this.serializedReferenceID = BitConverter.GetBytes(referenceID);
                this.serializedNodeName = System.Text.Encoding.UTF8.GetBytes(name);
                this.serializedNodeNameLength = BitConverter.GetBytes(this.serializedNodeName.Length);

                this.childNodes = new List<NodeResource>();
                this.serializedChildCount = BitConverter.GetBytes(childCount);
            }

            this.resourceType = resourceType;
            this.referenceID = referenceID;
            this.nodeName = name;
            this.numberOfChildren = childCount;
        }

        public void AddChildNode(NodeResource node)
        {
            //we cant add to it, if it doesn't exist now...can we.
            if (this.childNodes == null)
                this.childNodes = new List<NodeResource>();

            this.childNodes.Add(node);
        }

        public int GetNumberOfChildren()
        {
            return this.numberOfChildren;
        }

        public List<NodeResource> GetChildren()
        {
            return childNodes;
        }

        public static NodeResource DeserializeNode(Stream file, ref int bytesRead, bool readNames, bool assemblyMode)
        {
            file.Read(CHUNK_BYTE_BUFFER, 0, ChunkByteLength);
            int childCount = BitConverter.ToInt32(CHUNK_BYTE_BUFFER, 0);

            bytesRead += ChunkByteLength;

            file.Read(CHUNK_BYTE_BUFFER, 0, ChunkByteLength);
            int rawResourceTypeValue = BitConverter.ToInt32(CHUNK_BYTE_BUFFER, 0);
            PCFResourceType resourceType = (PCFResourceType)Enum.ToObject(typeof(PCFResourceType), rawResourceTypeValue);

            bytesRead += ChunkByteLength;

            file.Read(CHUNK_BYTE_BUFFER, 0, ChunkByteLength);
            UInt32 referenceID = BitConverter.ToUInt32(CHUNK_BYTE_BUFFER, 0);

            bytesRead += ChunkByteLength;

            file.Read(CHUNK_BYTE_BUFFER, 0, ChunkByteLength);
            int nameLength = BitConverter.ToInt32(CHUNK_BYTE_BUFFER, 0);

            bytesRead += ChunkByteLength;

            string nodeName = null;

            if (readNames)
            {
                file.Read(NODE_NAME_BUFFER, 0, nameLength);
                nodeName = System.Text.Encoding.UTF8.GetString(NODE_NAME_BUFFER, 0, nameLength);
            }
            else
            {
                //Object nodes must have names read in because other nodes may reference them by name.
                if (resourceType == PCFResourceType.OBJECT || resourceType == PCFResourceType.TRANSFORM)
                {
                    file.Read(NODE_NAME_BUFFER, 0, nameLength);
                    nodeName = System.Text.Encoding.UTF8.GetString(NODE_NAME_BUFFER, 0, nameLength);
                }
                else
                {
                    nodeName = DEFAULT_NODE_NAME;
                    file.Seek(nameLength, SeekOrigin.Current);
                }
            }

            bytesRead += nameLength;

            return new NodeResource(resourceType, referenceID, nodeName, childCount, assemblyMode);
        }

        public T Recreate<T>(UnityNodeBase parentNode, PCFResourceType loadFlags) where T : UnityNodeBase
        {
            UnityNodeBase recreatedNode = null;

            //Allows us to mask nodes to load, makes it easier to do quick lookup of specific things. (like reading script data, no need to load textures etc)
            if ((this.resourceType & loadFlags) != 0)
            {
                //Create implementation specific node based on typetree info.
                recreatedNode = nodeFactory.CreateNodeImplementation(this.nodeName, this.resourceType, this.referenceID);
            }

            //If we are not the root node we will have a parent.
            if (parentNode != null && recreatedNode != null)
            {
                parentNode.AddChildNode(recreatedNode);
            }

            if (this.childNodes != null)
            {
                for (int i = 0; i < this.childNodes.Count; i++)
                {
                    this.childNodes[i].Recreate<T>(recreatedNode, loadFlags);
                }
            }

            //Only true for root node.
            if (parentNode == null)
            {
                return recreatedNode as T;
            }

            return null;
        }

        public override int GetLength()
        {
            int length = serializedChildCount.Length +
                         serializedResourceType.Length +
                         serializedReferenceID.Length +
                         serializedNodeNameLength.Length +
                         serializedNodeName.Length;

            return length;
        }

        //Summary: Get a bytestream representation of this element.
        public override byte[] GetBytes()
        {
            //Only calculate this when we know the exact number of children.
            this.serializedChildCount = BitConverter.GetBytes(childNodes.Count);

            int dataLength = GetLength();
            byte[] bytes = new byte[dataLength];

            int offset = 0;
            Buffer.BlockCopy(this.serializedChildCount, 0, bytes, offset, this.serializedChildCount.Length);

            offset += this.serializedChildCount.Length;
            Buffer.BlockCopy(this.serializedResourceType, 0, bytes, offset, this.serializedResourceType.Length);

            offset += this.serializedResourceType.Length;
            Buffer.BlockCopy(this.serializedReferenceID, 0, bytes, offset, this.serializedReferenceID.Length);

            offset += this.serializedReferenceID.Length;
            Buffer.BlockCopy(this.serializedNodeNameLength, 0, bytes, offset, this.serializedNodeNameLength.Length);

            offset += this.serializedNodeNameLength.Length;
            Buffer.BlockCopy(this.serializedNodeName, 0, bytes, offset, this.serializedNodeName.Length);

            return bytes;
        }
    }
}