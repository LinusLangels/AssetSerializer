using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace PCFFileFormat
{
    public class UnitySerializeAnimation : UnitySerializerBase
    {
        private Animation animation;

        public UnitySerializeAnimation(GameObject parentGO, Animation animation, NodeBase rootNode)
        {
            this.animation = animation;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.ANIMATION, referenceID, null);

            //Component nodes must always be parented to objNodes.
            objNode.AddChildNode(componentNode);

            //Get fields using reflection
            PropertyInfo[] properties = animation.GetType().GetProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].CanWrite)
                {
                    PropertyInfo currentProperty = properties[i];
                    Type valueType = currentProperty.PropertyType;

                    //Find matching deserializer using reflection and field type as string or similar.
                    if (valueType.IsPrimitive || valueType == typeof(string))
                    {
                        UnitySerializerBase primitiveSerializer = new UnitySerializePrimitive(currentProperty.GetValue(animation, null), valueType, currentProperty.Name, false, this.rootNode);
						primitiveSerializer.Serialize(serializedAssets, serializeOptions, componentNode, postSerializeActions);
                    }
                    else if (typeof(IList).IsAssignableFrom(properties[i].PropertyType))
                    {
                        //Convoluted way to make sure we have an array of System.Object.
                        IEnumerable collection = currentProperty.GetValue(animation, null) as IEnumerable;
                        List<System.Object> intermediateList = new List<System.Object>();

                        foreach (System.Object item in collection)
                        {
                            intermediateList.Add(item);
                        }

                        System.Object[] values = intermediateList.ToArray();

                        if (values != null && values.Length > 0)
                        {
                            UnitySerializerBase collectionSerializer = new UnitySerializeCollection(values, values[0].GetType(), animation, currentProperty.Name, rootNode);
							collectionSerializer.Serialize(serializedAssets, serializeOptions, componentNode, postSerializeActions);
                        }
                    }
                    else if (valueType.IsClass || valueType.IsLayoutSequential)
                    {
                        //If the datatype is not a primitive look for a class specific serializer to invoke.
                        Assembly assembly = Assembly.GetAssembly(this.GetType());
                        string namespaceName = this.GetType().Namespace;
                        string serializerName = namespaceName + "." + valueType.Name + "Serialization";
                        Type serializerType = assembly.GetType(serializerName, false);
                        System.Object val = properties[i].GetValue(this.animation, null);

                        //If we have implemented a custom serializer for this type we invoke it and let it serialize relevant data.
                        if (serializerType != null)
                        {
                            object serializerClass = Activator.CreateInstance(serializerType, Convert.ChangeType(val, valueType), currentProperty.Name, false, this.rootNode);
                            MethodInfo serializeMethod = serializerType.GetMethod("Serialize");

                            object[] parameters = new object[4];
                            parameters[0] = serializedAssets;
							parameters[1] = serializeOptions;
                            parameters[2] = componentNode;
                            parameters[3] = postSerializeActions;

                            serializeMethod.Invoke(serializerClass, parameters);
                        }
                        else
                        {
                            //Use generic serializer incase not found.
                            UnitySerializerBase classSerialize = new UnitySerializeClass(currentProperty.GetValue(this.animation, null), valueType, currentProperty.Name, false, this.rootNode);
							classSerialize.Serialize(serializedAssets, serializeOptions, componentNode, postSerializeActions);
                        }
                    }
                }
            }

            //Create serialized asset by converting data to a bytearray and give it to the constructor.
            AssetResource resource = new AssetResource(false);
            
            resource.Serialize(referenceID, MetaDataType.UNKOWN, null, null);

            serializedAssets.AddResource(referenceID, PCFResourceType.ANIMATION, resource);
        }
    }
}