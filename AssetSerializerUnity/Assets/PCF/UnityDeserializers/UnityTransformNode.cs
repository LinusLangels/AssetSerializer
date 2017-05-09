using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnityTransformNode : UnityComponentNode
    {
        public UnityTransformNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.TRANSFORM;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

                byte[] bytes = resource.GetResourceData();

                int vectorCount = (bytes.Length / 12);
                Vector3[] vectors = new Vector3[vectorCount];

                for (int i = 0; i < vectorCount; i++)
                {
                    vectors[i] = new Vector3(
                        BitConverter.ToSingle(bytes, (i * 12)),
                        BitConverter.ToSingle(bytes, (i * 12) + 4),
                        BitConverter.ToSingle(bytes, (i * 12) + 8)
                    );
                }

                GameObject parentGO = parentNode.GetGameObject();

                //Gameobject creates a transform itself when its created, we only need to get the component.
                if (parentGO != null)
                {
                    this.transform = parentGO.GetComponent<Transform>();
                }

                if (this.transform != null)
                {
                    //Get the node above the parentNode and parent to it.
                    Transform parentTransform = parentNode.ParentNode.GetTransform();

                    //Parent this transform to the object above it, if one exists.
                    if (parentTransform != null)
                    {
                        this.transform.SetParent(parentTransform);
                    }

                    //Set the attributes after we have parented the transform.
                    this.transform.localPosition = vectors[0];
                    this.transform.localRotation = Quaternion.Euler(vectors[1]);
                    this.transform.localScale = vectors[2];
                }

                this.isDeserialized = true;
            }
        }

        public override void Destroy()
        {
            //Destroy transform?
        }
    }
}
