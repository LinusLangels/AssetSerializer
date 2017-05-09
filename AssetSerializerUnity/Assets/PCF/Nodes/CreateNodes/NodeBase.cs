using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PCFFileFormat.Debugging;

namespace PCFFileFormat
{
    public class NodeBase : INodeInterface<NodeBase>
    {
        protected UInt32 referenceID;
        protected string name;
        protected PCFResourceType resourceType;
        protected UnitySerializerBase serializer;
        protected IDGenerator generator;

        //TODO: This realllly.. should not be here.
        protected string resourceIdentifier;

        private List<NodeBase> children;
        private NodeBase parentNode;
        private NodeBase transformNode;

        public NodeBase()
        {
            this.children = new List<NodeBase>();
        }

        public List<NodeBase> ChildNodes { get { return this.children;  } }
        public NodeBase ParentNode { get { return this.parentNode;  } }
        public NodeBase TransformNode { get { return this.transformNode; } }

        public UInt32 GenerateID()
        {
            if (this.generator != null)
            {
                return this.generator.GenerateID();
            }
            else
            {
                Debug.LogError("Unable to generate controlled ID!");
            }

            return 0;
        }

        public void AddChildNode(NodeBase node)
        {
            if (this.children == null)
                this.children = new List<NodeBase>();

            //When parenting nodes give them a reference to their parent node.
            node.SetParent(this);

            this.children.Add(node);
        }

        public void AddTransformNode(NodeBase node)
        {
            this.transformNode = node;
        }

        public void SetParent(NodeBase node)
        {
            this.parentNode = node;
        }

        public void SetSerializer(UnitySerializerBase serializer)
        {
            this.serializer = serializer;
        }

        public UInt32 GetReferenceID()
        {
            return this.referenceID;
        }

        public string GetResourceIndentifier()
        {
            return this.resourceIdentifier;
        }

        public string GetName()
        {
            return this.name;
        }

        public PCFResourceType GetResourceType()
        {
            return this.resourceType;
        }

        public UnitySerializerBase GetSerializer()
        {
            return this.serializer;
        }
			
        public JObject GetJSONRepresentation()
        {
            JObject representation = new JObject();

            representation["type"] = GetType().Name;
            representation["id"] = this.referenceID;
            representation["internalID"] = this.referenceID;
            representation["text"] = this.resourceType.ToString();
            representation["parent"] = this.parentNode != null ? this.parentNode.referenceID : 0;

            return representation;
        }

        public virtual NodeResource Serialize(SerializedAssets serializedAssets, NodeResource serializedParent, ISerializeLogging logger)
        {
            return null;
        }
    }
}
