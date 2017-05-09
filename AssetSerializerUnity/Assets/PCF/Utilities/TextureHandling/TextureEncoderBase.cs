using UnityEngine;
using System.Collections;
using System.IO;

namespace PCFFileFormat
{
	public abstract class TextureEncoderBase
	{
		public static string CACHE_SUFFIX = "_cache";

		public TextureEncoderBase()
		{
		}

		public abstract byte[] GetData(string filePath, int mipLevels);

		public abstract void EncodeToDisk(string filePath, Texture2D texture, int quality, int mipLevels);

		public static void SaveTextureToDisk(string savePath, byte[] data)
		{
			FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
			fileStream.Write(data, 0, data.Length);

			if (fileStream != null)
			{
				//Stop reading pcf file.
				fileStream.Dispose();
				fileStream.Close();
			}
		}

		public static byte[] GetImageBytes(Texture2D texture)
		{
			//Get raw pixel data for the texture.
			Color32[] pixelData = texture.GetPixels32();

			//TODO: Explore later...
			//pixelData = FlipChannels(pixelData);

			//Each pixel needs 4 bytes.
			byte[] pixelByteData = new byte[pixelData.Length * 4];
			int index = 0;

			//Convert pixel data to bytearray
			for (int i = 0; i < pixelData.Length; i++)
			{
				Color32 pixel = pixelData[i];

				pixelByteData[index + 0] = pixel.r;
				pixelByteData[index + 1] = pixel.g;
				pixelByteData[index + 2] = pixel.b;
				pixelByteData[index + 3] = pixel.a;

				index += 4;
			}

			return pixelByteData;
		}
	}
}
