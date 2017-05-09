using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnityScriptNode : UnityComponentNode
    {
        private MonoBehaviour script;
        private List<string> scriptMask;

        public UnityScriptNode(string name, PCFResourceType resourceType, UInt32 referenceID, List<string> scriptMask) : base(name, resourceType, referenceID)
        {
            this.scriptMask = scriptMask;
            this.resourceType = PCFResourceType.SCRIPT;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

                byte[] metaDataBuffer = resource.GetMetaData();
                JObject metaData = JObject.Parse(System.Text.Encoding.UTF8.GetString(metaDataBuffer));

                string scriptName = metaData["scriptname"].ToString();
                Type scriptType = null;

                if (scriptMask == null)
                {
                    scriptType = Type.GetType(scriptName);
                }
                else if (this.scriptMask != null && this.scriptMask.Contains(scriptName))
                {
                    scriptType = Type.GetType(scriptName);
                }
                
                if (scriptType != null)
                {
                    this.script = parentNode.GetGameObject().AddComponent(scriptType) as MonoBehaviour;
                    FieldDeserializer fieldDeserializer = new FieldDeserializer(scriptType.GetFields(), this.script);

                    ResourceResponse response = new ResourceResponse();
                    response.SetFieldDeserializer(fieldDeserializer);

                    foreach (UnityNodeBase node in this.ChildNodes)
                    {
                        node.Deserialize(dataBlocks, this, response, postInstallActions, optimizedLoad);
                    }
                }
               
                this.isDeserialized = true;
            }
        }
    }
}
