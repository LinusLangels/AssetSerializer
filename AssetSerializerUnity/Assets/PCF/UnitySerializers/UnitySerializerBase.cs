using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public abstract class UnitySerializerBase
    {
        protected UInt32 referenceID;
        protected UInt32 pointedID;
        protected NodeBase rootNode;
        protected string assetPath;
        protected string fieldName;
        protected bool arrayItem;

        public UInt32 GetReferenceID()
        {
            return this.referenceID;
        }

        public UInt32 GetPointerID()
        {
            return this.pointedID;
        }

		public abstract void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions);
    }
}
