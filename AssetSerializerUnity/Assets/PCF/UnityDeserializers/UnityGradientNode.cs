using UnityEngine;
using System;
using PCFFileFormat.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace PCFFileFormat
{
	public class UnityGradientNode : UnityComponentNode
	{

		public UnityGradientNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
		{
			this.resourceType = PCFResourceType.GRADIENT;
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
				SerializedFieldData fieldData = ProtocolBufferSerializer.DeserializeFieldData(metaDataBuffer);
				string assemblyName = ProtocolBufferSerializer.GetAssemblyName(fieldData.assemblyType);
				string scriptTypeName = fieldData.typeName;
				string fieldName = fieldData.fieldName;

				Type scriptType = null;

				//Qualify type check with assembly name, GetType only looks in current assembly otherwise.
				if (!string.IsNullOrEmpty(assemblyName))
				{
					scriptType = Type.GetType(scriptTypeName + ", " + assemblyName);
				}
				else
				{
					scriptType = Type.GetType(scriptTypeName);
				}

				if (scriptType != null)
				{
					Gradient gradient = new Gradient();

					PropertySetter colorKeySetter = new PropertySetter("colorKeys", (System.Object val) => { gradient.colorKeys = val as GradientColorKey[]; });
					PropertySetter alphaKeySetter = new PropertySetter("alphaKeys", (System.Object val) => { gradient.alphaKeys = val as GradientAlphaKey[]; });

					List<PropertySetter> customValueSetters = new List<PropertySetter>();
					customValueSetters.Add(colorKeySetter);
					customValueSetters.Add(alphaKeySetter);

					FieldDeserializer fieldDeserializer = new FieldDeserializer(customValueSetters, gradient);
					ResourceResponse response = new ResourceResponse();
					response.SetFieldDeserializer(fieldDeserializer);

					for (int i = 0; i < this.ChildNodes.Count; i++)
					{
						UnityNodeBase child = this.ChildNodes[i];

						child.Deserialize(dataBlocks, this, response, postInstallActions, optimizedLoad);
					}

					if (resourceResponse != null)
					{
						resourceResponse.GetFieldDeserializer.SetField(fieldName, gradient);
					}
				}                      

				this.isDeserialized = true;
			}
		}
	}
}
