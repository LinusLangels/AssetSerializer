using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PCFFileFormat
{
    public class SerializedAssets
    {
        private Dictionary<PCFResourceType, DataBlockBase> dataBlocks;
        private string destinationDirectory;

        public SerializedAssets(string destinationDirectory)
        {
            this.destinationDirectory = destinationDirectory;
            this.dataBlocks = new Dictionary<PCFResourceType, DataBlockBase>();
        }

        public string GetDestinationDirectory()
        {
            return this.destinationDirectory;
        }

        public void AddResource(uint refID, PCFResourceType resourceType, AssetResource resource)
        {
            if (this.dataBlocks.ContainsKey(resourceType))
            {
                ResourceBlock dataBlock = this.dataBlocks[resourceType] as ResourceBlock;
                dataBlock.AddResource(refID, resource);
            }
            else
            {
                ResourceBlock block = new ResourceBlock(resourceType);
                block.AddResource(refID, resource);

                this.dataBlocks.Add(resourceType, block);
            }
        }

        public Dictionary<PCFResourceType, DataBlockBase> GetDataBlocks()
        {
            return this.dataBlocks;
        }
    }
}
