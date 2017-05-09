using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LightprobeSerializeOpts {

	private string probesDir;

	public LightprobeSerializeOpts(string probesDir)
	{
		this.probesDir = probesDir;
	}

	public virtual Dictionary<string, FileInfo> ListInternalBundles()
	{
		Dictionary<string, FileInfo> internalBundle = new Dictionary<string, FileInfo>();

		BuildTarget[] targets = new BuildTarget[] { BuildTarget.StandaloneOSXIntel, BuildTarget.iOS, BuildTarget.Android };

		foreach (BuildTarget target in targets)
		{
			DirectoryInfo bundleDir = new DirectoryInfo(Path.Combine(this.probesDir, target.ToString()));

			if (bundleDir.Exists)
			{
				FileInfo[] bundlePath = bundleDir.GetFiles("*.internalbundle");

				if (bundlePath.Length > 0)
				{
					internalBundle.Add(target.ToString(), bundlePath[0]);
				}
			}
		}

		return internalBundle;
	}
}
