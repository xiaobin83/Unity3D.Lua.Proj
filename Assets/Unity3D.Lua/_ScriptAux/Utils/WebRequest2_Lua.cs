using System.Collections;
using System.Collections.Generic;
using System.Net;
using lua;

namespace utils
{
	public class WebRequest2_Lua
	{
		static lua.Lua luaVm;

		public static void SetLua(lua.Lua luaVm)
		{
			WebRequest2_Lua.luaVm = luaVm;
		}

		public static void Download_Lua(string url, lua.LuaFunction complete)
		{
			var localComplete = complete.Retain();
			WebRequest2.Download(url, (data) =>
			{
				localComplete.Invoke(data);
				localComplete.Dispose();
			});
		}

		public static void Post_Lua(string url, string function, lua.LuaTable parameter, lua.LuaFunction complete, WebRequest2.Context context = null, string parametersStr = "")
		{
			Dictionary<string, object> param = new Dictionary<string, object>();

			if (parameter != null)
			{
				var L = luaVm;
				parameter.Push();
				Api.lua_pushnil(L);
				while (Api.lua_next(L, -2) != 0)
				{
					var key = Api.lua_tostring(L, -2);
					var value = L.ValueAt(-1);
					param.Add(key, value);
					Api.lua_pop(L, 1); // pop value
				}
				Api.lua_pop(L, 1); // pop table
			}

			var localComplete = complete.Retain();
			WebRequest2.Post(new System.Uri(url), function, param,
				(s, resCode, payload, cookies, headers, localContext) =>
				{
					if (s == WebExceptionStatus.Success && resCode == HttpStatusCode.OK)
					{
						localComplete.Invoke(true, payload);
					}
					else
					{
						localComplete.Invoke(false);
					}
					localComplete.Dispose();
				}, context, parametersStr);
		}
	}
}