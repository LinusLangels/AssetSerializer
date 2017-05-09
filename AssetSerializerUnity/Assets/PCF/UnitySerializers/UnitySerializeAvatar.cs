using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnitySerializeAvatar : UnitySerializerBase
    {
		private Animator animator;
		private GameObject parentGO;

		public UnitySerializeAvatar(Animator animator, GameObject parentGO, NodeBase rootNode)
        {
			this.animator = animator;
			this.parentGO = parentGO;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

			AvatarSerializeOpts serializeOption = null;
			for (int i = 0; i < serializeOptions.Length; i++)
			{
				object opt = serializeOptions[i];

				if (opt is AvatarSerializeOpts)
				{
					serializeOption = opt as AvatarSerializeOpts;
					break;
				}
			}

			if (serializeOption == null)
				return;

            ComponentNode componentNode = new ComponentNode(PCFResourceType.AVATAR, referenceID, null);

            //Component nodes must always be parented to objNodes.
            objNode.AddChildNode(componentNode);

			JObject metaData = serializeOption.SerializeAvatar(this.parentGO, this.animator);

            //Create serialized asset by converting data to a bytearray and give it to the constructor.
            AssetResource resource = new AssetResource(false);

            byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));
            resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, null);

            serializedAssets.AddResource(referenceID, PCFResourceType.AVATAR, resource);

            //Nodes store their resource when serializing
            componentNode.SetSerializer(this);
        }
    }
}