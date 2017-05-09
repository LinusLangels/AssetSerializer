using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PCFFileFormat.Serialization;

namespace PCFFileFormat
{
    public class UnityPrimitiveNode : UnityComponentNode
    {
        public UnityPrimitiveNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.PRIMITIVE;
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

                byte[] metaDataBuffer = resource.GetMetaData();
                SerializedFieldData fieldData = ProtocolBufferSerializer.DeserializeFieldData(metaDataBuffer);

                if (fieldData != null)
                {
                    byte[] data = resource.GetResourceData();
                    System.Object obj = null;

                    if (data != null)
                    {
                        if (fieldData.type == 1)
                        {
                            obj = System.Text.Encoding.UTF8.GetString(data);
                        }
                        else if (fieldData.type == 2)
                        {
                            obj = BitConverter.ToInt32(data, 0);
                        }
                        else if (fieldData.type == 3)
                        {
                            obj = BitConverter.ToUInt32(data, 0);
                        }
                        else if (fieldData.type == 4)
                        {
                            obj = BitConverter.ToSingle(data, 0);
                        }
                        else if (fieldData.type == 5)
                        {
                            obj = BitConverter.ToDouble(data, 0);
                        }
                        else if (fieldData.type == 6)
                        {
                            obj = BitConverter.ToBoolean(data, 0);
                        }
                        else if (fieldData.type == 7)
                        {
                            obj = data;
                        }

                        if (resourceResponse != null)
                        {
                            if (fieldData.arrayItem)
                            {
                                resourceResponse.GetFieldDeserializer.SetArrayItem(obj);
                            }
                            else
                            {
                                resourceResponse.GetFieldDeserializer.SetField(fieldData.fieldName, obj);
                            }
                        }
                    }
                }

                this.isDeserialized = true;
            }
        }
    }
}
