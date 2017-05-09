using System;
using UnityEngine;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace PCFFileFormat
{
	public enum ASTCCompressionQuality
	{
	    veryfast,
	    fast,
	    medium,
	    thorough,
	    exhaustive
	};

	public enum ASTCCompressionRate
	{
	    Four_bpp,
	    Six_bpp,
	    Eight_bpp,
	}

	public class ASTCEncoderWrapper : TextureEncoderBase
	{
		[DllImport("AssetSerializerASTC")]
	    static extern IntPtr CompressTextureToFile(string output_file, byte[] pvrData, int xsize, int ysize,
	        string quality, string rate);

		[DllImport("AssetSerializerASTC")]
	    static extern IntPtr GetCompressedData(string astcFile, ref int dataSize);

		[DllImport("AssetSerializerASTC")]
	    private static extern void SetDebugLogCallback(PVRTCEncoderWrapper.stringParameterCallback functionDelegate);

		[DllImport("AssetSerializerASTC")]
	    private static extern IntPtr CompressTextureToBuffer(byte[] pvrData, int xsize, int ysize, string quality,
	        string rate, ref int dataSise);

	    [AttributeUsage(AttributeTargets.Method)]
	    public sealed class MonoPInvokeCallbackAttribute : Attribute
	    {
	        public MonoPInvokeCallbackAttribute(Type t)
	        {
	        }
	    }

	    public delegate void stringParameterCallback(string str);

	    static ASTCEncoderWrapper()
	    {
	        SetDebugLogCallback(NativeDebugLog);
	    }

	    [MonoPInvokeCallback(typeof(stringParameterCallback))]
	    public static void NativeDebugLog(string message)
	    {
	        Debug.Log(message);
	    }

		static string textureRate = null;

		public static void SetRate(ASTCCompressionRate rate)
		{
			switch (rate)
			{
			case ASTCCompressionRate.Four_bpp:
				textureRate = "4x4";
				break;
			case ASTCCompressionRate.Six_bpp:
				textureRate = "6x6";
				break;
			case ASTCCompressionRate.Eight_bpp:
				textureRate = "8x8";
				break;
			default:
				throw new NotImplementedException();
			}
		}

		public override byte[] GetData(string filePath, int mipLevels)
	    {
	        #if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
	        int dataSize = 0;
	        byte[] asrtcData = null;
	        if (filePath != null)
	        {
	            IntPtr dataPointer = GetCompressedData(filePath, ref dataSize);

	            asrtcData = new byte[dataSize];
	            Marshal.Copy(dataPointer, asrtcData, 0, dataSize);
	        }

	        return asrtcData;
	        #else
	        return null;
	        #endif
	    }

		public override void EncodeToDisk(string filePath, Texture2D texture, int quality, int mipLevels)
	    {
			#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
			byte[] pixelByteData = texture != null ? GetImageBytes(texture) : null;

			if (pixelByteData == null)
			{
				Debug.LogError("Input texture is null!");
				return;
			}

			int width = texture.width;
			int height = texture.height;
			ASTCCompressionQuality textureQuality = (ASTCCompressionQuality)quality;

			CompressTextureToFile(filePath, pixelByteData, height, width, textureQuality.ToString(), textureRate);
			#endif
	    }
	}
}