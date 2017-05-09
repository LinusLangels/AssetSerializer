using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnitySerializeAnimator : UnitySerializerBase
    {
        private Animator animator;
		private GameObject parentGO;

        public UnitySerializeAnimator(GameObject parentGO, Animator animator, NodeBase rootNode)
        {
			this.parentGO = parentGO;
            this.animator = animator;
            this.rootNode = rootNode;
        }

		public override void Serialize(SerializedAssets serializedAssets, object[] serializeOptions, NodeBase objNode, List<Action<NodeBase>> postSerializeActions)
        {
            //Make sure this is always 36 characters long.
            this.referenceID = this.rootNode.GenerateID();

            ComponentNode componentNode = new ComponentNode(PCFResourceType.ANIMATOR, referenceID, null);

            //Component nodes must always be parented to objNodes.
            objNode.AddChildNode(componentNode);

            //Serialize child node ( the avatar )
            //We serialize as reference.
			UnitySerializeAvatar avatarReferenceSerializer = new UnitySerializeAvatar(this.animator, this.parentGO, this.rootNode);
			avatarReferenceSerializer.Serialize(serializedAssets, serializeOptions, componentNode, postSerializeActions);

            JObject metaData = new JObject();

            //Serialize apply root motion variable in the metadata.
            metaData["applyRootMotion"] = this.animator.applyRootMotion;

            //Serialize avatarReference in the metadata.
            metaData["avatarReferenceID"] = avatarReferenceSerializer.GetReferenceID();

            //Create serialized asset by converting data to a bytearray and give it to the constructor.
            AssetResource resource = new AssetResource(false);

            byte[] metaDataBuffer = System.Text.Encoding.UTF8.GetBytes(metaData.ToString(Formatting.None));
            resource.Serialize(referenceID, MetaDataType.JSON, metaDataBuffer, null);

            serializedAssets.AddResource(referenceID, PCFResourceType.ANIMATOR, resource);

            //Nodes store their resource when serializing
            componentNode.SetSerializer(this);
        }
    }
}