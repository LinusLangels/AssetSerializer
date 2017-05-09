using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnitySerializeTransform : UnitySerializerBase
    {
        private Transform transform;

        public UnitySerializeTransform(GameObject parentGO, Transform transform, NodeBase rootNode)
        {
            this.transform = transform;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.TRANSFORM, referenceID, null, this.transform.name);

            //Component nodes must always be parented to objNodes.
            objNode.AddChildNode(componentNode);
            
            Vector3[] vectors = new Vector3[3];
            vectors[0] = this.transform.localPosition;
            vectors[1] = this.transform.localRotation.eulerAngles;
            vectors[2] = this.transform.localScale;
                        
            byte[] bytes = WriteVector3ArrayToBytes(vectors);

            //Create serialized asset by converting data to a bytearray and give it to the constructor.
            AssetResource resource = new AssetResource(false);
            resource.Serialize(referenceID, MetaDataType.UNKOWN, null, bytes);

            serializedAssets.AddResource(referenceID, PCFResourceType.TRANSFORM, resource);

            //Nodes store their resource when serializing
            componentNode.SetSerializer(this);
        }

        public static byte[] WriteVector3ArrayToBytes(Vector3[] vectors)
        {
            byte[] bytes = new byte[12 * vectors.Length];

            for (int i = 0; i < vectors.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(vectors[i].x), 0, bytes, (12 * i), 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vectors[i].y), 0, bytes, (12 * i) + 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vectors[i].z), 0, bytes, (12 * i) + 8, 4);
            }

            return bytes;
        }
    }
}
