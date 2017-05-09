using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace PCFFileFormat
{
    public class UnitySerializeCollection : UnitySerializerBase
    {
        System.Object[] values;
        Type type;

        public UnitySerializeCollection(System.Object[] values, Type type, System.Object script, string fieldName, NodeBase rootNode)
        {
            this.values = values;
            this.type = type;
            this.fieldName = fieldName;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.COLLECTION, referenceID, null, this.type.Name.ToString());
            objNode.AddChildNode(componentNode);

            List<UInt32> collectionIDs = new List<UInt32>();

            if (this.type.IsPrimitive || this.type == typeof(string))
            {
                //Try to serialize the collection using primitive data serialization.
                for (int i = 0; i < this.values.Length; i++)
                {
                    UnitySerializerBase primitiveSerializer = new UnitySerializePrimitive(this.values[i], this.values[i].GetType(), "", true, this.rootNode);
					primitiveSerializer.Serialize(serializedAssets, serializeOptions, componentNode, postSerializeActions);

                    collectionIDs.Add(primitiveSerializer.GetReferenceID());
                }
            }
            else
            {
                //If the datatype is not a primitive look for a class specific serializer to invoke.
                Assembly assembly = Assembly.GetAssembly(this.GetType());
                string namespaceName = this.GetType().Namespace;
                string serializerName = namespaceName + "." + this.type.Name + "Serialization";
                Type serializerType = assembly.GetType(serializerName, false);

                //See if there is a custom serializer defined for this type.
                if (serializerType != null)
                {
                    for (int i = 0; i < this.values.Length; i++)
                    {
                        System.Object obj = this.values[i];

                        object serializerClass = Activator.CreateInstance(serializerType, Convert.ChangeType(obj, this.type), "", true, this.rootNode);
                        MethodInfo serializeMethod = serializerType.GetMethod("Serialize");
                        MethodInfo referenceIDMethod = serializerType.GetMethod("GetReferenceID");

                        object[] parameters = new object[4];
                        parameters[0] = serializedAssets;
						parameters[1] = serializeOptions;
                        parameters[2] = componentNode;
                        parameters[3] = postSerializeActions;

                        serializeMethod.Invoke(serializerClass, parameters);

                        UInt32 refID = (UInt32)referenceIDMethod.Invoke(serializerClass, null);

                        collectionIDs.Add(refID);
                    }
                }
                //Fallback to generic class serializer.
                else
                {
                    for (int i = 0; i < this.values.Length; i++)
                    {
                        System.Object obj = this.values[i];

                        UnitySerializerBase classSerialize = new UnitySerializeClass(obj, this.type, "", true, this.rootNode);
						classSerialize.Serialize(serializedAssets, serializeOptions, componentNode, postSerializeActions);

                        collectionIDs.Add(classSerialize.GetReferenceID());
                    }
                }
            }

            AssetResource resource = new AssetResource(false);

            byte[] metaDataBuffer = ProtocolBufferSerializer.SerializeCollectionData(this.type, this.fieldName, collectionIDs.Count, null, this.type.Assembly);
            resource.Serialize(this.referenceID, MetaDataType.PROTOBUF, metaDataBuffer, null);

            serializedAssets.AddResource(referenceID, PCFResourceType.COLLECTION, resource);
        }
    }
}
