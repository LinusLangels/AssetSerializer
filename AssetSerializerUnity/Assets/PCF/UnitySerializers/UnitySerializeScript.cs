using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnitySerializeScript : UnitySerializerBase
    {
        private MonoBehaviour script;

        public UnitySerializeScript(GameObject parentGO, MonoBehaviour script, NodeBase rootNode)
        {
            this.script = script;
            this.rootNode = rootNode;
        }
        
		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode scriptNode = new ComponentNode(PCFResourceType.SCRIPT, referenceID, null, this.script.GetType().Name.ToString());

            //Parent top level scripts to ObjectNode.
            objNode.AddChildNode(scriptNode);

            JObject metaData = new JObject();
            metaData["scriptname"] = this.script.GetType().ToString();

            //Get fields using reflection
            FieldInfo[] fields = script.GetType().GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsPublic && !fields[i].IsStatic)
                {
                    FieldInfo currentField = fields[i];
                    Type valueType = currentField.FieldType;

                    //Find matching deserializer using reflection and field type as string or similar.
                    if (valueType.IsPrimitive || valueType == typeof(string))
                    {
                        UnitySerializerBase primitiveSerializer = new UnitySerializePrimitive(currentField.GetValue(script), valueType, currentField.Name, false, this.rootNode);
						primitiveSerializer.Serialize(serializedAssets, serializeOptions, scriptNode, postSerializeActions);
                    }
                    else if (typeof(IList).IsAssignableFrom(fields[i].FieldType))
                    {
                        //Convoluted way to make sure we have an array of System.Object.
                        IEnumerable collection = currentField.GetValue(script) as IEnumerable;
                        List<System.Object> intermediateList = new List<System.Object>();

                        foreach (System.Object item in collection)
                        {
                            intermediateList.Add(item);
                        }

                        System.Object[] values = intermediateList.ToArray();

                        if (values != null && values.Length > 0)
                        {
                            UnitySerializerBase collectionSerializer = new UnitySerializeCollection(values, values[0].GetType(), script, currentField.Name, rootNode);
							collectionSerializer.Serialize(serializedAssets, serializeOptions, scriptNode, postSerializeActions);
                        }
                    }
                    else if (valueType.IsClass || valueType.IsLayoutSequential)
                    {
                        //If the datatype is not a primitive look for a class specific serializer to invoke.
                        Assembly assembly = Assembly.GetAssembly(this.GetType());
                        string namespaceName = this.GetType().Namespace;
                        string serializerName = namespaceName + "." + valueType.Name + "Serialization";
                        Type serializerType = assembly.GetType(serializerName, false);
                        System.Object val = fields[i].GetValue(this.script);

                        //If we have implemented a custom serializer for this type we invoke it and let it serialize relevant data.
                        if (serializerType != null)
                        {
                            object serializerClass = Activator.CreateInstance(serializerType, Convert.ChangeType(val, valueType), currentField.Name, false, this.rootNode);
                            MethodInfo serializeMethod = serializerType.GetMethod("Serialize");

                            object[] parameters = new object[4];
                            parameters[0] = serializedAssets;
							parameters[1] = serializeOptions;
                            parameters[2] = scriptNode;
                            parameters[3] = postSerializeActions;

                            serializeMethod.Invoke(serializerClass, parameters);
                        }
                        else
                        {
                            //Use generic serializer incase not found.
                            UnitySerializerBase classSerialize = new UnitySerializeClass(currentField.GetValue(this.script), valueType, currentField.Name, false, this.rootNode);
							classSerialize.Serialize(serializedAssets, serializeOptions, scriptNode, postSerializeActions);
                        }
                    }
                }
            }

            AssetResource resource = new AssetResource(false);

            byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));
            resource.Serialize(this.referenceID, MetaDataType.JSON, metaDataBuffer, null);

            serializedAssets.AddResource(referenceID, PCFResourceType.SCRIPT, resource);
        }
    }
}