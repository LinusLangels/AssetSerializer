using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;

namespace PCFFileFormat.Editor
{
    public class LightprobeBundleGenerator
    {
		[MenuItem("Assets/PCF/Export Lightprobes")]
        static void BuildAllAssetBundles()
        {
			bool validFolder = AssetDatabase.IsValidFolder("Assets/LightingData");

			if (!validFolder)
				AssetDatabase.CreateFolder("Assets", "LightingData");

			BuildTarget[] buildTargets = new BuildTarget[] { BuildTarget.StandaloneOSXIntel, BuildTarget.iOS, BuildTarget.Android };

			foreach (BuildTarget target in buildTargets)
			{
				string outputBundlePath = "Assets/LightingData/" + target.ToString();

				if (!AssetDatabase.IsValidFolder(outputBundlePath))
					AssetDatabase.CreateFolder("Assets/LightingData", target.ToString());

				string sceneName = EditorSceneManager.GetActiveScene().name;
				string probeAssetPath = "Assets/LightingData/" + sceneName + ".probes.asset";

				string[] foundProbeAsset = AssetDatabase.FindAssets(sceneName + ".probes");

				if (foundProbeAsset.Length > 0)
				{
					AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
					buildMap[0].assetNames = new string[1] { probeAssetPath };
					buildMap[0].assetBundleName = sceneName + ".internalbundle";

					BuildPipeline.BuildAssetBundles(outputBundlePath, buildMap, BuildAssetBundleOptions.None, target);
				}
			}

            AssetDatabase.Refresh();
        }
    }
}
