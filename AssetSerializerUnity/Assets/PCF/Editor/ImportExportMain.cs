using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Reflection;
using PCFFileFormat.Debugging;

namespace PCFFileFormat.Editor
{
    public class ImportExportMain
    {
        [MenuItem("Assets/PCF/Export PCF")]
        static void ExportPCF()
        {
			//Serialize all selected objects in sequence.
			ExportAsset(Selection.gameObjects);
        }

		static void ExportAsset(GameObject[] selectedObjects)
        {
            string path = EditorUtility.SaveFolderPanel("Save scene", Directory.GetCurrentDirectory() + "/Assets/Bundles/Characters", "");

            if (path == null || path.Length < 1)
                return;

            List<string> successes = new List<string>();
            List<string> failures = new List<string>();

            foreach (GameObject selectedObject in selectedObjects)
            {
				MonoBehaviour[] behaviours = selectedObject.GetComponents<MonoBehaviour>();
				PCFSerializeAttribute attrib = null;

				foreach (MonoBehaviour behaviour in behaviours)
				{
					System.Reflection.MemberInfo info = behaviour.GetType();

					if (info != null)
					{
						object[] attributes = info.GetCustomAttributes(true);
						for (int i = 0; i < attributes.Length; i++)
						{
							if (attributes[i] is PCFSerializeAttribute)
							{
								attrib = attributes[i] as PCFSerializeAttribute;
								break;
							}
						}
					}

					//Ensure we grab the first matching attribute, ignore all others.
					if (attrib != null)
					{
						break;
					}
				}

				if (attrib != null)
				{
					string outputDirectory = Path.Combine(path, selectedObject.name);
					attrib.SerializePCF(selectedObject, outputDirectory, (bool success, string message) => {
						if (success)
						{
							successes.Add(message);
						}
						else
						{
							failures.Add(message);
						}
					});
				}
            }

            string output = "";
            foreach (string msg in successes)
            {
                output += msg + "\n";
            }

            output += "\n";
            foreach (string msg in failures)
            {
                output += msg + "\n";
            }

            EditorUtility.DisplayDialog("Exported content", output, "OK");
            AssetDatabase.Refresh();
        }

		[MenuItem("Assets/PCF/Import PCF")]
		public static void ImportPCF()
		{
			string rootDirectory = new DirectoryInfo(Application.dataPath).Parent.FullName;
			string localPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			string pcfFilePath = Path.Combine(rootDirectory, localPath);

			if (File.Exists(pcfFilePath) && Path.GetExtension(pcfFilePath) == ".pcf")
			{
				ImportAsset(pcfFilePath);
			}
		}

		private static void ImportAsset(string filePath)
		{
			System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
			timer.Start();

			IFileHandle fileHandle = new NormalFileHandle(filePath);
			PCFFile file = new PCFFile(fileHandle, false);
			file.LoadFile(PCFResourceType.ALL, false);

			Dictionary<PCFResourceType, DataBlockBase> dataBlocks = file.GetDataBlocks();

			UnityNodeBase rootNode = null;

			//Deserialize and create main asset.
			rootNode = UnityRootNode.CreateNodeTree(dataBlocks, fileHandle, new NodeFactory(null));

			NodeBlockDebugging debugger = new NodeBlockDebugging();
			debugger.DrawNodeTree(rootNode);

			if (rootNode != null)
			{
				List<Action<UnityNodeBase>> postInstallActions = new List<Action<UnityNodeBase>>();

				//Start rebuilding the asset, starting with the root node.
				rootNode.Deserialize(dataBlocks, null, null, postInstallActions, true);

				for (int i = 0; i < postInstallActions.Count; i++)
				{
					//Some nodes deserialize themselfves as a post process because they depend on other stuff being created/deserialized first.
					postInstallActions[i](rootNode);
				}
			}

			//Unload pcf file data, clear everything out. No longer needed.
			file.Unload();

			rootNode.InstantiateContent();

			timer.Stop();
			UnityEngine.Debug.Log("total: " + timer.ElapsedMilliseconds);
		}
    }
}