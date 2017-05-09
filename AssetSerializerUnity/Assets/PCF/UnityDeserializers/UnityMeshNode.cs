using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnityMeshNode : UnityComponentNode
    {
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh mesh;

        public UnityMeshNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.MESH;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                GameObject parentGameObject = parentNode.GetGameObject();

                this.meshFilter = parentGameObject.AddComponent<MeshFilter>();
                this.meshRenderer = parentGameObject.AddComponent<MeshRenderer>();

                ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

                byte[] metaDataBuffer = resource.GetMetaData();
                JObject metaData = JObject.Parse(System.Text.Encoding.UTF8.GetString(metaDataBuffer));
                UInt32 materialID = metaData.Value<UInt32>("materialID");

                this.mesh = MeshSerializeUtilities.ReadMesh(resource.GetResourceData(), false);
                this.meshFilter.sharedMesh = this.mesh;

                if (optimizedLoad)
                {
                    //Free up cached ram memory for this mesh, disables access to mesh components like verts etc.
                    this.mesh.UploadMeshData(true);
                }

                for (int i = 0; i < this.ChildNodes.Count; i++)
                {
                    UnityNodeBase child = this.ChildNodes[i];

                    //Create a request to be send down the chain of children, see if any child will handle it.
                    ResourceResponse materialRequest = new ResourceResponse(materialID, (ResourceResponse response) => {
                        meshRenderer.material = response.GetMaterialRequest;
                    });

                    child.Deserialize(dataBlocks, this, materialRequest, postInstallActions, optimizedLoad);
                }

                this.isDeserialized = true;
            }

        }

        public override void SetActive(bool toggle)
        {
            if (this.meshRenderer != null)
            {
                this.meshRenderer.enabled = toggle;
            }
        }

        public override void Destroy()
        {
            if (this.mesh != null)
            {
                UnityEngine.Object.Destroy(this.mesh);
            }

            if (this.meshFilter != null)
            {
                UnityEngine.Object.Destroy(this.meshFilter);
            }

            if (this.meshRenderer != null)
            {
                UnityEngine.Object.Destroy(this.meshRenderer);
            }

            base.Destroy();
        }
    }
}
