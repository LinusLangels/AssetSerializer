using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    class TransformSerialization : UnitySerializerBase
    {
        private UnityEngine.Transform transform;

        public TransformSerialization(System.Object value, string fieldName, bool arrayItem, NodeBase rootNode)
        {
            this.transform = value as UnityEngine.Transform;
            this.fieldName = fieldName;
            this.arrayItem = arrayItem;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postserializeActions)
        {
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.TRANSFORMPOINTER, referenceID, null, this.transform.GetType().Name.ToString());

            objNode.AddChildNode(componentNode);

            //Find the node this node points to in the tree.
            NodeBase transformNode = FindTransformInTree(rootNode, this.transform.name);
            UInt32 referencedID = transformNode.GetReferenceID();

            AssetResource resource = new AssetResource(false);

            byte[] metaDataBuffer = ProtocolBufferSerializer.SerializeFieldData(this.transform.GetType(), this.fieldName, this.arrayItem, this.transform.GetType().Assembly);
            resource.Serialize(this.referenceID, MetaDataType.PROTOBUF, metaDataBuffer, BitConverter.GetBytes(referencedID));

            serializedAssets.AddResource(referenceID, PCFResourceType.TRANSFORMPOINTER, resource);

            this.pointedID = transformNode.GetReferenceID();
        }

        NodeBase FindTransformInTree(NodeBase node, string name)
        {
            if (node.GetResourceType() == PCFResourceType.TRANSFORM)
            {
                string nodeName = node.GetName();

                if (!string.IsNullOrEmpty(nodeName))
                {
                    if (string.CompareOrdinal(nodeName, name) == 0)
                    {
                        return node;
                    }
                }
            }

            List<NodeBase> children = node.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                NodeBase foundChild = FindTransformInTree(children[i], name);

                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }
    }
}