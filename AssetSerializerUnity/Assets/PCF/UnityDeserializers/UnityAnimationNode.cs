using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PCFFileFormat
{
    public class UnityAnimationNode : UnityComponentNode
    {
        public UnityAnimationNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.ANIMATION;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                Type scriptType = typeof(Animation);

                if (scriptType != null)
                {
                    Animation animation = parentNode.GetGameObject().AddComponent(scriptType) as Animation;

					PropertySetter clipSetter = new PropertySetter("clip", (System.Object val) => { animation.clip = val as AnimationClip; });

					List<PropertySetter> customValueSetters = new List<PropertySetter>();
					customValueSetters.Add(clipSetter);

					FieldDeserializer fieldDeserializer = new FieldDeserializer(customValueSetters, animation);
                    
                    ResourceResponse response = new ResourceResponse();
                    response.SetFieldDeserializer(fieldDeserializer);

                    foreach (UnityNodeBase node in this.ChildNodes)
                    {
                        node.Deserialize(dataBlocks, this, response, postInstallActions, optimizedLoad);
                    }
                }

                this.isDeserialized = true;
            }
        }
    }
}


