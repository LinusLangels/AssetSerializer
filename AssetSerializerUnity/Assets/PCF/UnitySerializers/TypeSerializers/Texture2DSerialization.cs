using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat
{
    public class Texture2DSerialization : UnitySerializerBase
    {
        private UnityEngine.Texture2D texture;

        public Texture2DSerialization(System.Object value, string fieldName, bool arrayItem, NodeBase rootNode)
        {
            this.texture = value as UnityEngine.Texture2D;
            this.fieldName = fieldName;
            this.arrayItem = arrayItem;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postserializeActions)
        {
			#if UNITY_EDITOR
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode scriptNode = new ComponentNode(PCFResourceType.TEXTURE, referenceID, null, typeof(UnityEngine.Texture2D).Name.ToString());

            objNode.AddChildNode(scriptNode);

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
				metaData["fieldName"] = this.fieldName;
				metaData["arrayItem"] = this.arrayItem;
				metaData["assembly"] = this.texture.GetType().Assembly.GetName().Name;

                byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));

				resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, textureData);

                serializedAssets.AddResource(referenceID, PCFResourceType.TEXTURE, resource);
            }
			#endif
        }
    }
}
