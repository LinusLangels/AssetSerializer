using UnityEngine;
using System.Collections;
using System;
using System.IO;

namespace PCFFileFormat
{
	public class GenericSerializerAttribute : PCFSerializeAttribute
	{
		public GenericSerializerAttribute() : base()
		{
		}

		public override void SerializePCF(GameObject go, string outputDirectory, Action<bool, string> OnComplete)
		{
			Debug.Log("serialize generic object to directory: " + outputDirectory);

			DirectoryInfo outDir = new DirectoryInfo(outputDirectory);
			if (!outDir.Exists)
			{
				outDir.Create();
			}

			//These export objects are custom to our project, override if specific behaviour needed.
			object[] opts = new object[4];
			opts[0] = new TextureSerializeOpts(TexturePackageOptions.ASTCPackage);
			opts[1] = new AvatarSerializeOpts("Transform");
			opts[2] = new AudioSerializeOpts();
			opts[3] = new LightprobeSerializeOpts(Path.Combine(Application.dataPath, "LightingData"));

			string outputFile = Path.Combine(outputDirectory, go.name + ".pcf");

			ExportUtils.AddVersionInfo(go);
			SerializeContent(go, opts, outputDirectory, outputFile, PCFfileType.Unknown);
			ExportUtils.AddChecksum(outputFile);

			OnComplete(true, "Success exporting content: " + go.name);
		}
	}
}
