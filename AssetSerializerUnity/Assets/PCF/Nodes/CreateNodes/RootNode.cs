using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PCFFileFormat.Debugging;

namespace PCFFileFormat
{
    public class RootNode : NodeBase
    {
        public RootNode(string name) : base()
        {
            this.name = name;
            this.resourceType = PCFResourceType.ROOT;
            this.generator = new IDGenerator();
            this.referenceID = GenerateID();
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
