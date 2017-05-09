using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using PCFFileFormat.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat
{
    public class AnimationClipSerialization : UnitySerializerBase
    {
        private UnityEngine.AnimationClip animationClip;

        public AnimationClipSerialization(System.Object value, string fieldName, bool arrayItem, NodeBase rootNode)
        {
            this.animationClip = value as UnityEngine.AnimationClip;
            this.fieldName = fieldName;
            this.arrayItem = arrayItem;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

			#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(animationClip);
            NodeBase animationNode = CheckSharedAnimationClip(this.rootNode, assetPath);

            if (animationNode != null)
            {
                UInt32 animationID = animationNode.GetReferenceID();

                ComponentNode componentNode = new ComponentNode(PCFResourceType.ANIMATIONCLIPREFERENCE, referenceID, null, typeof(UnityEngine.AnimationClip).Name.ToString());

                objNode.AddChildNode(componentNode);

                byte[] bytes = BitConverter.GetBytes(animationID);

                JObject metaData = new JObject();
                metaData["fieldName"] = this.fieldName;

                AssetResource resource = new AssetResource(false);

                byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));

                resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, bytes);

                serializedAssets.AddResource(referenceID, PCFResourceType.ANIMATIONCLIPREFERENCE, resource);

                this.pointedID = animationID;
            }
            else
            {
                ComponentNode componentNode = new ComponentNode(PCFResourceType.ANIMATIONCLIP, referenceID, assetPath, typeof(UnityEngine.AnimationClip).Name.ToString());

                objNode.AddChildNode(componentNode);

                EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(animationClip);

                AnimationCurve initialCurve = AnimationUtility.GetEditorCurve(animationClip, curveBindings[0]);
                SerializedAnimationClip serializedAnimationClip = new SerializedAnimationClip();

                serializedAnimationClip.PostWrapMode = (int)initialCurve.postWrapMode;
                serializedAnimationClip.PreWrapMode = (int)initialCurve.preWrapMode;

                for (int i = 0; i < curveBindings.Length; i++)
                {
                    string propertyName = curveBindings[i].propertyName;

                    AnimationCurve animCurve = AnimationUtility.GetEditorCurve(animationClip, curveBindings[i]);

                    SerializedAnimationKeyFrame[] serializedKeyFrames = new SerializedAnimationKeyFrame[animCurve.keys.Length];
                    for(int j = 0; j < animCurve.keys.Length; j++)
                    {
                        serializedKeyFrames[j] = new SerializedAnimationKeyFrame();

                        serializedKeyFrames[j].InTagent = animCurve.keys[j].inTangent;
                        serializedKeyFrames[j].OutTangent = animCurve.keys[j].outTangent;
                        serializedKeyFrames[j].Time = animCurve.keys[j].time;
                        serializedKeyFrames[j].Value = animCurve.keys[j].value;
                    }
                    
                    serializedAnimationClip.AddChannel(AnimationClipUtils.GetAnimationClipChannelName(propertyName), serializedKeyFrames);                          
                }

                JObject metaData = new JObject();
                metaData["name"] = animationClip.name;
                metaData["frameRate"] = animationClip.frameRate;
                metaData["wrapMode"] = (int)animationClip.wrapMode;
                metaData["legacy"] = animationClip.legacy;
                metaData["fieldName"] = this.fieldName;

                byte[] data = ProtocolBufferSerializer.SerializeAnimationClipData(serializedAnimationClip);
                                
                AssetResource resource = new AssetResource(false);

                byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));
                resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, data);

                serializedAssets.AddResource(referenceID, PCFResourceType.ANIMATIONCLIP, resource);

                //Nodes store their resource when serializing
                componentNode.SetSerializer(this);

                //Make sure caller can get the actual material referenceID when serializing its metadata.
                this.pointedID = this.referenceID;
            }
			#endif
        }

        NodeBase CheckSharedAnimationClip(NodeBase node, string resourcePath)
        {
            string path = node.GetResourceIndentifier();
            if (!string.IsNullOrEmpty(path))
            {
                if (string.CompareOrdinal(path, resourcePath) == 0)
                {
                    return node;
                }
            }

            List<NodeBase> children = node.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                NodeBase foundChild = CheckSharedAnimationClip(children[i], resourcePath);

                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }
    }
}
