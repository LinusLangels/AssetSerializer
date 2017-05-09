using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnitySerializeCamera : UnitySerializerBase
    {
        private Camera camera;

        public UnitySerializeCamera(GameObject parentGO, Camera camera, NodeBase rootNode)
        {
            this.camera = camera;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.CAMERA, referenceID, null);

            //Material nodes can and are most likely to be children of other component nodes.
            objNode.AddChildNode(componentNode);

            byte[] bytes = new byte[24];

            float[] data = new float[6];

            data[0] = this.camera.backgroundColor.r;
            data[1] = this.camera.backgroundColor.g;
            data[2] = this.camera.backgroundColor.b;
            data[3] = this.camera.backgroundColor.a;
            data[4] = this.camera.fieldOfView;
            data[5] = this.camera.aspect;

            for (int i = 0; i < data.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(data[i]), 0, bytes, 4 * i, 4);
            }

            //Create serialized asset by converting data to a bytearray and give it to the constructor.
            AssetResource resource = new AssetResource(false);
            resource.Serialize(referenceID, MetaDataType.UNKOWN, null, bytes);

            serializedAssets.AddResource(referenceID, PCFResourceType.CAMERA, resource);

            //Nodes store their resource when serializing
            componentNode.SetSerializer(this);
        }
    }
}
