using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace PCFFileFormat
{
    public abstract class UnityNodeBase : INodeInterface<UnityNodeBase>
    {
        protected UInt32 referenceID;
        protected string name;
        protected PCFResourceType resourceType;
        protected GameObject gameobject;
        protected Transform transform;
        protected bool nodeEnabled;

        private List<UnityNodeBase> children;
        private UnityNodeBase parentNode;
        private UnityNodeBase transformNode;

        public UnityNodeBase() : base()
        {
            this.children = new List<UnityNodeBase>();
            this.nodeEnabled = true;
        }

        public List<UnityNodeBase> ChildNodes { get { return this.children; } }
        public UnityNodeBase ParentNode { get { return this.parentNode; } }

        public void AddChildNode(UnityNodeBase node)
        {
            if (this.children == null)
                this.children = new List<UnityNodeBase>();

            //When parenting nodes give them a reference to their parent node.
            node.SetParent(this);

            this.children.Add(node);
        }

        public void SetParent(UnityNodeBase node)
        {
            this.parentNode = node;
        }

        public UInt32 GetReferenceID()
        {
            return this.referenceID;
        }

        public string GetName()
        {
            return this.name;
        }

        public PCFResourceType GetResourceType()
        {
            return this.resourceType;
        }

        public UnityNodeBase GetChildNodeByType(PCFResourceType type)
        {
            if (this.ChildNodes != null)
            {
                for (int i = 0; i < this.ChildNodes.Count; i++)
                {
                    UnityNodeBase child = this.ChildNodes[i];

                    if (child.GetResourceType() == type)
                    {
                        return child;
                    }
                }
            }

            return null;
        }
			
        public JObject GetJSONRepresentation()
        {
            JObject representation = new JObject();

            representation["type"] = GetType().Name;
            representation["id"] = this.referenceID;
            representation["internalID"] = this.referenceID;
            representation["text"] = this.name;
            representation["parent"] = this.parentNode != null ? this.parentNode.referenceID : 0;

            return representation;
        }

        public virtual GameObject GetGameObject()
        {
            return this.gameobject;
        }

        public virtual Transform GetTransform()
        {
            return this.transform;
        }

        public virtual GameObject InstantiateContent()
        {
            return null;
        }

		public virtual System.Object GetObject()
		{
			return null;
		}

        public virtual void SetActive(bool toggle)
        {
            if (this.gameobject != null)
            {
                if (this.nodeEnabled)
                {
                    this.gameobject.SetActive(toggle);
                }
                else
                {
                    this.gameobject.SetActive(false);
                }
            }

            for (int i = 0; i < this.ChildNodes.Count; i++)
            {
                UnityNodeBase child = this.ChildNodes[i];

                child.SetActive(toggle);
            }
        }

        public void ToggleNode(bool toggle)
        {
            this.nodeEnabled = toggle;
        }

        public static UnityNodeBase FindNodeWithName(UnityNodeBase current, string name)
        {
            if (string.CompareOrdinal(current.GetName(), name) == 0)
            {
                return current;
            }

            for (int i = 0; i < current.ChildNodes.Count; i++)
            {
                UnityNodeBase child = current.ChildNodes[i];

                UnityNodeBase foundChild = FindNodeWithName(child, name);

                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }

        public static void FindNodesWithType(UnityNodeBase current, List<UnityNodeBase> foundNodes, PCFResourceType type)
        {
            if (current.GetResourceType() == type)
            {
				foundNodes.Add(current);
            }

            for (int i = 0; i < current.ChildNodes.Count; i++)
            {
                UnityNodeBase child = current.ChildNodes[i];

				FindNodesWithType(child, foundNodes, type);
            }
        }

		public static UnityNodeBase FindNodeWithObject(UnityNodeBase current, System.Object obj)
		{
			System.Object nodeObject = current.GetObject();

			if (nodeObject != null)
			{
				if (System.Object.ReferenceEquals(nodeObject, obj))
				{
					return current;
				}
			}
				
			for (int i = 0; i < current.ChildNodes.Count; i++)
			{
				UnityNodeBase child = current.ChildNodes[i];

				UnityNodeBase foundNode = FindNodeWithObject(child, obj);

				if (foundNode != null)
					return foundNode;
			}

			return null;
		}

        public abstract void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks, 
                                        UnityNodeBase parentNode, 
                                        ResourceResponse resourceResponse, 
                                        List<Action<UnityNodeBase>> postInstallActions, 
                                        bool optimizedLoad);

        public virtual void Destroy()
        {
            for (int i = 0; i < this.ChildNodes.Count; i++)
            {
                UnityNodeBase child = this.ChildNodes[i];
                child.Destroy();
            }
        }

        public virtual void Reconstruct() { }
    }
}
