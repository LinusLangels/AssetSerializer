using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat
{
    public class AudioClipSerialization : UnitySerializerBase
    {
        private UnityEngine.AudioClip audioClip;

        public AudioClipSerialization(System.Object value, string fieldName, bool arrayItem, NodeBase rootNode)
        {
            this.audioClip = value as UnityEngine.AudioClip;
            this.fieldName = fieldName;
            this.arrayItem = arrayItem;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postserializeActions)
        {
			if (this.audioClip == null) 
			{
				Debug.Log ("Missing audio clip! If this is intentional ignore this message.");
				return;			
			}

			#if UNITY_EDITOR
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

			AudioSerializeOpts serializeOption = null;
			for (int i = 0; i < serializeOptions.Length; i++)
			{
				object opt = serializeOptions[i];

				if (opt is AudioSerializeOpts)
				{
					serializeOption = opt as AudioSerializeOpts;
					break;
				}
			}

			if (serializeOption == null)
				return;

            ComponentNode scriptNode = new ComponentNode(PCFResourceType.AUDIO, referenceID, null, typeof(UnityEngine.AudioClip).Name.ToString());

            objNode.AddChildNode(scriptNode);

			bool streamed = true;
			byte[] audioData = serializeOption.PackageAudio(audioClip, serializedAssets, this.referenceID, ref streamed);

            JObject metaData = new JObject();
            metaData["fieldName"] = this.fieldName;
            metaData["arrayItem"] = this.arrayItem;
            metaData["name"] = this.audioClip.name;
            metaData["sampleRate"] = this.audioClip.frequency;
            metaData["channels"] = this.audioClip.channels;
            metaData["samples"] = this.audioClip.samples * this.audioClip.channels;
			metaData["streamed"] = streamed;

            byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));

            AssetResource resource = new AssetResource(true);
			resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, audioData);

            serializedAssets.AddResource(referenceID, PCFResourceType.AUDIO, resource);
			#endif
        }
    }
}
