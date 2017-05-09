using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PCFFileFormat.Debugging;

namespace PCFFileFormat
{
    public class ObjectNode : NodeBase
    {
        public ObjectNode(string name, NodeBase rootNode) : base()
        {
            this.name = name;
            this.referenceID = rootNode.GenerateID();
            this.resourceType = PCFResourceType.OBJECT;
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
