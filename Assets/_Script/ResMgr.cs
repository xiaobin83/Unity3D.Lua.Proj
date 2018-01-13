using UnityEngine;
using System.Collections;

public class ResMgr {

	public static Sprite LoadSprite(string uri)
	{
		return Resources.Load<Sprite>(uri);
	}

	public static Object LoadObject(string uri)
	{
		return Resources.Load(uri);
	}

	public static byte[] LoadBytes(string uri)
	{
		var text = Resources.Load<TextAsset>(uri);
		return text.bytes;
	}

	public static string LoadString(string uri)
	{
		var text = Resources.Load<TextAsset>(uri);
		return text.text;
    }

	public static void UnloadUnused()
	{
		Resources.UnloadUnusedAssets();
	}
}
