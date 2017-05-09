using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnitySerializeSkinnedMesh : UnitySerializerBase
    {
        private SkinnedMeshRenderer skinnedMesh;

        public UnitySerializeSkinnedMesh(GameObject parentGO, SkinnedMeshRenderer skinnedMesh, NodeBase rootNode)
        {
            this.skinnedMesh = skinnedMesh;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.SKINNEDMESH, referenceID, null);

            //Component nodes must always be parented to objNodes.
            objNode.AddChildNode(componentNode);

            byte[] bytes = MeshSerializeUtilities.WriteMesh(this.skinnedMesh.sharedMesh, true, true);

            //Serialize Material
            UnitySerializeMaterial materialSerializer = new UnitySerializeMaterial(this.skinnedMesh.sharedMaterial, this.rootNode);
			materialSerializer.Serialize(serializedAssets, serializeOptions, componentNode, postSerializeActions);

            //Make sure mesh knows that material it needs.
            JObject metaData = new JObject();
            metaData["materialID"] = materialSerializer.GetPointerID();
            metaData["rootBone"] = skinnedMesh.rootBone.name;

            if (skinnedMesh.probeAnchor != null)
            {
                metaData["probeAnchor"] = skinnedMesh.probeAnchor.name;
            }

            metaData["quality"] = (int)skinnedMesh.quality;

            JArray bones = new JArray();
            for (int i = 0; i < skinnedMesh.bones.Length; i++)
            {
                bones.Add(skinnedMesh.bones[i].name);
            }
            metaData["bones"] = bones;

            JArray blendShapeWeights = new JArray();
            for (int i = 0; i < skinnedMesh.sharedMesh.blendShapeCount; i++)
            {
                blendShapeWeights.Add(skinnedMesh.GetBlendShapeWeight(i));
            }
            metaData["blendShapeWeights"] = blendShapeWeights;

            //Create serialized asset by converting data to a bytearray and give it to the constructor.
            AssetResource resource = new AssetResource(false);

            byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));
            resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, bytes);

            serializedAssets.AddResource(referenceID, PCFResourceType.SKINNEDMESH, resource);

            //Nodes store their resource when serializing
            componentNode.SetSerializer(this);
        }
    }
}
