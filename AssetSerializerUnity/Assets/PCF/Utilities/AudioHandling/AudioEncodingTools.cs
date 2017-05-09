using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat
{
    public class AudioEncodingTools
    {
        public static string STREAMED_CACHE_SUFFIX = "_streamedcache";
		public static string RAM_CACHE_SUFFIX = "_ramcache";

        [DllImport("AssetSerializerAudio")]
        private static extern void SetDebugLogCallback(LogCallback functionDelegate);

		[DllImport("AssetSerializerAudio")]
        private static extern void EncodeSampleData(IntPtr samplesPointer, int samplesLength, int channels, int sampleRate, int sampleSize, float quality, string outputOgg);

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class MonoPInvokeCallbackAttribute : Attribute
        {
            public MonoPInvokeCallbackAttribute(Type t) { }
        }

        public delegate void LogCallback(IntPtr instance, string str);

        static AudioEncodingTools()
        {
            //Allow plugin to log back into unity console.
            SetDebugLogCallback(NativeDebugLog);
        }

        [MonoPInvokeCallback(typeof(LogCallback))]
        public static void NativeDebugLog(IntPtr instance, string message)
        {
            Debug.Log(message);
        }

        private AudioClip audioClip;
        private string outputPath;
        private float quality;

        public AudioEncodingTools(AudioClip audioClip, float quality, string outputPath)
        {
            this.audioClip = audioClip;
            this.outputPath = outputPath;
            this.quality = quality;
        }

        public byte[] Encode()
        {
            int channels = this.audioClip.channels;
            int sampleRate = this.audioClip.frequency;
            int sizeOfSample = sizeof(float);
            float[] samples = new float[audioClip.samples * channels];
            audioClip.GetData(samples, 0);            

            Debug.Log("Channels: " + channels);
            Debug.Log("ByteRate: " + sampleRate);
            Debug.Log("Sample count: " + samples.Length);

            //Convert sample data to byte array.
            byte[] data = ConvertToBytes(samples);

            //Verify and correct for any strange sample length.
            int samplesLength = data.Length;
            int sampleOffset = samplesLength % (channels * sizeof(float));
            int dataSize = sampleOffset == 0 ? samplesLength : (samplesLength - sampleOffset);

            //Allocate unmanaged memory for the raw sample data.
            IntPtr samplesPointer = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, samplesPointer, data.Length);

            //Encode sample data to ogg.
            EncodeSampleData(samplesPointer, dataSize, channels, sampleRate, sizeOfSample, quality, this.outputPath);

            //Free memory for umanaged array.
            Marshal.FreeHGlobal(samplesPointer);

            byte[] byteData = null;

            if (File.Exists(this.outputPath))
            {
                using (FileStream stream = new FileStream(outputPath, FileMode.Open))
                {
                    byteData = new byte[stream.Length];
                    stream.Read(byteData, 0, (int)stream.Length);
                }
            }

            return byteData;
        }

        byte[] ConvertToBytes(float[] samples)
        {
            byte[] byteArray = new byte[samples.Length * sizeof(float)];

            Buffer.BlockCopy(samples, 0, byteArray, 0, byteArray.Length);

            return byteArray;
        }
    }
}
