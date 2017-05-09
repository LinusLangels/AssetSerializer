using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using System.IO;
using PCFFileFormat.Serialization;

namespace PCFFileFormat
{
    public enum SerializedFieldType
    {
        UNKNOWN = 0,
        STRING = 1,
        INT = 2,
        UINT = 3,
        FLOAT = 4,
        DOUBLE = 5,
        BOOLEAN = 6,
        BYTEBUFFER = 7,
    }

    public enum SerializedAssemblyType
    {
        UNKOWN = 0,
        UNITY = 1,
        CURRENT = 2,
        CORE = 3,
        OTHER = 4,
    }

    public class ProtocolBufferSerializer
    {
		private static AssetSerializerTypeTree serializer = new AssetSerializerTypeTree();

        public static byte[] SerializeFieldData(Type type, string fieldName, bool arrayItem, Assembly assembly)
        {
            SerializedFieldData serializedField = new SerializedFieldData();

            serializedField.type = MapPrimitiveType(type);
            serializedField.fieldName = fieldName;
            serializedField.arrayItem = arrayItem;
            serializedField.assemblyType = MapAssembly(assembly);

            //For user defined types we cant have them all mapped in an enum.
            if (serializedField.type == 0)
            {
                serializedField.typeName = type.ToString();
            }

            byte[] serializedBuffer = null;

            using (var ms = new MemoryStream())
            {
                serializer.Serialize(ms, serializedField);
                serializedBuffer = ms.ToArray();
            }

            return serializedBuffer;
        }

        public static byte[] SerializeCollectionData(Type type, string fieldName, int itemCount, UInt32[] itemIDs, Assembly assembly)
        {
            SerializedCollectionData serializedCollection = new SerializedCollectionData();

            serializedCollection.type = MapPrimitiveType(type);
            serializedCollection.fieldName = fieldName;
            serializedCollection.count = itemCount;
            serializedCollection.itemIDs = itemIDs;
            serializedCollection.assemblyType = MapAssembly(assembly);
            serializedCollection.typeName = type.ToString();

            byte[] serializedBuffer = null;

            using (var ms = new MemoryStream())
            {
                serializer.Serialize(ms, serializedCollection);
                serializedBuffer = ms.ToArray();
            }

            return serializedBuffer;
        }

        public static byte[] SerializedMaterial(SerializedMaterial serializedMaterial)
        {
            byte[] serializedBuffer = null;

            using (var ms = new MemoryStream())
            {
                serializer.Serialize(ms, serializedMaterial);
                serializedBuffer = ms.ToArray();
            }

            return serializedBuffer;
        }

        public static byte[] SerializeAnimationClipData(SerializedAnimationClip serializedClip)
        {
            byte[] serializedBuffer = null;

            using (var ms = new MemoryStream())
            {
                serializer.Serialize(ms, serializedClip);
                serializedBuffer = ms.ToArray();
            }

            return serializedBuffer;
        }

        public static SerializedFieldData DeserializeFieldData(byte[] buffer)
        {
            SerializedFieldData deserializedFieldData = null;

            if (buffer == null || buffer.Length == 0)
                return deserializedFieldData;

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                deserializedFieldData = serializer.Deserialize(stream, null, typeof(SerializedFieldData)) as SerializedFieldData;
            }

            return deserializedFieldData;
        }

        public static SerializedCollectionData DeserializeCollectionData(byte[] buffer)
        {
            SerializedCollectionData deserializedCollectionData = null;

            if (buffer == null || buffer.Length == 0)
                return deserializedCollectionData;

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                deserializedCollectionData = serializer.Deserialize(stream, null, typeof(SerializedCollectionData)) as SerializedCollectionData;
            }

            return deserializedCollectionData;
        }

        public static SerializedMaterial DeserializeMaterialData(byte[] buffer)
        {
            SerializedMaterial deserializedMaterialData = null;

            if (buffer == null || buffer.Length == 0)
                return deserializedMaterialData;

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                deserializedMaterialData = serializer.Deserialize(stream, null, typeof(SerializedMaterial)) as SerializedMaterial;
            }

            return deserializedMaterialData;
        }

        public static SerializedAnimationClip DeserializeAnimationClipData(byte[] buffer)
        {
            SerializedAnimationClip animationClip = null;

            if (buffer == null || buffer.Length == 0)
                return animationClip;

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                stream.Position = 0;
                animationClip = serializer.Deserialize(stream, null, typeof(SerializedAnimationClip)) as SerializedAnimationClip;
            }

            return animationClip;
        }

        public static string GetAssemblyName(int assemblyType)
        {
            string assemblyName = null;

            switch (assemblyType)
            {
                case 1:
                    assemblyName = "UnityEngine";
                    break;
                case 2:
                    assemblyName = "Assembly-CSharp";
                    break;
                case 3:
                    assemblyName = "mscorlib";
                    break;
            }

            return assemblyName;
        }

        private static int MapPrimitiveType(Type type)
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

        private static int MapAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name;

            SerializedAssemblyType assemblyType = SerializedAssemblyType.UNKOWN;

            switch (name)
            {
                case "UnityEngine":
                    assemblyType = SerializedAssemblyType.UNITY;
                    break;
                case "Assembly-CSharp":
                    assemblyType = SerializedAssemblyType.CURRENT;
                    break;
                case "mscorlib":
                    assemblyType = SerializedAssemblyType.CORE;
                    break;
            }
            return (int)assemblyType;
        }
    }
}
