using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnitySerializeLight : UnitySerializerBase
    {
        private Light light;

        public UnitySerializeLight(GameObject parentGO, Light light, NodeBase rootNode)
        {
            this.light = light;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.LIGHT, referenceID, null);

            //Material nodes can and are most likely to be children of other component nodes.
            objNode.AddChildNode(componentNode);
            
            byte[] bytes = new byte[24];

            float[] data = new float[6];

            data[0] = this.light.color.r;
            data[1] = this.light.color.g;
            data[2] = this.light.color.b;
            data[3] = this.light.color.a;
            data[4] = (float)this.light.type;
            data[5] = this.light.intensity;

            for (int i = 0; i < data.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(data[i]), 0, bytes, 4 * i, 4);
            }

            //Create serialized asset by converting data to a bytearray and give it to the constructor.
            AssetResource resource = new AssetResource(false);
            resource.Serialize(referenceID, MetaDataType.UNKOWN, null, bytes);

            serializedAssets.AddResource(referenceID, PCFResourceType.LIGHT, resource);

            //Nodes store their resource when serializing
            componentNode.SetSerializer(this);
        }
    }
}
