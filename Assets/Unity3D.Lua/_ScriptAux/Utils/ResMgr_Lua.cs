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
				new lua.Api.luaL_Reg("LoadSprite", LoadSprite_Lua),
				new lua.Api.luaL_Reg("LoadSprites", LoadSprites_Lua),
			};
			lua.Api.luaL_newlib(L, reg);
			return 1;
		}


		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadSprite_Lua(IntPtr L)
		{
			lua.Lua.PushObjectInternal(L, ResMgr.LoadSprite(lua.Api.lua_tostring(L, 1)));
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int LoadSprites_Lua(IntPtr L)
		{
			var sprites = ResMgr.LoadSprites(lua.Api.lua_tostring(L, 1));
			lua.Api.lua_createtable(L, sprites.Length, 0);
			for (int i = 0; i < sprites.Length; ++i)
			{
				lua.Lua.PushObjectInternal(L, sprites[i]);
				lua.Api.lua_seti(L, -2, i);
			}
			return 1;
		}

		

	}
}
