using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PCFFileFormat
{
    public class IDGenerator
    {
        private List<UInt32> generatedIDs;

        public IDGenerator()
        {
            this.generatedIDs = new List<UInt32>();
        }

        public UInt32 GenerateID()
        {
            byte[] buffer = Guid.NewGuid().ToByteArray();
            UInt32 id = BitConverter.ToUInt32(buffer, 0);

            if (this.generatedIDs.Contains(id))
            {
                return GenerateID();
            }
            else
            {
                this.generatedIDs.Add(id);
            }

            return id;
        }
    }
}
