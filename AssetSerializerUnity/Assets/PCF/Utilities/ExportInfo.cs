using UnityEngine;
using System.Collections;

namespace PCFFileFormat
{
	public class ExportInfo : MonoBehaviour 
	{
		[SerializeField]
		public UnityVersion Version;

		[SerializeField]
		public ExportDate Date;
	}

	[System.Serializable]
	public class UnityVersion
	{
		public string Comment;
		public int VersionMajor;
		public int VersionMinor;
		public int Fix;
	}

	[System.Serializable]
	public class ExportDate
	{
		public int Year;
		public int Month;
		public int Day;
		public int Hour;
		public int Minute;
	}
}

