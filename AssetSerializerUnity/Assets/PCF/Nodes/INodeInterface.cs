using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace PCFFileFormat
{
    public interface INodeInterface<T> where T : class
    {
        List<T> ChildNodes { get; }
        T ParentNode { get; }

        void AddChildNode(T node);
        void SetParent(T node);
        JObject GetJSONRepresentation();
        UInt32 GetReferenceID();
        string GetName();
        PCFResourceType GetResourceType();
    }
}
