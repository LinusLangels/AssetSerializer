using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.InteropServices;

namespace PCFFileFormat
{
    public enum PVRTCCompressionQuality
    {
        ePVRTCFastest = 0,        //!< PVRTC fastest
        ePVRTCFast,             //!< PVRTC fast
        ePVRTCNormal,           //!< PVRTC normal
        ePVRTCHigh,             //!< PVRTC high
        ePVRTCBest,             //!< PVRTC best
        eNumPVRTCModes,         //!< Number of PVRTC modes
    }

	public class PVRTCEncoderWrapper : TextureEncoderBase
    {
        #if UNITY_STANDALONE || UNITY_EDITOR
		[DllImport("AssetSerializerPVRTC")]
        private static extern void SetDebugLogCallback(stringParameterCallback functionDelegate);

		[DllImport("AssetSerializerPVRTC")]
        public static extern IntPtr GetTextureData(string filePath, UInt32 mipLevels, ref IntPtr dataSizes);

		[DllImport("AssetSerializerPVRTC")]
        public static extern IntPtr GetUncompressedData(byte[] data, UInt32 size, UInt32 width, UInt32 height, bool premultiplied, ref int dataSizes);

		[DllImport("AssetSerializerPVRTC")]
        public static extern void FreeUncompressedData(IntPtr dataPointer);

		[DllImport("AssetSerializerPVRTC")]
        public static extern void CompressTextureToFile(string filePath, byte[] data, UInt32 height, UInt32 width, UInt32 mipLevels, UInt32 quality, bool preMultiplied, bool dither);
        #endif

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class MonoPInvokeCallbackAttribute : Attribute
        {
            public MonoPInvokeCallbackAttribute(Type t) { }
        }

        public delegate void stringParameterCallback(string str);

        static PVRTCEncoderWrapper()
        {
            //Allow plugin to log back into unity console.
            #if UNITY_STANDALONE || UNITY_EDITOR
            SetDebugLogCallback(NativeDebugLog);
            #endif
        }

        [MonoPInvokeCallback(typeof(stringParameterCallback))]
        public static void NativeDebugLog(string message)
        {
            Debug.Log(message);
        }

		static bool dither = false;
		static bool premultiply = false;

		public static void ApplyDither(bool toggle)
		{
			dither = toggle;
		}

		public static void ApplyPremultiplication(bool toggle)
		{
			premultiply = toggle;
		}

		public static IntPtr DecompressData(byte[] compressedData, int width, int height, bool premultiplied, ref int dataSize)
		{
			#if UNITY_STANDALONE || UNITY_EDITOR
			return GetUncompressedData(compressedData, (UInt32)compressedData.Length, (UInt32)width, (UInt32)height, premultiplied, ref dataSize);
			#else
			return IntPtr.Zero;
			#endif
		}

		public static void FreeCompressedDataPointer(IntPtr dataPointer)
		{
			#if UNITY_STANDALONE || UNITY_EDITOR
			FreeUncompressedData(dataPointer);
			#endif
		}

        static Color32[] FlipChannels(Color32[] pixelData)
        {
            for (int i = 0; i < pixelData.Length; i++)
            {
                Color32 pixel = pixelData[i];
                Color32 flippedPixel = new Color32(pixel.a, pixel.b, pixel.g, pixel.r);
                pixelData[i] = flippedPixel;
            }

            return pixelData;
        }

		public override byte[] GetData(string filePath, int mipLevels)
        {
            #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            System.IntPtr sizePtr = IntPtr.Zero;
            IntPtr dataPointer = GetTextureData(filePath, (UInt32)mipLevels, ref sizePtr);

            int[] managedArray = new int[mipLevels];
            Marshal.Copy(sizePtr, managedArray, 0, mipLevels);

            int dataLength = managedArray[0];

            byte[] pvrtcData = new byte[dataLength];
            Marshal.Copy(dataPointer, pvrtcData, 0, dataLength);

            return pvrtcData;
            #else
            return null;
            #endif
        }

		public override void EncodeToDisk(string filePath, Texture2D texture, int quality, int mipLevels)
        {
            #if UNITY_STANDALONE || UNITY_EDITOR
			byte[] pixelByteData = texture != null ? GetImageBytes(texture) : null;

			if (pixelByteData == null)
			{
				Debug.LogError("Input texture is null!");
				return;
			}

			int width = texture.width;
			int height = texture.height;

			CompressTextureToFile(filePath, pixelByteData, (UInt32)width, (UInt32)height, (UInt32)mipLevels, (UInt32)quality, premultiply, dither);
            #endif
        }
    }
}
