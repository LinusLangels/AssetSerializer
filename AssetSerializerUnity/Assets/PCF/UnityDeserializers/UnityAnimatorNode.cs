using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnityAnimatorNode : UnityComponentNode
    {
        private Animator animator;

        public UnityAnimatorNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.ANIMATOR;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                this.animator = parentNode.GetGameObject().AddComponent<UnityEngine.Animator>();

                ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

                byte[] metaDataBuffer = resource.GetMetaData();
                JObject metaData = JObject.Parse(System.Text.Encoding.UTF8.GetString(metaDataBuffer));

                this.animator.applyRootMotion = metaData["applyRootMotion"].ToObject<bool>();
                UInt32 avatarID = metaData.Value<UInt32>("avatarReferenceID");              

                for (int i = 0; i < this.ChildNodes.Count; i++)
                {
                    UnityNodeBase child = this.ChildNodes[i];

                    ResourceResponse avatarResponse = new ResourceResponse(avatarID, (ResourceResponse response) =>
                    {
                        this.animator.avatar = response.GetAvatarRequest;
                    });

                    child.Deserialize(dataBlocks, this, avatarResponse, postInstallActions, optimizedLoad);
                }

                this.isDeserialized = true;
            }
        }

        public override void Destroy()
        {
            UnityEngine.Object.Destroy(this.animator);

            base.Destroy();
        }
    }
}
