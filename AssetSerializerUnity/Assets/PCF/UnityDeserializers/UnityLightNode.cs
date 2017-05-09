using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class UnityLightNode : UnityComponentNode
    {
        private Light light;

        public UnityLightNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.LIGHT;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                this.light = parentNode.GetGameObject().AddComponent<Light>();

                ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

                byte[] bytes = resource.GetResourceData();

                int count = (bytes.Length / 4);
                float[] data = new float[count];

                for (int i = 0; i < count; i++)
                {
                    data[i] = BitConverter.ToSingle(bytes, (i * 4));
                }

                light.color = new Color(data[0], data[1], data[2], data[3]);
                light.type = (LightType)Enum.ToObject(typeof(LightType), (int)data[4]);
                light.intensity = data[5];

                this.isDeserialized = true;
            }
        }

        public override void Destroy()
        {
            base.Destroy();

            //Destroy gameobject after all children have destroyed themselves.
            UnityEngine.Object.Destroy(this.light);
        }
    }
}
