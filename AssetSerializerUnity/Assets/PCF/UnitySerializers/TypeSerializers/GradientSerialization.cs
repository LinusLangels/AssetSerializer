using System;
using UnityEngine;
using System.Collections.Generic;
using PCFFileFormat.Serialization;

namespace PCFFileFormat
{
    class GradientSerialization : UnitySerializerBase
    {
        private UnityEngine.Gradient gradient;

        public GradientSerialization(System.Object value, string fieldName, bool arrayItem, NodeBase rootNode)
        {
            this.gradient = value as UnityEngine.Gradient;
            this.fieldName = fieldName;
            this.arrayItem = arrayItem;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postserializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode scriptNode = new ComponentNode(PCFResourceType.GRADIENT, referenceID, null, typeof(UnityEngine.Gradient).Name.ToString());

            objNode.AddChildNode(scriptNode);

            GradientColorKey[] colorKeys = this.gradient.colorKeys;
            System.Object[] convertedColorKeys = new System.Object[colorKeys.Length];

            for (int i = 0; i < colorKeys.Length; i++)
            {
                GradientColorKey colorKey = colorKeys[i];
                convertedColorKeys[i] = colorKey as System.Object;
            }
            
            UnitySerializeCollection colorKeySerializer = new UnitySerializeCollection(convertedColorKeys, typeof(UnityEngine.GradientColorKey), null, "colorKeys", rootNode);
			colorKeySerializer.Serialize(serializedAssets, serializeOptions, scriptNode, postserializeActions);


            GradientAlphaKey[] alphaKeys = this.gradient.alphaKeys;
            System.Object[] convertedAlphaKeys = new System.Object[alphaKeys.Length];

            for (int i = 0; i < alphaKeys.Length; i++)
            {
                GradientAlphaKey alphaKey = alphaKeys[i];
                convertedAlphaKeys[i] = alphaKey as System.Object;
            }

            UnitySerializeCollection alphaKeySerializer = new UnitySerializeCollection(convertedAlphaKeys, typeof(UnityEngine.GradientAlphaKey), null, "alphaKeys", rootNode);
			alphaKeySerializer.Serialize(serializedAssets, serializeOptions, scriptNode, postserializeActions);

            AssetResource resource = new AssetResource(false);

            byte[] metaDataBuffer = ProtocolBufferSerializer.SerializeFieldData(this.gradient.GetType(), this.fieldName, this.arrayItem, this.gradient.GetType().Assembly);
            resource.Serialize(this.referenceID, MetaDataType.PROTOBUF, metaDataBuffer, null);

            serializedAssets.AddResource(referenceID, PCFResourceType.GRADIENT, resource);
        }
    }
}

