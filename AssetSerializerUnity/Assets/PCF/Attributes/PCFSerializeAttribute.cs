using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <Summary>
/// PCFSerializeAttribute
/// 
/// Description: Apply this attribute to classes or structs that you want to act as control objects for export
/// Example: You have a character asset, u want to have custom control of serialization.
/// Add a Monobehaviour control script to that asset and apply a serialization attribute.
/// You can inerhit this attribute and create even more customized export behaviour.
/// 
/// All meta files is loaded when the application is started.
/// </Summary>

namespace PCFFileFormat
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
	public class PCFSerializeAttribute : Attribute {

		public PCFSerializeAttribute()
		{
		}

		public virtual void SerializePCF(GameObject go, string outputDirectory, Action<bool, string> OnComplete)
		{
			Debug.LogError("Default serializer does nothing, please specify your own!");
		}

		protected void SerializeContent(GameObject go, object[] serializeOptions, string outputDirectory, string outputPath, PCFfileType contentFileType)
		{
			AssetSerializer serializedObject = new AssetSerializer(go, outputDirectory);
			Dictionary<PCFResourceType, DataBlockBase> dataBlocks = serializedObject.Serialize(serializeOptions);

			IFileHandle fileHandle = new NormalFileHandle(outputPath);
			PCFFile file = new PCFFile(fileHandle, false, contentFileType);

			file.AddDataBlocks(dataBlocks);
			file.SaveFile();
		}
	}
}
