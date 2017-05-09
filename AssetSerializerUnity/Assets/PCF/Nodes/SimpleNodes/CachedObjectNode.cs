using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class CachedObjectNode : UnityNodeBase
    {
        public CachedObjectNode(string name, UInt32 referenceID, GameObject deserializeObject) : base()
        {
            this.name = name;
            this.referenceID = referenceID;
            this.resourceType = PCFResourceType.OBJECT;

            //Use a single gameobject as reference point.
            this.gameobject = deserializeObject;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            for (int i = 0; i < this.ChildNodes.Count; i++)
            {
                UnityNodeBase child = this.ChildNodes[i];
                child.Deserialize(dataBlocks, this, null, postInstallActions, optimizedLoad);
            }            
        }
    }
}
