using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace PCFFileFormat
{
    public class UnitySerializeClass : UnitySerializerBase
    {
        private System.Object parentObject;
        private Type type;

        public UnitySerializeClass(System.Object parentObject, Type type, string fieldName, bool arrayItem, NodeBase rootNode)
        {
            this.parentObject = parentObject;
            this.type = type;
            this.fieldName = fieldName;
            this.arrayItem = arrayItem;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode scriptNode = new ComponentNode(PCFResourceType.CLASS, referenceID, null, this.type.Name.ToString());

            //Parent top level scripts to ObjectNode.
            objNode.AddChildNode(scriptNode);

            FieldInfo[] fields = this.type.GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsPublic && !fields[i].IsStatic)
                {
					SerializeField(serializedAssets, serializeOptions, scriptNode, postSerializeActions, fields[i]);
                }
            }

            AssetResource resource = new AssetResource(false);

            byte[] metaDataBuffer = ProtocolBufferSerializer.SerializeFieldData(this.type, this.fieldName, this.arrayItem, this.type.Assembly);
            resource.Serialize(this.referenceID, MetaDataType.PROTOBUF, metaDataBuffer, null);

            serializedAssets.AddResource(referenceID, PCFResourceType.CLASS, resource);
        }

		void SerializeField(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase scriptNode, List<Action<NodeBase>> postSerializeActions, FieldInfo field)
        {
            Type fieldType = field.FieldType;

            //Serialize public primitive members (yes we know, string is a not actually a primitive, but we pretend it is...)
            if (fieldType.IsPrimitive || fieldType == typeof(string))
            {
                UnitySerializerBase primitiveSerializer = new UnitySerializePrimitive(field.GetValue(this.parentObject), fieldType, field.Name, false, this.rootNode);
				primitiveSerializer.Serialize(serializedAssets, serializeOptions, scriptNode, postSerializeActions);
            }
            else if (typeof(IList).IsAssignableFrom(field.FieldType))
            {
                //Convoluted way to make sure we have an array of System.Object.
                IEnumerable collection = field.GetValue(this.parentObject) as IEnumerable;
                List<System.Object> intermediateList = new List<System.Object>();

                foreach (System.Object item in collection)
                {
                    intermediateList.Add(item);
                }

                System.Object[] values = intermediateList.ToArray();

                if (values != null && values.Length > 0)
                {
                    UnitySerializerBase collectionSerializer = new UnitySerializeCollection(values, values[0].GetType(), parentObject, field.Name, rootNode);
					collectionSerializer.Serialize(serializedAssets, serializeOptions, scriptNode, postSerializeActions);
                }
            }
            else if (fieldType.IsClass || fieldType.IsLayoutSequential)
            {
                //If the datatype is not a primitive look for a class specific serializer to invoke.
                Assembly assembly = Assembly.GetAssembly(this.GetType());
                string namespaceName = this.GetType().Namespace;
                string serializerName = namespaceName + "." + fieldType.Name + "Serialization";
                Type serializerType = assembly.GetType(serializerName, false);

                System.Object val = field.GetValue(this.parentObject);

                if (serializerType != null)
                {
                    object serializerClass = Activator.CreateInstance(serializerType, Convert.ChangeType(val, field.FieldType), field.Name, false, this.rootNode);
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
                    UnitySerializerBase classSerialize = new UnitySerializeClass(field.GetValue(this.parentObject), fieldType, field.Name, false, this.rootNode);
					classSerialize.Serialize(serializedAssets, serializeOptions, scriptNode, postSerializeActions);
                }
            }
        }
    }
}

