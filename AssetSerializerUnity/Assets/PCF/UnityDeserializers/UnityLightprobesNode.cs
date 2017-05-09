using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnityLightProbesNode : UnityComponentNode
    {
		private static Dictionary<RuntimePlatform, string> mapping;

		static UnityLightProbesNode()
		{
			mapping = new Dictionary<RuntimePlatform, string>();
			mapping.Add(RuntimePlatform.WindowsEditor, "StandaloneOSXIntel");
			mapping.Add(RuntimePlatform.WindowsPlayer, "StandaloneOSXIntel");
			mapping.Add(RuntimePlatform.OSXEditor, "StandaloneOSXIntel");
			mapping.Add(RuntimePlatform.OSXPlayer, "StandaloneOSXIntel");
			mapping.Add(RuntimePlatform.IPhonePlayer, "iOS");
			mapping.Add(RuntimePlatform.Android, "Android");
		}

        private LightProbes lightProbes;

        public UnityLightProbesNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.LIGHTPROBES;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

                /*
                byte[] serializedProbes = resource.GetResourceData();

                //30 floats in each array element.
                int sizeOfArray = numberOfProbes * 30;
                float[] lightProbeData = new float[sizeOfArray];

                for (int i = 0; i < sizeOfArray; i++)
                {                    
                    lightProbeData[i] = BitConverter.ToSingle(serializedProbes, i * sizeof(float));
                }

                Vector3[] probePositions = new Vector3[numberOfProbes];
                SphericalHarmonicsL2[] bakedProbes = new SphericalHarmonicsL2[numberOfProbes];

                int stride = 0;
                for (int i = 0; i < numberOfProbes; i++)
                {
                    probePositions[i] = new Vector3(lightProbeData[stride], lightProbeData[stride + 1], lightProbeData[stride + 2]);                    
                    stride += 3;

                    SphericalHarmonicsL2 probe = new SphericalHarmonicsL2();
                    for (int k = 0; k < 27; k++)
                    {
                        probe[0, k] = lightProbeData[stride + k];
                    }
                    bakedProbes[i] = probe;

                    stride += 27;
                }
                */

				byte[] metaDataBuffer = resource.GetMetaData();
				JObject metaData = JObject.Parse(System.Text.Encoding.UTF8.GetString(metaDataBuffer));
				int numberOfProbes = metaData.Value<int>("numberOfProbes");
				JArray bundleReferences = metaData.Value<JArray>("bundleReferences");

				UInt32 mappedNodeID = 0;

				//Load the correct bundle depending on what platform we are running.
				if (mapping.ContainsKey(Application.platform))
				{
					string mappedPlatform = mapping[Application.platform];

					foreach (JObject item in bundleReferences)
					{
						string platform = item.Value<string>("platform");

						if (string.CompareOrdinal(mappedPlatform, platform) == 0)
						{
							mappedNodeID = item.Value<UInt32>("referenceID");
						}
					}
				}

				if (mappedNodeID != 0)
				{
					//Create a request to be send down the chain of children, see if any child will handle it.
					ResourceResponse lightprobeResponse = new ResourceResponse(mappedNodeID, (ResourceResponse response) => {
						AssetBundle internalBundle = response.GetAssetBundleRequest;

						if (internalBundle != null)
						{
							UnityEngine.Object[] objects = internalBundle.LoadAllAssets<UnityEngine.Object>();

							foreach (UnityEngine.Object obj in objects)
							{
								if (obj is LightProbes)
								{
									this.lightProbes = obj as LightProbes;
									break;
								}
							}

							if (this.lightProbes == null)
							{
								Debug.LogError("Unable to find lightprobe data!");
							}
						}
					});

					for (int i = 0; i < this.ChildNodes.Count; i++)
					{
						UnityNodeBase child = this.ChildNodes[i];
						child.Deserialize(dataBlocks, this, lightprobeResponse, postInstallActions, optimizedLoad);
					}
				}

                this.isDeserialized = true;
            }
        }

        public LightProbes GetLightProbes()
        {
            return this.lightProbes;
        }
    }
}