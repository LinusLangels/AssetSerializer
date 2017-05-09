//#define DEBUG_MODE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PCFFileFormat
{
	public class AudioDecoder
	{
	    [AttributeUsage(AttributeTargets.Method)]
	    public sealed class MonoPInvokeCallbackAttribute : Attribute
	    {
	        public MonoPInvokeCallbackAttribute(Type t) { }
	    }

		static Dictionary<UInt32, AudioDecoder> ActiveDecoders = new Dictionary<uint, AudioDecoder>();

		#if UNITY_IOS && !UNITY_EDITOR
		[DllImport ("__Internal")]
		#else
		[DllImport ("AssetSerializerAudio")]
		#endif
		private static extern IntPtr CreateDecoder(UInt32 instanceID, int bufferSize, DebugLogCallback logCallback, SampleDataCallback sampleCallback, AudioParametersCallback parameterCallback);

	    private int bufferSize;
		private IntPtr decoderInstance;
		private bool initialized;
		private bool endOfStream;
		private int sampleRate;
		private int channelCount;
	    private int samplesLength;
		private string decoderName;
	    private AudioDataProviderBase provider;
		private PlayerSampleCallback sampleCallback;
		private UInt32 instanceID;
	    	
		public AudioDecoder(UInt32 id, string name, int bufferSize, AudioDataProviderBase provider, PlayerSampleCallback sampleCallback)
		{
	        this.instanceID = id;

			ActiveDecoders.Add(this.instanceID, this);

			this.decoderName = name;
	        this.bufferSize = bufferSize;
	        this.provider = provider;
			this.sampleCallback = sampleCallback;
			this.decoderInstance = CreateDecoder(this.instanceID, this.bufferSize, DebugLogCallback, SampleCallback, ParametersCallback);
	        this.initialized = this.provider.Initialize(this.decoderInstance);
	    }

	    public string GetName()
		{
			return this.decoderName;
		}

		public void SendSampleCallback(IntPtr sampleBuffer, int size)
		{
			this.sampleCallback(sampleBuffer, size);
		}

	    public int GetSampleRate()
	    {
	        return this.sampleRate;
	    }

	    public int GetChannelCount()
	    {
	        return this.channelCount;
	    }

	    public int GetSamplesLength()
	    {
	        return this.samplesLength;
	    }

	    public void SetParameters(int sampleRate, int channels, int samplesLength)
		{
			this.sampleRate = sampleRate;
			this.channelCount = channels;
	        this.samplesLength = samplesLength;
	    }
		
		public bool GenerateSampleData()
		{
			if (this.initialized)
			{
	            this.endOfStream = this.provider.Generate(this.decoderInstance, this.bufferSize);
			}

			return this.endOfStream;
		}

		public bool SetPosition(int position)
		{
			if (this.initialized)
			{
				return this.provider.SetPosition(this.decoderInstance, position);
			}

			return false;
		}
		
		public void Destroy()
		{
			try
			{
				this.provider.Destroy(this.decoderInstance);
			}
			catch (Exception e)
			{
				Debug.LogError("Unable to destroy audio decoder instance: " + e.Message);
			}

			ActiveDecoders[this.instanceID] = null;
			ActiveDecoders.Remove(this.instanceID);
				
			this.initialized = false;
		}

		[MonoPInvokeCallback(typeof(SampleDataCallback))]
		static void SampleCallback(UInt32 instance, IntPtr samples, int size)
		{
			if (ActiveDecoders.ContainsKey(instance))
			{
				AudioDecoder decoder = ActiveDecoders[instance];
				decoder.SendSampleCallback(samples, size);
			}
		}
		
		[MonoPInvokeCallback(typeof(AudioParametersCallback))]
		static void ParametersCallback(UInt32 instance, int sampleRate, int channels, long samplesLength)
		{
			if (ActiveDecoders.ContainsKey(instance))
			{
				AudioDecoder decoder = ActiveDecoders[instance];
				decoder.SetParameters(sampleRate, channels, (int)samplesLength);
			}
		}
		
		[MonoPInvokeCallback(typeof(DebugLogCallback))]
		static void DebugLogCallback(UInt32 instance, string message)
		{
	        #if DEBUG_MODE
	        Debug.Log("Native Log: " + message);
	        #endif
	    }
	}

	public delegate void SampleDataCallback(UInt32 instance, IntPtr sampleBuffer, int size);
	public delegate void AudioParametersCallback(UInt32 instance, int sampleRate, int channels, long samplesLength);
	public delegate void DebugLogCallback(UInt32 instance, string str);
}
