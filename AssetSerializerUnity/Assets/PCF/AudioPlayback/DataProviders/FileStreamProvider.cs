using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

namespace PCFFileFormat
{
	public class FileStreamProvider : AudioDataProviderBase
	{
		#if UNITY_IOS && !UNITY_EDITOR
		[DllImport ("__Internal")]
		#else
		[DllImport ("AssetSerializerAudio")]
		#endif
	    private static extern bool InitializeFileDecoder(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)]string filePath);

		#if UNITY_IOS && !UNITY_EDITOR
		[DllImport ("__Internal")]
		#else
		[DllImport ("AssetSerializerAudio")]
		#endif
	    private static extern bool GenerateFileSampleData(IntPtr instance);

		#if UNITY_IOS && !UNITY_EDITOR
		[DllImport ("__Internal")]
		#else
		[DllImport ("AssetSerializerAudio")]
		#endif
		private static extern bool SetFilePosition(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)]string filePath, int position);

		#if UNITY_IOS && !UNITY_EDITOR
		[DllImport ("__Internal")]
		#else
		[DllImport ("AssetSerializerAudio")]
		#endif
	    private static extern void DestroyFileDecoder(IntPtr instance);

	    private string filePath;

	    public FileStreamProvider(string filePath)
	    {
	        this.filePath = filePath;
	    }

	    public override bool Initialize(IntPtr instance)
	    {
	        bool initialized = false;

	        if (File.Exists(this.filePath))
	        {
	            initialized = InitializeFileDecoder(instance, this.filePath);
	        }

	        return initialized;
	    }

	    public override bool Generate(IntPtr instance, int bufferSize)
	    {
	        bool endOfStream = GenerateFileSampleData(instance);

	        return endOfStream;
	    }

		public override bool SetPosition(IntPtr instance, int position)
		{
			//Debug.Log("Seeking to ogg position: " + position);

			return SetFilePosition(instance, this.filePath, position);
		}

	    public override void Destroy(IntPtr instance)
	    {
	        DestroyFileDecoder(instance);
	    }
	}
}