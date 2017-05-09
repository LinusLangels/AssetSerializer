using UnityEngine;
using System.Collections;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat
{
	public class AudioSerializeOpts
	{
		public AudioSerializeOpts()
		{
			
		}

		public virtual byte[] PackageAudio(AudioClip clip, SerializedAssets serializedAssets, UInt32 nodeID, ref bool streamed)
		{
			string cachedOGG = GetCachedOGG(clip, ref streamed);
			byte[] sampleData = null;

			if (File.Exists(cachedOGG))
			{
				using (FileStream stream = new FileStream(cachedOGG, FileMode.Open))
				{
					sampleData = new byte[stream.Length];
					stream.Read(sampleData, 0, (int)stream.Length);
				}
			}
			else
			{
				//Create new encoding.
				AudioEncodingTools oggEncoder = new AudioEncodingTools(clip, 0.4f, cachedOGG);
				sampleData = oggEncoder.Encode();

				//Default to streamed.
				streamed = true;
			}

			return sampleData;
		}

		private string GetCachedOGG(AudioClip clip, ref bool streamed)
		{
			string rootDir = new DirectoryInfo(Application.dataPath).Parent.FullName;
			string audioPath = Path.Combine(rootDir, AssetDatabase.GetAssetPath(clip));

			string oggDirectory = Path.GetDirectoryName(audioPath);
			string streamedOGGFile = Path.Combine(oggDirectory, Path.GetFileNameWithoutExtension(audioPath) + AudioEncodingTools.STREAMED_CACHE_SUFFIX + ".ogg");

			if (File.Exists(streamedOGGFile))
			{
				streamed = true;
				return streamedOGGFile;
			}

			string ramcachedOGGFile = Path.Combine(oggDirectory, Path.GetFileNameWithoutExtension(audioPath) + AudioEncodingTools.RAM_CACHE_SUFFIX + ".ogg");

			if (File.Exists(ramcachedOGGFile))
			{
				streamed = false;
				return ramcachedOGGFile;
			}

			return string.Empty;
		}
	}
}
