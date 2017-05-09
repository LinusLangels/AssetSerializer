using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using PCFFileFormat.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnityCollectionNode : UnityComponentNode
    {
        public UnityCollectionNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.COLLECTION;
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
                    SerializedCollectionData collectionData = ProtocolBufferSerializer.DeserializeCollectionData(metaDataBuffer);
                    string assemblyName = ProtocolBufferSerializer.GetAssemblyName(collectionData.assemblyType);
                    string scriptTypeName = collectionData.typeName;
                    string fieldName = collectionData.fieldName;
                    int itemCount = collectionData.count;
                    Type scriptType = null;

                    //Qualify type check with assembly name, GetType only looks in current assembly otherwise.
                    if (!string.IsNullOrEmpty(assemblyName))
                    {
                        scriptType = Type.GetType(scriptTypeName + ", " + assemblyName);
                    }
                    else
                    {
                        scriptType = Type.GetType(scriptTypeName);
                    }

                    if (scriptType != null)
                    {
                        Array array = Array.CreateInstance(scriptType, itemCount);

                        if (array != null)
                        {
                            FieldDeserializer arrayDeserializer = new FieldDeserializer(array);
                            ResourceResponse response = new ResourceResponse();
                            response.SetFieldDeserializer(arrayDeserializer);

                            foreach (UnityNodeBase node in this.ChildNodes)
                            {
                                node.Deserialize(dataBlocks, this, response, postInstallActions, optimizedLoad);
                                arrayDeserializer.IncrementIndex();
                            }
                        }

                        if (resourceResponse != null)
                        {
                            resourceResponse.GetFieldDeserializer.SetField(fieldName, array);
                        }
                    }
                }

                this.isDeserialized = true;                
            }
        }
    }
}
