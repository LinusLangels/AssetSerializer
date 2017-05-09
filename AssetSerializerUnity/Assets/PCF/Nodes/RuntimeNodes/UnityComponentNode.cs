using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public abstract class UnityComponentNode : UnityNodeBase
    {
        //Make sure we cannot deserialize/create the same component twice.
        protected bool isDeserialized;

        public UnityComponentNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base()
        {
            this.name = name;
            this.resourceType = resourceType;
            this.referenceID = referenceID;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks, 
                                        UnityNodeBase parentNode, 
                                        ResourceResponse resourceResponse, 
                                        List<Action<UnityNodeBase>> postInstallActions, 
                                        bool optimizedLoad)
        {
            throw new NotImplementedException();
        }
    }
}
