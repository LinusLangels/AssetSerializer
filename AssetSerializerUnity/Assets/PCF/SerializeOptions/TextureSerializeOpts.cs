using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat
{
	public enum TexturePackageOptions
	{
		IncompletePackage,
		ASTCPackage,
		PVRTCPackage,
	}

	public class TextureSerializeOpts 
	{
		private Dictionary<string, TextureEncoderBase> encoders;
		private TexturePackageOptions packageOption;

		public TextureSerializeOpts (TexturePackageOptions packageOption) 
		{
			this.packageOption = packageOption;
			this.encoders = new Dictionary<string, TextureEncoderBase>();
			this.encoders.Add("astc", new ASTCEncoderWrapper());
			this.encoders.Add("pvrtc", new PVRTCEncoderWrapper());
		}

		public virtual byte[] PackageTexture(Texture2D texture, SerializedAssets serializedAssets, UInt32 nodeID, ref TextureDataFormat format)
		{
			if (packageOption == TexturePackageOptions.ASTCPackage)
			{
				string cachedASTC = GetCachedASTC(texture);

				if (File.Exists(cachedASTC))
				{
					TextureEncoderBase encoder = encoders["astc"];
					byte[] astcData = encoder.GetData(cachedASTC, 1);
					format = TextureDataFormat.ASTC6X6;

					return astcData;
				}
			}
			else if (packageOption == TexturePackageOptions.PVRTCPackage)
			{
				string cachedPVRTC = GetCachedPVRTC(texture);

				Debug.Log(cachedPVRTC);

				if (File.Exists(cachedPVRTC))
				{
					TextureEncoderBase encoder = encoders["pvrtc"];
					byte[] pvrtcData = encoder.GetData(cachedPVRTC, 1);
					format = TextureDataFormat.PVRTC4BPP;

					return pvrtcData;
				}
			}
			else if (packageOption == TexturePackageOptions.IncompletePackage)
			{
				string cachedASTC = GetCachedASTC(texture);
				string cachedPVRTC = GetCachedPVRTC(texture);
				string outputDirectory = serializedAssets.GetDestinationDirectory();
				bool hasCache = true;

				if (hasCache = File.Exists(cachedASTC))
				{
					TextureEncoderBase encoder = encoders["astc"];
					byte[] texData = encoder.GetData(cachedASTC, 1);
					format = TextureDataFormat.Empty;

					string pvrtcPath = outputDirectory + "/" + nodeID + ".astc";
					TextureEncoderBase.SaveTextureToDisk(pvrtcPath, texData);
				}
				else if (hasCache = File.Exists(cachedPVRTC))
				{
					TextureEncoderBase encoder = encoders["pvrtc"];
					byte[] texData = encoder.GetData(cachedASTC, 1);
					format = TextureDataFormat.Empty;

					string pvrtcPath = outputDirectory + "/" + nodeID + ".pvrtc";
					TextureEncoderBase.SaveTextureToDisk(pvrtcPath, texData);
				}

				//Incase a cached texture resource was used we dont serialize the texture into the node, its left empty.
				if (hasCache)
				{
					return null;
				}
			}
				
			//Default is packaging textures are raw rgba.
			byte[] textureData = TextureEncoderBase.GetImageBytes(texture);
			format = TextureDataFormat.RGB32;

			return textureData;
		}

		private string GetCachedASTC(Texture2D texture)
		{
			string rootDir = new DirectoryInfo(Application.dataPath).Parent.FullName;
			string texturePath = Path.Combine(rootDir, AssetDatabase.GetAssetPath(texture));

			string textureDirectory = Path.GetDirectoryName(texturePath);
			string cachedASTCFile = Path.Combine(textureDirectory, Path.GetFileNameWithoutExtension(texturePath) + PVRTCEncoderWrapper.CACHE_SUFFIX + ".astc");

			return cachedASTCFile;
		}

		private string GetCachedPVRTC(Texture2D texture)
		{
			string rootDir = new DirectoryInfo(Application.dataPath).Parent.FullName;
			string texturePath = Path.Combine(rootDir, AssetDatabase.GetAssetPath(texture));

			string textureDirectory = Path.GetDirectoryName(texturePath);
			string cachedPVRTCFile = Path.Combine(textureDirectory, Path.GetFileNameWithoutExtension(texturePath) + PVRTCEncoderWrapper.CACHE_SUFFIX + ".pvr");

			return cachedPVRTCFile;
		}
	}
}