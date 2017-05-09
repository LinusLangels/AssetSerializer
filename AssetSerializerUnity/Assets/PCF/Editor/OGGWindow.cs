using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace PCFFileFormat.Editor
{
    public class OGGWindow : EditorWindow
    {
        private UnityEngine.Object[] selectedObjects;
        private float quality;
        private bool streamed;

        Queue<AudioClip> audioClipsToCache = new Queue<AudioClip>();

        [MenuItem("Assets/PCF/Compress Audio")]
        static void Init()
        {
            OGGWindow myWindow = (OGGWindow)EditorWindow.GetWindow(typeof(OGGWindow));

            myWindow.SetObjects(Selection.objects);
            myWindow.Show();
        }

        public void SetObjects(UnityEngine.Object[] selectedObjects)
        {
            this.selectedObjects = selectedObjects;
            this.quality = 0.4f;
			this.streamed = true;
            
            if(selectedObjects == null || selectedObjects.Length < 1)
            {
                return;
            }
        }

        void OnGUI()
        {
            if (this.selectedObjects == null)
            {
                this.Close();
                return;
            }

            this.quality = EditorGUILayout.Slider("Quality", this.quality, 0.0f, 1.0f);

            for (int i = 0; i < 3; i++)
                EditorGUILayout.Space();

            this.streamed = EditorGUILayout.Toggle("Streamed: ", this.streamed);

            for (int i = 0; i < 5; i++)
                EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                for (int i = 0; i < this.selectedObjects.Length; i++)
                {
                    UnityEngine.Object selectedObject = this.selectedObjects[i];

                    if (selectedObject is AudioClip)
                    {
                        audioClipsToCache.Enqueue(selectedObject as AudioClip);
                    }
                    else
                    {
                        Debug.Log("The selected object is not a audio file: " + selectedObject.name);
                    }
                }

                float textureCount = audioClipsToCache.Count;
                float progress = 0;

                while (audioClipsToCache.Count > 0)
                {
                    progress++;
                    AudioClip audioClip = audioClipsToCache.Dequeue();

                    if (EditorUtility.DisplayCancelableProgressBar("Audio encoder", "Encoding audio: " + audioClip.name, progress / textureCount))
                    {
                        break;
                    }

                    StartEncoding(this.quality, this.streamed, audioClip);
                }
                EditorUtility.ClearProgressBar();

                AssetDatabase.Refresh();
                this.Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                this.Close();
            }

            if (this != null)
            {
                EditorGUILayout.EndHorizontal();
            }            
        }

        void StartEncoding(float audioQuality, bool isStreamed, AudioClip audioClip)
        {
            //Build a path of a cached ogg file in the same location as the original audio file.
            string rootDir = new DirectoryInfo(Application.dataPath).Parent.FullName;
            string assetPath = AssetDatabase.GetAssetPath(audioClip);
            string assetDir = Path.GetDirectoryName(assetPath);

            string fileName = Path.GetFileNameWithoutExtension(assetPath);

			if (isStreamed)
			{
				fileName += AudioEncodingTools.STREAMED_CACHE_SUFFIX + ".ogg";
			}
			else
			{
				fileName += AudioEncodingTools.RAM_CACHE_SUFFIX + ".ogg";
			}

            string oggPath = Path.Combine(rootDir, Path.Combine(assetDir, fileName)).Replace("\\", "/");

            if (File.Exists(oggPath))
            {
                File.Delete(oggPath);
            }

            AudioEncodingTools oggEncoder = new AudioEncodingTools(audioClip, audioQuality, oggPath);
            oggEncoder.Encode();

            AssetDatabase.Refresh();
        }
    }
}
