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

		public static string LoadText(string path)
		{
			var asset = Resources.Load<TextAsset>(path);
			if (asset != null)
			{
				return asset.text;
			}
			return null;
		}

		public static Object LoadObject(string path)
		{
			return Resources.Load(path);
		}

		public static Object[] LoadAllObjects(string path)
		{
			return Resources.LoadAll(path);
		}

		public static Sprite LoadSprite(string path)
		{
			return Resources.Load<Sprite>(path);
		}

		public static Sprite[] LoadSprites(string path)
		{
			return Resources.LoadAll<Sprite>(path);
		}

	}
}
