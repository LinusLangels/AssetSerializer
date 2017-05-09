using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat
{
	public class AvatarBoneTransform
	{
		private string name;
		private Transform transform;
		private Vector3 localPosition;
		private Quaternion localRotation;
		private Vector3 localScale;
		private HumanBone humanBone;
		private SkeletonBone skeletonBone;
		private bool hasHumanMapping;

		public AvatarBoneTransform(Transform t)
		{
			this.name = t.name;
			this.transform = t;
			this.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, t.localPosition.z);
			this.localRotation = new Quaternion(t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w);
			this.localScale = new Vector3(t.localScale.x, t.localScale.y, t.localScale.z);
			this.hasHumanMapping = false;
		}

		public void SetSkeletonBone(SkeletonBone bone)
		{
			this.skeletonBone = bone;
		}

		public void SetHumanBone(HumanBone bone)
		{
			this.humanBone = bone;
			this.hasHumanMapping = true;
		}

		public string GetName()
		{
			return this.name;
		}

		public Vector3 GetPosition()
		{
			return this.localPosition;
		}

		public Vector3 GetScale()
		{
			return this.localScale;
		}

		public Quaternion GetRotation()
		{
			return this.localRotation;
		}

		public JObject Serialize()
		{
			JObject serializedBone = new JObject();

			if (!string.IsNullOrEmpty(this.humanBone.humanName))
			{
				JObject hBone = new JObject();
				hBone["humanName"] = this.humanBone.humanName;
				hBone["boneName"] = this.name;
				hBone["useDefaultValues"] = true;

				serializedBone["humanBone"] = hBone;
			}

			JObject sBone = new JObject();
			sBone["name"] = this.name;
			sBone["position"] = new JArray() { this.localPosition.x, this.localPosition.y, this.localPosition.z };
			sBone["rotation"] = new JArray() { this.localRotation.x, this.localRotation.y, this.localRotation.z, this.localRotation.w };
			sBone["scale"] = new JArray() { this.localScale.x, this.localScale.y, this.localScale.z };
		
			serializedBone["skeletonBone"] = sBone;

			return serializedBone;
		}

		public HumanBone GetHumanBone()
		{
			return this.humanBone;
		}

		public bool IsHumanMapped()
		{
			return this.hasHumanMapping;
		}

		public SkeletonBone GetSkeletonBone()
		{
			return this.skeletonBone;
		}
	}

	public class AvatarSerializeOpts {

		private string filter;
		private Dictionary<string, string> mapping;

		public AvatarSerializeOpts (string filter) 
		{
			this.filter = filter;
			this.mapping = new Dictionary<string, string>();

			//Spine
			mapping.Add("Mid_Spine_Jnt_00", "Hips");
			mapping.Add("Mid_Spine_Jnt_01", "Spine");
			mapping.Add("Mid_Spine_Jnt_02", "Chest");

			//Left arm
			mapping.Add("L_Arm_Jnt_00", "LeftShoulder");
			mapping.Add("L_Arm_Jnt_01", "LeftUpperArm");
			mapping.Add("L_Arm_Jnt_02", "LeftLowerArm");
			mapping.Add("L_Hand_Jnt_00", "LeftHand");

			//Right arm
			mapping.Add("R_Arm_Jnt_00", "RightShoulder");
			mapping.Add("R_Arm_Jnt_01", "RightUpperArm");
			mapping.Add("R_Arm_Jnt_02", "RightLowerArm");
			mapping.Add("R_Hand_Jnt_00", "RightHand");

			//Left leg
			mapping.Add("L_Leg_Jnt_00", "LeftUpperLeg");
			mapping.Add("L_Leg_Jnt_01", "LeftLowerLeg");
			mapping.Add("L_Leg_Jnt_02", "LeftFoot");
			mapping.Add("L_Leg_Jnt_03", "LeftToe");

			//Right leg
			mapping.Add("R_Leg_Jnt_00", "RightUpperLeg");
			mapping.Add("R_Leg_Jnt_01", "RightLowerLeg");
			mapping.Add("R_Leg_Jnt_02", "RightFoot");
			mapping.Add("R_Leg_Jnt_03", "RightToe");

			//Head
			mapping.Add("Mid_Head_Jnt_00", "Neck");
			mapping.Add("Mid_Head_Jnt_01", "Head");

			//Left hand
			mapping.Add("L_Thumb_Jnt_00", "Left Thumb Proximal");
			mapping.Add("L_Thumb_Jnt_01", "Left Thumb Intermediate");
			mapping.Add("L_Thumb_Jnt_02", "Left Thumb Distal");
			mapping.Add("L_Index_Jnt_00", "Left Index Proximal");
			mapping.Add("L_Index_Jnt_01", "Left Index Intermediate");
			mapping.Add("L_Index_Jnt_02", "Left Index Distal");
			mapping.Add("L_Middle_Jnt_00", "Left Middle Proximal");
			mapping.Add("L_Middle_Jnt_01", "Left Middle Intermediate");
			mapping.Add("L_Middle_Jnt_02", "Left Middle Distal");
			mapping.Add("L_Ring_Jnt_00", "Left Ring Proximal");
			mapping.Add("L_Ring_Jnt_01", "Left Ring Intermediate");
			mapping.Add("L_Ring_Jnt_02", "Left Ring Distal");
			mapping.Add("L_Pinky_Jnt_00", "Left Little Proximal");
			mapping.Add("L_Pinky_Jnt_01", "Left Little Intermediate");
			mapping.Add("L_Pinky_Jnt_02", "Left Little Distal");

			//Right hand
			mapping.Add("R_Thumb_Jnt_00", "Right Thumb Proximal");
			mapping.Add("R_Thumb_Jnt_01", "Right Thumb Intermediate");
			mapping.Add("R_Thumb_Jnt_02", "Right Thumb Distal");
			mapping.Add("R_Index_Jnt_00", "Right Index Proximal");
			mapping.Add("R_Index_Jnt_01", "Right Index Intermediate");
			mapping.Add("R_Index_Jnt_02", "Right Index Distal");
			mapping.Add("R_Middle_Jnt_00", "Right Middle Proximal");
			mapping.Add("R_Middle_Jnt_01", "Right Middle Intermediate");
			mapping.Add("R_Middle_Jnt_02", "Right Middle Distal");
			mapping.Add("R_Ring_Jnt_00", "Right Ring Proximal");
			mapping.Add("R_Ring_Jnt_01", "Right Ring Intermediate");
			mapping.Add("R_Ring_Jnt_02", "Right Ring Distal");
			mapping.Add("R_Pinky_Jnt_00", "Right Little Proximal");
			mapping.Add("R_Pinky_Jnt_01", "Right Little Intermediate");
			mapping.Add("R_Pinky_Jnt_02", "Right Little Distal");
		}

		public virtual JObject SerializeAvatar(GameObject parentGO, Animator animator)
		{
			JObject serializedAvatar = new JObject();
			serializedAvatar["avatarName"] = animator.avatar.name;
			serializedAvatar["sharedAvatar"] = false;
			serializedAvatar["rootBone"] = parentGO.name;

			List<AvatarBoneTransform> boneTransforms = new List<AvatarBoneTransform>();

			Transform searchChain = FindTransformInHierarchy(this.filter, parentGO.transform);

			//Add main root parent
			boneTransforms.Add(new AvatarBoneTransform(searchChain.parent));

			GetTransformHierarchy(searchChain, boneTransforms);

			JArray serializedBoneMappings = new JArray();
			int humanBones = 0;
			int skeletonBones = 0;

			foreach (AvatarBoneTransform bone in boneTransforms)
			{
				string name = bone.GetName();

				if (this.mapping.ContainsKey(name))
				{
					HumanBone humanBone = new HumanBone();
					humanBone.boneName = name;
					humanBone.humanName = this.mapping[name];
					humanBone.limit.useDefaultValues = true;

					bone.SetHumanBone(humanBone);
					humanBones++;
				}
				else
				{
					Debug.Log("No human bone mapping for: " + name);
				}

				SkeletonBone skeletonBone = new SkeletonBone();
				skeletonBone.name = name;
				skeletonBone.rotation = bone.GetRotation();
				skeletonBone.position = bone.GetPosition();
				skeletonBone.scale = bone.GetScale();

				bone.SetSkeletonBone(skeletonBone);
				skeletonBones++;

				//Serialize each mapped avatar bone to JSON so we can recreate avatar at runtime.
				serializedBoneMappings.Add(bone.Serialize());
			}

			Debug.Log("Mapped: " + humanBones + " Human bones");
			Debug.Log("Indexed: " + skeletonBones + " Skeleton bones");

			serializedAvatar["mappedBones"] = serializedBoneMappings;

			return serializedAvatar;
		}

		static void GetTransformHierarchy(Transform transform, List<AvatarBoneTransform> boneTransforms)
		{
			//Only map bones without mesh transforms (pure transforms)
			if (transform.GetComponent<MeshFilter>() == null)
			{
				//Prune child chain based on filter
				boneTransforms.Add(new AvatarBoneTransform(transform));
			}

			foreach (Transform child in transform)
			{
				GetTransformHierarchy(child, boneTransforms);
			}
		}

		static Transform FindTransformInHierarchy(string name, Transform current)
		{
			if (current.name == name)
			{
				return current;
			}
			else
			{
				for (int i = 0; i < current.childCount; ++i)
				{
					Transform found = FindTransformInHierarchy(name, current.GetChild(i));

					if (found != null)
					{
						return found;
					}
				}
			}

			return null;
		}
	}
}
