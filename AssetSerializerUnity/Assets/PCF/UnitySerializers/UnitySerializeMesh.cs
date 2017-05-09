using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnitySerializeMesh : UnitySerializerBase
    {
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        public UnitySerializeMesh(GameObject parentGO, MeshRenderer meshRenderer, NodeBase rootNode)
        {
            this.meshFilter = parentGO.GetComponent<MeshFilter>();
            this.meshRenderer = meshRenderer;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.MESH, referenceID, null);

            //Component nodes must always be parented to objNodes.
            objNode.AddChildNode(componentNode);

            //Serialize mesh into a byte array.
            byte[] bytes = MeshSerializeUtilities.WriteMesh(this.meshFilter.sharedMesh, true, false);

            //Serialize Material
            UnitySerializeMaterial materialSerializer = new UnitySerializeMaterial(this.meshRenderer.sharedMaterial, this.rootNode);
			materialSerializer.Serialize(serializedAssets, serializeOptions, componentNode, postSerializeActions);

            //Make sure mesh knows that material it needs.
            JObject metaData = new JObject();
            metaData["materialID"] = materialSerializer.GetPointerID();

            //Create serialized asset by converting data to a bytearray and give it to the constructor.
            AssetResource resource = new AssetResource(false);

            byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));
            resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, bytes);

            serializedAssets.AddResource(referenceID, PCFResourceType.MESH, resource);

            //Nodes store their resource when serializing
            componentNode.SetSerializer(this);
        }
    }
}
