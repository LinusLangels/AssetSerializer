using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PCFFileFormat.Serialization;

namespace PCFFileFormat
{
    public class UnityMaterialNode : UnityComponentNode
    {
        private Material material;

        public UnityMaterialNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base (name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.MATERIAL;
        }

        public Material GetMaterial()
        {
            return this.material;
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

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                byte[] metaDataBuffer = resource.GetMetaData();
                SerializedMaterial serializedMaterial = ProtocolBufferSerializer.DeserializeMaterialData(metaDataBuffer);

				string shaderName = serializedMaterial.ShaderName;
                this.material = new UnityEngine.Material(Shader.Find(shaderName));
                this.material.name = serializedMaterial.DisplayName;

                ResourceResponse previousRequest = null;

                if (serializedMaterial.MaterialProperties != null)
                {
                    foreach (KeyValuePair<string, SerializedMaterialProperty> pair in serializedMaterial.MaterialProperties)
                    {
                        if (pair.Value != null)
                        {
                            MaterialPropertyType propertyType = (MaterialPropertyType)pair.Value.PropertyType;

                            if (propertyType == MaterialPropertyType.FloatType)
                            {
                                SerializedMaterialFloatProperty floatProperty = pair.Value as SerializedMaterialFloatProperty;
                                this.material.SetFloat(pair.Key, floatProperty.Val);
                            }
                            else if (propertyType == MaterialPropertyType.VectorType)
                            {
                                SerializedMaterialVectorProperty vectorProperty = pair.Value as SerializedMaterialVectorProperty;

                                Vector4 vector = new Vector4(vectorProperty.X, vectorProperty.Y, vectorProperty.Z, vectorProperty.W);
                                this.material.SetVector(pair.Key, vector);
                            }
                            else if (propertyType == MaterialPropertyType.ColorType)
                            {
                                SerializedMaterialColorProperty colorProperty = pair.Value as SerializedMaterialColorProperty;

                                Color color = new Color(colorProperty.R, colorProperty.G, colorProperty.B, colorProperty.A);
                                this.material.SetColor(pair.Key, color);
                            }
                            else if (propertyType == MaterialPropertyType.TextureType)
                            {
                                SerializedMaterialTextureProperty textureProperty = pair.Value as SerializedMaterialTextureProperty;
                                string propertyName = pair.Key;

                                ResourceResponse textureRequest = new ResourceResponse(textureProperty.ReferenceID, (ResourceResponse response) =>
                                {
                                    material.SetTexture(propertyName, response.GetTextureRequest);
                                });

                                if (previousRequest != null)
                                {
                                    textureRequest.SetPreviousResponse(previousRequest);
                                    previousRequest.SetNextResponse(textureRequest);
                                }

                                previousRequest = textureRequest;
                            }
                        }
                    }
                }

                if (previousRequest != null)
                {
                    //Get the first request created.
                    ResourceResponse firstRequest = previousRequest;
                    while (true)
                    {
                        ResourceResponse previous = firstRequest.GetPreviousResponse();

                        if (previous == null)
                            break;

                        firstRequest = previous;
                    }

                    //Have each request set by one of the child nodes.
                    for (int i = 0; i < this.ChildNodes.Count; i++)
                    {
                        UnityNodeBase child = this.ChildNodes[i];

                        child.Deserialize(dataBlocks, parentNode, firstRequest, postInstallActions, optimizedLoad);
                    }
                }

                //Finish up and set material to whatever field/object that owns this node.
                ResourceResponse request = resourceResponse.CanHandle(GetReferenceID());
                if (request != null)
                {
                    request.HandleMaterialResponse(this.material);
                }

                watch.Stop();
                //Debug.Log("Time to deserialize material: " + watch.ElapsedMilliseconds);

                this.isDeserialized = true;
            }
        }

		public override System.Object GetObject()
		{
			return this.material;
		}

        public override void Destroy()
        {
	        //Destroy gameobject after all children have destroyed themselves.
	        UnityEngine.Object.Destroy(this.material);

            base.Destroy();
        }
    }
}
