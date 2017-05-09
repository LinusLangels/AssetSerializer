using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCFFileFormat.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat
{
    public class UnitySerializeMaterial : UnitySerializerBase
    {
        private Material material;

        public UnitySerializeMaterial(Material material, NodeBase rootNode)
        {
            this.material = material;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
			#if UNITY_EDITOR
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            //Check if node hierarchy contains this material already.
            string path = AssetDatabase.GetAssetPath(this.material);
            NodeBase materialNode = CheckSharedMaterial(this.rootNode, path);

            //If material is already serialized (aka shared we simply serialize a pointer to it.
            if (materialNode != null)
            {
                //Debug.Log("Shared Material: " + path);

                UInt32 materialID = materialNode.GetReferenceID();

                ComponentNode componentNode = new ComponentNode(PCFResourceType.MATERIALPOINTER, referenceID, null);

                objNode.AddChildNode(componentNode);

                byte[] bytes = BitConverter.GetBytes(materialID);

                //Create serialized asset by converting data to a bytearray and give it to the constructor.
                AssetResource resource = new AssetResource(false);
                resource.Serialize(referenceID, MetaDataType.UNKOWN, null, bytes);

                serializedAssets.AddResource(referenceID, PCFResourceType.MATERIALPOINTER, resource);

                //Make sure caller can get the actual material referenceID when serializing its metadata.
                this.pointedID = materialID;
            }
            else
            {
                ComponentNode componentNode = new ComponentNode(PCFResourceType.MATERIAL, referenceID, path);

                //Material nodes can and are most likely to be children of other component nodes.
                objNode.AddChildNode(componentNode);

                SerializedMaterial serializedMaterial = new SerializedMaterial();
                serializedMaterial.DisplayName = this.material.name;
				serializedMaterial.ShaderName = this.material.shader.name;

                Dictionary<string, SerializedMaterialProperty> materialProperties = new Dictionary<string, SerializedMaterialProperty>();

                // Iterate through children and get the textures of the material.
                Shader shader = this.material.shader;

                int count = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < count; i++)
                {
                    ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(shader, i);
                    string propertyName = ShaderUtil.GetPropertyName(shader, i);

                    SerializedMaterialProperty property = null;

                    //Color property.
                    if (propertyType == ShaderUtil.ShaderPropertyType.Float || propertyType == ShaderUtil.ShaderPropertyType.Range)
                    {
                        SerializedMaterialFloatProperty prop = new SerializedMaterialFloatProperty();

                        prop.Val = this.material.GetFloat(propertyName);
                        prop.PropertyType = (int)MaterialPropertyType.FloatType;

                        property = prop;
                    }
                    //Vector property.
                    if (propertyType == ShaderUtil.ShaderPropertyType.Vector)
                    {
                        SerializedMaterialVectorProperty prop = new SerializedMaterialVectorProperty();

                        Vector4 vector = this.material.GetVector(propertyName);
                        prop.X = vector.x;
                        prop.Y = vector.y;
                        prop.Z = vector.z;
                        prop.W = vector.w;

                        prop.PropertyType = (int)MaterialPropertyType.VectorType;

                        property = prop;
                    }
                    //Float property.
                    if (propertyType == ShaderUtil.ShaderPropertyType.Color)
                    {
                        SerializedMaterialColorProperty prop = new SerializedMaterialColorProperty();

                        Color color = this.material.GetColor(propertyName);

                        prop.R = color.r;
                        prop.G = color.g;
                        prop.B = color.b;
                        prop.A = color.a;

                        prop.PropertyType = (int)MaterialPropertyType.ColorType;

                        property = prop;
                    }

                    //Texture property.
					if (propertyType == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        Texture2D texture = (Texture2D)this.material.GetTexture(propertyName);

                        if (texture != null)
                        {
                            UnitySerializeTexture textureSerializer = new UnitySerializeTexture(texture, this.rootNode);
							textureSerializer.Serialize(serializedAssets, serializeOptions, componentNode, postSerializeActions);

                            SerializedMaterialTextureProperty prop = new SerializedMaterialTextureProperty();

                            prop.ReferenceID = textureSerializer.GetReferenceID();
                            prop.PropertyType = (int)MaterialPropertyType.TextureType;

                            property = prop;
                        }
                    }

                    materialProperties.Add(propertyName, property);
                }

                serializedMaterial.MaterialProperties = materialProperties;

                //Create serialized asset by converting data to a bytearray and give it to the constructor.
                AssetResource resource = new AssetResource(false);

                byte[] metaDataBuffer = ProtocolBufferSerializer.SerializedMaterial(serializedMaterial);
                resource.Serialize(this.referenceID, MetaDataType.PROTOBUF, metaDataBuffer, null);

                serializedAssets.AddResource(referenceID, PCFResourceType.MATERIAL, resource);

                //Nodes store their resource when serializing
                componentNode.SetSerializer(this);

                //Make sure caller can get the actual material referenceID when serializing its metadata.
                this.pointedID = this.referenceID;
            }
			#endif
        }

        NodeBase CheckSharedMaterial(NodeBase node, string resourcePath)
        {
            string path = node.GetResourceIndentifier();
            if (!string.IsNullOrEmpty(path))
            {
                if (string.CompareOrdinal(path, resourcePath) == 0)
                {
                    return node;
                }
            }

            List<NodeBase> children = node.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                NodeBase foundChild = CheckSharedMaterial(children[i], resourcePath);

                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }
    }
}
