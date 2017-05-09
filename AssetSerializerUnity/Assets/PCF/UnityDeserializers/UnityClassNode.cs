using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using PCFFileFormat.Serialization;

namespace PCFFileFormat
{
    public class UnityClassNode : UnityComponentNode
    {
        public UnityClassNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.CLASS;
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

                if (resource != null)
                {
                    byte[] metaDataBuffer = resource.GetMetaData();
                    SerializedFieldData fieldData = ProtocolBufferSerializer.DeserializeFieldData(metaDataBuffer);

                    string assemblyName = ProtocolBufferSerializer.GetAssemblyName(fieldData.assemblyType);

                    if (assemblyName != null)
                    {
                        Type scriptType = null;

                        //Qualify type check with assembly name, GetType only looks in current assembly otherwise.
                        if (!string.IsNullOrEmpty(assemblyName))
                        {
                            scriptType = Type.GetType(fieldData.typeName + ", " + assemblyName);
                        }
                        else
                        {
                            scriptType = Type.GetType(fieldData.typeName);
                        }

                        if (scriptType != null)
                        {
                            System.Object objectInstance = Activator.CreateInstance(scriptType);

                            FieldDeserializer fieldDeserializer = new FieldDeserializer(scriptType.GetFields(), objectInstance);
                            ResourceResponse response = new ResourceResponse();
                            response.SetFieldDeserializer(fieldDeserializer);

                            foreach (UnityNodeBase node in this.ChildNodes)
                            {
                                node.Deserialize(dataBlocks, this, response, postInstallActions, optimizedLoad);
                            }

                            //if fieldname is empty its an array item.
                            if (resourceResponse != null)
                            {
                                if (fieldData.arrayItem)
                                {
                                    resourceResponse.GetFieldDeserializer.SetArrayItem(objectInstance);
                                }
                                else
                                {
                                    resourceResponse.GetFieldDeserializer.SetField(fieldData.fieldName, objectInstance);
                                }
                            }
                        }
                    }
                }

                this.isDeserialized = true;
            }
        }
    }
}
