using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PCFFileFormat.Debugging;

namespace PCFFileFormat
{
    public class ComponentNode : NodeBase
    {
        public ComponentNode(PCFResourceType resourceType, UInt32 referenceID, string resourceIdentifier, string name = "none") : base()
        {
            this.resourceType = resourceType;
            this.referenceID = referenceID;
            this.resourceIdentifier = resourceIdentifier;
            this.name = name;
        }

        public override NodeResource Serialize(SerializedAssets serializedAssets, NodeResource serializedParent, ISerializeLogging logger)
        {
            NodeResource serializedNode = new NodeResource(this.resourceType, this.referenceID, this.name);

            logger.LogNode(this);

            if (serializedParent != null)
            {
                serializedParent.AddChildNode(serializedNode);
            }

            foreach (NodeBase child in this.ChildNodes)
            {
                child.Serialize(serializedAssets, serializedNode, logger);
            }

            if (serializedParent == null)
                return serializedNode;

            return null;
        }
    }
}
