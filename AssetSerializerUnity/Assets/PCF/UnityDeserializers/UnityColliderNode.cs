using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnityColliderNode : UnityComponentNode
    {
        private BoxCollider collider;

        public UnityColliderNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.COLLIDER;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                this.collider = parentNode.GetGameObject().AddComponent<BoxCollider>();

                this.isDeserialized = true;
            }
        }

        public override void Destroy()
        {
            UnityEngine.Object.Destroy(this.collider);

            base.Destroy();
        }
    }
}
