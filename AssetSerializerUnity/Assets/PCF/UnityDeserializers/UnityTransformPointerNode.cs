using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnityTransformPointerNode : UnityComponentNode
    {
        public UnityTransformPointerNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.TRANSFORMPOINTER;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                //Deserialize pointed node id.
                ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

                byte[] bytes = resource.GetResourceData();

                UInt32 referencedNodeID = BitConverter.ToUInt32(bytes, 0);
                //JObject metaData = JObject.Parse(System.Text.Encoding.UTF8.GetString(metaDataBuffer));

                //UInt32 referencedNodeID = metaData.Value<UInt32>("targetReferenceID");

                Dictionary<UInt32, UnityNodeBase> referencedNodes = resourceResponse.GetReferencedNodes;

                if (referencedNodes.ContainsKey(referencedNodeID))
                {
                    UnityNodeBase referencedNode = referencedNodes[referencedNodeID];

                    Transform transform = referencedNode.GetTransform();

                    if (transform != null)
                    {
                        resourceResponse.GetFieldDeserializer.SetArrayItem(transform);
                    }
                }

                this.isDeserialized = true;
            }            
        }

        public override void Destroy()
        {
            //Does nothing, main asset cleans up.
        }
    }
}
