using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

namespace PCFFileFormat
{
	public class MemoryStreamProvider : AudioDataProviderBase
	{
		#if UNITY_IOS && !UNITY_EDITOR
		[DllImport ("__Internal")]
		#else
		[DllImport ("AssetSerializerAudio")]
		#endif
	    private static extern bool InitializeStreamDecoder(IntPtr instance, IntPtr data, int size);

		#if UNITY_IOS && !UNITY_EDITOR
		[DllImport ("__Internal")]
		#else
		[DllImport ("AssetSerializerAudio")]
		#endif
	    private static extern bool GenerateStreamedSampleData(IntPtr instance, IntPtr data, int size);

	    #if UNITY_IOS && !UNITY_EDITOR
		[DllImport ("__Internal")]
	    #else
		[DllImport ("AssetSerializerAudio")]
		#endif
	    private static extern void DestroyStreamDecoder(IntPtr instance);

	    public static int INITIAL_READ_SIZE = 8192;

	    private FileStream stream;
	    private int bytesLeftToRead;

	    public MemoryStreamProvider(FileStream stream, int length)
	    {
	        this.stream = stream;
	        this.bytesLeftToRead = length;
	    }

	    public override bool Initialize(IntPtr instance)
	    {
	        bool initialized = false;

	        if (this.stream != null)
	        {
	            byte[] buffer = new byte[INITIAL_READ_SIZE];
	            int bytesRead = stream.Read(buffer, 0, INITIAL_READ_SIZE);

	            this.bytesLeftToRead -= bytesRead;

	            if (bytesRead < INITIAL_READ_SIZE)
	            {
	                initialized = false;

	                Debug.LogError("Malformed ogg stream!");
	            }
	            else
	            {
	                IntPtr blockPointer = Marshal.AllocHGlobal(buffer.Length);
	                Marshal.Copy(buffer, 0, blockPointer, buffer.Length);

	                initialized = InitializeStreamDecoder(instance, blockPointer, bytesRead);

	                //Free memory for umanaged array.
	                Marshal.FreeHGlobal(blockPointer);
	            }
	        }

	        return initialized;
	    }

	    public override bool Generate(IntPtr instance, int bufferSize)
	    {
	        if (this.stream != null)
	        {
	            int readCount = 0;

	            if (bufferSize >= this.bytesLeftToRead)
	            {
	                readCount = this.bytesLeftToRead;
	            }
	            else
	            {
	                readCount = bufferSize;
	            }

	            byte[] buffer = new byte[readCount];
	            int bytesRead = stream.Read(buffer, 0, readCount);

	            this.bytesLeftToRead -= bytesRead;

	            IntPtr dataPointer = Marshal.AllocHGlobal(buffer.Length);
	            Marshal.Copy(buffer, 0, dataPointer, buffer.Length);

	            bool endOfStream = GenerateStreamedSampleData(instance, dataPointer, bytesRead);

	            //Free memory for umanaged array.
	            Marshal.FreeHGlobal(dataPointer);

	            //Make more sense for caller to know when this process is done. Flip bool.
	            return endOfStream;
	        }

	        return true;
	    }

		public override bool SetPosition(IntPtr instance, int position)
		{
			return false;
		}

	    public override void Destroy(IntPtr instance)
	    {
	        DestroyStreamDecoder(instance);
	    }
	}
}