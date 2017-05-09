using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnitySerializeInternalBundle : UnitySerializerBase
    {
        string internalBundlePath;
        string platform;
        string contents;

		public UnitySerializeInternalBundle(string platform, string internalBundlePath, string contents, NodeBase rootNode)
        {
			this.platform = platform;
            this.internalBundlePath = internalBundlePath;
            this.contents = contents;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            if (File.Exists(this.internalBundlePath))
            {
                //Make sure this is always 36 characters long.
                this.referenceID = this.rootNode.GenerateID();

                ComponentNode componentNode = new ComponentNode(PCFResourceType.INTERNALBUNDLE, referenceID, null, platform);

                //Material nodes can and are most likely to be children of other component nodes.
                objNode.AddChildNode(componentNode);

                JObject metaData = new JObject();
                metaData["platform"] = platform;
                metaData["contents"] = contents;

                byte[] serializedBundle = File.ReadAllBytes(internalBundlePath);

                //Create serialized asset by converting data to a bytearray and give it to the constructor.
                AssetResource resource = new AssetResource(false);

                byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));
                resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, serializedBundle);

                serializedAssets.AddResource(referenceID, PCFResourceType.INTERNALBUNDLE, resource);
            }
        }
    }
}