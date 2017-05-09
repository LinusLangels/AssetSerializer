//#define MEASURE_PERFORMANCE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
	public enum TextureDataFormat
	{
		PVRTC4BPP = 1,
		RGB32 = 2,
		RGB24 = 3,
        ASTC6X6 = 4,
		Empty = 5,
	}
		
    public class UnityTextureNode : UnityComponentNode
    {
        Texture2D texture;

        public UnityTextureNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.TEXTURE;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                ResourceBlock dataBlock = dataBlocks[this.resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

                if (resource == null)
                {
                    ResourceResponse textureLookUp = resourceResponse.CanHandle(GetReferenceID());
                    if (textureLookUp != null)
                    {
                        textureLookUp.HandleTextureResponse(null);
                    }

                    return;
                }

                byte[] textureBytes = resource.GetResourceData();
                byte[] metaDataBuffer = resource.GetMetaData();
                JObject metaData = JObject.Parse(System.Text.Encoding.UTF8.GetString(metaDataBuffer));

                int width = metaData.Value<int>("width");
                int height = metaData.Value<int>("height");
                TextureDataFormat textureFormat = (TextureDataFormat)metaData.Value<int>("textureFormat");
                string fieldName = metaData.Value<string>("fieldName");

                if (textureFormat == TextureDataFormat.PVRTC4BPP)
                {
                    #if MEASURE_PERFORMANCE
                    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                    watch.Start();
                    #endif

                    #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
                    int dataSize = 0;
                    IntPtr dataPointer = PVRTCEncoderWrapper.DecompressData(textureBytes, width, height, false, ref dataSize);
                    if (dataPointer != IntPtr.Zero)
                    {
                        this.texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                        this.texture.LoadRawTextureData(dataPointer, dataSize);
                        this.texture.Apply(false, true);

                        //TODO: Probably better to use IDisposable here....
                        PVRTCEncoderWrapper.FreeCompressedDataPointer(dataPointer);
                    }
                    #else
                    this.texture = new Texture2D(width, height, TextureFormat.PVRTC_RGBA4, false);
                    this.texture.LoadRawTextureData(textureBytes);
                    this.texture.Apply(false, true);
                    #endif

                    #if MEASURE_PERFORMANCE
                    watch.Stop();
                    Debug.Log("Time to deserialize texture: " + watch.ElapsedMilliseconds);
                    #endif
                }
                else if(textureFormat == TextureDataFormat.ASTC6X6)
                {
                    #if MEASURE_PERFORMANCE
                    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                    watch.Start();
                    #endif

                    this.texture = new Texture2D(width, height, TextureFormat.ASTC_RGBA_6x6, false);
                    this.texture.LoadRawTextureData(textureBytes);
                    this.texture.Apply(false, true);

                    #if MEASURE_PERFORMANCE
                    watch.Stop();
                    Debug.Log("Time to deserialize texture: " + watch.ElapsedMilliseconds);
                    #endif
                }
                else if (textureFormat == TextureDataFormat.RGB32)
                {
                    this.texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    this.texture.LoadRawTextureData(textureBytes);

                    if (optimizedLoad)
                    {
                        this.texture.Apply(false, true);
                    }
                    else
                    {
                        this.texture.Apply(false);
                    } 
                }

                if (fieldName == null)
                {
                    ResourceResponse request = resourceResponse.CanHandle(GetReferenceID());
                    if (request != null)
                    {
                        request.HandleTextureResponse(texture);
                    }
                }
                else
                {
                    if (resourceResponse != null)
                    {
                        resourceResponse.GetFieldDeserializer.SetField(fieldName, this.texture);
                    }
                }

                this.isDeserialized = true;
            }
        }

		public override System.Object GetObject()
		{
			return this.texture;
		}

        public override void Destroy()
        {
            Texture2D.Destroy(this.texture);
        }
    }
}
