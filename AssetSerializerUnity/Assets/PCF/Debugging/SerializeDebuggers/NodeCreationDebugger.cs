using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PCFFileFormat.Debugging
{
    public class NodeCreationDebugger : ISerializeLogging
    {
        Dictionary<PCFResourceType, List<NodeBase>> loggedNodes;

        public NodeCreationDebugger()
        {
            this.loggedNodes = new Dictionary<PCFResourceType, List<NodeBase>>();
        }

        public void LogNode(NodeBase node)
        {
            if (this.loggedNodes.ContainsKey(node.GetResourceType()))
            {
                this.loggedNodes[node.GetResourceType()].Add(node);
            }
            else
            {
                this.loggedNodes.Add(node.GetResourceType(), new List<NodeBase> { node });
            }
        }

        public void PrintResult()
        {
            foreach (KeyValuePair<PCFResourceType, List<NodeBase>> pair in this.loggedNodes)
            {
                Debug.Log(pair.Key.ToString() + " :  " + pair.Value.Count);
            }
        }
    }
}
