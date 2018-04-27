using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using AOT;

namespace utils
{

	public class ResMgr_Lua 
	{

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		public static int Open(IntPtr L)
		{
			var reg = new lua.Api.luaL_Reg[]
			{
				new lua.Api.luaL_Reg("LoadBytes", LoadBytes_Lua),
				new lua.Api.luaL_Reg("LoadText", LoadText_Lua),
				new lua.Api.luaL_Reg("LoadObject", LoadObject_Lua),
				new lua.Api.luaL_Reg("LoadObjects", LoadObjects_Lua),
				new lua.Api.luaL_Reg("LoadSprite", LoadSprite_Lua),
				new lua.Api.luaL_Reg("LoadSprites", LoadSprites_Lua),
				new lua.Api.luaL_Reg("LoadTexture2D", LoadTexture2D_Lua),
				new lua.Api.luaL_Reg("Load", Load_Lua),
				new lua.Api.luaL_Reg("LoadAll", LoadAll_Lua),
				new lua.Api.luaL_Reg("LoadAsync", LoadAsync_Lua),
				new lua.Api.luaL_Reg("SetCryptoKey", SetCryptoKey_Lua),
			};
			lua.Api.luaL_newlib(L, reg);
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadBytes_Lua(IntPtr L)
		{
			var encrypted = false;
			if (lua.Api.lua_gettop(L) == 2)
			{
				if (lua.Api.lua_type(L, 2) == lua.Api.LUA_TBOOLEAN)
					encrypted = lua.Api.lua_toboolean(L, 2);
			}
			var bytes = ResMgr.LoadBytes(lua.Api.lua_tostring(L, 1), encrypted);
			if (bytes == null) return 0;
			lua.Api.lua_pushbytes(L, bytes);
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadText_Lua(IntPtr L)
		{
			var encrypted = false;
			if (lua.Api.lua_gettop(L) == 2)
			{
				if (lua.Api.lua_type(L, 2) == lua.Api.LUA_TBOOLEAN)
					encrypted = lua.Api.lua_toboolean(L, 2);
			}
			var text = ResMgr.LoadText(lua.Api.lua_tostring(L, 1), encrypted);
			if (text == null) return 0;
			lua.Api.lua_pushstring(L, text);
			return 1;
		}


		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadObject_Lua(IntPtr L)
		{
			var obj = ResMgr.LoadObject(lua.Api.lua_tostring(L, 1));
			if (obj == null) return 0;
			lua.Lua.PushObjectInternal(L, obj);
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadObjects_Lua(IntPtr L)
		{
			var objs = ResMgr.LoadObjects(lua.Api.lua_tostring(L, 1));
			lua.Api.lua_createtable(L, objs.Length, 0);
			for (int i = 0; i < objs.Length; ++i)
			{
				lua.Lua.PushObjectInternal(L, objs[i]);
				lua.Api.lua_seti(L, -2, i);
			}
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadSprite_Lua(IntPtr L)
		{
			var sprite = ResMgr.LoadSprite(lua.Api.lua_tostring(L, 1));
			if (sprite == null) return 0;
			lua.Lua.PushObjectInternal(L, sprite);
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadSprites_Lua(IntPtr L)
		{
			var sprites = ResMgr.LoadSprites(lua.Api.lua_tostring(L, 1));
			if (sprites == null) return 0;
			lua.Api.lua_createtable(L, sprites.Length, 0);
			for (int i = 0; i < sprites.Length; ++i)
			{
				lua.Lua.PushObjectInternal(L, sprites[i]);
				lua.Api.lua_seti(L, -2, i);
			}
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadTexture2D_Lua(IntPtr L)
		{
			var tex = ResMgr.LoadTexture2D(lua.Api.lua_tostring(L, 1));
			if (tex == null) return 0;
			lua.Lua.PushObjectInternal(L, tex);
			return 1;
		}


		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int Load_Lua(IntPtr L)
		{
			var obj = ResMgr.Load(lua.Api.lua_tostring(L, 1), (System.Type)lua.Lua.ObjectAtInternal(L, 2));
			if (obj == null) return 0;
			lua.Lua.PushObjectInternal(L, obj);
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadAll_Lua(IntPtr L)
		{
			var objs = ResMgr.LoadAll(lua.Api.lua_tostring(L, 1), (System.Type)lua.Lua.ObjectAtInternal(L, 2));
			if (objs == null) return 0;

			lua.Api.lua_createtable(L, objs.Length, 0);
			for (int i = 0; i < objs.Length; ++i)
			{
				lua.Lua.PushObjectInternal(L, objs[i]);
				lua.Api.lua_seti(L, -2, i);
			}
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadAsync_Lua(IntPtr L)
		{
			MonoBehaviour workerBehaviour = null;
			var func = (lua.LuaFunction)lua.Lua.ValueAtInternal(L, 2);
			ResMgr.LoadAsync(lua.Api.lua_tostring(L, 1), (progress, obj) => {
				func.Invoke(progress, obj);
				if (progress == 100)
					func.Dispose();
			}, workerBehaviour);
			return 0;
		}


		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int SetCryptoKey_Lua(IntPtr L)
		{
			var key = (uint)(long)lua.Api.lua_tointeger(L, 1);
			ResMgr.SetCryptoKey(key);
			return 0;
		}

	}
}
