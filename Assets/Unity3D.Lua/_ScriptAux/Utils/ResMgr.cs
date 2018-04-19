using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using G00D1DEA.Pike;

namespace utils
{
	public class ResMgr
	{
		public static byte[] LoadBytes(string path, bool encrypted=false)
		{
			var asset = Resources.Load<TextAsset>(path);
			if (asset != null)
			{
				if (encrypted)
				{
					if (pike != null)
					{
						var bytes = asset.bytes;
						pike.Codec(ref bytes);
						return bytes;
					}
					else
					{
						Debug.LogError("load encrypted data without crypto key");
						return null;
					}
				}
				return asset.bytes;
			}
			return null;
		}

		public static string LoadText(string path, bool encrypted=false)
		{
			var asset = Resources.Load<TextAsset>(path);
			if (asset != null)
			{
				if (encrypted)
				{
					if (pike != null)
					{
						var bytes = asset.bytes;
						pike.Codec(ref bytes);
						return System.Text.Encoding.UTF8.GetString(bytes);
					}
					else
					{
						Debug.LogError("load encrypted data wihtout crypto key");
						return null;
					}
				}
				return asset.text;
			}
			return null;
		}

		public static Object LoadObject(string path)
		{
			return Resources.Load(path);
		}

		public static Object[] LoadObjects(string path)
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

		public static Texture LoadTexture2D(string uri)
		{
			return Resources.Load<Texture2D>(uri);
		}

		public static Object Load(string uri, System.Type type)
		{
			Debug.Assert(type != null);
			return Resources.Load(uri, type);
		}

		public static Object[] LoadAll(string uri, System.Type type)
		{
			Debug.Assert(type != null);
			return Resources.LoadAll(uri, type);
		}

		static IEnumerator AsyncLoader(ResourceRequest r, System.Action<int, Object> progress)
		{
			while (!r.isDone)
			{
				int prog = (int)((r.progress / 100) * 99);
				progress(prog, null);
				yield return null;
			}
			progress(100, r.asset);
		}

		public static void LoadAsync(string uri, System.Action<int, Object> progress, MonoBehaviour workerBehaviour = null)
		{
			var r =  Resources.LoadAsync(uri);
			TaskManager.StartCoroutine(AsyncLoader(r, progress), workerBehaviour);
		}
		

		public static void UnloadUnused()
		{
			Resources.UnloadUnusedAssets();
		}

		static Pike pike;
		public static void SetCryptoKey(uint key)
		{
			pike = new Pike(key);
		}

	}
}
