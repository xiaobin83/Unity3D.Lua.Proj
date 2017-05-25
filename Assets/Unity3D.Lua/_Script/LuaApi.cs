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
using System.Runtime.InteropServices;

namespace lua
{
	public class Api
	{
#if UNITY_EDITOR || UNITY_ANDROID
		public const string LIBNAME = "lua";
#elif !UNITY_EDITOR || UNITY_IPHONE
		public const string LIBNAME = "__Internal";
#endif

		public const int LUA_MULTRET = -1;

		/*
		** Pseudo-indices
		*/
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int lua_const_LUA_REGISTRYINDEX();

		public static int LUA_REGISTRYINDEX
		{
			get
			{
				return lua_const_LUA_REGISTRYINDEX();
			}
		}
		public static int lua_upvalueindex(int i)
		{
			return LUA_REGISTRYINDEX - i;
		}


		/* thread status */
		public const int LUA_OK = 0;
		public const int LUA_YIELD = 1;
		public const int LUA_ERRRUN	= 2;
		public const int LUA_ERRSYNTAX = 3;
		public const int LUA_ERRMEM = 4;
		public const int LUA_ERRGCMM = 5;
		public const int LUA_ERRERR	= 6;

		/*
		** basic types
		*/
		public const int LUA_TNONE = -1;
		public const int LUA_TNIL = 0;
		public const int LUA_TBOOLEAN = 1;
		public const int LUA_TLIGHTUSERDATA = 2;
		public const int LUA_TNUMBER = 3;
		public const int LUA_TSTRING = 4;
		public const int LUA_TTABLE = 5;
		public const int LUA_TFUNCTION = 6;
		public const int LUA_TUSERDATA = 7;
		public const int LUA_TTHREAD = 8;
		public const int LUA_NUMTAGS = 9;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int lua_CFunction(IntPtr L);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int lua_KFunction(IntPtr L, int status, IntPtr ctx);

		/*
		** Type	for	functions that read/write blocks when loading/dumping Lua chunks
		*/
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr lua_Reader(IntPtr L, IntPtr ud, out UIntPtr sz);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int lua_Writer(IntPtr L, IntPtr p, UIntPtr sz, IntPtr ud);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr lua_Alloc(IntPtr ud, IntPtr ptr, UIntPtr osize, UIntPtr nsize);

		/*
		** state manipulation
		*/
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_newstate(lua_Alloc f, IntPtr ud);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_close(IntPtr L);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_newthread(IntPtr L);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern lua_CFunction lua_atpanic(IntPtr L, lua_CFunction panicf);

		/*
		** basic stack manipulation
		*/
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_absindex(IntPtr L, int idx);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_gettop(IntPtr L);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_settop(IntPtr L, int idx);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushvalue(IntPtr L, int idx);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rotate(IntPtr L, int idx, int n);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_copy(IntPtr L, int fromidx, int toidx);
		[DllImport(LIBNAME, EntryPoint = "lua_checkstack", CallingConvention = CallingConvention.Cdecl)]
		static extern int lua_checkstack_(IntPtr L, int n);
		public static bool lua_checkstack(IntPtr L, int n)
		{
			return lua_checkstack_(L, n) != 0;
		}

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_xmove(IntPtr from, IntPtr to, int n);



		/*
		** access functions (stack -> C)
		*/
		[DllImport(LIBNAME, EntryPoint = "lua_isnumber", CallingConvention = CallingConvention.Cdecl)]
		static extern int lua_isnumber_(IntPtr L, int idx);
		public static bool lua_isnumber(IntPtr L, int idx)
		{
			return lua_isnumber_(L, idx) == 1;
		}


		[DllImport(LIBNAME, EntryPoint = "lua_isstring", CallingConvention = CallingConvention.Cdecl)]
		static extern int lua_isstring_(IntPtr L, int idx);
		public static bool lua_isstring(IntPtr L, int idx)
		{
			return lua_isstring_(L, idx) == 1;
		}

		[DllImport(LIBNAME, EntryPoint = "lua_iscfunction", CallingConvention = CallingConvention.Cdecl)]
		static extern int lua_iscfunction_(IntPtr L, int idx);
		public static bool lua_iscfunction(IntPtr L, int idx)
		{
			return lua_iscfunction_(L, idx) == 1;
		}

		[DllImport(LIBNAME, EntryPoint = "lua_isinteger", CallingConvention = CallingConvention.Cdecl)]
		static extern int lua_isinteger_(IntPtr L, int idx);
		public static bool lua_isinteger(IntPtr L, int idx)
		{
			return lua_isinteger_(L, idx) == 1;
		}


		[DllImport(LIBNAME, EntryPoint = "lua_isuserdata", CallingConvention = CallingConvention.Cdecl)]
		static extern int lua_isuserdata_(IntPtr L, int idx);
		public static bool lua_isuserdata(IntPtr L, int idx)
		{
			return lua_isuserdata_(L, idx) == 1;
		}
		
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_type(IntPtr L, int idx);
		[DllImport(LIBNAME, EntryPoint = "lua_typename", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr lua_typename_(IntPtr L, int tp);
		public static string lua_typename(IntPtr L, int tp)
		{
			var ptr = lua_typename_(L, tp);
			if (ptr != IntPtr.Zero)
				return Marshal.PtrToStringAnsi(ptr);
			return null;
		}
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern double lua_tonumberx(IntPtr L, int idx, ref int isnum);
		public static double lua_tonumber(IntPtr L, int idx)
		{
			int isnum = 0;
			return lua_tonumberx(L, idx, ref isnum);
		}
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern long lua_tointegerx(IntPtr L, int idx, ref int isnum);
		public static long lua_tointeger(IntPtr L, int idx)
		{
			int isnum = 0;
			return lua_tointegerx(L, idx, ref isnum);
		}
		[DllImport(LIBNAME, EntryPoint = "lua_toboolean", CallingConvention = CallingConvention.Cdecl)]
		static extern int lua_toboolean_(IntPtr L, int idx);
		public static bool lua_toboolean(IntPtr L, int idx)
		{
			return lua_toboolean_(L, idx) != 0;
		}

		[DllImport(LIBNAME, EntryPoint = "lua_tolstring", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_tolstring(IntPtr L, int idx, out IntPtr len);

		public static void lua_pushbytes(IntPtr L, byte[] bytes)
		{
			var h = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			var ptr = h.AddrOfPinnedObject();
			lua_pushlstring(L,	ptr, new UIntPtr((uint)bytes.Length));
			h.Free();
		}

		public static byte[] lua_tobytes(IntPtr L, int idx)
		{
			if (lua_isstring(L, idx))
			{
				IntPtr len;
				var ptr = lua_tolstring(L, idx, out len);
				if ((int)len > 0 && IntPtr.Zero != ptr)
				{
					var bytes = new byte[(int)len];
					Marshal.Copy(ptr, bytes, 0, (int)len);
					return bytes;
				}
			}
			return null;
		}

		public static string lua_tostring(IntPtr L, int idx)
		{
			IntPtr len;
			var strPtr = lua_tolstring(L, idx, out len);
			if (strPtr != IntPtr.Zero)
				return Marshal.PtrToStringAnsi(strPtr, (int)len);
			return null;
		}

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_touserdata(IntPtr L, int idx);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_tothread(IntPtr L, int idx);



		public const int LUA_OPEQ = 0;
		public const int LUA_OPLT = 1;
		public const int LUA_OPLE = 2;

		[DllImport(LIBNAME, EntryPoint = "lua_rawequal", CallingConvention = CallingConvention.Cdecl)]
		static extern int lua_rawequal_(IntPtr L, int idx1, int idx2);
		public static bool lua_rawequal(IntPtr L, int idx1, int idx2)
		{
			return lua_rawequal_(L, idx1, idx2) == 1;
		}
		[DllImport(LIBNAME, EntryPoint = "lua_compare", CallingConvention = CallingConvention.Cdecl)]
		static extern int lua_compare_(IntPtr L, int idx1, int idx2, int op);
		public static bool lua_compare(IntPtr L, int idx1, int idx2, int op)
		{
			return lua_compare_(L, idx1, idx2, op) == 1;
		}

		/*
		** push	functions (C ->	stack)
		*/

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushnil(IntPtr L);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushnumber(IntPtr L, double n);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushinteger(IntPtr L, long n);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_pushlstring(IntPtr L, IntPtr s, UIntPtr len);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_pushstring(IntPtr L, string str);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushcclosure(IntPtr L, lua_CFunction fn, int n);
		[DllImport(LIBNAME, EntryPoint = "lua_pushboolean", CallingConvention = CallingConvention.Cdecl)]
		static extern void lua_pushboolean_(IntPtr L, int b);
		public static void lua_pushboolean(IntPtr L, bool b)
		{
			lua_pushboolean_(L, b ? 1 : 0);
		}

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushlightuserdata(IntPtr L, IntPtr p);
		/*
		LUA_API int (lua_pushthread) (IntPtr *L);
		*/


		public static void lua_newtable(IntPtr L)
		{
			lua_createtable(L, 0, 0);
		}


		/*
		** get functions (Lua -> stack)
		*/
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_getglobal(IntPtr L, string name);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_gettable(IntPtr L, int idx);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_getfield(IntPtr L, int idx, string k);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_geti(IntPtr L, int idx, long n);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_rawget(IntPtr L, int idx);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_rawgeti(IntPtr L, int idx, long n);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_rawgetp(IntPtr L, int idx, IntPtr p);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_createtable(IntPtr L, int narr, int nrec);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_newuserdata(IntPtr L, UIntPtr sz);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_getmetatable(IntPtr L, int objindex);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_getuservalue(IntPtr L, int idx);


		/*
		** set functions (stack	-> Lua)
		*/
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_setglobal(IntPtr L, string name);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_settable(IntPtr L, int idx);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_setfield(IntPtr L, int idx, string k);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_seti(IntPtr L, int idx, long n);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawset(IntPtr L, int idx);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawseti(IntPtr L, int idx, long n);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawsetp(IntPtr L, int idx, IntPtr p);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_setmetatable(IntPtr L, int objindex);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_setuservalue(IntPtr L, int idx);



		static int HandleError(IntPtr L)
		{
			var errMessage = lua_tostring(L, -1);
			lua_pop(L, 1); // pop error object
			luaL_traceback(L, L, errMessage, 1); // push stack trace
			return 1;
		}

		/*
		** 'load' and 'call' functions (load and run Lua code)
		*/
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_callk(IntPtr L, int nargs, int nresults, IntPtr ctx, lua_KFunction k);
		internal static void lua_call(IntPtr L, int nargs, int nresults)
		{
			lua_callk(L, nargs, nresults, IntPtr.Zero, null);
		}

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_pcallk(IntPtr L, int nargs, int nresults, int errfunc, IntPtr ctx, lua_KFunction k);

		internal static int lua_pcall(IntPtr L, int nargs, int nresults, int errfunc)
		{
			return lua_pcallk(L, nargs, nresults, errfunc, IntPtr.Zero, null);
		}

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_len(IntPtr L, int idx);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_load(IntPtr L, lua_Reader reader, IntPtr data, string chunkname, string mode);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_dump(IntPtr L, lua_Writer writer, IntPtr data, int strip);


		/*
		** coroutine functions
		*/
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_yieldk(IntPtr L, int nresults, IntPtr ctx, lua_KFunction k);

		public static int lua_yield(IntPtr L, int n)
		{
			return lua_yieldk(L, (n), IntPtr.Zero, null);
		}

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_resume(IntPtr L, IntPtr from, int narg);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_status(IntPtr L);


		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_next(IntPtr L, int idx);

		/*
		** {==============================================================
		** some useful macros
		** ===============================================================
		*/
		public static void lua_pop(IntPtr L, int n) { lua_settop(L, -(n) - 1); }

		public static bool lua_isfunction(IntPtr L, int n) { return (lua_type(L, (n)) == LUA_TFUNCTION); }
		public static bool lua_istable(IntPtr L, int n) { return (lua_type(L, (n)) == LUA_TTABLE); }
		public static bool lua_islightuserdata(IntPtr L, int n) { return (lua_type(L, (n)) == LUA_TLIGHTUSERDATA); }
		public static bool lua_isnil(IntPtr L, int n) { return (lua_type(L, (n)) == LUA_TNIL); }
		public static bool lua_isboolean(IntPtr L, int n) { return (lua_type(L, (n)) == LUA_TBOOLEAN); }
		public static bool lua_isthread(IntPtr L, int n) { return (lua_type(L, (n)) == LUA_TTHREAD); }
		public static bool lua_isnone(IntPtr L, int n) { return (lua_type(L, (n)) == LUA_TNONE); }
		public static bool lua_isnoneornil(IntPtr L, int n) { return (lua_type(L, (n)) <= 0); }


		public static void lua_insert(IntPtr L, int idx) { lua_rotate(L, (idx), 1); }
		public static void lua_remove(IntPtr L, int idx) { lua_rotate(L, (idx), -1); lua_pop(L, 1); }
		public static void lua_replace(IntPtr L, int idx) { lua_copy(L, -1, (idx)); lua_pop(L, 1); }



		// helpers
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern long luaL_optinteger(IntPtr L, int arg, long def);


		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_checkstack(IntPtr L, int sz, string msg);


		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_loadbufferx(IntPtr state, string s, UIntPtr size, string name, string mode);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_loadstring(IntPtr state, string s);
		
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_newstate();

		public static bool luaL_dostring(IntPtr L, string s)
		{
			int r = luaL_loadstring(L, s);
			if (r == LUA_OK)
			{
				r = lua_pcall(L, 0, LUA_MULTRET, 0);
				return r != LUA_OK;
			}
			return true;
		}

		public static int luaL_getmetatable(IntPtr L, string k)
		{
			return lua_getfield(L, LUA_REGISTRYINDEX, k);
		}


		/* predefined references */
		public const int LUA_NOREF = -2;
		public const int LUA_REFNIL = -1;

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_ref(IntPtr L, int t);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_unref(IntPtr L, int t, int r);


		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_loadfilex(IntPtr L, string filename, string mode);

		public static int luaL_loadfile(IntPtr L, string filename)
		{
			return luaL_loadfilex(L, filename, "bt");
		}

		public static bool luaL_dofile(IntPtr L, string filename)
		{
			var r = luaL_loadfile(L, filename);
			if (r == LUA_OK)
			{
				r = lua_pcall(L, 0, LUA_MULTRET, 0);
				return r != LUA_OK;
			}
			return true;
		}

		public static void luaL_setfuncs(IntPtr L, luaL_Reg[] l, int nup)
		{
			luaL_checkstack(L, nup, "too many upvalues");
			for (int i = 0; i < l.Length; ++i)
			{
				for (int u = 0; u < nup; ++u)  /* copy upvalues to the top */
					lua_pushvalue(L, -nup);
				lua_pushcclosure(L, l[i].func, nup);  /* closure with those upvalues */
				lua_setfield(L, -(nup + 2), l[i].name);
			}
			lua_pop(L, nup);  /* remove upvalues */
		}

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_traceback(IntPtr L, IntPtr L1, string msg, int level);


		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_requiref(IntPtr L, string modname, lua_CFunction openf, int glb);





		// lualib
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_openlibs(IntPtr L);




		public struct luaL_Reg
		{
			public string name;
			public lua_CFunction func;

			public luaL_Reg(string n, lua_CFunction f)
			{
				name = n;
				func = f;
			}
		}

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_newmetatable(IntPtr L, string tname);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_setmetatable(IntPtr L, string tname);
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_testudata(IntPtr L, int ud, string tname);

		public static void luaL_newlibtable(IntPtr L, luaL_Reg[] l)
		{
			lua_createtable(L, 0, l.Length);
		}

		public static void luaL_newlib(IntPtr L, luaL_Reg[] l)
		{
			luaL_newlibtable(L, l);
			luaL_setfuncs(L, l, 0);
		}

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr lua_const_ttypename(int type);
		public static string ttypename(int type)
		{
			var strPtr = lua_const_ttypename(type);
			if (strPtr != IntPtr.Zero)
				return Marshal.PtrToStringAnsi(strPtr);
			return "invalid_type_bad_ttypename";
		}


		// externs
		public static bool luaL_teststring_strict(IntPtr L, int idx, out string str)
		{
			if (lua_type(L, idx) == Api.LUA_TSTRING)
			{
				str = lua_tostring(L, idx);
				return true;
			}
			str = string.Empty;
			return false;
		}


	}
}
