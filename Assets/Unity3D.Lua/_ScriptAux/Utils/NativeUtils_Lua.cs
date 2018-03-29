using AOT;
using System;
using System.IO;
using UnityEngine;

public class NativeUtils_Lua
{

	public static lua.LuaTable Setup()
	{
		var L = _Init.luaVm;
		var tbl = new lua.LuaTable(L);

		tbl.Push();

		lua.Api.lua_pushstring(L, "LoadTextureFromPersistentData");
		lua.Api.lua_pushcclosure(L, LoadTexture_Lua, 0);
		lua.Api.lua_settable(L, -3);

		lua.Api.lua_pushstring(L, "FileExistsInPersistentData");
		lua.Api.lua_pushcclosure(L, FileExists_Lua, 0);
		lua.Api.lua_settable(L, -3);

		lua.Api.lua_pop(L, 1);
		return tbl;	
	}


	[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
	static int LoadTexture_Lua(IntPtr L)
	{
		var path = lua.Api.lua_tostring(L, 1);
		try
		{
			var bytes = File.ReadAllBytes(Path.Combine(Application.persistentDataPath, path));
			TextureFormat fmt = TextureFormat.ARGB32;
			if (path.EndsWith(".jpg"))
			{
				fmt = TextureFormat.RGB24;
			}
			var tex = new Texture2D(2, 2, fmt, false, true);
			if (tex.LoadImage(bytes))
			{
				lua.Lua.PushObjectInternal(L, tex);
				return 1;
			} 
			else
			{
				lua.Api.lua_pushnil(L);
				lua.Api.lua_pushstring(L, "err LoadImage from " + path);
				return 2;
			}
		}
		catch (Exception e)
		{
			lua.Api.lua_pushnil(L);
			lua.Api.lua_pushstring(L, e.Message);
			return 2;
		}
	}

	[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
	static int FileExists_Lua(IntPtr L)
	{
		var path = lua.Api.lua_tostring(L, 1);
		var b = File.Exists(Path.Combine(Application.persistentDataPath, path));
		lua.Api.lua_pushboolean(L, b);
		return 1;
	}

}
