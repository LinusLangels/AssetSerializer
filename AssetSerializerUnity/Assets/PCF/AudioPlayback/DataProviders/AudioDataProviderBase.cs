using UnityEngine;
using System.Collections;
using System;

namespace PCFFileFormat
{
	public abstract class AudioDataProviderBase
	{
	    public abstract bool Initialize(IntPtr instance);
	    public abstract bool Generate(IntPtr instance, int bufferSize);
		public abstract bool SetPosition(IntPtr instance, int position);
	    public abstract void Destroy(IntPtr instance);
	}
}
