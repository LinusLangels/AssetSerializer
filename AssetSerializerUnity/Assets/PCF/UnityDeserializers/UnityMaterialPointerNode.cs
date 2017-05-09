using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnityMaterialPointerNode : UnityComponentNode
    {
        public UnityMaterialPointerNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.MATERIALPOINTER;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            //Deserialize pointed node id.
            ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
            AssetResource resource = dataBlock.GetResource(this.referenceID);

            UInt32 pointedNodeID = BitConverter.ToUInt32(resource.GetResourceData(), 0);

            UnityNodeBase currentParent = parentNode;
            while (currentParent.ParentNode != null)
            {
                currentParent = currentParent.ParentNode;
            }

            UnityNodeBase referencedNode = FindNodeWithID(currentParent, pointedNodeID);

            if (referencedNode is UnityMaterialNode)
            {
                UnityMaterialNode materialNode = referencedNode as UnityMaterialNode;

                Material mat = materialNode.GetMaterial();

                ResourceResponse request = resourceResponse.CanHandle(pointedNodeID);
                if (request != null)
                {
                    request.HandleMaterialResponse(mat);
                }
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
