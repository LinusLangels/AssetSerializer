using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnityAnimationClipPointerNode : UnityComponentNode
    {
        public UnityAnimationClipPointerNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.ANIMATIONCLIPREFERENCE;
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

                UInt32 pointedNodeID = BitConverter.ToUInt32(resource.GetResourceData(), 0);

                postInstallActions.Add((UnityNodeBase rootNode) => {

                    UnityNodeBase referencedNode = FindNodeWithID(rootNode, pointedNodeID);

                    if (referencedNode is UnityAnimationClipNode)
                    {
                        UnityAnimationClipNode animationClip = referencedNode as UnityAnimationClipNode;

                        AnimationClip animation = animationClip.GetAnimationClip();

                        string jsonString = System.Text.Encoding.UTF8.GetString(resource.GetMetaData());
                        JObject jsonObject = JObject.Parse(jsonString);

                        string fieldName = jsonObject.Value<string>("fieldName");
                        if (resourceResponse != null)
                        {
                            resourceResponse.GetFieldDeserializer.SetField(fieldName, animation);
                        }
                    }
                });

                this.isDeserialized = true;
            }
        }

        UnityNodeBase FindNodeWithID(UnityNodeBase node, UInt32 referenceID)
        {
            if (node.GetReferenceID() == referenceID)
            {
                return node;
            }

            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                UnityNodeBase foundChild = FindNodeWithID(node.ChildNodes[i], referenceID);

                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }
    }
}

