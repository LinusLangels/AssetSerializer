using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using PCFFileFormat.Serialization;

namespace PCFFileFormat
{
    public class UnityPointerCollectionNode : UnityComponentNode
    {
        public UnityPointerCollectionNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.POINTERCOLLECTION;
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
                    postInstallActions.Add((UnityNodeBase node) => {

                        byte[] metaDataBuffer = resource.GetMetaData();
                        SerializedCollectionData collectionData = ProtocolBufferSerializer.DeserializeCollectionData(metaDataBuffer);
                        string assemblyName = ProtocolBufferSerializer.GetAssemblyName(collectionData.assemblyType);
                        string scriptTypeName = collectionData.typeName;
                        string fieldName = collectionData.fieldName;
                        int itemCount = collectionData.count;

                        Dictionary <UInt32, UnityNodeBase> referencedNodes = new Dictionary<UInt32, UnityNodeBase>();

                        //Create dictionary with the referenced IDs, populate later.
                        foreach (UInt32 id in collectionData.itemIDs)
                        {
                            referencedNodes.Add(id, null);
                        }

                        //Find root node.
                        UnityNodeBase currentParent = parentNode;
                        while (currentParent.ParentNode != null)
                        {
                            currentParent = currentParent.ParentNode;
                        }

                        //Fill dictionary with matching nodes.
                        PopulateReferencedNodes(referencedNodes, currentParent);

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
                                response.SetReferencedNodes(referencedNodes);

                                foreach (UnityNodeBase child in this.ChildNodes)
                                {
                                    child.Deserialize(dataBlocks, this, response, postInstallActions, optimizedLoad);
                                    arrayDeserializer.IncrementIndex();
                                }
                            }

                            if (resourceResponse != null)
                            {
                                resourceResponse.GetFieldDeserializer.SetField(fieldName, array);
                            }
                        }
                    });
                }

                this.isDeserialized = true;
            }
        }

        void PopulateReferencedNodes(Dictionary<UInt32, UnityNodeBase> referencedNodes, UnityNodeBase node)
        {
            if (referencedNodes.ContainsKey(node.GetReferenceID()))
            {
                referencedNodes[node.GetReferenceID()] = node;
            }

            List<UnityNodeBase> children = node.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                UnityNodeBase child = children[i];
                PCFResourceType type = child.GetResourceType();

                if (type == PCFResourceType.ROOT || type == PCFResourceType.OBJECT || type == PCFResourceType.TRANSFORM)
                {
                    PopulateReferencedNodes(referencedNodes, children[i]);
                }
            }
        }
    }
}
