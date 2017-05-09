using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnityInternalBundleNode : UnityComponentNode
    {
        static string currentPlatform;

        private AssetBundle internalBundle;

        public UnityInternalBundleNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.INTERNALBUNDLE;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                ResourceBlock dataBlock = dataBlocks[this.resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

				ResourceResponse request = resourceResponse.CanHandle(GetReferenceID());
				if (request != null)
				{
					byte[] bundleBytes = resource.GetResourceData();
					this.internalBundle = AssetBundle.LoadFromMemory(bundleBytes);

					request.HandleAssetBundleResponse(this.internalBundle);
				}

                this.isDeserialized = true;
            }
        }

        public override void Destroy()
        {
            base.Destroy();

            //Unload internal bundle.
            if (this.internalBundle != null)
            {
                this.internalBundle.Unload(true);
            }
        }
    }
}

