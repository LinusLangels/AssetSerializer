using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.InteropServices;

namespace PCFFileFormat
{
	public class StreamedAudioPlayer : AudioPlayerBase
	{
		private bool beginStreaming;

	    //Used for debugging purposes.
	    private int prebufferedSamples;

		public StreamedAudioPlayer(UInt32 id, string name, string path, int referenceLength)
	    {
	        this.path = path;
	        this.referenceLength = referenceLength;
			this.decoder = new AudioDecoder(id, name, 4096, new FileStreamProvider(path), SamplesCallback);

	        this.channelCount = decoder.GetChannelCount();
	        this.sampleRate = decoder.GetSampleRate();

	        //No need to factor in channels in this length, already taken care of.
	        this.samplesLength = decoder.GetSamplesLength();

	        //Half a second is what we want.
	        this.cacheSize = this.sampleRate / this.channelCount;

	        //Create an empty bufferpool. We populate it later.
	        int poolSize = this.cacheSize / BUFFER_SIZE;
	        this.bufferPool = new Stack<SampleBuffer>(this.cacheSize / BUFFER_SIZE);
			for (int i = 0; i < poolSize; i++)
			{
				SampleBuffer buffer = new SampleBuffer(BUFFER_SIZE, i);
				bufferPool.Push(buffer);
			}
				
			this.clip = AudioClip.Create(decoder.GetName(), samplesLength, channelCount, sampleRate, true, OnAudioRead, OnAudioSetPosition);
			this.beginStreaming = false;
	    }

		void BeginStreaming()
		{
			this.beginStreaming = true;
		}

	    void OnAudioRead(float[] data)
	    {
			if (!this.beginStreaming)
			{
	            //Debug: Used to track how many samples are actually requested before the audio stream starts playing.
				this.prebufferedSamples += data.Length;

				for (int i = 0; i < data.Length; i++)
				{
					data[i] = 0.0f;
				}
			}
			else
			{
				StreamSampleData(this.TailBuffer, ref data, 0, data.Length, this.cacheSize);
			}
	    }

	    void OnAudioSetPosition(int position)
	    {
			if (this.HeadBuffer != null)
			{
				SampleBuffer current = this.HeadBuffer;
				while (current != null)
				{
					SampleBuffer previous = current.Previous;

					Recycle(current);

					current = previous;
				}
			}

	        this.decoder.SetPosition(position);
	        this.HeadBuffer = null;
			this.TailBuffer = null;

	        //Set up about half a second of prebuffered sample data.
	        BuildInitialBuffer(this.cacheSize);

	        //Open up stream buffers for reading.
	        BeginStreaming();
	    }
	}
}