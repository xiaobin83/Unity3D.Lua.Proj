using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace utils
{
	public class ResMgr
	{

		public static byte[] LoadBytes(string path)
		{
			var asset = Resources.Load<TextAsset>(path);
			if (asset != null)
			{
				return asset.bytes;
			}
			return null;
		}

	}
}
