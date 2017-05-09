using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.InteropServices;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat
{
    public class UnitySerializeTexture : UnitySerializerBase
    {
        private Texture2D texture;

        public UnitySerializeTexture(Texture2D texture, NodeBase rootNode)
        {
            this.texture = texture;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
			#if UNITY_EDITOR
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.TEXTURE, referenceID, null);

            //Component nodes must always be parented to objNodes.
            objNode.AddChildNode(componentNode);

            if (this.texture != null)
            {
				TextureSerializeOpts serializeOption = null;
				for (int i = 0; i < serializeOptions.Length; i++)
				{
					object opt = serializeOptions[i];

					if (opt is TextureSerializeOpts)
					{
						serializeOption = opt as TextureSerializeOpts;
						break;
					}
				}

				if (serializeOption == null)
					return;

                //Create serialized asset by converting data to a bytearray and give it to the constructor.
                AssetResource resource = new AssetResource(false);

				TextureDataFormat format = TextureDataFormat.Empty;
				byte[] textureData = serializeOption.PackageTexture(this.texture, serializedAssets, referenceID, ref format);

				JObject metaData = new JObject();
				metaData["width"] = this.texture.width;
				metaData["height"] = this.texture.height;
				metaData["textureFormat"] = (int)format;

                byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));
					
				resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, textureData);

                serializedAssets.AddResource(referenceID, PCFResourceType.TEXTURE, resource);

                //Nodes store their resource when serializing
                componentNode.SetSerializer(this);
            }
			#endif
        }
    }
}
