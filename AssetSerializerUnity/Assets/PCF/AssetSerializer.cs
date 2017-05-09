using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PCFFileFormat.Debugging;

namespace PCFFileFormat
{
    public class AssetSerializer
    {
        private GameObject selectedGameObject;
        private NodeBase rootNode;

        private string destinationDirectory;

        public AssetSerializer(GameObject selectedGameObject, string destinationDirectory)
        {
            this.destinationDirectory = destinationDirectory;
            this.selectedGameObject = selectedGameObject;
            this.rootNode = new RootNode("root");
        }

		public Dictionary<PCFResourceType, DataBlockBase> Serialize(object[] serializeOptions)
        {
            //Holds and indexes all serialized assets/resources.
            SerializedAssets serializedAssets = new SerializedAssets(this.destinationDirectory);

            //Allow serialized nodes to define actions to do after the entire tree has been serialized.
            List<Action<NodeBase>> postSerializeActions = new List<Action<NodeBase>>();

            //Populate dataBlocks with stuff.
			SerializeRecursivly(this.selectedGameObject, serializedAssets, serializeOptions, this.rootNode, postSerializeActions);

            //Run all post serialize actions.
            foreach (Action<NodeBase> action in postSerializeActions)
            {
                action(this.rootNode);
            }

            //Tracking object, used to debug what happens during serialization.
            ISerializeLogging logger = new NodeCreationDebugger();

            //Recursivly create serialized representations of node tree. (Aka NodeResource objects)
            NodeResource serializedRootNode = this.rootNode.Serialize(serializedAssets, null, logger);

            //Lets see...
            logger.PrintResult();

            Dictionary<PCFResourceType, DataBlockBase> dataBlocks = serializedAssets.GetDataBlocks();

            //Create nodeblock and set it to the datablocks dictionary.
            DataBlockBase nodeBlock = new NodeBlock(serializedRootNode);
            dataBlocks.Add(PCFResourceType.NODE, nodeBlock);

            return dataBlocks;
        }

		void SerializeRecursivly(GameObject go, SerializedAssets serializedAssets, object[] serializeOptions, NodeBase node, List<Action<NodeBase>> postSerializeActions)
        {
            //Serialize an abstract representation of the node tree.
            NodeBase objNode = new ObjectNode(go.name, this.rootNode);

            //Parent objNode to ParentNode, ParentNode is always an ObjectNode. (Gameobject if you will)
            node.AddChildNode(objNode);

            Component[] objectComponents = go.GetComponents(typeof(Component));

            //Some resources will serialize subresources such as materials, lightprobes and textures, they will become children of their NodeComponents.
            for (int i = 0; i < objectComponents.Length; i++)
            {
                if (objectComponents[i] is SkinnedMeshRenderer)
                {
                    UnitySerializeSkinnedMesh skinnedMeshSerializer = new UnitySerializeSkinnedMesh(go, objectComponents[i] as SkinnedMeshRenderer, this.rootNode);
					skinnedMeshSerializer.Serialize(serializedAssets, serializeOptions, objNode, postSerializeActions);
                }
                else if (objectComponents[i] is MeshRenderer)
                {
                    UnitySerializeMesh meshSerializer = new UnitySerializeMesh(go, objectComponents[i] as MeshRenderer, this.rootNode);
					meshSerializer.Serialize(serializedAssets, serializeOptions, objNode, postSerializeActions);
                }
                else if (objectComponents[i] is MonoBehaviour)
                {
                    UnitySerializeScript scriptSerializer = new UnitySerializeScript(go, objectComponents[i] as MonoBehaviour, this.rootNode);
					scriptSerializer.Serialize(serializedAssets, serializeOptions, objNode, postSerializeActions);
                }
                else if (objectComponents[i] is Transform)
                {
                    UnitySerializeTransform transformSerializer = new UnitySerializeTransform(go, objectComponents[i] as Transform, this.rootNode);
					transformSerializer.Serialize(serializedAssets, serializeOptions, objNode, postSerializeActions);
                }
                else if (objectComponents[i] is Animator)
                {
                    UnitySerializeAnimator avtarSerializer = new UnitySerializeAnimator(go, objectComponents[i] as Animator, this.rootNode);
					avtarSerializer.Serialize(serializedAssets, serializeOptions, objNode, postSerializeActions);
                }
                else if (objectComponents[i] is Animation)
                {
                    UnitySerializeAnimation animationSerializer = new UnitySerializeAnimation(go, objectComponents[i] as Animation, this.rootNode);
					animationSerializer.Serialize(serializedAssets, serializeOptions, objNode, postSerializeActions);
                }
                else if (objectComponents[i] is Camera)
                {
                    UnitySerializeCamera cameraSerializer = new UnitySerializeCamera(go, objectComponents[i] as Camera, this.rootNode);
					cameraSerializer.Serialize(serializedAssets, serializeOptions, objNode, postSerializeActions);
                }
                else if (objectComponents[i] is LightProbeGroup)
                {
                    UnitySerializeLightProbes lightprobeSerializer = new UnitySerializeLightProbes(go, LightmapSettings.lightProbes, this.rootNode);
					lightprobeSerializer.Serialize(serializedAssets, serializeOptions, objNode, postSerializeActions);
                }
                else if (objectComponents[i] is Collider)
                {
                    UnitySerializeCollider colliderSerializer = new UnitySerializeCollider(go, objectComponents[i] as Collider, this.rootNode);
					colliderSerializer.Serialize(serializedAssets, serializeOptions, objNode, postSerializeActions);
                }
                else if (objectComponents[i] is Light)
                {
                    UnitySerializeLight lightSerializer = new UnitySerializeLight(go, objectComponents[i] as Light, this.rootNode);
					lightSerializer.Serialize(serializedAssets, serializeOptions, objNode, postSerializeActions);
                }
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                GameObject child = go.transform.GetChild(i).gameObject;
				SerializeRecursivly(child, serializedAssets, serializeOptions, objNode, postSerializeActions);
            }
        }
    }
}
