using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.IO;

namespace PCFFileFormat.Editor
{
    [InitializeOnLoad]
    public class GenerateLightprobeData
    {
        static GenerateLightprobeData Instance;

        static GenerateLightprobeData()
        {
            if (Instance == null)
            {
                Instance = new GenerateLightprobeData();
            }
        }

        bool beginLightMapping;

        public GenerateLightprobeData()
        {
            EditorApplication.update += Update;
        }

        void Update()
        {
            bool isRunning = Lightmapping.isRunning;

            if (isRunning)
            {
                if (!this.beginLightMapping)
                {
                    this.beginLightMapping = true;

                    Debug.Log("Begin Lightmapping");
                }
            }
            else
            {
                if (this.beginLightMapping)
                {
                    this.beginLightMapping = false;

                    ExtractProbeData();

                    Debug.Log("Stop Lightmapping");
                }

                this.beginLightMapping = false;
            }
        }

        void ExtractProbeData()
        {
            bool validFolder = AssetDatabase.IsValidFolder("Assets/LightingData");

            if (!validFolder)
                AssetDatabase.CreateFolder("Assets", "LightingData");

            string sceneName = EditorSceneManager.GetActiveScene().name;
            string probeAssetPath = "Assets/LightingData/" + sceneName + ".probes.asset";
            LightingDataAsset lightingAsset = Lightmapping.lightingDataAsset;

            if (lightingAsset != null)
            {
				AssetDatabase.CreateAsset(LightmapSettings.Instantiate(LightmapSettings.lightProbes), probeAssetPath);
            }
        }
    }
}
