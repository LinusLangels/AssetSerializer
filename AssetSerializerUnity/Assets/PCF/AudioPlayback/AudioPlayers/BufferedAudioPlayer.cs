//#define DEBUG_OUTPUT

using UnityEngine;
using System.Collections;
using System;

namespace PCFFileFormat
{
	public class BufferedAudioPlayer : AudioPlayerBase
	{
	    public BufferedAudioPlayer(UInt32 id, string name, string path, int referenceLength)
	    {
	        this.path = path;
	        this.referenceLength = referenceLength;
	        this.decoder = new AudioDecoder(id, name, 4096, new FileStreamProvider(path), SamplesCallback);

	        this.channelCount = decoder.GetChannelCount();
	        this.sampleRate = decoder.GetSampleRate();

	        //No need to factor in channels in this length, already taken care of.
	        this.samplesLength = decoder.GetSamplesLength();

	        //Generate all samples up front.
	        while (!decoder.GenerateSampleData()) {}

	        //Find tail.
	        SampleBuffer currentTail = this.HeadBuffer;
	        while (true)
	        {
	            SampleBuffer previous = currentTail.Previous;

	            if (previous == null)
	                break;

	            currentTail = currentTail.Previous;
	        }
	        this.TailBuffer = currentTail;

	        float[] samples = new float[samplesLength];
	        StreamSampleData(this.TailBuffer, ref samples, 0, samples.Length, 4096);

	        this.clip = AudioClip.Create(decoder.GetName(), samplesLength / channelCount, channelCount, sampleRate, false);
	        this.clip.SetData(samples, 0);
	        this.clip.LoadAudioData();

	        #if DEBUG_OUTPUT
	        OutputWavFile(samples);
	        #endif
	    }

	    #if DEBUG_OUTPUT
	    void OutputWavFile(float[] samples)
	    {
	        string debugPath = Application.persistentDataPath + "/" + decoder.GetName() + ".wav";

	        if (System.IO.File.Exists(debugPath))
	        {
	            System.IO.File.Delete(debugPath);
	        }

	        WavObject wav = new WavObject(debugPath, WavFileMode.WriteWav);
	        WavObject.WavFileData data = wav.ConstructHeaderPrototype(sampleRate, channelCount, 1);
	        data.SAMPLES = samples;

	        wav.WriteWav(data);
	    }
	    #endif
	}

	public delegate void PlayerSampleCallback(IntPtr sampleBuffer, int size);
}
