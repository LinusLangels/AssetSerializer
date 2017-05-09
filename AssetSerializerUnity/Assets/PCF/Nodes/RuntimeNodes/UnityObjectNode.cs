using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnityObjectNode : UnityNodeBase
    {
        public UnityObjectNode(string name, UInt32 referenceID) : base()
        {
            this.name = name;
            this.referenceID = referenceID;
            this.resourceType = PCFResourceType.OBJECT;
        }

        public override Transform GetTransform()
        {
            UnityNodeBase transformNode = GetChildNodeByType(PCFResourceType.TRANSFORM);

            if (transformNode != null)
            {
                return transformNode.GetTransform();
            }

            return this.transform;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            this.gameobject = new GameObject(this.name);
            this.gameobject.SetActive(false);

            for (int i = 0; i < this.ChildNodes.Count; i++)
            {
                UnityNodeBase child = this.ChildNodes[i];
                child.Deserialize(dataBlocks, this, null, postInstallActions, optimizedLoad);
            }            
        }

        public override void Destroy()
        {
            for (int i = 0; i < this.ChildNodes.Count; i++)
            {
                UnityNodeBase child = this.ChildNodes[i];
                child.Destroy();
            }

            //Destroy gameobject after all children have destroyed themselves.
            UnityEngine.Object.Destroy(this.gameobject);
        }
    }
}
