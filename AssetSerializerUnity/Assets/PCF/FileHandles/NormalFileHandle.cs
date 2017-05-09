using UnityEngine;
using System.Collections;
using System.IO;
using System;

namespace PCFFileFormat
{
	public class NormalFileHandle : IFileHandle
	{
	    private FileInfo fileInfo;

	    public string Extension { get { return this.fileInfo.Extension; } }
	    public string Name { get { return this.fileInfo.Name; } }
	    public string FullName { get { return this.fileInfo.FullName; } }
	    public bool Exists { get { return this.fileInfo.Exists; } }
	    public bool Internal { get { return false; } }

	    public NormalFileHandle(FileInfo fileInfo)
	    {
	        this.fileInfo = fileInfo;
	    }

		public NormalFileHandle(string path)
		{
			this.fileInfo = new FileInfo(path);
		}

	    public void Delete()
	    {
	        if (this.fileInfo != null)
	        {
	            this.fileInfo.Delete();
	        }
	    }

	    public Stream GetFileStream(FileMode mode)
	    {
	        FileStream stream = null;
	        try
	        {
	            stream = new FileStream(FullName, mode);
	        }
	        catch (Exception e)
	        {
	            if (stream != null)
	            {
	                stream.Close();
	            }

	            stream = null;

	            Debug.LogError("Unable to open file: " + FullName + " error: " + e.Message);
	        }

	        return stream;
	    }
	}
}
