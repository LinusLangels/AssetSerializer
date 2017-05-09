using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnitySkinnedMeshNode : UnityComponentNode
    {
        private SkinnedMeshRenderer skinnedMesh;
        private Mesh mesh;

        public UnitySkinnedMeshNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.SKINNEDMESH;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            GameObject parentGameObject = parentNode.GetGameObject();
            
            this.skinnedMesh = parentGameObject.AddComponent<SkinnedMeshRenderer>();

            ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
            AssetResource resource = dataBlock.GetResource(this.referenceID);

            byte[] metaDataBuffer = resource.GetMetaData();
            JObject metaData = JObject.Parse(System.Text.Encoding.UTF8.GetString(metaDataBuffer));

            UInt32 materialID = metaData.Value<UInt32>("materialID");
            string rootBoneName = metaData.Value<string>("rootBone");
            string probeBoneName = metaData.Value<string>("probeAnchor");
            int quality = metaData.Value<int>("quality");
            string[] boneNames = metaData.Value<JArray>("bones").ToObject<string[]>();
            float[] blendShapeWeights = metaData.Value<JArray>("blendShapeWeights").ToObject<float[]>();

            // Add a post install action so the bones will have time to be created.
            postInstallActions.Add((UnityNodeBase node) => {

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                Transform[] bones = new Transform[boneNames.Length];

                //Mapp all bone transform in the hierarchy.
                Dictionary<string, Transform> bonesMapping = new Dictionary<string, Transform>();
                FindTransformsInHierarchy(node, bonesMapping);

                for (int i = 0; i < boneNames.Length; i++)
                {
                    string name = boneNames[i];

                    if (bonesMapping.ContainsKey(name))
                    {
                        bones[i] = bonesMapping[name];
                    }
                    else
                    {
                        bones[i] = null;
                    }
                }                
                
                this.mesh = MeshSerializeUtilities.ReadMesh(resource.GetResourceData(), true);

                if (optimizedLoad)
                {
                    //Free up mono memory for this mesh, now owned by the GPU.
                    this.mesh.UploadMeshData(true);
                }

                this.skinnedMesh.sharedMesh = this.mesh;
                this.skinnedMesh.bones = bones;            
                this.skinnedMesh.quality = (SkinQuality)Enum.ToObject(typeof(SkinQuality), quality);

                for (int i = 0; i < blendShapeWeights.Length; i++)
                {
                    this.skinnedMesh.SetBlendShapeWeight(i, blendShapeWeights[i]);
                }

                if (bonesMapping.ContainsKey(rootBoneName))
                {
                    this.skinnedMesh.rootBone = bonesMapping[rootBoneName];
                }
                if(probeBoneName != null)
                {
                    this.skinnedMesh.probeAnchor = bonesMapping[probeBoneName];
                }

                watch.Stop();
                //Debug.Log("time to deserialize skinned mesh: " + watch.ElapsedMilliseconds);
            });

            for (int i = 0; i < this.ChildNodes.Count; i++)
            {
                UnityNodeBase child = this.ChildNodes[i];

                //Create a request to be send down the chain of children, see if any child will handle it.
                ResourceResponse materialRequest = new ResourceResponse(materialID, (ResourceResponse response) => {
                    skinnedMesh.material = response.GetMaterialRequest;
                });

                child.Deserialize(dataBlocks, this, materialRequest, postInstallActions, optimizedLoad);
            }
        }

        private void FindTransformsInHierarchy(UnityNodeBase node, Dictionary<string, Transform> mapping)
        {
            if (node.GetResourceType() == PCFResourceType.TRANSFORM)
            {
                Transform trans = node.GetTransform();
                mapping.Add(node.GetName(), trans);
            }

            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                UnityNodeBase child = node.ChildNodes[i];
                PCFResourceType type = child.GetResourceType();

                //Optimize search by not going into subtress that do not contain bones.
                if (type == PCFResourceType.ROOT || type == PCFResourceType.OBJECT || type == PCFResourceType.TRANSFORM)
                {
                    FindTransformsInHierarchy(child, mapping);
                }
            }
        }

        public override void SetActive(bool toggle)
        {
            this.skinnedMesh.enabled = toggle;
        }

        public override void Destroy()
        {
            if (this.mesh != null)
            {
                UnityEngine.Object.Destroy(this.mesh);
            }

            if (this.skinnedMesh != null)
            {
                UnityEngine.Object.Destroy(this.skinnedMesh);
            }

            base.Destroy();
        }
    }
}
