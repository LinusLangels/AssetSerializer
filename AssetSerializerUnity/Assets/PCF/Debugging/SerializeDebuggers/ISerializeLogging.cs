using UnityEngine;
using System.Collections;

namespace PCFFileFormat.Debugging
{
    public interface ISerializeLogging
    {
        void LogNode(NodeBase node);
        void PrintResult();
    }
}
