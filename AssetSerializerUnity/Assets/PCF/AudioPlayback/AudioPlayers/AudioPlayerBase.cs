using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace PCFFileFormat
{
	public abstract class AudioPlayerBase
	{
	    [AttributeUsage(AttributeTargets.Method)]
	    public sealed class MonoPInvokeCallbackAttribute : Attribute
	    {
	        public MonoPInvokeCallbackAttribute(Type t) { }
	    }

	    public class SampleBuffer
	    {
	        public SampleBuffer Next;
	        public SampleBuffer Previous;

	        private float[] sampleBuffer;
	        private int start;
	        private int stop;
	        private int position;
	        private bool inUse;
	        private int timeStamp;

	        public SampleBuffer(int bufferSize, int timeStamp)
	        {
	            this.sampleBuffer = new float[bufferSize];
	            this.position = 0;
	            this.start = 0;
	            this.stop = 0;
	            this.timeStamp = timeStamp;
	            this.inUse = false;
	        }

	        public void Consume(int start, int stop)
	        {
	            this.stop = start;
	            this.stop = stop;
	            this.inUse = true;
	        }

	        public void Reset()
	        {
	            this.inUse = false;
	            this.start = 0;
	            this.stop = 0;
	            this.position = 0;
	            this.Previous = null;
	            this.Next = null;
	        }

	        public int GetTimeStamp()
	        {
	            return this.timeStamp;
	        }

	        public float[] GetBuffer()
	        {
	            return this.sampleBuffer;
	        }

	        public int SamplesAvailible()
	        {
	            return this.stop - this.position;
	        }

	        public bool TryConsume(ref float[] targetSamples, ref int offset, ref int samplesLeft)
	        {
	            int samplesAvailible = SamplesAvailible();
	            int samplesToWrite = samplesLeft;

	            bool consumed = false;

	            if (samplesLeft >= samplesAvailible)
	            {
	                samplesToWrite = samplesAvailible;
	                consumed = true;
	            }

	            for (int i = offset; i < samplesToWrite + offset; i++)
	            {
	                float sample = this.sampleBuffer[this.position];
	                targetSamples[i] = sample;

	                this.position++;
	            }

	            offset += samplesToWrite;
	            samplesLeft -= samplesToWrite;

	            return consumed;
	        }
	    }

	    //Number derived by checking vorbis callback length/output.
	    protected static int BUFFER_SIZE = 2048;

	    protected AudioDecoder decoder;
	    protected AudioClip clip;
	    protected string path;
	    protected int channelCount;
	    protected int sampleRate;
	    protected int samplesLength;
	    protected int referenceLength;
	    protected int cacheSize;

	    protected Stack<SampleBuffer> bufferPool;
	    protected SampleBuffer TailBuffer;
	    protected SampleBuffer HeadBuffer;

	    public AudioClip GetAudioClip()
	    {
	        return this.clip;
	    }

	    public void Destroy()
	    {
	        AudioClip.Destroy(this.clip);
	        this.clip = null;

	        this.decoder.Destroy();
	        this.decoder = null;
	    }

	    protected void SamplesCallback(IntPtr sampleBuffer, int size)
	    {
	        if (size == 0)
	            return;

	        if (size > 2048)
	        {
	            Debug.LogError("Input buffer is larger than target size!");
	        }

	        SampleBuffer buffer = GetEmptyBuffer();

	        //Copy samples into empty buffer.
	        Marshal.Copy(sampleBuffer, buffer.GetBuffer(), 0, size);

	        //Set buffer to in use.
	        buffer.Consume(0, size);

	        //Link the buffer.
	        if (this.HeadBuffer == null)
	        {
	            this.HeadBuffer = buffer;
	        }
	        else
	        {
	            this.HeadBuffer.Next = buffer;
	            buffer.Previous = this.HeadBuffer;
	            this.HeadBuffer = buffer;
	        }
	    }

	    protected SampleBuffer GetEmptyBuffer()
	    {
	        if (this.bufferPool != null && this.bufferPool.Count > 0)
	        {
	            return this.bufferPool.Pop();
	        }

	        return new SampleBuffer(2048, 0);
	    }

	    protected void BuildBuffer(SampleBuffer buffer, int bufferSize, ref int currentSize)
	    {
	        if (buffer != null)
	        {
	            CheckBufferSize(buffer, ref currentSize);

	            if (currentSize < bufferSize)
	            {
	                //Decode more sample data
	                bool endOfStream = decoder.GenerateSampleData();

	                if (!endOfStream)
	                {
	                    BuildBuffer(buffer, bufferSize, ref currentSize);
	                }
	            }
	        }
	    }

	    protected void BuildInitialBuffer(int size)
	    {
	        //Buffer initial sampledata.
	        decoder.GenerateSampleData();

	        //Get tail buffer.
	        SampleBuffer currentTail = this.HeadBuffer;
	        while (true)
	        {
	            SampleBuffer previous = currentTail.Previous;

	            if (previous == null)
	                break;

	            currentTail = currentTail.Previous;
	        }

	        this.TailBuffer = currentTail;

	        //Build initial buffer.
	        int currentSize = 0;
	        BuildBuffer(this.TailBuffer, size, ref currentSize);
	    }

	    protected void CheckBufferSize(SampleBuffer buffer, ref int currentSize)
	    {
	        if (buffer != null)
	        {
	            currentSize += buffer.SamplesAvailible();

	            CheckBufferSize(buffer.Next, ref currentSize);
	        }
	    }

	    protected void Recycle(SampleBuffer buffer)
	    {
	        buffer.Reset();

	        if (this.bufferPool != null)
	        {
	            this.bufferPool.Push(buffer);
	        }
	    }

	    protected void StreamSampleData(SampleBuffer buffer, ref float[] targetSamples, int offset, int samplesLeft, int cacheSize)
	    {
	        if (buffer != null)
	        {
	            bool consumed = buffer.TryConsume(ref targetSamples, ref offset, ref samplesLeft);

	            if (consumed)
	            {
	                if (System.Object.ReferenceEquals(this.HeadBuffer, this.TailBuffer))
	                {
	                    //Debug.LogWarning("Reached Head Buffer");

	                    for (int i = offset; i < (offset + samplesLeft); i++)
	                    {
	                        targetSamples[i] = 0.0f;
	                    }
	                }
	                else
	                {
	                    this.TailBuffer = buffer.Next;
	                    this.TailBuffer.Previous = null;

	                    //Recycle current buffer.
	                    Recycle(buffer);

	                    //Make sure we always have 44100 samples ahead of playhead in the buffer.
	                    int currentSize = 0;
	                    BuildBuffer(this.TailBuffer, cacheSize, ref currentSize);

	                    StreamSampleData(this.TailBuffer, ref targetSamples, offset, samplesLeft, cacheSize);
	                }
	            }
	        }
	    }
	}
}
