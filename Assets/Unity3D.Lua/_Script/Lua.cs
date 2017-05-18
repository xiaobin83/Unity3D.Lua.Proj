/*
MIT License

Copyright (c) 2016 xiaobin83

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

// ALL MonoPInvokeCallback SHOULD BE NO THROW
// ALL MonoPInvokeCallback SHOULD BE NO THROW
// ALL MonoPInvokeCallback SHOULD BE NO THROW

// NEVER throw Lua exception in a C# native function
// NEVER throw C# exception in lua_CFunction
// catch all C# exception in any lua_CFunction and use PushErrorObject and return 1 result if error happens
// CHECK above when you write down any thing.
// if Panic (LuaFatalException) thrown, there is no way to resume execution after it (stack frame changed)


namespace lua
{
	public delegate void LogDelegate(string message);

	public class Config
	{
		// if array.Length > value, then array is passed as C# object.
		// except byte[], bytes always copied to Lua state.
		public static int PassAsObjectIfArrayLengthGreatThan = 30;

		static LogDelegate log_;
		public static LogDelegate Log
		{
			get
			{
				if (log_ != null)
					return log_;
				return (message) => Debug.Log(message);
			}
			set
			{
				log_ = value;
			}
		}

		static LogDelegate logWarning_;
		public static LogDelegate LogWarning
		{
			get
			{
				if (logWarning_ != null)
					return logWarning_;
				return (message) => Debug.LogWarning(message);
			}
			set
			{
				logWarning_ = value;
			}
		}

		static LogDelegate logError_;
		public static LogDelegate LogError
		{
			get
			{
				if (logError_ != null)
					return logError_;
				return (message) => Debug.LogError(message);
			}
			set
			{
				logError_ = value;
			}
		}


	}

	public class LuaException : Exception
	{
		public LuaException(string errorMessage)
			: base(errorMessage)
		{
		}
		public LuaException(string errorMessage, int code)
			: base("["+code+"]"+errorMessage)
		{
		}
	}

	public class LuaFatalException : Exception
	{
		public LuaFatalException(string errorMessage)
			: base(errorMessage)
		{
			Config.LogError("LUA FATAL: " + errorMessage);
		}
	}




	public class Lua : IDisposable
	{
		IntPtr L;

		public bool valid
		{
			get
			{
				return L != IntPtr.Zero;
			}
		}

		[MonoPInvokeCallback(typeof(Api.lua_Alloc))]
		static IntPtr Alloc(IntPtr ud, IntPtr ptr, UIntPtr osize, UIntPtr nsize)
		{
			try
			{
				if (nsize == UIntPtr.Zero)
				{
					if (ptr != IntPtr.Zero)
						Marshal.FreeHGlobal(ptr);
					return IntPtr.Zero;
				}
				else
				{
					if (ptr != IntPtr.Zero)
						return Marshal.ReAllocHGlobal(ptr, new IntPtr((long)nsize.ToUInt64()));
					else
						return Marshal.AllocHGlobal(new IntPtr((long)nsize.ToUInt64()));
				}
			}
			catch (Exception e)
			{
				Config.LogError(string.Format("Alloc nsize = {0} failed: {1}", nsize, e.Message));
				return IntPtr.Zero;
			}
		}

		LuaTable csharp;

		const string kLuaStub_SetupSearcher = 
			"return function(s)\n" + 
#if UNITY_EDITOR
			"  table.insert(package.searchers, 2, s)\n" + // after preload
#else
			"  package.searchers = { package.searchers[1], s }\n" + // keep only preload
#endif
			"end";

		// http://lua-users.org/wiki/HexDump
		const string kLuaStub_HexDump =
			"function (buf)\n" +
			"  local s = { '' }\n" + 
			"  for byte=1, #buf, 16 do\n" +
			"    local chunk = buf:sub(byte, byte+15)\n" +
			"    s[1] = s[1] .. string.format('%08X ',byte-1)\n" +
			"    chunk:gsub('.', function (c) s[1] = s[1] .. string.format('%02X ', string.byte(c)) end)\n" +
			"    s[1] = s[1] .. string.rep(' ',3*(16-#chunk))\n" +
			"    s[1] = s[1] .. '\t' .. chunk:gsub('[^%a%d]','.') .. '\\n'\n"+
			"  end\n" +
			"  return s[1]\n" +
			"end";
		LuaFunction hexDump;

		const string kLuaStub_Privillage =
			"local private_mark_meta = {}\n" +
			"local exact_mark_meta = {}\n" +
			"local mark_private = function()\n" +
			"  return setmetatable({}, private_mark_meta)\n" +
			"end\n" +
			"local mark_exact = function(...)\n" +
			"  return setmetatable({...}, exact_mark_meta)\n" +
			"end\n" +
			"local mark_generic = function(...)\n" +
			"  return setmetatable({...}, generic_mark_meta)\n" +
			"end\n" +
			"local test_privillage = function(attr, testOnlyPrivate)\n" +
			"  if #attr < 2 then error('incorrect privillage') end\n" +
			"  testOnlyPrivate = testOnlyPrivate or 1\n" +
			"  local name = attr[#attr]\n" +
			"  local private = false\n" +
			"  local exact\n" +
			"  local generic\n" +
			"  for i = 1, #attr - 1 do\n" +
			"    local v = attr[i]\n" +
			"    local meta = getmetatable(v)\n" +
			"    if meta == private_mark_meta then\n" +
			"      private = true\n" +
			"    elseif (testOnlyPrivate == 0) and (meta == exact_mark_meta or meta == generic_mark_meta) then\n" +
			"      local types = csharp.make_array('System.Type', #v)\n" +
			"      for j, t in ipairs(v) do\n" +
			"        types[j-1] = csharp.get_type(t)\n" +
			"      end\n" +
			"      if meta == exact_mark_meta then\n" +
			"        exact = types\n" +
			"      else\n" +
			"        generic = types\n" +
			"      end\n" +
			"    end\n" +
			"  end\n" +
			"  return name, private, exact, generic\n" +
			"end\n" +
			"return mark_private, mark_exact, mark_generic, test_privillage";
		internal LuaFunction testPrivillage;

		const string kLuaStub_ErrorObject =
			"local error_meta = { __tostring = function(e) return e.message end }\n" +
			"local table_pack, setmetatable, getmetatable = table.pack, setmetatable, getmetatable\n" +
			"return function(message) -- push error object\n" + 
			"  local errObj = { message = message }\n" +
			"  return setmetatable(errObj, error_meta)\n" +
			"end,\n" +
			"function(...) -- check error object, error() if got error, returns all value got if no error\n" +
			"  local r = table_pack(...)\n" + 
			"  if #r > 0 then\n" +
			"    local e = r[1] \n" +
			"    local m = getmetatable(e)\n" +
			"    if m == error_meta then error('\\ninvoking native function failed: ' .. e.message) end\n" +
			"  end\n" +
			"  return ... -- nothing to check\n" +
			"end,\n" +
			"function(...) -- test error object, return isErrorObject and values got from parameters\n" +
			"  local r = table_pack(...)\n" +
			"  if #r > 0 then\n" + 
			"    local e = r[1]\n" +
			"    local m = getmetatable(e)\n" + 
			"    if m == error_meta then return true, ... end\n" + 
			"  end\n" +
			"  return false, ...\n" +
			"end";


		LuaFunction pushError;
		LuaFunction checkError;
		LuaFunction testError; // todo, replace checkError with testError

		const string kLuaStub_Bytes =
			"local hex_dump = csharp.hex_dump\n" +
			"local bytes_meta = { __tostring = function(t) return 'Length: ' .. tostring(#t[1]) .. '\\n' .. hex_dump(t[1]) end}\n" +
			"local assert, type = assert, type\n" +
			"return function(s) -- as_bytes, convert string to bytes object\n" +
			"  assert(type(s) == 'string', 'expected string, but '.. type(s) .. ' got')\n" +
			"  return setmetatable({s}, bytes_meta)\n" +
			"end,\n" +
			"function(b) -- return string if is a bytes object, or nil\n" +
			"  return getmetatable(b) == bytes_meta and b[1] or nil\n" +
			"end";
		LuaFunction testBytes;


		const string kLuaStub_AsArray = 
			"return function(t, tbl)\n" +
			"  if #tbl > 0 then\n" +
			"    local arr = csharp.make_array(t, #tbl)\n" +
			"    for i, e in ipairs(tbl) do\n"+
			"      arr[i-1] = e\n"+
			"    end\n"+
			"    return arr\n" + 
			"  end\n" + 
			"  return nil\n" +
			"end";

		const string kLuaStub_CheckedImport = 
			"function(lib) return csharp.check_error(csharp.import(lib)) end";

		const string kLuaStub_TypeOf = 
			"local type_of_mark = {}\n" +  // use table to avoid check string for every indexing in C#
			"return function(obj) -- typeof\n" +
			"  return obj[type_of_mark]\n" +
			"end,\n" +
			"function(mark) return mark == type_of_mark end -- isIndexingTypeObject";
		internal LuaFunction isIndexingTypeObject;

		// temp	solution for bp in lua
		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int _Break(IntPtr L)
		{
			return 0;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int Print(IntPtr L)
		{
			Config.Log(Api.lua_tostring(L, 1));
			return 0;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int ToEnum(IntPtr L)
		{
			var enumType = (Type)ObjectAtInternal(L, 1);
			if (enumType == null || !enumType.IsEnum)
			{
				PushErrorObject(L, "expected enum type at argument 1");
				return 1;
			}
			if (Api.lua_isinteger(L, 2))
			{
				var value = Api.lua_tointeger(L, 2);
				try
				{
					PushObjectInternal(L, System.Enum.ToObject(enumType, value));
				}
				catch (Exception e)
				{
					PushErrorObject(L, e.Message);
				}
			}
			else
			{
				PushErrorObject(L, "expected integer at argumetn 2");
			}
			return 1;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int GetTypeFromString(IntPtr L)
		{
			if (Api.lua_isstring(L, 1))
			{
				var typeName = Api.lua_tostring(L, 1);
				var typeObj = GetTypeFromString(typeName);
				if (typeObj != null)
				{
					PushObjectInternal(L, typeObj);
					return 1;
				}
			}
			Api.lua_pushnil(L);
			return 1;
		}

		static Type GetTypeFromString(string typeName)
		{
			if (typeName == "string")
			{
				return typeof(string);
			}
			else if (typeName == "int")
			{
				return typeof(int);
			}
			else if (typeName == "float")
			{
				return typeof(float);
			}
			else if (typeName == "double")
			{
				return typeof(double);
			}
			else if (typeName == "long")
			{
				return typeof(long);
			}
			else if (typeName == "byte")
			{
				return typeof(byte);
			}
			else if (typeName == "char")
			{
				return typeof(char);
			}
			else if (typeName == "uint")
			{
				return typeof(uint);
			}
			else if (typeName == "ulong")
			{
				return typeof(ulong);
			}
			else if (typeName == "object")
			{
				return typeof(object);
			}
            else
			{
				try
				{
					return loadType(typeName);
				}
				catch
				{
					return null;
				}
			}
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int MakeArray(IntPtr L)
		{
			Type typeObj;
			if (Api.lua_isstring(L, 1))
			{
				var typeName = Api.lua_tostring(L, 1);
				typeObj = GetTypeFromString(typeName);
				if (typeObj == null)
				{
					PushErrorObject(L, string.Format("MakeArray error, typeName = '{0}' at argument 1 not found", typeName));
					return 1;
				}
			}
			else
			{
				typeObj = (Type)ObjectAtInternal(L, 1);
				if (typeObj == null)
				{
					PushErrorObject(L, "MakeArray error, expected Type object for argument 1 but nil got");
					return 1;
				}
			}

			if (!Api.lua_isnumber(L, 2))
			{
				PushErrorObject(L, "MakeArray error, expected number for argument 2");
				return 1;
			}

			var length = (int)Api.lua_tonumber(L, 2);
			if (typeObj is Type)
			{
				var obj = Array.CreateInstance((Type)typeObj, length);
				PushObjectInternal(L, obj);
			}
			else
			{
				PushErrorObject(L, string.Format("MakeArray error, expected Type object but {0} got", typeObj.GetType().ToString()));
			}
			return 1;
		}

		public Lua()
		{
#if ALLOC_FROM_CSHARP
			L = Api.lua_newstate(Alloc, IntPtr.Zero);
#else
			L = Api.luaL_newstate();
#endif
			Api.luaL_openlibs(L);
			Api.lua_atpanic(L, Panic);

			// put an anchor at _G['__host']
			SetHost();

			Api.luaL_requiref(L, "csharp", OpenCsharpLib, 1);
			csharp = LuaTable.MakeRefTo(this, -1);
			Api.lua_pop(L, 1); // pop csharp

			// Helpers
			try
			{
				DoString(kLuaStub_Privillage, 4, "privillage");
				// "return mark_private, mark_exact, mark_generic, test_privillage";
				testPrivillage = LuaFunction.MakeRefTo(this, -1);
				var markGeneric = LuaFunction.MakeRefTo(this, -2);
				var markExact = LuaFunction.MakeRefTo(this, -3);
				var markPrivate = LuaFunction.MakeRefTo(this, -4);

				csharp["p_private"] = markPrivate;
				markPrivate.Dispose();

				csharp["p_exact"] = markExact;
				markExact.Dispose();

				csharp["p_generic"] = markGeneric;
				markGeneric.Dispose();

				Api.lua_pop(L, 4); // pop

				DoString(kLuaStub_TypeOf, 2, "typeof");
				// -1 isIndexingTypeObject, -2 typeof
				isIndexingTypeObject = LuaFunction.MakeRefTo(this, -1);
				var luaTypeOf = LuaFunction.MakeRefTo(this, -2);
				csharp["typeof"] = luaTypeOf;
				luaTypeOf.Dispose();
				Api.lua_pop(L, 2); // pop

				// csharp.check_error && csharp.push_error
				DoString(kLuaStub_ErrorObject, 3, "error_object");
				// -1 test, -2 check, -3 push
				testError = LuaFunction.MakeRefTo(this, -1);
				checkError = LuaFunction.MakeRefTo(this, -2);
				pushError = LuaFunction.MakeRefTo(this, -3);
				// also set to csharp table
				csharp["test_error"] = testError;
				csharp["check_error"] = checkError;
				Api.lua_pop(L, 3); // pop

				// ------ BEFORE THIS LINE, ERROR OBJECET is not prepared, so all errors pushed as string

				// checked import
				var f = LuaFunction.NewFunction(this, kLuaStub_CheckedImport, "checked_import");
				csharp["checked_import"] = f;
				f.Dispose();

				// hex dump
				hexDump = LuaFunction.NewFunction(this, kLuaStub_HexDump, "hex_dump");
				csharp["hex_dump"] = hexDump;


				// addPath (C#)
				addPath = LuaFunction.NewFunction(this, kLuaStub_AddPath, "add_path");

				// csharp.as_array (lua)
				DoString(kLuaStub_AsArray, 1);
				var asArray = LuaFunction.MakeRefTo(this, -1);
				csharp["as_array"] = asArray;
				asArray.Dispose();
				Api.lua_pop(L, 1);

				// csharp.to_bytes (lua) and test_bytes (C#)
				DoString(kLuaStub_Bytes, 2);
				// -1 test_bytes (C#), -2 as_bytes (Lua)
				testBytes = LuaFunction.MakeRefTo(this, -1);
				var asBytes = LuaFunction.MakeRefTo(this, -2);
				csharp["as_bytes"] = asBytes;
				asBytes.Dispose();

				Api.lua_pop(L, 2); // pop those

				LuaAdditionalFunctions.Open(this);
			}
			catch (Exception e)
			{
				Config.LogError(e.Message);
				throw e;
			}


			// override searcher (insert in the second place, and also set path and cpath)
			// Searcher provided here will load all script/modules except modules/cmodules running only
			// in editor. Lua will handle those modules itself.
			try
			{
				DoString(kLuaStub_SetupSearcher, 1);
				Api.lua_pushcclosure(L, Searcher, 0);
				Call(1, 0);
			}
			catch (Exception e)
			{
				Config.LogError("Replace searchers failed." + e.Message);
			}


			// set default path
#if UNITY_EDITOR
			// path
			var path = Application.dataPath;
			path = System.IO.Path.Combine(path, "Plugins");
			path = System.IO.Path.Combine(path, "Unity3D.Lua");
			path = System.IO.Path.Combine(path, "Modules");
			path = path.Replace('\\', '/');
			var luaPath = path + "/?.lua;" + path +"/?/init.lua";
			AddPath(luaPath);

			// cpath
			path = Application.dataPath;
			path = System.IO.Path.Combine(path, "Plugins");
			path = System.IO.Path.Combine(path, "Unity3D.Lua");
			path = System.IO.Path.Combine(path, "Windows");
			if (IntPtr.Size > 4)
				path = System.IO.Path.Combine(path, "x86_64");
			else
				path = System.IO.Path.Combine(path, "x86");
			path = path.Replace('\\', '/');
			var luaCPath = path + "/?.dll;"+path + "/?/init.dll";
			AddCPath(luaCPath);
#endif
		}

		public void Dispose()
		{
			if (L == IntPtr.Zero)
				return;

			addPath.Dispose();
			addPath = null;

			testPrivillage.Dispose();
			testPrivillage = null;

			isIndexingTypeObject.Dispose();
			isIndexingTypeObject = null;

			testError.Dispose();
			testError = null;

			checkError.Dispose();
			checkError = null;

			pushError.Dispose();
			pushError = null;

			testBytes.Dispose();
			testBytes = null;

			hexDump.Dispose();
			hexDump = null;

			csharp.Dispose();
			csharp = null;
			
			Api.lua_close(L);
			L = IntPtr.Zero;
		}

		public static implicit operator IntPtr(Lua l)
		{
			if (l != null)
				return l.L;
			return IntPtr.Zero;
		}

		const string kLuaStub_AddPath = 
			"function(p, path)\n" + 
#if UNITY_EDITOR
			"  package[p] = path .. ';' .. package[p]\n" +
#endif
			"end";
		LuaFunction addPath;

		public void AddCPath(string cpath)
		{
			addPath.Invoke(null, "cpath", cpath);
		}

		public void AddPath(string path)
		{
			addPath.Invoke(null, "path", path);
		}

		const string kLuaStub_ForbidGlobalVar =
			"return function(forbid)\n" + 
			"  if forbid then\n" +
			"    setmetatable(_G, forbid and { __newindex = function(t, k, v) assert(false, 'set value on global table is forbidden.') end} or {})\n" +
			"  end\n" +
			"end";

		public void ForbidGlobalVar(bool forbid)
		{
			Api.luaL_dostring(L, kLuaStub_ForbidGlobalVar);
			Api.lua_pushboolean(L, forbid);
			Call(1, 0);
		}

		public void SetGlobal(string name, object value)
		{
			PushValue(value);
			Api.lua_setglobal(L, name);
		}

		public void RunScript(string scriptName)
		{
			string scriptPath;
			LoadChunkFromFile(L, scriptName, out scriptPath);
			Call(0, 0);
		}

		public object RunScript1(string scriptName)
		{
			string scriptPath;
			var top = Api.lua_gettop(L);
			LoadChunkFromFile(L, scriptName, out scriptPath);
			Call(0, 1);
			var ret = ValueAt(-1);
			Api.lua_settop(L, top);	// should left nothing on stack
			return ret;
		}

		public object Require(string scriptName)
		{
			Api.luaL_requiref(L, scriptName, LoadScript1, 0);
			string errorMessage;
			if (Lua.TestError(L, -1, out errorMessage))
			{
				Config.LogError(errorMessage);
				return null;
			}
			var ret = ValueAt(-1);
			Api.lua_pop(L, 1);
			return ret;
		}

		public string HexDump(byte[] data)
		{
			if (hexDump != null)
			{
				return (string)hexDump.Invoke1(null, data);
			}
			return string.Empty;
		}

		const string kHost = "__host";

		void SetHost()
		{
			PushObject(this);
			Api.lua_setglobal(L, kHost);
		}

		internal static Lua CheckHost(IntPtr L)
		{
			Lua host = null;
			var top = Api.lua_gettop(L);
			if (Api.lua_getglobal(L, kHost) == Api.LUA_TUSERDATA)
			{
				host = ObjectAtInternal(L, -1) as Lua;
			}
			Api.lua_settop(L, top);
			if (host == null) // coroutine -> host.L != L)
			{
				throw new LuaException("__host not found or mismatch.");
			}
			return host;
		}

		internal static void Assert(bool condition, string message = "assertion failed.")
		{
			if (!condition) throw new LuaException(message);
		}

		// Searchers

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int Searcher(IntPtr L)
		{
			var top = Api.lua_gettop(L);
			var scriptName = Api.lua_tostring(L, 1);
			try
			{
				string scriptPath = string.Empty;
				LoadChunkFromFile(L, scriptName, out scriptPath);
				PushValueInternal(L, scriptPath);
				return 2;
			}
			catch (Exception e)
			{
				Api.lua_settop(L, top);
				Api.lua_pushstring(L, string.Format("\n\terror loading module '{0}' {1}", scriptName, e.Message));
				return 1;
			}
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int DoFile(IntPtr L)
		{
			var top = Api.lua_gettop(L);
			var scriptName = Api.lua_tostring(L, 1);
			try
			{
				string scriptPath = string.Empty;
				LoadChunkFromFile(L, scriptName, out scriptPath);
				CallInternal(L, 0, Api.LUA_MULTRET);
				return Api.lua_gettop(L) - top;
			}
			catch (Exception e)
			{
				Api.lua_settop(L, top);
				Api.lua_pushstring(L, e.Message);
			}
			return 1;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int OpenCsharpLib(IntPtr L)
		{
			var regs = new Api.luaL_Reg[]
			{
				new Api.luaL_Reg("import", Import),
				new Api.luaL_Reg("dofile", DoFile),
				new Api.luaL_Reg("_break", _Break),
				new Api.luaL_Reg("make_array", MakeArray),
				new	Api.luaL_Reg("to_enum",	ToEnum),
				new Api.luaL_Reg("get_type", GetTypeFromString),
                new	Api.luaL_Reg("print", Print)
			};
			Api.luaL_newlib(L, regs);
			return 1;
		}

		public static LuaTypeLoaderAttribute.TypeLoader typeLoader;
		static LuaTypeLoaderAttribute.TypeLoader loadType
		{
			get
			{
				if (typeLoader == null)
					return DefaultTypeLoader;
				return typeLoader;
			}
		}

		static Type DefaultTypeLoader(string typename)
		{
			var type = Type.GetType(typename);
			if (type == null)
			{
#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					var load = LuaTypeLoaderAttribute.GetLoader();
					if (load != null)
						type = load(typename);
				}
#endif
			}
			return type;
		}
		

		public static LuaScriptLoaderAttribute.ScriptLoader scriptLoader;
		static LuaScriptLoaderAttribute.ScriptLoader loadScriptFromFile
		{
			get
			{
				if (scriptLoader == null)
					return DefaultScriptLoader;
				return scriptLoader;
			}
		}


		static string GetScriptPath(string scriptName)
		{
			if (string.IsNullOrEmpty(scriptName)) return scriptName;
			var path = "";
			path = System.IO.Path.Combine(Application.streamingAssetsPath, "LuaRoot");
			path = System.IO.Path.Combine(path, scriptName);
			path = path + ".lua";
			if (System.IO.Path.DirectorySeparatorChar != '/')
			{
				path = path.Replace(System.IO.Path.DirectorySeparatorChar, '/');
			}
			return path;
		}

		static byte[] DefaultScriptLoader(string scriptName, out string scriptPath)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				var loader = LuaScriptLoaderAttribute.GetLoader();
				if (loader != null)
				{
					return loader(scriptName, out scriptPath);
				}
			}
#endif

			var path = GetScriptPath(scriptName);
#if !UNITY_EDITOR && UNITY_ANDROID
			var www = new WWW(path);
#else
			var www = new WWW("file:///" + path);
#endif
			while (!www.isDone);

			if (!string.IsNullOrEmpty(www.error))
			{
				throw new Exception(www.error);
			}

			scriptPath = path;
			return www.bytes;
		}

		static void LoadChunkFromFile(IntPtr L, string scriptName, out string scriptPath)
		{
			var bytes = loadScriptFromFile(scriptName, out scriptPath);
			if (bytes == null)
			{
				throw new LuaException(string.Format("0 bytes loaded from {0}", scriptName));
			}
			var chunkName = string.Format("@{0}", scriptPath);
#if UNITY_EDITOR
			chunkName = chunkName.Replace('/', '\\');
#endif
			LoadChunkInternal(L, bytes, chunkName);
		}

		static void LoadScriptInternal(IntPtr L, string scriptName, int nret, out string scriptPath)
		{
			LoadChunkFromFile(L, scriptName, out scriptPath);
			CallInternal(L, 0, nret);
		}

		// Run script and adjust the numb of return	value to 1
		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int LoadScript(IntPtr L)
		{
			try
			{
				return LoadScriptInternal(L);
			}
			catch (Exception e)
			{
				PushErrorObject(L, e.Message);
				return 1;
			}
		}

		static int LoadScriptInternal(IntPtr L)
		{
			string scriptName;
			if (!Api.luaL_teststring_strict(L, 1, out scriptName))
			{
				throw new ArgumentException("expected string", "scriptName (arg 1)");
			}
			var top = Api.lua_gettop(L);
			try
			{
				string scriptPath;
				LoadScriptInternal(L, scriptName, Api.LUA_MULTRET, out scriptPath);
			}
			catch (Exception e)
			{
				throw new Exception(string.Format("LoadScript \"{0}\" failed: {1}", scriptName), e);
			}
			return Api.lua_gettop(L) - top;
		}

		// Run script and adjust the numb of return	value to 1
		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int LoadScript1(IntPtr L)
		{
			try
			{
				return LoadScript1Internal(L);
			}
			catch (Exception e)
			{
				PushErrorObject(L, e.Message);
				return 1;
			}
		}

		static int LoadScript1Internal(IntPtr L)
		{
			string scriptName;
			if (!Api.luaL_teststring_strict(L, 1, out scriptName))
			{
				throw new ArgumentException("expected string", "scriptName (arg 1)");
			}
			string scriptPath = string.Empty;
			try
			{
				LoadScriptInternal(L, scriptName, 1, out scriptPath);
			}
			catch (Exception e)
			{
				throw new Exception(string.Format("LoadScript \"{0}\" from path {1} failed:\n{2}", scriptName, scriptPath, e.Message));
			}
			return 1;
		}


#if UNITY_EDITOR
		// LoadScript, return result, scriptPath , have to public for Editor script
		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		public static int LoadScript1InEditor(IntPtr L)
		{
			try
			{
				return LoadScript1InEditorInternal(L);
			}
			catch (Exception e)
			{
				PushErrorObject(L, e.Message);
				return 1;
			}
		}

		static int LoadScript1InEditorInternal(IntPtr L)
		{
			string scriptName;
			if (!Api.luaL_teststring_strict(L, 1, out scriptName))
			{
				throw new ArgumentException("expected string", "scriptName (arg 1)");
			}
			try
			{
				string scriptPath;
				LoadScriptInternal(L, scriptName, 1, out scriptPath);
				PushValueInternal(L, scriptPath);
			}
			catch (Exception e)
			{
				PushErrorObject(L, string.Format("LoadScript2 \"{0}\" failed: {1}", scriptName, e.Message));
				return 1;
			}
			return 2;
		}
#endif

		[MonoPInvokeCallback(typeof(Api.lua_Reader))]
		unsafe static IntPtr ChunkLoader(IntPtr L, IntPtr data, out UIntPtr size)
		{
			try
			{
				return ChunkLoaderInternal(L, data, out size);
			}
			catch (Exception e)
			{
				Config.LogError(string.Format("ChunkLoader error: {0}", e.Message));
				size = UIntPtr.Zero;
				return IntPtr.Zero;
			}
		}

		unsafe static IntPtr ChunkLoaderInternal(IntPtr L, IntPtr data, out UIntPtr size)
		{
			var handleToBinaryChunk = GCHandle.FromIntPtr(data);
			var chunk = handleToBinaryChunk.Target as Chunk;
			var bytes = chunk.bytes.Target as byte[];
			if (chunk.pos < bytes.Length)
			{
				var curPos = chunk.pos;
				size = new UIntPtr((uint)bytes.Length); // read all at once
				chunk.pos = bytes.Length;
				return Marshal.UnsafeAddrOfPinnedArrayElement(bytes, curPos);
			}
			size = UIntPtr.Zero;
			return IntPtr.Zero;
		}

		class Chunk
		{
			public GCHandle bytes;
			public int pos;
		}

		public void LoadChunk(byte[] bytes, string chunkname, string mode = "bt")
		{
			LoadChunkInternal(L, bytes, chunkname, mode);
		}

		public static void LoadChunkInternal(IntPtr L, byte[] bytes, string chunkname, string mode = "bt")
		{
			Assert(bytes != null);

			var c = new Chunk();
			c.bytes = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			c.pos = 0;
			var handleToChunkBytes = GCHandle.Alloc(c);
			var ret = Api.lua_load(L, ChunkLoader, GCHandle.ToIntPtr(handleToChunkBytes), chunkname, mode);
			if (ret != Api.LUA_OK)
			{
				c.bytes.Free();
				handleToChunkBytes.Free();
				var errMsg = Api.lua_tostring(L, -1);
				Api.lua_pop(L, 1);
				throw new LuaException(errMsg, ret);
			}
			c.bytes.Free();
			handleToChunkBytes.Free();
		}

		[MonoPInvokeCallback(typeof(Api.lua_Writer))]
		static int ChunkWriter(IntPtr L, IntPtr p, UIntPtr sz, IntPtr ud)
		{
			try
			{
				return ChunkWriterInternal(L, p, sz, ud);
			}
			catch (Exception e)
			{
				Config.LogError(string.Format("ChunkWriter error: {0}", e.Message));
				return 0;
			}
		}
		static int ChunkWriterInternal(IntPtr L, IntPtr p, UIntPtr sz, IntPtr ud)
		{
			var handleToOutput = GCHandle.FromIntPtr(ud);
			var output = handleToOutput.Target as System.IO.MemoryStream;
			var toWrite = new byte[(int)sz];
			unsafe
			{
				Marshal.Copy(p, toWrite, 0, toWrite.Length);
			}
			output.Write(toWrite, 0, toWrite.Length);
			return 0;
		}

		// Caution! the dumpped chunk is not portable. you can not save it and run on another device.
		public byte[] DumpChunk(bool strip = true)
		{
			if (!Api.lua_isfunction(L, -1))
				return null;

			var output = new System.IO.MemoryStream();
			var outputHandle = GCHandle.Alloc(output);
			Api.lua_dump(L, ChunkWriter, GCHandle.ToIntPtr(outputHandle), strip ? 1:0);
			outputHandle.Free();

			output.Flush();
			output.Seek(0, System.IO.SeekOrigin.Begin);

			var bytes = new byte[output.Length];
			output.Read(bytes, 0, bytes.Length);
			return bytes;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int Panic(IntPtr L)
		{
			var host = CheckHost(L);
			Api.luaL_traceback(host, L, Api.lua_tostring(L, -1), 1);
			Config.LogError(string.Format("LUA FATAL: {0}", Api.lua_tostring(host, -1)));
			return 0;
		}


		internal const string objectMetaTable = "object_meta";
		internal const string classMetaTable = "class_meta";

	
		// [-0,	+1,	m]
		// return 1 if Metatable of object is newly created. 
		internal int PushObject(object obj, string metaTableName = objectMetaTable)
		{
			return PushObjectInternal(L, obj, metaTableName);
		}

		internal static int PushObjectInternal(IntPtr L, object obj, string metaTableName = objectMetaTable)
		{
			var handleToObj = GCHandle.Alloc(obj);
			var ptrToObjHandle = GCHandle.ToIntPtr(handleToObj);
			var userdata = Api.lua_newuserdata(L, new UIntPtr((uint)IntPtr.Size));
			// stack: userdata
			Marshal.WriteIntPtr(userdata, ptrToObjHandle);

			var newMeta = NewObjectMetatable(L, metaTableName);
			// stack: userdata, meta
			Api.lua_setmetatable(L, -2);
			// stack: userdata
			return newMeta;
		}

		public object ObjectAt(int idx)
		{
			return ObjectAtInternal(L, idx);
		}

		internal static IntPtr TestUdata(IntPtr L, int idx)
		{
			var userdata = Api.luaL_testudata(L, idx, objectMetaTable);
			if (userdata == IntPtr.Zero) userdata = Api.luaL_testudata(L, idx, classMetaTable);
			return userdata;
		}

		internal static object ObjectAtInternal(IntPtr L, int idx)
		{
			var userdata = TestUdata(L, idx);
			if (userdata != IntPtr.Zero)
				return UdataToObject(userdata);
			return null;
		}

		internal static object UdataToObject(IntPtr userdata)
		{
			if (userdata == IntPtr.Zero)
			{
				Config.LogError("userdata is null");
				return null;
			}
			var ptrToObjHandle = Marshal.ReadIntPtr(userdata);
			var handleToObj = GCHandle.FromIntPtr(ptrToObjHandle);
			if (handleToObj.Target == null)
			{
				Config.LogError("handleToObj is null");
			}
			return handleToObj.Target;
		}

		public int MakeRefTo(object obj)
		{
			return MakeRefToInternal(L, obj);
		}

		internal static int MakeRefToInternal(IntPtr L, object obj)
		{
			Assert(obj != null);
			var type = obj.GetType();
			Assert(type.IsClass);
			PushObjectInternal(L, obj);
			var refVal = MakeRefAtInternal(L, -1);
			Api.lua_pop(L, 1);
			return refVal;
		}

		public int MakeRefAt(int index)
		{
			return MakeRefAtInternal(L, index);
		}

		internal static int MakeRefAtInternal(IntPtr L, int index)
		{
			Api.lua_pushvalue(L, index);
			var refVal = Api.luaL_ref(L, Api.LUA_REGISTRYINDEX);
			return refVal;
		}

		public void PushRef(int objReference)
		{
			PushRefInternal(L, objReference);
		}

		internal static void PushRefInternal(IntPtr L, int objReference)
		{
			Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, objReference);
		}

		public void Unref(int objReference)
		{
			UnrefInternal(L, objReference);
		}

		internal static void UnrefInternal(IntPtr L, int objReference)
		{
			Api.luaL_unref(L, Api.LUA_REGISTRYINDEX, objReference);
		}

		static byte[] TestBytes(IntPtr L, int idx)
		{
			var host = CheckHost(L);
			Api.lua_pushvalue(L, idx);
			host.testBytes.Push(L); // dont use invoke, preventing conversion from Lua -> C# before value is correct
			Api.lua_insert(L, -2);
			CallInternal(L, 1, 1);
			if (Api.lua_isnil(L, -1))
			{
				Api.lua_pop(L, 1);
				return null;
			}
			else // convert to bytes
			{
				var ret = Api.lua_tobytes(L, -1);
				Api.lua_pop(L, 1);
				return ret;
			}
		}

		public object ValueAt(int idx)
		{
			return ValueAtInternal(L, idx);
		}

		internal static object ValueAtInternal(IntPtr L, int idx)
		{
			var type = Api.lua_type(L, idx);
			switch (type)
			{
				case Api.LUA_TNONE:
				case Api.LUA_TNIL:
					return null;
				case Api.LUA_TBOOLEAN:
					return Api.lua_toboolean(L, idx);
				case Api.LUA_TLIGHTUSERDATA:
					return Api.lua_touserdata(L, idx);
				case Api.LUA_TNUMBER:
					if (Api.lua_isinteger(L, idx))
					{
						return Api.lua_tointeger(L, idx);
					}
					else
					{
						return Api.lua_tonumber(L, idx);
					}
				case Api.LUA_TSTRING:
					return Api.lua_tostring(L, idx);

				case Api.LUA_TTABLE:
					{
						var bytes = TestBytes(L, idx); // maybe a bytes
						if (bytes != null)
						{
							return bytes;
						}
						var host = CheckHost(L);
						if (host == L)
						{
							return LuaTable.MakeRefTo(host, idx);	
						}
						else
						{
							Api.lua_pushvalue(L, idx);
							Api.lua_xmove(L, host, 1);
							var t = LuaTable.MakeRefTo(host, -1);
							Api.lua_pop(host, 1);
							return t;
						}
					}
				case Api.LUA_TFUNCTION:
					{
						var host = CheckHost(L);
						if (host == L)
						{
							return LuaFunction.MakeRefTo(host, idx);
						}
						else
						{
							Api.lua_pushvalue(L, idx);
							Api.lua_xmove(L, host, 1);
							var t = LuaFunction.MakeRefTo(host, -1);
							Api.lua_pop(host, 1);
							return t;
						}
					}
				case Api.LUA_TTHREAD:
					{
						var host = CheckHost(L);
						if (host == L)
						{
							return LuaThread.MakeRefTo(host, idx);
						}
						else
						{
							Api.lua_pushvalue(L, idx);
							Api.lua_xmove(L, host, 1);
							var t = LuaThread.MakeRefTo(host, -1);
							Api.lua_pop(host, 1);
							return t;
						}
					}

				case Api.LUA_TUSERDATA:
					return ObjectAtInternal(L, idx);
				default:
					Config.LogError("Not supported");
					return null;
			}
		}

		static bool IsIntegerType(System.Type type)
		{
			return (type == typeof(System.Int32)
					|| type == typeof(System.UInt32)
					|| type == typeof(System.Int16)
					|| type == typeof(System.UInt16)
					|| type == typeof(System.Int64)
					|| type == typeof(System.UInt64)
					|| type == typeof(System.Byte)
					|| type == typeof(System.SByte)
					|| type == typeof(System.Char));
		}


		static bool IsNumberType(System.Type type)
		{
			return type == typeof(System.Single)
				|| type == typeof(System.Double)
				|| type == typeof(System.Decimal);
		}


		static bool IsNumericType(System.Type type)
		{
			return type.IsPrimitive
				&& (IsIntegerType(type)
				|| type == typeof(System.Single)
				|| type == typeof(System.Double)
				|| type == typeof(System.Decimal));
		}

		static System.Reflection.ParameterInfo IsLastArgVariadic(System.Reflection.ParameterInfo[] args)
		{
			if (args.Length > 0)
			{
				// check if	last one is	variadic parameter
				var lastArg = args[args.Length - 1];
				if (lastArg.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
				{
					return lastArg;
				}
			}
			return null;
		}

		static bool IsVariadic(System.Reflection.ParameterInfo arg)
		{
			return arg.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0;
		}

		static int GetMatchScore(IntPtr L, int argIdx, Type type, int luaArgType)
		{
			if (type.IsByRef)
				type = type.GetElementType(); // strip byref
			if (IsNumericType(type))
			{
				if (luaArgType == Api.LUA_TNUMBER) return 10;
			}
			else if (type == typeof(string))
			{
				if (luaArgType == Api.LUA_TSTRING) return 10;
				else if (luaArgType == Api.LUA_TNUMBER) return 5; // can be converted to string
			}
			else if (type == typeof(bool))
			{
				if (luaArgType == Api.LUA_TBOOLEAN) return 10;
				return 4; // convert to bool
			}
			else if (type == typeof(LuaFunction) || type == typeof(System.Action) || type == typeof(UnityEngine.Events.UnityAction)
				|| typeof(System.Delegate).IsAssignableFrom(type))
			{
				if (luaArgType == Api.LUA_TFUNCTION) return 10;
			}
			else if (type == typeof(LuaTable))
			{
				if (luaArgType == Api.LUA_TTABLE) return 10;
			}
			else if (type == typeof(LuaThread))
			{
				if (luaArgType == Api.LUA_TTHREAD) return 10;
			}
			else if (type == typeof(byte[]))
			{
				if (luaArgType == Api.LUA_TTABLE) return 10; // checked in conversion part
			}
			else if (type == typeof(object))
			{
				return 5; // can be converted to object
			}
			else if (!type.IsPrimitive) 
			{
				if (luaArgType == Api.LUA_TUSERDATA)
				{
					// check into userdata
					var obj = ObjectAtInternal(L, argIdx);
					if (obj != null)
					{
						var objType = obj.GetType();
						if (type == objType) return 10;
						if (type.IsAssignableFrom(objType)) return 5;
					}
					else
					{
						throw new ArgumentNullException();
					}
				}
			}
			return int.MinValue;
		}

		static int GetMatchScore(IntPtr L, int argIdx, System.Reflection.ParameterInfo arg, int luaArgType)
		{
			if (arg.IsOut) return 10;
			if (arg.ParameterType.IsValueType)
			{
				if (luaArgType == Api.LUA_TNIL)
				{
					throw new ArgumentNullException(arg.Name);
				}
			}
			else
			{
				if (luaArgType == Api.LUA_TNIL)
				{
					// non-valuetype can be nil
					return 10;
				}
			}
			try
			{
				return GetMatchScore(L, argIdx, arg.ParameterType, luaArgType);
			}
			catch (ArgumentNullException)
			{
				throw new ArgumentNullException(arg.Name);
			}
		}

		static int GetMatchScoreOfVaridicArg(IntPtr L, int argStart, System.Reflection.ParameterInfo arg, int[] luaArgTypes, int va_start)
		{
			var type = arg.ParameterType.GetElementType();
			var num_va_arg = luaArgTypes.Length - va_start;
			if (num_va_arg == 0)
			{
				return -1;
			}
			if (type == typeof(object)) // params object[] args, common form
			{
				return num_va_arg * 5;
			}
			else
			{
				// all rest type should be the same if not 'params object[] args'
				var t = luaArgTypes[va_start];
				for (int i = va_start + 1; i < luaArgTypes.Length; ++i)
				{
					if (t != luaArgTypes[i]) return int.MinValue; // not match at all
				}
				try
				{
					return (GetMatchScore(L, argStart + va_start, type, t) - 2)* num_va_arg;
				}
				catch (ArgumentNullException)
				{
					throw new ArgumentNullException(arg.Name);
				}
			}
		}

		internal static int GetMatchScoreOfArgs(IntPtr L, int argStart, System.Reflection.ParameterInfo[] args, int[] luaArgTypes)
		{
			var totalScore = 0;
			for (int i = 0; i < args.Length; ++i)
			{
				var arg = args[i];
				var isVariadic = IsVariadic(arg);
				if (isVariadic)
				{
					return totalScore + GetMatchScoreOfVaridicArg(L, argStart, arg, luaArgTypes, i);
				}
				else
				{
					if (i >= luaArgTypes.Length)
					{
						// not enough parameter from lua
						if (arg.IsOptional)
						{
							return totalScore; // if is a optional, return totalScore. implicit that more parameters will get higher score
						}
						return int.MinValue; // required more params than provided
					}
					var s = GetMatchScore(L, argStart + i, arg, luaArgTypes[i]);
					if (s < 0)
					{
						return int.MinValue; // not match at all, no need to continue
					}
					totalScore += s;
				}
			}
			return totalScore;
		}

		static readonly object[] csharpArgs_NoArgs = null;

		internal static object GetDefaultValue(Type type)
		{
			if (type.IsValueType  && type != typeof(void))
			{
				return Activator.CreateInstance(type);
			}
			return null;
		}

		static object SetArg(IntPtr L, System.Array actualArgs, int idx, int luaArgIdx, Type type, int luaType, out bool isDisposable)
		{
			isDisposable = false;
			switch (luaType)
			{
				case Api.LUA_TNIL:
					// do nothing
					break;
				case Api.LUA_TBOOLEAN:
					actualArgs.SetValue(Api.lua_toboolean(L, luaArgIdx), idx);
					break;
				case Api.LUA_TNUMBER:
					object nvalue;
					if (Api.lua_isinteger(L, luaArgIdx))
					{
						nvalue = Api.lua_tointeger(L, luaArgIdx);
					}
					else
					{
						nvalue = Api.lua_tonumber(L, luaArgIdx);
					}

					if (type.IsByRef)
					{
						type = type.GetElementType();
					}
					var converted = System.Convert.ChangeType(nvalue, type);
					actualArgs.SetValue(converted, idx);
					break;
				case Api.LUA_TSTRING:
					actualArgs.SetValue(Api.lua_tostring(L, luaArgIdx), idx);
					break;
				case Api.LUA_TTABLE:
					var bytes = TestBytes(L, luaArgIdx);
					if (bytes != null)
					{
						actualArgs.SetValue(bytes, idx);
					}
					else
					{
						var host = CheckHost(L);
						object t = null;
						if (host == L)
						{
							t = LuaTable.MakeRefTo(host, luaArgIdx);
						}
						else
						{
							Api.lua_pushvalue(L, luaArgIdx);
							Api.lua_xmove(L, host, 1);
							t = LuaTable.MakeRefTo(host, -1);
							Api.lua_pop(L, 1);
						}
						isDisposable = true;
						actualArgs.SetValue(t, idx);
					}
					break;
				case Api.LUA_TFUNCTION:
					{
						var host = CheckHost(L);
						LuaFunction t = null;
						if (host == L)
						{
							t = LuaFunction.MakeRefTo(host, luaArgIdx);
						}
						else
						{
							Api.lua_pushvalue(L, luaArgIdx);
							Api.lua_xmove(L, host, 1);
							t = LuaFunction.MakeRefTo(host, -1);
							Api.lua_pop(L, 1);
						}
						if (type == typeof(System.Action))
						{
							actualArgs.SetValue(LuaFunction.ToAction(t), idx);
							t.Dispose(); // retained in ToAction, unused here
						}
						else if (type == typeof(UnityEngine.Events.UnityAction))
						{
							actualArgs.SetValue(LuaFunction.ToUnityAction(t), idx);
							t.Dispose(); // retained in ToUnityAction, unused here
						}
						else if (type == typeof(LuaFunction))
						{
							isDisposable = true;
							actualArgs.SetValue(t, idx);
						}
						else // generic part
						{
							actualArgs.SetValue(LuaFunction.ToDelegate(type, t), idx);
							t.Dispose(); // retained in LuaFunction.CreateDelegate, unused here
						}

					}
					break;
				case Api.LUA_TTHREAD:
					{
						var host = CheckHost(L);
						object t = null;
						if (host == L)
						{
							t = LuaThread.MakeRefTo(host, luaArgIdx);
						}
						else
						{
							Api.lua_pushvalue(L, luaArgIdx);
							Api.lua_xmove(L, host, 1);
							t = LuaThread.MakeRefTo(host, -1);
							Api.lua_pop(L, 1);
						}
						isDisposable = true;
						actualArgs.SetValue(t, idx);
					}
					break;
				case Api.LUA_TUSERDATA:
					actualArgs.SetValue(ObjectAtInternal(L, luaArgIdx), idx);
					break;
				default:
					if (type != typeof(string) && type != typeof(System.Object))
					{
						Config.LogWarning(string.Format("Convert lua type {0} to string, wanted to fit {1}", Api.ttypename(luaType), type.ToString()));
					}
					actualArgs.SetValue(Api.lua_tostring(L, luaArgIdx), idx);
					break;
			}
			return actualArgs.GetValue(idx);
		}

		internal static object[] ArgsFrom(IntPtr L, System.Reflection.ParameterInfo[] args, System.Reflection.ParameterInfo variadicArg, int argStart, int numArgs, out IDisposable[] disposableArgs)
		{
			if (args == null || args.Length == 0)
			{
				disposableArgs = null;
				return csharpArgs_NoArgs;
			}

			var luaArgCount = numArgs;
			var requiredArgCount = args.Length;
			if (variadicArg != null)
				requiredArgCount = requiredArgCount - 1;

			if (luaArgCount < requiredArgCount)
			{
				Assert(args[luaArgCount].IsOptional, "not enough parameters");
			}

			object[] actualArgs = null;
			if (variadicArg != null)
				actualArgs = new object[requiredArgCount + 1];
			else
				actualArgs = new object[requiredArgCount];

			disposableArgs = new IDisposable[luaArgCount];

			int idx = 0;
			for (int i = 0; i < requiredArgCount; ++i, ++idx)
			{
				var luaArgIdx = argStart + i;
				var arg = args[idx];
				var type = arg.ParameterType;
				if (arg.IsOut)
				{
					actualArgs[idx] = GetDefaultValue(type);
				}
				else
				{
					var luaType = Api.LUA_TNIL;
					if (idx < luaArgCount)
					{
						luaType = Api.lua_type(L, luaArgIdx);
						bool isDisposable = false;
						var obj = SetArg(L, actualArgs, idx, luaArgIdx, type, luaType, out isDisposable);
						if (isDisposable)
							disposableArgs[idx] = (IDisposable)obj;
					}
					else
					{
						actualArgs[idx] = arg.DefaultValue;
					}
				}
			}

			if (variadicArg != null)
			{
				var numOptionalArgCount = luaArgCount - requiredArgCount;
				if (numOptionalArgCount > 0)
				{
					var optArgs = System.Array.CreateInstance(variadicArg.ParameterType.GetElementType(), numOptionalArgCount);
					var type = variadicArg.ParameterType.GetElementType();
					var optArgIdx = 0;
					var vaArgStart = argStart + requiredArgCount;
					for (int i = 0; i < numOptionalArgCount; ++i, ++idx, ++optArgIdx)
					{
						var luaArgIdx = vaArgStart + i;
						var luaType = Api.lua_type(L, luaArgIdx);
						bool isDisposable = false;
						var obj = SetArg(L, optArgs, optArgIdx, luaArgIdx, type, luaType, out isDisposable);
						if (isDisposable)
							disposableArgs[idx] = (IDisposable)obj;
					}
					actualArgs[actualArgs.Length - 1] = optArgs;
				}
				else
				{
					if (variadicArg.ParameterType == typeof(object[]))
						actualArgs[actualArgs.Length - 1] = csharpArgs_NoArgs;
					else
						actualArgs[actualArgs.Length - 1] =  System.Array.CreateInstance(variadicArg.ParameterType.GetElementType(), 0);
				}
			}

			return actualArgs;
		}

		internal static readonly int[] luaArgTypes_NoArgs = new int[0];

		internal static string GetLuaInvokingSigniture(string methodName, int[] args)
		{
			var sb = new System.Text.StringBuilder();
			sb.Append(methodName);
			sb.Append("(");
			if (args != null && args.Length > 0)
			{
				for (var i = 0; i < args.Length; ++i)
				{
					sb.Append(Api.ttypename(args[i]));
					if (i < args.Length - 1)
					{
						sb.Append(",");
					}
				}
			}
			sb.Append(")");
			return sb.ToString();
		}

		internal class MethodCache
		{
			public System.Reflection.MethodBase method;
			public System.Reflection.ParameterInfo[] parameters;
			public System.Reflection.ParameterInfo variadicArg;
        }
		static Dictionary<Type, Dictionary<string, MethodCache>> methodCache = new Dictionary<Type, Dictionary<string, MethodCache>>();

		public static void CleanMethodCache()
		{
			methodCache.Clear();
		}

		internal static MethodCache GetMethodFromCache(Type targetType, string mangledName)
		{
			MethodCache method = null;
			Dictionary<string, MethodCache> cachedMethods;
			if (methodCache.TryGetValue(targetType, out cachedMethods))
			{
				cachedMethods.TryGetValue(mangledName, out method);
			}

			return method;
		}

		System.Text.StringBuilder tempSb = new System.Text.StringBuilder();
		internal string Mangle(string methodName, int[] luaArgTypes, bool invokingStaticMethod, int argStart)
		{
			var sb = tempSb;
			sb.Length = 0;

			if (invokingStaticMethod)
			{
				sb.Append("_s_");
			}
			else
			{
				sb.Append("_");
			}
			sb.Append(methodName);
			sb.Append(luaArgTypes.Length);

			for (int i = 0; i < luaArgTypes.Length; ++i)
			{

				if (luaArgTypes[i] == Api.LUA_TUSERDATA)
				{
					sb.Append('c');
					var obj = ObjectAtInternal(L, argStart + i);
					if (obj != null)
					{
						sb.Append(obj.GetType().FullName);
					}
					sb.Append('|');
				}
				else
				{
					sb.Append(luaArgTypes[i]);
				}
			}
			return sb.ToString();
		}

		internal static MethodCache CacheMethod(Type targetType, string mangledName, System.Reflection.MethodBase method, System.Reflection.ParameterInfo[] parameters)
		{
			Dictionary<string, MethodCache> cachedMethods;
			if (!methodCache.TryGetValue(targetType, out cachedMethods))
			{
				cachedMethods = new Dictionary<string, MethodCache>();
				methodCache.Add(targetType, cachedMethods);
			}
			Assert(!cachedMethods.ContainsKey(mangledName), string.Format("{0} of {1} already cached with mangled name {2}", method.ToString(), targetType.ToString(), mangledName));
			var cachedData = new MethodCache()
			{
				method = method,
				parameters = parameters,
				variadicArg = IsLastArgVariadic(parameters),
			};
			cachedMethods.Add(mangledName, cachedData);
			return cachedData;
		}

		internal static void PushErrorObject(IntPtr L, string message) // No Throw
		{
			try
			{
				Api.luaL_traceback(L, L, "===== script =====", 1);
				var luaStack = Api.lua_tostring(L, -1);
				Api.lua_pop(L, 1);
				message = string.Format("error: {0}\n{1}", message, luaStack);
				Config.LogError(message);
				var host = CheckHost(L);
				if (host.pushError != null) // pushError may not be prepared, 
				{
					host.pushError.Push(L);  // DO NOT use Invoke, or you have infinite recursive call
					Api.lua_pushstring(L, message);
					if (Api.LUA_OK != Api.lua_pcall(L, 1, 1, 0))
					{
						var err = Api.lua_tostring(L, -1);
						Api.lua_pop(L, 1);
						throw new LuaException(err); // err in push err, WTF, catches below 
					}
					// here we got error object on stack
				}
				else // host.pushError not ready
				{
					Api.lua_pushstring(L, message);
				}
			}
			catch (Exception e) // host not ready? host.pushError error? err in test err
			{
				Api.lua_pushstring(L, message + "\n" + e.Message); // have to left message + e.Message on stack, what's happing?
			}
		}

		public static bool TestError(IntPtr L, int idx, out string errorMessage)
		{
			try
			{
				var host = CheckHost(L);
				if (host.checkError != null) // huh, in Lua.Ctor, checkError may no be prepared
				{
					Api.lua_pushvalue(L, idx);
					host.checkError.Push(L); // dont use Invoke. host.Call inside of it results an infinite recursive call.
					Api.lua_insert(L, -2);
					if (Api.LUA_OK != Api.lua_pcall(L, 1, 0, 0)) // do not use host.Call, or you have infinite recursive call.
					{
						// has error, check_error calls error() in lua script, catches here
						errorMessage = Api.lua_tostring(L, -1);
						Api.lua_pop(L, 1);
						return true;
					}
					errorMessage = string.Empty; // correct
					return false;
				}
				else
				{
					// if checkError not prepared, there is no error object
					errorMessage = string.Empty;
					return false;
				}
			}
			catch (Exception e) // host not ready? checkError error
			{
				errorMessage = "TestError failed: " + e.Message;
				return true;
			}
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int InvokeMethod(IntPtr L)
		{
			try
			{
				return InvokeMethodInternal(L);
			}
			catch (System.Reflection.TargetInvocationException e)
			{
				PushErrorObject(L, 
					string.Format(
						"{0}\nnative stack traceback:\n{1}",
						e.InnerException.Message,
						e.InnerException.StackTrace));
				return 1;
			}
			catch (Exception e)
			{
				PushErrorObject(L,
					string.Format(
						"{0}\nnative stack traceback:\n{1}",
						e.Message,
						e.StackTrace));
				return 1;
			}
		}

		internal static void MatchingParameters(
			IntPtr L,
			int	argStart,
			System.Reflection.MethodBase m,
			int[] luaArgTypes,
			ref int highScore, ref System.Reflection.MethodBase	selected,
			ref System.Reflection.ParameterInfo[] parameters)
		{
			var matchingParameters = m.GetParameters();
			var s = GetMatchScoreOfArgs(L, argStart, matchingParameters, luaArgTypes);
			if (s > highScore)
			{
				highScore = s;
				selected = m;
				parameters = matchingParameters;
			}
			else if (s != int.MinValue && s == highScore)
			{
				throw new System.Reflection.AmbiguousMatchException(string.Format("ambiguous, two or more function matched {0}", GetLuaInvokingSigniture(m.Name, luaArgTypes)));
			}
		}

		static MethodCache MatchMethod(IntPtr L, 
			Type invokingType, Type type, string methodName, string mangledName, bool invokingStaticMethod,
			ref object target, int argStart, int[] luaArgTypes,
			bool hasPrivatePrivillage,
			Type[] exactParamTypes, Type[] genericParamTypes,
			bool dontCache)
		{
			System.Reflection.MethodBase method;
			var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
			if (hasPrivatePrivillage)
				flags |= System.Reflection.BindingFlags.NonPublic;

			System.Reflection.MethodBase selected = null;
			System.Reflection.ParameterInfo[] parameters = null;
			List<Exception> pendingExceptions = null;

			if (exactParamTypes != null)
			{
				var mi = type.GetMethod(methodName, flags, null, exactParamTypes, null);
				if (mi != null)
				{
					selected = mi;
					parameters = selected.GetParameters();
				}
			}
			else
			{
				var members = type.GetMember(methodName, System.Reflection.MemberTypes.Method, flags);
				var score = Int32.MinValue;

				foreach (var member in members)
				{
					var m = (System.Reflection.MethodInfo)member;
					if (m.IsStatic)
					{
						if (!invokingStaticMethod)
						{
							Assert(false, string.Format("invoking static method {0} with incorrect syntax.", m.ToString()));
						}
						target = null;
					}
					else
					{
						if (invokingStaticMethod)
						{
							Assert(false, string.Format("invoking non-static method {0} with incorrect syntax.", m.ToString()));
						}
					}
					if (m.ContainsGenericParameters && genericParamTypes != null)
					{
						var gps = m.GetGenericArguments();
						if (gps.Length == genericParamTypes.Length)
						{
							selected = m.MakeGenericMethod(genericParamTypes);
							parameters = m.GetParameters();
							dontCache = true;
							break;
						}
					}
					else
					{
						try
						{
							MatchingParameters(L, argStart, m, luaArgTypes, ref score, ref selected, ref parameters);
						}
						catch (System.Reflection.AmbiguousMatchException e)
						{
							throw e;
						}
						catch (Exception e)
						{
							if (pendingExceptions == null)
								pendingExceptions = new List<Exception>();
							pendingExceptions.Add(e);
						}
					}
				}
			}
			method = selected;
			if (method != null)
			{
				if (dontCache)
				{
					return new MethodCache() { method = method, parameters = parameters, variadicArg = IsLastArgVariadic(parameters) };
				}
				else
				{
					return CacheMethod(invokingType, mangledName, method, parameters);
				}
			}

			if (type != typeof(object))
			{
				// search into parent
				return MatchMethod(L, invokingType, type.BaseType, methodName, mangledName, invokingStaticMethod, ref target, argStart, luaArgTypes, hasPrivatePrivillage, exactParamTypes, genericParamTypes, dontCache);
			}
			else
			{
				var additionalMessage = string.Empty;
				if (pendingExceptions != null && pendingExceptions.Count > 0)
				{
					var sb = new System.Text.StringBuilder();
					for (int i = 0; i < pendingExceptions.Count; ++i)
					{
						sb.AppendLine(pendingExceptions[i].Message);
					}
					additionalMessage = sb.ToString();
				}
				throw new Exception(string.Format("no corresponding csharp method for {0}\n{1}", GetLuaInvokingSigniture(methodName, luaArgTypes), additionalMessage));
			}
		}

		static int InvokeMethodInternal(IntPtr L)
		{
			// upvalue 1 --> isInvokingFromClass
			// upvalue 2 --> userdata (host of metatable).
			// upvalue 3 --> member name
			var isInvokingFromClass = Api.lua_toboolean(L, Api.lua_upvalueindex(1));
			var obj = ObjectAtInternal(L, Api.lua_upvalueindex(2));
			Assert(obj != null, "invoking target not found at upvalueindex(2)");
			string methodName;
			var hasPriviatePrivillage = false;
			Type[] exactTypes = null;
			Type[] genericTypes = null;
			var host = CheckHost(L);
			if (Api.lua_istable(L, Api.lua_upvalueindex(3)))
			{
				using (var p = LuaTable.MakeRefTo(host, Api.lua_upvalueindex(3)))
				{
					var testOnlyPrivate = 0;
					using (var privillageTable = host.testPrivillage.InvokeMultiRet(p, testOnlyPrivate))
					{
						methodName = (string)privillageTable[1];
						hasPriviatePrivillage = (bool)privillageTable[2];
						exactTypes = (Type[])privillageTable[3];
						genericTypes = (Type[])privillageTable[4];
					}
				}
			}
			else if (!Api.luaL_teststring_strict(L, Api.lua_upvalueindex(3), out methodName))
			{
				throw new ArgumentException("expected string", "methodName (upvalueindex(3))");
			}

			var dontCache = exactTypes != null || genericTypes != null;
			
			int[] luaArgTypes = luaArgTypes_NoArgs;
			var argStart = 1;
			var numArgs = Api.lua_gettop(L);
			var invokingStaticMethod = true;
			if (numArgs > 0)
			{
				if (Api.lua_rawequal(L, 1, Api.lua_upvalueindex(2)))
				{
					invokingStaticMethod = false;
				}
				else
				{
					// for lua behaviour, check if there is __behaviour in metatable
					if (1 == Api.lua_getmetatable(L, 1))
					{
						Api.lua_rawgeti(L, -1, 1);
						if (Api.lua_rawequal(L, -1, Api.lua_upvalueindex(2)))
						{
							invokingStaticMethod = false;
						}
						Api.lua_pop(L, 2);
					}
				}

				// adjust args
				if (invokingStaticMethod)
				{
					luaArgTypes = new int[numArgs];
					argStart = 1;
				}
				else
				{
					if (numArgs - 1 == 0)
					{
						luaArgTypes = luaArgTypes_NoArgs;
					}
					else
					{
						luaArgTypes = new int[numArgs - 1];
						argStart = 2;
					}
				}
			}

			if (luaArgTypes != luaArgTypes_NoArgs)
			{
				// fill	arg	types
				for (var i = argStart; i <= numArgs; ++i)
				{
					luaArgTypes[i - argStart] = Api.lua_type(L, i);
				}
			}

			object target = null;
			System.Type type = null;
			if (isInvokingFromClass)
			{
				type = (System.Type)obj;
				if (!invokingStaticMethod)
				{
					Assert(false, string.Format("invoking static method {0} from class {1} with incorrect syntax", methodName, type.ToString()));
				}
			}
			else
			{
				target = obj;
				type = obj.GetType();
			}

			var mangledName = host.Mangle(methodName, luaArgTypes, invokingStaticMethod, argStart);
			MethodCache mc = null;
			if (!dontCache)
				mc = GetMethodFromCache(type, mangledName);
			if (mc == null)
			{
				// match method	throws not matching exception
				mc = MatchMethod(L, type, type, methodName, mangledName, invokingStaticMethod, ref target, argStart, luaArgTypes, hasPriviatePrivillage, exactTypes, genericTypes, dontCache);
			}

			var top = Api.lua_gettop(L);
			IDisposable[] disposableArgs;
			var actualArgs = ArgsFrom(L, mc.parameters, mc.variadicArg, argStart, luaArgTypes.Length, out disposableArgs);
			Assert(top == Api.lua_gettop(L), "stack changed after converted args from lua.");

			var retVal = mc.method.Invoke(target, actualArgs);

			if (disposableArgs != null)
			{
				foreach (var d in disposableArgs)
				{
					if (d != null) d.Dispose();
				}
			}

			int outValues = 0;
			if (retVal != null)
			{
				PushValueInternal(L, retVal);
				var t = retVal.GetType();
				if (t == typeof(LuaTable)
					|| t == typeof(LuaThread)
					|| t == typeof(LuaFunction))
				{
					var d = (IDisposable)retVal;
					d.Dispose();
				}
				++outValues;
			}
			// out and ref parameters
			for (int i = 0; i < mc.parameters.Length; ++i)
			{
				if (mc.parameters[i].IsOut || mc.parameters[i].ParameterType.IsByRef)
				{
					PushValueInternal(L, actualArgs[i]);
					++outValues;
				}
			}
			return outValues;
		}

		

		public void Import(Type type, string name)
		{
			Import(type);
			Api.lua_setglobal(L, name);
		}

		// [ 0 | +1 | -]
		internal bool Import(Type type)
		{
			Api.lua_pushcclosure(L, Import, 0);
			Api.lua_pushstring(L, type.AssemblyQualifiedName);
			if (Api.LUA_OK != Api.lua_pcall(L, 1, 1, 0))
			{
				Config.LogError(Api.lua_tostring(L, -1));
				Api.lua_pop(L, 1);
				Api.lua_pushnil(L);
				return false;
			}
			return true;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int ImportInternal(IntPtr L)
		{
			try
			{
				return ImportInternal_(L);
			}
			catch (Exception e)
			{
				PushErrorObject(L, e.Message);
				return 1;
			}
		}

		static int ImportInternal_(IntPtr L) // called only if the type not imported
		{
			string typename;
			if (!Api.luaL_teststring_strict(L, 1, out typename))
			{
				throw new ArgumentException("expected string", "typename (arg 1)");
			}
			var type = loadType(typename);
			if (type == null)
			{
				throw new Exception(string.Format("Cannot import type {0}", typename));
			}
			if (PushObjectInternal(L, type, classMetaTable) == 1) // type object in ImportInternal_ is cached by luaL_requiref
			{
				Api.lua_getmetatable(L, -1); // append info in metatable

				Api.lua_pushboolean(L, true);
				Api.lua_rawseti(L, -2, 1); // isClassObject = true

				Api.lua_pushcclosure(L, MetaMethod.MetaConstructFunction, 0);
				Api.lua_setfield(L, -2, "__call");

				Api.lua_pop(L, 1);
			}
			return 1;
		}


		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int Import(IntPtr L)
		{
			string typename;
			if (!Api.luaL_teststring_strict(L, 1, out typename))
			{
				var e = new ArgumentException("expected string", "typename (arg 1)");
				PushErrorObject(L, e.Message);
				return 1;
			}
			Api.luaL_requiref(L, typename, ImportInternal, 0);
			return 1;
		}

		public void PushArray(object value, bool byRef = false)
		{
			PushArrayInternal(L, value, byRef);
		}

		internal static void PushArrayInternal(IntPtr L, object value, bool byRef = false)
		{
			if (value == null)
			{
				Api.lua_pushnil(L);
				return;
			}

			var type = value.GetType();
			Debug.Assert(type.IsArray, "PushArray called with non-array value");
			if (type.IsArray)
			{
				if (type == typeof(byte[]))
				{
					Api.lua_pushbytes(L, (byte[])value);
					return;
				}

				if (byRef)
				{
					PushObjectInternal(L, value);
					return;
				}

				// other primitive array?
				var arr = (System.Array)value;
				if (arr.Length <= Config.PassAsObjectIfArrayLengthGreatThan)
				{
					Api.lua_createtable(L, arr.Length, 0);
					for (int i = 0; i < arr.Length; ++i)
					{
						PushValueInternal(L, arr.GetValue(i));
						Api.lua_seti(L, -2, i + 1);
					}
					return;
				}

				PushObjectInternal(L, value);
			}
			else
			{
				Api.lua_pushnil(L);
			}
		}
		public void PushValue(object value)
		{
			PushValueInternal(L, value);
		}

		internal static void PushValueInternal(IntPtr L, object value)
		{
			if (value == null)
			{
				Api.lua_pushnil(L);
				return;
			}

			var type = value.GetType();
			if (type.IsArray)
			{
				PushArrayInternal(L, value, byRef: true);
				return;
			}

			if (type.IsPrimitive)
			{
				if (IsIntegerType(type))
				{
					var number = System.Convert.ToInt64(value);
					Api.lua_pushinteger(L, number);
				}
				else if (IsNumberType(type))
				{
					var number = System.Convert.ToDouble(value);
					Api.lua_pushnumber(L, number);
				}
				else if (type == typeof(System.Boolean))
				{
					Api.lua_pushboolean(L, (bool)value);
				}
				else if (type == typeof(System.IntPtr)
					|| type == typeof(System.UIntPtr))
				{
					Api.lua_pushlightuserdata(L, (IntPtr)value);
				}
			}
			else if (type == typeof(string))
			{
				Api.lua_pushstring(L, (string)value);
			}
			else if (type == typeof(LuaFunction))
			{
				var f = (LuaFunction)value;
				f.Push(L);
			}
			else if (type == typeof(LuaTable))
			{
				var t = (LuaTable)value;
				t.Push(L);
			}
			else if (type.IsEnum)
			{
				Api.lua_pushinteger(L, (int)value);
			}
			else if (type == typeof(LuaThread))
			{
				var th = (LuaThread)value;
				th.Push(L);
			}
			else if (typeof(System.Delegate).IsAssignableFrom(type))
			{
				var host = CheckHost(L);
				var f = LuaFunction.CreateDelegate(host, (System.Delegate)value);
				f.Push(L);
				f.Dispose(); // safely Dispose here
			}
			else
			{
				PushObjectInternal(L, value);
			}
		}

		static Dictionary<Type, Dictionary<string, System.Reflection.MemberInfo>> memberCache = new Dictionary<Type, Dictionary<string, System.Reflection.MemberInfo>>();


		public static void CleanMemberCache()
		{
			memberCache.Clear();
		}

		static void CacheMember(Type type, string memberName, System.Reflection.MemberInfo mi)
		{
			Dictionary<string, System.Reflection.MemberInfo> cache;
			if (!memberCache.TryGetValue(type, out cache))
			{
				cache = new Dictionary<string, System.Reflection.MemberInfo>();
				memberCache[type] = cache;
			}
			cache.Add(memberName, mi);
		}
		static bool GetMemberFromCache(Type type, string memberName, out System.Reflection.MemberInfo mi)
		{
			Dictionary<string, System.Reflection.MemberInfo> cache;
			if (memberCache.TryGetValue(type, out cache))
			{
				return cache.TryGetValue(memberName, out mi);
			}
			mi = null;
			return false;
		}

		internal static int GetMember(IntPtr L, object obj, Type objType, string memberName, bool hasPrivatePrivillage = false)
		{
			System.Reflection.MemberInfo member = null;
			var hasCachedMember = GetMemberFromCache(objType, memberName, out member);
			if (!hasCachedMember)
			{
				var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
				if (hasPrivatePrivillage) flags |= System.Reflection.BindingFlags.NonPublic;
				var members = objType.GetMember(memberName, flags);
				if (members.Length > 0)
				{
					member = members[0];
				}
				CacheMember(objType, memberName, member);
			}

			if (member == null)
			{
				// search into base	class of obj
				if (objType != typeof(object))
					return GetMember(L, obj, objType.BaseType, memberName);
				Api.lua_pushnil(L);
				return 1;
			}
			else if (member.MemberType == System.Reflection.MemberTypes.Field)
			{
				var field = (System.Reflection.FieldInfo)member;
				if (field.FieldType.IsEnum)
				{
					PushValueInternal(L, (int)field.GetValue(obj));
				}
				else
				{
					PushValueInternal(L, field.GetValue(obj));
				}
				return 1;
			}
			else if (member.MemberType == System.Reflection.MemberTypes.Property)
			{
				var prop = (System.Reflection.PropertyInfo)member;
				try
				{
					if (prop.PropertyType.IsEnum)
					{
						PushValueInternal(L, (int)prop.GetValue(obj, null));
					}
					else
					{
						PushValueInternal(L, prop.GetValue(obj, null));
					}
					return 1;
				}
				catch (ArgumentException ae)
				{
					// search into base	class of obj
					Assert(objType != typeof(object), string.Format("Member {0} not found. {1}", memberName, ae.Message));
					return GetMember(L, obj, objType.BaseType, memberName);
				}
			}
			else if (member.MemberType == System.Reflection.MemberTypes.Method)
			{
				bool isInvokingFromClass = (obj == null);
				Api.lua_pushboolean(L, isInvokingFromClass);      // upvalue 1 --> isInvokingFromClass
				Api.lua_pushvalue(L, 1);                          // upvalue 2 --> userdata, first parameter of __index
				Api.lua_pushvalue(L, 2);                          // upvalue 3 --> member name
				// TODO: maybe i should	cache here, to avoid name manglings
				Api.lua_pushcclosure(L, InvokeMethod, 3);         // return a wrapped lua_CFunction
				return 1;
			}
			else
			{
				// search into base	class of obj
				if (objType != typeof(object))
					return GetMember(L, obj, objType.BaseType, memberName);
				Api.lua_pushnil(L);
				return 1;
			}
		}

		internal static int IndexObjectInternal(IntPtr L, object obj, Type type, object[] index)
		{
			Assert(index != null);
			var prop = type.GetProperty("Item");
			if (prop == null)
			{
				Assert(type != typeof(object), string.Format("No indexer found in {0}", obj.GetType()));
				return IndexObjectInternal(L, obj, type.BaseType, index);
			}
			try
			{
				if (prop.PropertyType.IsEnum)
				{
					PushValueInternal(L, (int)prop.GetValue(obj, index));
				}
				else
				{
					PushValueInternal(L, prop.GetValue(obj, index));
				}
				return 1;
			}
			catch (ArgumentException ae)
			{
				Assert(type != typeof(object), string.Format("Incorrect indexer called in {0}: {1}", obj.GetType(), ae.Message));
				return IndexObjectInternal(L, obj, type.BaseType, index);
			}
		}

		internal static void SetValueAtIndexOfObject(IntPtr L, object obj, Type type, object[] index, object value)
		{
			Assert(index != null);
			var prop = type.GetProperty("Item");
			if (prop == null)
			{
				Assert(type != typeof(object), string.Format("No indexer found in {0}", obj.GetType()));
				SetValueAtIndexOfObject(L, obj, type.BaseType, index, value);
			}
			try
			{
				prop.SetValue(obj, ConvertTo(value, prop.PropertyType), index);
			}
			catch (ArgumentException ae)
			{
				Assert(type != typeof(object), string.Format("Incorrect indexer called in {0}: {1}", obj.GetType(), ae.Message));
				SetValueAtIndexOfObject(L, obj, type.BaseType, index, value);
			}
		}

		internal static object ConvertTo(object value, Type type)
		{
			if (value.GetType() == type)
			{
				return value;
			}
			else if (type == typeof(System.Action))
			{
				var f = (LuaFunction)value;
				var converted = LuaFunction.ToAction(f);
				f.Dispose();
				return converted;
			}
			else if (type == typeof(UnityEngine.Events.UnityAction))
			{
				var f = (LuaFunction)value;
				var converted = LuaFunction.ToUnityAction(f);
				f.Dispose();
				return converted;
			}
			else if (type == typeof(LuaFunction))
			{
				return (LuaFunction)value;
			}
			else if (typeof(System.Delegate).IsAssignableFrom(type))
			{
				var f = (LuaFunction)value;
				var converted = LuaFunction.ToDelegate(type, f);
				f.Dispose();
				return converted;
			}
			if (value is System.Type && type is System.Type)
			{
				return value;
			}
			return Convert.ChangeType(value, type);
		}

		internal static void SetMember(IntPtr L, object thisObject, Type type, string memberName, object value, bool hasPrivatePrivillage)
		{
			if (!type.IsClass && !type.IsAnsiClass)
			{
				Assert(false, string.Format("Setting property {0} of {1} object", memberName, type.ToString()));
			}

			System.Reflection.MemberInfo member;
			if (!GetMemberFromCache(type, memberName, out member))
			{
				var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
				if (hasPrivatePrivillage)
					flags |= System.Reflection.BindingFlags.NonPublic;
				var members = type.GetMember(memberName, flags);
				if (members.Length > 0)
				{
					member = members[0];
				}
				else
				{
					Assert(false, string.Format("Cannot find property with name {0} of type {1}", memberName, type.ToString()));

				}
				CacheMember(type, memberName, member);
			}

			if (member == null)
			{
				Assert(false, string.Format("Cannot find property with name {0} of type {1}", memberName, type.ToString()));
			}
			else if (member.MemberType == System.Reflection.MemberTypes.Field)
			{
				var field = (System.Reflection.FieldInfo)member;
				field.SetValue(thisObject, ConvertTo(value, field.FieldType));
			}
			else if (member.MemberType == System.Reflection.MemberTypes.Property)
			{
				var prop = (System.Reflection.PropertyInfo)member;
				prop.SetValue(thisObject, ConvertTo(value, prop.PropertyType), null);
			}
			else
			{
				Assert(false, string.Format("Member type {0} and {1} expected, but {2} got.",
					System.Reflection.MemberTypes.Field, System.Reflection.MemberTypes.Property, member.MemberType));
			}
		}

		// http://stackoverflow.com/a/3016653/84998
		internal enum BinaryOp
		{
			op_Addition = 0,
			op_Subtraction,
			op_Multiply,
			op_Division,
			op_Modulus,

			op_BitwiseAnd,
			op_BitwiseOr,
			op_ExclusiveOr,
			op_OnesComplement,

			op_LeftShift,
			op_RightShift,

			op_Equality,
			op_LessThan,

			op_LessThanOrEqual,

		}

		static readonly KeyValuePair<string, BinaryOp>[] binaryOps = new KeyValuePair<string, BinaryOp>[]
		{
			new KeyValuePair<string, BinaryOp>("__add", BinaryOp.op_Addition),
			new KeyValuePair<string, BinaryOp>("__sub", BinaryOp.op_Subtraction),
			new KeyValuePair<string, BinaryOp>("__mul", BinaryOp.op_Multiply),
			new KeyValuePair<string, BinaryOp>("__div", BinaryOp.op_Division),
			new KeyValuePair<string, BinaryOp>("__mod", BinaryOp.op_Modulus),

			new KeyValuePair<string, BinaryOp>("__band", BinaryOp.op_BitwiseAnd),
			new KeyValuePair<string, BinaryOp>("__bor", BinaryOp.op_BitwiseOr),
			new KeyValuePair<string, BinaryOp>("__bxor", BinaryOp.op_ExclusiveOr),
			new KeyValuePair<string, BinaryOp>("__bnot", BinaryOp.op_OnesComplement),

			new KeyValuePair<string, BinaryOp>("__shl", BinaryOp.op_LeftShift),
			new KeyValuePair<string, BinaryOp>("__shr", BinaryOp.op_RightShift),

			new KeyValuePair<string, BinaryOp>("__eq", BinaryOp.op_Equality),
			new KeyValuePair<string, BinaryOp>("__lt", BinaryOp.op_LessThan),
			new KeyValuePair<string, BinaryOp>("__le", BinaryOp.op_LessThanOrEqual),
		};

		internal enum UnaryOp
		{
			op_UnaryNegation = 0,
		}

		static readonly KeyValuePair<string, UnaryOp>[] unaryOps = new KeyValuePair<string, UnaryOp>[]
		{
			new KeyValuePair<string, UnaryOp>("__unm", UnaryOp.op_UnaryNegation),
		};


		// [-0, +1, -]
		static int NewObjectMetatable(IntPtr L, string metaTableName)
		{
			if (Api.luaL_newmetatable(L, metaTableName) == 1)
			{
				// Config.Log(string.Format("Registering object meta table {0} ... ", metaTableName));
				Api.lua_pushboolean(L, false);
				Api.lua_rawseti(L, -2, 1); // isClassObject = false

				foreach (var op in binaryOps)
				{
					Api.lua_pushstring(L, op.Value.ToString());
					Api.lua_pushcclosure(L, MetaMethod.MetaBinaryOpFunction, 1);
					Api.lua_setfield(L, -2, op.Key);
				}

				foreach(var op in unaryOps)
				{
					Api.lua_pushstring(L, op.Value.ToString());
					Api.lua_pushcclosure(L, MetaMethod.MetaUnaryOpFunction, 1);
					Api.lua_setfield(L, -2, op.Key);
				}

				Api.lua_pushcclosure(L, MetaMethod.MetaIndexFunction, 0);
				Api.lua_setfield(L, -2, "__index");

				Api.lua_pushcclosure(L, MetaMethod.MetaNewIndexFunction, 0);
				Api.lua_setfield(L, -2, "__newindex");

				Api.lua_pushcclosure(L, MetaMethod.MetaToStringFunction, 0);
				Api.lua_setfield(L, -2, "__tostring");

				Api.lua_pushcclosure(L, MetaMethod.MetaGcFunction, 0);
				Api.lua_setfield(L, -2, "__gc");

				return 1;
			}
			return 0;
		}

		public void DoString(string luaScript, int nrets = 0, string name = null)
		{
			var ret = Api.luaL_loadbufferx(L, 
				luaScript, 
				new	UIntPtr((uint)luaScript.Length),
				name != null ? name : luaScript,
				"bt");
			if (ret != Api.LUA_OK)
			{
				var err = Api.lua_tostring(L, -1);
				Api.lua_pop(L, 1);
				throw new LuaException(err, ret);
			}
			Call(0, nrets);
		}


		// Invoking Lua Function
		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		static int HandleLuaFunctionInvokingError(IntPtr L)
		{
			var host = CheckHost(L);
			var err = Api.lua_tostring(L, 1);
			Api.luaL_traceback(host, L, err, 0);
			err = Api.lua_tostring(host, -1);
			Api.lua_pop(host, 1);
			PushErrorObject(L, err);
			return 1;
		}

		public void Call(int nargs, int nresults)
		{
			CallInternal(L, nargs, nresults);
		}

		internal static void CallInternal(IntPtr L, int nargs, int nresults)
		{
			var stackTop = Api.lua_gettop(L) - nargs - 1; // function and args

			Api.lua_pushcclosure(L, HandleLuaFunctionInvokingError, 0);
			Api.lua_insert(L, stackTop + 1); // put err func to stackTop
			var ret = Api.lua_pcall(L, nargs, nresults, stackTop + 1);
			if (ret != Api.LUA_OK)
			{
				string errorMessage;
				if (!TestError(L, -1, out errorMessage))
				{
					errorMessage = Api.lua_tostring(L, -1);
				}
				Api.lua_settop(L, stackTop);
				throw new LuaException(errorMessage, ret);
			}
			else
			{
				// check error object
				string errorMessage;
				if (TestError(L, -1, out errorMessage))
				{
					Api.lua_settop(L, stackTop);
					throw new LuaException(errorMessage);
				}
				Api.lua_remove(L, stackTop + 1); // remove err func
			}
		}

		public static string DebugStack(IntPtr L)
		{
			var top = Api.lua_gettop(L);
			var sb = new System.Text.StringBuilder();
			for (int i = top; i > 0; i--)
			{
				var type = Api.lua_type(L, i);
				sb.Append(i);
				sb.Append(":\t");
				sb.Append(Api.lua_typename(L, type));
				sb.Append("\t");
				Api.lua_pushvalue(L, i);
				sb.Append(Api.lua_tostring(L, -1));
				Api.lua_pop(L, 1);
				sb.AppendLine();
			}
			return sb.ToString();
		}
	}
}
