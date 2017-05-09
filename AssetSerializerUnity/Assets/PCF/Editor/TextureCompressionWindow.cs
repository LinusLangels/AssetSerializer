using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

namespace PCFFileFormat.Editor
{
    public class TextureCompressionWindow : EditorWindow
    {
        static string[] QUALITY_OPTIONS;
        static Dictionary<int, CompressionQuality> QUALITY_MAPPING;

        private UnityEngine.Object[] selectedObjects;
        private bool dither;
        private bool premultiplied;

        private int pvrtcSelectedOption;
        private int astcSelectedOption;

        Queue<Texture2D> texturesToCache = new Queue<Texture2D>();

        enum CompressionQuality
        {
            Fastest,
            Fast,
            Normal,
            High,
            Best
        }

        static TextureCompressionWindow()
        {
            QUALITY_OPTIONS = Enum.GetNames(typeof(CompressionQuality));
            QUALITY_MAPPING = new Dictionary<int, CompressionQuality>();

            int index = 0;

            foreach (CompressionQuality val in Enum.GetValues(typeof(CompressionQuality)))
            {
                QUALITY_MAPPING.Add(index, val);

                index++;
            }
        }

        [MenuItem("Assets/PCF/Compress Texture")]
        static void Init()
        {
            TextureCompressionWindow myWindow = (TextureCompressionWindow)EditorWindow.GetWindow(typeof(TextureCompressionWindow));
            
            myWindow.SetObjects(Selection.objects);
            myWindow.Show();
        }

        private PVRTCCompressionQuality MapPVRTCQuality(CompressionQuality compressionQuality)
        {
            switch (compressionQuality)
            {
                case CompressionQuality.Fastest:
                    return PVRTCCompressionQuality.ePVRTCFastest;
                case CompressionQuality.Fast:
                    return PVRTCCompressionQuality.ePVRTCFast;
                case CompressionQuality.Normal:
                    return PVRTCCompressionQuality.ePVRTCNormal;
                case CompressionQuality.High:
                    return PVRTCCompressionQuality.ePVRTCHigh;
                case CompressionQuality.Best:
                    return PVRTCCompressionQuality.ePVRTCBest;
                default: return PVRTCCompressionQuality.ePVRTCBest;
            }
        }

        private ASTCCompressionQuality MapASTCQuality(CompressionQuality compressionQuality)
        {
            switch (compressionQuality)
            {
                case CompressionQuality.Fastest:
                    return ASTCCompressionQuality.veryfast; ;
                case CompressionQuality.Fast:
                    return ASTCCompressionQuality.fast;
                case CompressionQuality.Normal:
                    return ASTCCompressionQuality.medium;
                case CompressionQuality.High:
                    return ASTCCompressionQuality.thorough;
                case CompressionQuality.Best:
                    return ASTCCompressionQuality.exhaustive;
                default: return ASTCCompressionQuality.exhaustive;
            }
        }

        public void SetObjects(UnityEngine.Object[] selectedObjects)
        {
            this.selectedObjects = selectedObjects;
            this.pvrtcSelectedOption = 4;
            this.astcSelectedOption = 3;
        }

        void OnGUI()
        {
            if (this.selectedObjects == null)
            {
                this.Close();
                return;
            }

            EditorGUILayout.LabelField("PVRTC");
            this.pvrtcSelectedOption = EditorGUILayout.Popup("PVRTC Quality", this.pvrtcSelectedOption, QUALITY_OPTIONS);
            this.premultiplied = EditorGUILayout.Toggle("Premultiplied", this.premultiplied);
            this.dither = EditorGUILayout.Toggle("Dither", this.dither);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("ASTC");
            this.astcSelectedOption = EditorGUILayout.Popup("ASTC Quality", this.astcSelectedOption, QUALITY_OPTIONS);

            for (int i = 0; i < 5; i++)
                EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {           
                for (int i = 0; i < this.selectedObjects.Length; i++)
                {
                    UnityEngine.Object selectedObject = this.selectedObjects[i];

                    if (selectedObject is Texture2D)
                    {
                        texturesToCache.Enqueue(selectedObject as Texture2D);
                    }                  
                    else
                    {
                        Debug.Log("The selected object is not a texture: " + selectedObject.name);
                    }  
                }               
                
                float textureCount = texturesToCache.Count;
                float progress = 0;

                while (texturesToCache.Count > 0)
                {
                    progress++;
                    Texture2D texture = texturesToCache.Dequeue();

                    if(EditorUtility.DisplayCancelableProgressBar("Texture encoder", "Encoding texture: " + texture.name, progress / textureCount))
                    {
                        break;
                    }

                    StartEncoding(MapPVRTCQuality(QUALITY_MAPPING[this.pvrtcSelectedOption]), MapASTCQuality(QUALITY_MAPPING[this.astcSelectedOption]), this.premultiplied, this.dither, texture);
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

        void StartEncoding(PVRTCCompressionQuality pvrtcQuality, ASTCCompressionQuality astcQuality, bool isPremultiplied, bool isDithered, Texture2D texture)
        {
            string rootDirectory = new DirectoryInfo(Application.dataPath).Parent.FullName;
            string localPath = AssetDatabase.GetAssetPath(texture);
            string texturePath = Path.Combine(rootDirectory, localPath);

            //Build new path that sits at the same spot as the original file.
            string textureDirectory = Path.GetDirectoryName(texturePath);

            // PVR
			string pvrPath = Path.Combine(textureDirectory, Path.GetFileNameWithoutExtension(texturePath) + TextureEncoderBase.CACHE_SUFFIX + ".pvr");

            if (File.Exists(pvrPath))
            {
                File.Delete(pvrPath);
            }
            
            PVRTCEncoderWrapper pvrtcEncoder = new PVRTCEncoderWrapper();
			PVRTCEncoderWrapper.ApplyDither(isDithered);
			PVRTCEncoderWrapper.ApplyPremultiplication(isPremultiplied);
			pvrtcEncoder.EncodeToDisk(pvrPath, texture, (int)pvrtcQuality, 1);

            // ASTC
			string astcPath = Path.Combine(textureDirectory, Path.GetFileNameWithoutExtension(texturePath) + TextureEncoderBase.CACHE_SUFFIX + ".astc");
	
            if (File.Exists(astcPath))
            {
                File.Delete(astcPath);
            }

			ASTCEncoderWrapper astcEncoder = new ASTCEncoderWrapper();
			ASTCEncoderWrapper.SetRate(ASTCCompressionRate.Six_bpp);
			astcEncoder.EncodeToDisk(astcPath, texture, (int)astcQuality, 1);
        }
    }
}
