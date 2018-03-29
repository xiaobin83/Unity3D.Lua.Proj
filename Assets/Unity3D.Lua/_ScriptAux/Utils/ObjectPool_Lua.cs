using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool_Lua
{
	static lua.LuaFunction poolDelegate;
	public static void SetDelegate(lua.LuaFunction poolDelegate)
	{
		if (ObjectPool_Lua.poolDelegate != null)
			ObjectPool_Lua.poolDelegate.Dispose();
		if (poolDelegate != null)
			ObjectPool_Lua.poolDelegate = poolDelegate.Retain();
		else
			ObjectPool_Lua.poolDelegate = null;
	}

	public static GameObject Obtain(string type, string uri)
	{
		return (GameObject)poolDelegate.Invoke1(0, type, uri);
	}

	public static GameObject Obtain(GameObject prefab)
	{
		return (GameObject)poolDelegate.Invoke1(1, prefab);
	}

	public static void Release(GameObject obj, float delay = 0)
	{
		poolDelegate.Invoke(2, obj, delay);
	}

}
