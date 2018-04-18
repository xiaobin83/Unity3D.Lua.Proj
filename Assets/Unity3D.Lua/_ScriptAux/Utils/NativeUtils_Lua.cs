using AOT;
using System;
using System.IO;
using UnityEngine;

namespace utils
{
	public class NativeUtils_Lua
	{

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		public static int Open(IntPtr L)
		{
			lua.Api.lua_newtable(L);

			lua.Api.lua_pushstring(L, "LoadTexture");
			lua.Api.lua_pushcclosure(L, LoadTexture_Lua, 0);
			lua.Api.lua_settable(L, -3);

			lua.Api.lua_pushstring(L, "FileExists");
			lua.Api.lua_pushcclosure(L, FileExists_Lua, 0);
			lua.Api.lua_settable(L, -3);

			lua.Api.lua_pushstring(L, "WriteAllBytes");
			lua.Api.lua_pushcclosure(L, WriteAllBytes_Lua, 0);
			lua.Api.lua_settable(L, -3);

			return 1;
		}


		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		public static int WriteAllBytes_Lua(IntPtr L)
		{
			var filename = lua.Api.lua_tostring(L, 1);
			var bytes = lua.Api.lua_tobytes(L, 2);
			File.WriteAllBytes(Path.Combine(Application.persistentDataPath, filename), bytes);
			return 0;
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
}
