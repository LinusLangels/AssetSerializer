using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnitySerializeLightProbes : UnitySerializerBase
    {
        private LightProbes lightProbes;
		private GameObject parentGO;

        public UnitySerializeLightProbes(GameObject parentGO, LightProbes lightProbes, NodeBase rootNode)
        {
			this.parentGO = parentGO;
            this.lightProbes = lightProbes;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

			LightprobeSerializeOpts serializeOption = null;
			for (int i = 0; i < serializeOptions.Length; i++)
			{
				object opt = serializeOptions[i];

				if (opt is LightprobeSerializeOpts)
				{
					serializeOption = opt as LightprobeSerializeOpts;
					break;
				}
			}

			if (serializeOption == null)
				return;

            ComponentNode componentNode = new ComponentNode(PCFResourceType.LIGHTPROBES, referenceID, null);

            //Material nodes can and are most likely to be children of other component nodes.
            objNode.AddChildNode(componentNode);

			//Manually serialize SH components,.
			//However unity does not currently expose any way to generate the probes at runtime.
			//So we cannot set them directly unless the probes exist. (Fix it unity :D)
            byte[] serializedProbes = SerializeLightProbes(this.lightProbes);

			Dictionary<string, FileInfo> internalBundles = serializeOption.ListInternalBundles();

			JArray bundleReferences = new JArray();

			foreach (KeyValuePair<string, FileInfo> pair in internalBundles)
			{
				UnitySerializeInternalBundle bundleSerializer = new UnitySerializeInternalBundle(pair.Key, pair.Value.FullName, string.Empty, this.rootNode);
				bundleSerializer.Serialize(serializedAssets, serializeOptions, componentNode, postSerializeActions);

				JObject item = new JObject();
				item["platform"] = pair.Key;
				item["referenceID"] = bundleSerializer.GetReferenceID();

				bundleReferences.Add(item);
			}

			JObject metaData = new JObject();
			metaData["numberOfProbes"] = this.lightProbes != null ? this.lightProbes.bakedProbes.Length : 0;
			metaData["bundleReferences"] = bundleReferences;

            //Create serialized asset by converting data to a bytearray and give it to the constructor.
            AssetResource resource = new AssetResource(false);

            byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));
            resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, serializedProbes);

            serializedAssets.AddResource(referenceID, PCFResourceType.LIGHTPROBES, resource);

            //Nodes store their resource when serializing
            componentNode.SetSerializer(this);
        }

        byte[] SerializeLightProbes(LightProbes lightprobe)
        {
            if (this.lightProbes != null)
            {
                SphericalHarmonicsL2[] probeData = LightmapSettings.lightProbes.bakedProbes;
                Vector3[] probePositions = lightprobe.positions;

                float[] probeDataArray = new float[30 * probeData.Length];                

                int stride = 0;

                for (int i = 0; i < probeData.Length; i++)
                {
                    Vector3 position = probePositions[i];
                    SphericalHarmonicsL2 probe = probeData[i];
                    
                    probeDataArray[stride] = position.x;
                    stride++;

                    probeDataArray[stride] = position.y;
                    stride++;

                    probeDataArray[stride] = position.z;
                    stride++;

                    //Save all coefficients for this probe.
                    for (int k = 0; k < 27; k++)
                    {
                        probeDataArray[stride + k] = probe[0, k];
                    }

                    stride += 27;
                }

                byte[] serializedLightProbes = new byte[probeDataArray.Length * sizeof(float)];
                int index = 0;

                for (int i = 0; i < probeDataArray.Length; i++)
                {
                    byte[] serializedFloat = BitConverter.GetBytes(probeDataArray[i]);

                    Buffer.BlockCopy(serializedFloat, 0, serializedLightProbes, index, serializedFloat.Length);

                    index += 4;
                }

                return serializedLightProbes;
            }

            //Do not return null, serializer will cry...
            return new byte[10];
        }
    }
}
