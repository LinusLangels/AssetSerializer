using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using PCFFileFormat.Serialization;

namespace PCFFileFormat
{
    public class UnitySerializePrimitive : UnitySerializerBase
    {
        private System.Object value;
        private Type type;

        public UnitySerializePrimitive(System.Object value, Type type, string fieldName, bool arrayItem, NodeBase rootNode)
        {
            this.value = value;
            this.type = type;
            this.fieldName = fieldName;
            this.arrayItem = arrayItem;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            byte[] bytes = null;

            if (this.type == typeof(string))
            {
                if (this.value != null)
                {
                    bytes = System.Text.Encoding.UTF8.GetBytes((string)this.value);
                }
            }
            else if (this.type == typeof(int))
            {
                bytes = BitConverter.GetBytes((int)this.value);
            }
            else if (this.type == typeof(float))
            {
                bytes = BitConverter.GetBytes((float)this.value);
            }
            else if (this.type == typeof(double))
            {
                bytes = BitConverter.GetBytes((double)this.value);
            }
            else if (this.type == typeof(bool))
            {
                bytes = BitConverter.GetBytes((bool)this.value);
            }
            else if (this.type == typeof(byte[]))
            {
                bytes = (byte[])this.value;
            }

            if (bytes != null)
            {
                ComponentNode componentNode = new ComponentNode(PCFResourceType.PRIMITIVE, referenceID, null, this.type.Name.ToString());
                objNode.AddChildNode(componentNode);

                AssetResource resource = new AssetResource(false);

                byte[] metaDataBuffer = ProtocolBufferSerializer.SerializeFieldData(this.type, this.fieldName, this.arrayItem, this.type.Assembly);
                resource.Serialize(this.referenceID, MetaDataType.PROTOBUF, metaDataBuffer, bytes);

                serializedAssets.AddResource(referenceID, PCFResourceType.PRIMITIVE, resource);
            }
        }

        int MapType(Type type)
        {
            string name = type.Name.ToString();
            SerializedFieldType serializedType = SerializedFieldType.UNKNOWN;

            switch (name)
            {
                case "String":
                    serializedType = SerializedFieldType.STRING;
                    break;
                case "Int32":
                    serializedType = SerializedFieldType.INT;
                    break;
                case "UInt32":
                    serializedType = SerializedFieldType.UINT;
                    break;
                case "Single":
                    serializedType = SerializedFieldType.FLOAT;
                    break;
                case "Double":
                    serializedType = SerializedFieldType.DOUBLE;
                    break;
                case "Boolean":
                    serializedType = SerializedFieldType.BOOLEAN;
                    break;
                case "Byte[]":
                    serializedType = SerializedFieldType.BYTEBUFFER;
                    break;
            }

            return (int)serializedType;
        }
    }
}
