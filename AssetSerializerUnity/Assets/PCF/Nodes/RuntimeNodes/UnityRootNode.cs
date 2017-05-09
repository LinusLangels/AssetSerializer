using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnityRootNode : UnityNodeBase
    {
        IFileHandle filePath;

        public UnityRootNode(string name, UInt32 referenceID) : base()
        {
            this.name = name;
            this.referenceID = referenceID;
            this.resourceType = PCFResourceType.ROOT;
        }

        public static UnityRootNode CreateNodeTree(Dictionary<PCFResourceType, DataBlockBase> dataBlocks, IFileHandle filePath, NodeFactory nodeFactory)
        {
            NodeBlock nodeBlock = dataBlocks[PCFResourceType.NODE] as NodeBlock;

            //Create and hook up node graph, but do no actual deserialization.
            UnityNodeBase node = nodeBlock.RecreateNodeGraph<UnityNodeBase>(nodeFactory);

            UnityRootNode rootNode = node as UnityRootNode;

            //Give rootnode a reference to the file we operate on, some nodes stream their data from this file.
            rootNode.SetFile(filePath);

            return rootNode;
        }

        public override GameObject GetGameObject()
        {
            if (this.ChildNodes != null)
            {
                UnityNodeBase objectNode = GetChildNodeByType(PCFResourceType.OBJECT);

                if (objectNode != null)
                {
                    return objectNode.GetGameObject();
                }
            }

            return null;
        }

        public override GameObject InstantiateContent()
        {
            GameObject mainObject = GetGameObject();

            if (mainObject != null)
            {
                GameObject go = GameObject.Instantiate<GameObject>(mainObject);
                SetActiveRecursivly(go);

                return go;
            }
            else
            {
                Debug.LogError("Unable to instantiate content, main object not found");
            }

            return null;
        }

        public void SetFile(IFileHandle filePath)
        {
            this.filePath = filePath;
        }

        public IFileHandle GetFile()
        {
            return this.filePath;
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

        public override void Destroy()
        {
            for (int i = 0; i < this.ChildNodes.Count; i++)
            {
                UnityNodeBase child = this.ChildNodes[i];
                child.Destroy();
            }
        }

        void SetActiveRecursivly(GameObject go)
        {
            go.SetActive(true);

            foreach (Transform child in go.transform)
            {
                SetActiveRecursivly(child.gameObject);
            }
        }
    }
}
