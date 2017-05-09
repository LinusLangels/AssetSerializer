using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnityCameraNode : UnityComponentNode
    {
        private Camera camera;

        public UnityCameraNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.CAMERA;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                this.camera = parentNode.GetGameObject().AddComponent<Camera>();

                ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

                byte[] bytes = resource.GetResourceData();

                int count = (bytes.Length / 4);
                float[] data = new float[count];

                for (int i = 0; i < count; i++)
                {
                    data[i] = BitConverter.ToSingle(bytes, (i * 4));
                }

                camera.backgroundColor = new Color(data[0], data[1], data[2], data[3]);
                camera.fieldOfView = data[4];
                camera.aspect = data[5];

                this.isDeserialized = true;
            }
        }

        public override void Destroy()
        {
            UnityEngine.Object.Destroy(this.camera);

            base.Destroy();
        }
    }
}
