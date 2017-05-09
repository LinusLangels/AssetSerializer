using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class NodeFactory
    {
        private Dictionary<PCFResourceType, Func<string, PCFResourceType, uint, UnityNodeBase>> customCreators;

        public NodeFactory(
            Dictionary<PCFResourceType, Func<string, PCFResourceType, uint, UnityNodeBase>> customCreators)
        {
            this.customCreators = customCreators;
        }

        public UnityNodeBase CreateNodeImplementation(string nodeName, PCFResourceType resourceType, UInt32 referenceID)
        {
            UnityNodeBase recreatedNode = null;

            if (this.customCreators != null && this.customCreators.ContainsKey(resourceType))
            {
                recreatedNode = this.customCreators[resourceType](nodeName, resourceType, referenceID);
            }
            else
            {
                switch (resourceType)
                {
                    case PCFResourceType.ROOT:
                        recreatedNode = new UnityRootNode(nodeName, referenceID);
                        break;
                    case PCFResourceType.OBJECT:
                        recreatedNode = new UnityObjectNode(nodeName, referenceID);
                        break;
                    case PCFResourceType.TRANSFORM:
                        recreatedNode = new UnityTransformNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.TRANSFORMPOINTER:
                        recreatedNode = new UnityTransformPointerNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.MESH:
                        recreatedNode = new UnityMeshNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.ANIMATOR:
                        recreatedNode = new UnityAnimatorNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.AUDIO:
                        recreatedNode = new UnityAudioNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.AVATAR:
                        recreatedNode = new UnityAvatarReferenceNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.SKINNEDMESH:
                        recreatedNode = new UnitySkinnedMeshNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.SCRIPT:
                        recreatedNode = new UnityScriptNode(nodeName, resourceType, referenceID, null);
                        break;
                    case PCFResourceType.MATERIAL:
                        recreatedNode = new UnityMaterialNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.MATERIALPOINTER:
                        recreatedNode = new UnityMaterialPointerNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.TEXTURE:
                        recreatedNode = new UnityTextureNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.CAMERA:
                        recreatedNode = new UnityCameraNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.LIGHT:
                        recreatedNode = new UnityLightNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.LIGHTPROBES:
                        recreatedNode = new UnityLightProbesNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.COLLIDER:
                        recreatedNode = new UnityColliderNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.PRIMITIVE:
                        recreatedNode = new UnityPrimitiveNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.COLLECTION:
                        recreatedNode = new UnityCollectionNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.POINTERCOLLECTION:
                        recreatedNode = new UnityPointerCollectionNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.CLASS:
                        recreatedNode = new UnityClassNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.INTERNALBUNDLE:
                        recreatedNode = new UnityInternalBundleNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.GRADIENT:
                        recreatedNode = new UnityGradientNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.ANIMATION:
                        recreatedNode = new UnityAnimationNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.ANIMATIONCLIP:
                        recreatedNode = new UnityAnimationClipNode(nodeName, resourceType, referenceID);
                        break;
                    case PCFResourceType.ANIMATIONCLIPREFERENCE:
                        recreatedNode = new UnityAnimationClipPointerNode(nodeName, resourceType, referenceID);
                        break;
                }
            }


            return recreatedNode;
        }
    }
}
