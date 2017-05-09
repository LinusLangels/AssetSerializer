using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PCFFileFormat.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnityAnimationClipNode : UnityComponentNode
    {
        AnimationClip animationClip;

        public UnityAnimationClipNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.ANIMATIONCLIP;
        }

        public AnimationClip GetAnimationClip()
        {
            return this.animationClip;
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

                string jsonString = System.Text.Encoding.UTF8.GetString(resource.GetMetaData());
                JObject jsonObject = JObject.Parse(jsonString);

                animationClip = new AnimationClip();
                animationClip.name = jsonObject.Value<string>("name");
                animationClip.frameRate = jsonObject.Value<float>("frameRate");
                animationClip.wrapMode = (UnityEngine.WrapMode)jsonObject.Value<int>("wrapMode");
                animationClip.legacy = true;
                                
                SerializedAnimationClip serializedAnimationClip = ProtocolBufferSerializer.DeserializeAnimationClipData(resource.GetResourceData());
                
                foreach(int key in serializedAnimationClip.AnimationChannels.Keys)
                {
                    SerializedAnimationChannelName channel = (SerializedAnimationChannelName)key;

                    SerializedAnimationKeyFrame[] keyFrames = serializedAnimationClip.GetChannel(channel);

                    AnimationCurve curve = CreateAnimationCurve(serializedAnimationClip.PostWrapMode, serializedAnimationClip.PreWrapMode, keyFrames);
                    
                    animationClip.SetCurve("", typeof(Transform), AnimationClipUtils.GetAnimationClipChannelName(channel), curve);
                }

                string fieldName = jsonObject.Value<string>("fieldName");
                if (resourceResponse != null)
                {
                    resourceResponse.GetFieldDeserializer.SetField(fieldName, animationClip);
                }

                this.isDeserialized = true;
            }
        }

        private AnimationCurve CreateAnimationCurve(int postWrapMode, int preWrapMode, SerializedAnimationKeyFrame[] keyFrames)
        {
            Keyframe[] keys = new Keyframe[keyFrames.Length];

            for(int i = 0; i < keyFrames.Length; i++)
            {
                keys[i] = new Keyframe(keyFrames[i].Time, keyFrames[i].Value, keyFrames[i].InTagent, keyFrames[i].OutTangent);
            }

            AnimationCurve animationCurve = new AnimationCurve(keys);

            animationCurve.postWrapMode = (WrapMode)postWrapMode;
            animationCurve.preWrapMode = (WrapMode)preWrapMode;

            return animationCurve;
        }

        public override void Destroy()
        {
            UnityEngine.Object.Destroy(this.animationClip);

            base.Destroy();
        }
    }
}

