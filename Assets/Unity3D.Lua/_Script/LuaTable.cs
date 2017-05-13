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
﻿using System.Collections.Generic;
using System;

namespace lua
{
	public class LuaTable : IDisposable
	{
		Lua L_;
		int tableRef = Api.LUA_NOREF;
		int refCount = 1;

		public LuaTable(Lua L)
		{
			L_ = L;
			Api.lua_createtable(L, 0, 0);
			tableRef = L.MakeRefAt(-1);
			Api.lua_pop(L, 1);
		}

		LuaTable(Lua L, int idx)
		{
			L_ = L;
			tableRef = L.MakeRefAt(idx);
		}

		public bool valid
		{
			get
			{
				return L_.valid;
			}
		}

		public void Dispose()
		{
			--refCount;
			if (refCount <= 0)
			{
				if (L_ != null && L_.valid && tableRef != Api.LUA_NOREF)
				{
					foreach (var c in cached)
					{
						if (c.Value != null)
						{
							c.Value.Dispose();
						}
					}
					L_.Unref(tableRef);
				}
				cached.Clear();
				tableRef = Api.LUA_NOREF;
				L_ = null;
			}
		}

		Lua CheckValid()
		{
			if (L_ != null && L_.valid)
				return L_;
			throw new System.InvalidOperationException("LuaTable already disposed.");
		}

		public LuaTable Retain()
		{
			++refCount;
			return this;
		}

		public void Push()
		{
			var L = CheckValid();
			L.PushRef(tableRef);
		}

		internal void Push(IntPtr L)
		{
			Lua.PushRefInternal(L, tableRef);
		}


		public int Length
		{
			get
			{
				var L = CheckValid();
				L.PushRef(tableRef);
				Api.lua_len(L, -1);
				var ret = Api.lua_tointeger(L, -1);
				Api.lua_pop(L, 2);
				return (int)ret;
			}
		}

		public object this[int index]
		{
			get
			{
				var L = CheckValid();
				L.PushRef(tableRef);
				Api.lua_geti(L, -1, index);
				var ret = L.ValueAt(-1);
				Api.lua_pop(L, 2);
				return ret;
			}
			set
			{
				var L = CheckValid();
				L.PushRef(tableRef);
				L.PushValue(value);
				Api.lua_seti(L, -2, index);
				Api.lua_pop(L, 1);
			}
		}

		public object this[string index]
		{
			get
			{
				var L = CheckValid();
				L.PushRef(tableRef);
				Api.lua_getfield(L, -1, index);
				var ret = L.ValueAt(-1);
				Api.lua_pop(L, 2);
				return ret;
			}
			set
			{
				var L = CheckValid();
				L.PushRef(tableRef);
				L.PushValue(value);
				Api.lua_setfield(L, -2, index);
				Api.lua_pop(L, 1);
			}
		}


		public void SetDelegate(string name, System.Delegate func)
		{
			var L = CheckValid();
			var f = LuaFunction.CreateDelegate(L, func);
			Push();
			f.Push();
			Api.lua_setfield(L, -2, name);
			Api.lua_pop(L, 1);
			Cache(name, renew: true, withFunc: f);
		}

		public void Invoke(string name, params object[] args)
		{
			var f = Cache(name);
			if (f != null)
			{
				f.Invoke(this, args);
			}
		}

		public void InvokeStatic(string name, params object[] args)
		{
			var f = Cache(name);
			if (f != null)
			{
				f.Invoke(null, args);
			}
		}

		public object Invoke1(string name, params object[] args)
		{
			var f = Cache(name);
			if (f != null)
			{
				return f.Invoke1(this, args);
			}
			return null;
		}

		public object InvokeStatic1(string name, params object[] args)
		{
			var f = Cache(name);
			if (f != null)
			{
				return f.Invoke1(null, args);
			}
			return null;
		}

		public LuaTable InvokeMultiRet(string name, params object[] args)
		{
			var f = Cache(name);
			if (f != null)
			{
				return f.InvokeMultiRet(this, args);
			}
			return null;
		}


		public LuaTable InvokeMultiRetStatic(string name, params object[] args)
		{
			var f = Cache(name);
			if (f != null)
			{
				return f.InvokeMultiRet(null, args);
			}
			return null;
		}

		Dictionary<string, LuaFunction> cached = new Dictionary<string, LuaFunction>();
		LuaFunction Cache(string name, bool renew = false, LuaFunction withFunc = null)
		{
			LuaFunction f = null;
			if (cached.TryGetValue(name, out f))
			{
				if (!renew)
					return f;

				// renew, dispose current
				if (f != null)
					f.Dispose();
				if (withFunc != null)
					cached[name] = withFunc;
			}
			var L = CheckValid();
			var top = Api.lua_gettop(L);
			Push();
			if (Api.lua_getfield(L, -1, name) == Api.LUA_TFUNCTION)
			{
				f = LuaFunction.MakeRefTo(L, -1);
			}
			else
			{
				Config.LogError(string.Format("attempt to call a non-function '{0}' ", name));
			}
			cached[name] = f;
			Api.lua_settop(L, top);
			return f;
		}

		internal static LuaTable MakeRefTo(Lua L, int idx)
		{
			Lua.Assert(Api.lua_istable(L, idx));
			return new LuaTable(L, idx);
		}


	}
}