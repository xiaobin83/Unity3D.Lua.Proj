using UnityEngine;
using AOT;
using System;
using System.Data;
using Mono.Data.SqliteClient;
using System.IO;


namespace utils
{
	public class SQLite_Lua
	{
		
		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		public static int Open(IntPtr L)
		{
			lua.Api.lua_newtable(L);

			lua.Api.lua_pushstring(L, "ConnectToFileDb");
			lua.Api.lua_pushcclosure(L, ConnectToFileDb_Lua, 0);
			lua.Api.lua_settable(L, -3);

			lua.Api.lua_pushstring(L, "ExecuteReader");
			lua.Api.lua_pushcclosure(L, ExecuteReader_Lua, 0);
			lua.Api.lua_settable(L, -3);

			lua.Api.lua_pushstring(L, "Close");
			lua.Api.lua_pushcclosure(L, Close_Lua, 0);
			lua.Api.lua_settable(L, -3);

			return 1;
		}


		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int ConnectToFileDb_Lua(IntPtr L)
		{
			var path = lua.Api.lua_tostring(L, 1);
			var overwrite = false;
			if (lua.Api.lua_gettop(L) == 2)
			{
				overwrite = lua.Api.lua_toboolean(L, 2);
			}
			var dbPath = Application.persistentDataPath + "/" + path;
			if (overwrite || !File.Exists(dbPath))
			{
				var dbDir = Path.GetDirectoryName(dbPath);
				if (!Directory.Exists(dbDir))
				{
					Directory.CreateDirectory(dbDir);
				}
				var allBytes = ResMgr.LoadBytes(path);
				File.WriteAllBytes(dbPath, allBytes);
			}
			var conn = new SqliteConnection("URI=file:" + dbPath);
			conn.Open();
			lua.Lua.PushObjectInternal(L, conn);
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int Close_Lua(IntPtr L)
		{
			var conn = (SqliteConnection)lua.Lua.ObjectAtInternal(L, 1);
			conn.Close();
			return 0;
		}


		[MonoPInvokeCallback(typeof(lua.Api.lua_CFunction))]
		static int ExecuteReader_Lua(IntPtr L)
		{
			var conn = (SqliteConnection)lua.Lua.ObjectAtInternal(L, 1);
			var cmdstr = lua.Api.lua_tostring(L, 2);
			var cmd = conn.CreateCommand();
			cmd.CommandText = cmdstr;
			cmd.CommandType = CommandType.Text;
			try
			{
				var reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					lua.Api.lua_pushvalue(L, 3);
					lua.Lua.PushObjectInternal(L, reader);
					lua.Lua.CallInternal(L, 1, 1);
					var shouldBreak = lua.Api.lua_toboolean(L, -1);
					lua.Api.lua_pop(L, 1);
					if (shouldBreak)
						break;
				}
			}
			catch (Exception e)
			{
				lua.Api.lua_pushstring(L, e.Message);
				return 1;
			}
			cmd.Dispose();
			return 0;
		}



	}
}