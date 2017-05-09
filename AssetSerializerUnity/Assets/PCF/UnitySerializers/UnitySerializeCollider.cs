using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnitySerializeCollider : UnitySerializerBase
    {
        public UnitySerializeCollider(GameObject parentGO, Collider collider, NodeBase rootNode)
        {
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.COLLIDER, referenceID, null);

            //Material nodes can and are most likely to be children of other component nodes.
            objNode.AddChildNode(componentNode);

            //Create serialized asset by converting data to a bytearray and give it to the constructor.
            AssetResource resource = new AssetResource(false);
            resource.Serialize(referenceID, MetaDataType.UNKOWN, null, null);

            serializedAssets.AddResource(referenceID, PCFResourceType.COLLIDER, resource);

            //Nodes store their resource when serializing
            componentNode.SetSerializer(this);
        }
    }
}
