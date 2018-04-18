using System.Collections.Generic;
using System.Net;
using AOT;
using System;

namespace utils
{
	public class WebRequest2_Lua
	{
		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		public static int Open(IntPtr L)
		{
			lua.Api.lua_newtable(L);


			lua.Api.lua_pushstring(L, "Download");
			lua.Api.lua_pushcclosure(L, Download_Lua, 0);
			lua.Api.lua_settable(L, -3);

			lua.Api.lua_pushstring(L, "POST");
			lua.Api.lua_pushcclosure(L, POST_Lua, 0);
			lua.Api.lua_settable(L, -3);

			lua.Api.lua_pushstring(L, "GET");
			lua.Api.lua_pushcclosure(L, GET_Lua, 0);
			lua.Api.lua_settable(L, -3);

			return 1;
		}




		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		public static int Download_Lua(IntPtr L)
		{
			string url = lua.Api.lua_tostring(L, 1);
			var host = lua.Lua.CheckHost(L);
			var complete = lua.LuaFunction.MakeRefTo(host, 2);
			WebRequest2.Download(url, (data) =>
			{
				complete.Invoke(data);
				complete.Dispose();
			});
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		public static int GET_Lua(IntPtr L)
		{
			string url = lua.Api.lua_tostring(L, 1);
			string function = lua.Api.lua_tostring(L, 2);
			string queryString = lua.Api.lua_tostring(L, 3);
			lua.LuaFunction complete = null;
			if (lua.Api.lua_isfunction(L, 4))
			{
				var host = lua.Lua.CheckHost(L);
				complete = lua.LuaFunction.MakeRefTo(host, 4);
			}
			var context = (WebRequest2.Context)lua.Lua.ObjectAtInternal(L, 5);
			WebRequest2.Get(new System.Uri(url), function, queryString, 
				(s, resCode, payload, cookies, headers, localContext) => 
				{
					if (complete != null)
					{
						if (s == WebExceptionStatus.Success && resCode == HttpStatusCode.OK)
						{
							complete.Invoke(true, payload);
						}
						else
						{
							complete.Invoke(false);
						}
						complete.Dispose();
					}
				}, context);
			return 0;
		}


		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		public static int POST_Lua(IntPtr L)
		{
			string url = lua.Api.lua_tostring(L, 1);
			string function = lua.Api.lua_tostring(L, 2);
			Dictionary<string, object> param = new Dictionary<string, object>();
			if (lua.Api.lua_istable(L, 3))
			{
				lua.Api.lua_pushvalue(L, 3);
				lua.Api.lua_pushnil(L);
				while (lua.Api.lua_next(L, -2) != 0)
				{
					var key = lua.Api.lua_tostring(L, -2);
					var value = lua.Lua.ValueAtInternal(L, -1);
					param.Add(key, value);
					lua.Api.lua_pop(L, 1); // pop value
				}
				lua.Api.lua_pop(L, 1); // pop table
			}

			lua.LuaFunction complete = null;
			if (lua.Api.lua_isfunction(L, 4))
			{
				var host = lua.Lua.CheckHost(L);
				complete = lua.LuaFunction.MakeRefTo(host, 4);
			}

			var context = (WebRequest2.Context)lua.Lua.ObjectAtInternal(L, 5);
			WebRequest2.Post(new System.Uri(url), function, param,
				(s, resCode, payload, cookies, headers, localContext) =>
				{
					if (complete != null)
					{
						if (s == WebExceptionStatus.Success && resCode == HttpStatusCode.OK)
						{
							complete.Invoke(true, payload);
						}
						else
						{
							complete.Invoke(false);
						}
						complete.Dispose();
					}
				}, context);

			return 0;
		}
	}
}