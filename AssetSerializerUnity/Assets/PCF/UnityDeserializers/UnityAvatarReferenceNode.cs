using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat
{
    public class UnityAvatarReferenceNode : UnityComponentNode
    {
        private Avatar avatar;
		private bool sharedAvatar;

        public UnityAvatarReferenceNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.AVATAR;
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

                byte[] metaDataBuffer = resource.GetMetaData();
                JObject metaData = JObject.Parse(System.Text.Encoding.UTF8.GetString(metaDataBuffer));
                string avatarName = metaData.Value<string>("avatarName");
				this.sharedAvatar = bool.Parse(metaData.Value<string>("sharedAvatar"));
				string rootBone = metaData.Value<string>("rootBone");
				JArray mappedBones = metaData.Value<JArray>("mappedBones");

				if (!sharedAvatar)
				{
					//Run after hierarchy is created since we need it when creating a avatar.
					postInstallActions.Add((UnityNodeBase rootNode) => {

						UnityNodeBase skeletonRootNode = FindNodeWithName(rootNode, rootBone);

						if (skeletonRootNode != null)
						{
							//recreate avatar from meta data.
							List<SkeletonBone> skeletonBones = new List<SkeletonBone>(mappedBones.Count);
							List<HumanBone> humanBones = new List<HumanBone>(mappedBones.Count);

							int humanBoneCount = 0;
							int skeletonBoneCount = 0;
							foreach (JObject bone in mappedBones)
							{
								JObject humanBoneData = bone.Value<JObject>("humanBone");

								if (humanBoneData != null)
								{
									HumanBone humanBone = new HumanBone();
									humanBone.humanName = humanBoneData.Value<string>("humanName");
									humanBone.boneName = humanBoneData.Value<string>("boneName");
									humanBone.limit.useDefaultValues = bool.Parse(humanBoneData.Value<string>("useDefaultValues"));

									humanBones.Add(humanBone);
								}

								JObject skeletonBoneData = bone.Value<JObject>("skeletonBone");
								SkeletonBone skeletonBone = new SkeletonBone();
								skeletonBone.name = skeletonBoneData.Value<string>("name");

								float[] posArray = skeletonBoneData.Value<JArray>("position").ToObject<float[]>();
								skeletonBone.position = new Vector3(posArray[0], posArray[1], posArray[2]);

								float[] rotArray = skeletonBoneData.Value<JArray>("rotation").ToObject<float[]>();
								skeletonBone.rotation = new Quaternion(rotArray[0], rotArray[1], rotArray[2], rotArray[3]);

								float[] scaleArray = skeletonBoneData.Value<JArray>("scale").ToObject<float[]>();
								skeletonBone.scale = new Vector3(scaleArray[0], scaleArray[1], scaleArray[2]);

								skeletonBones.Add(skeletonBone);
							}

							HumanDescription desc = new HumanDescription();
							desc.human = humanBones.ToArray();
							desc.skeleton = skeletonBones.ToArray();

							//set the default values for the rest of the human descriptor parameters
							desc.upperArmTwist = 0.5f;
							desc.lowerArmTwist = 0.5f;
							desc.upperLegTwist = 0.5f;
							desc.lowerLegTwist = 0.5f;
							desc.armStretch = 0.05f;
							desc.legStretch = 0.05f;
							desc.feetSpacing = 0.0f;

							this.avatar = AvatarBuilder.BuildHumanAvatar(skeletonRootNode.GetGameObject(), desc);
							this.avatar.name = avatarName;

							ResourceResponse request = resourceResponse.CanHandle(GetReferenceID());
							if (request != null)
							{
								request.HandleAvatarResponse(this.avatar);
							}
						}
						else
						{
							Debug.LogError("Unable to find rootnode for avatar: " + avatarName);
						}
					});
				}
				else
				{
					//Load avatar from resources by name.
					string avatarResourcePath = Path.Combine("Avatars", avatarName);
					this.avatar = Resources.Load(avatarResourcePath) as Avatar;

					ResourceResponse request = resourceResponse.CanHandle(GetReferenceID());
					if (request != null)
					{
						request.HandleAvatarResponse(this.avatar);
					}
				}

                this.isDeserialized = true;
            }
        }

        public override void Destroy()
        {
			if (!this.sharedAvatar)
			{
				Avatar.Destroy(this.avatar);
			}

            base.Destroy();
        }
    }
}
