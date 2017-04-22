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
using System;

namespace lua
{
	public class LuaThread : IDisposable
	{
		Lua L_;
		int threadRef = Api.LUA_NOREF;
		int currentRef = Api.LUA_NOREF;

		public bool hasYields
		{
			get
			{
				return currentRef != Api.LUA_NOREF;
			}
		}

		LuaTable current_;
		public LuaTable current
		{
			get
			{
				if (current_ == null)
				{
					var L = CheckValid();
					if (currentRef != Api.LUA_NOREF)
					{
						L.PushRef(currentRef);
						current_ = LuaTable.MakeRefTo(L, -1);
						Api.lua_pop(L, 1);
					}
				}
				return current_;
			}
		}



		public void Dispose()
		{
			DisposeCurrent();
			if (L_ != null && L_.valid)
			{
				if (threadRef != Api.LUA_NOREF)
					L_.Unref(threadRef);
			}
			threadRef = Api.LUA_NOREF;
			L_ = null;
		}

		public void DisposeCurrent()
		{
			if (current_ != null) current_.Dispose();
			current_ = null;

			if (L_ != null && L_.valid)
			{
				if (currentRef != Api.LUA_NOREF)
				{
					L_.Unref(currentRef);
				}
			}
			currentRef = Api.LUA_NOREF;
		}

		public bool Resume(params object[] args)
		{
			DisposeCurrent();

			var L = CheckValid();
			Push();
			var thread = Api.lua_tothread(L, -1);
			Api.lua_pop(L, 1); // pop thread

			if (!Api.lua_checkstack(thread, args != null ? args.Length : 0))
			{
				throw new LuaException("too many arguments to resume");
			}
			if (Api.lua_status(thread) == Api.LUA_OK && Api.lua_gettop(thread) == 0)
			{
				throw new LuaException("attempt to resume dead coroutine");
			}

			int nargs = 0;
			if (args != null && args.Length > 0)
			{
				for (int i = 0; i < args.Length; ++i)
				{
					Lua.PushValueInternal(thread, args[i]);
				}
				nargs = args.Length;
			}
			var status = Api.lua_resume(thread, L, nargs);
			if (status == Api.LUA_OK || status == Api.LUA_YIELD)
			{
				var nrets = Api.lua_gettop(thread);
				if (!Api.lua_checkstack(thread, nrets + 1))
				{
					Api.lua_pop(thread, nrets);
					throw new LuaException("too many arguments to resume");
				}
				if (nrets > 0)
				{
					Api.lua_createtable(thread, nrets, 0);
					for (int i = 1; i <= nrets; ++i)
					{
						Api.lua_pushvalue(thread, i);
						Api.lua_seti(thread, -2, i);
					}
					currentRef = Api.luaL_ref(thread, Api.LUA_REGISTRYINDEX); 
					Api.lua_pop(thread, nrets); // pop rets
				}
				if (status == Api.LUA_OK) // coroutine ends
				{
					return false;
				}
				return true;
			}
			else
			{
				var errorMessage = Api.lua_tostring(thread, -1);
				Api.lua_pop(thread, 1); // pop error message
				throw new LuaException(errorMessage, status);
			}
		}


		internal Lua CheckValid()
		{
			if (L_.valid)
				return L_;
			throw new System.InvalidOperationException("Lua vm already destroyed.");
		}

		// currentRef is dropped in retained copy
		public LuaThread Retain()
		{
			var L = CheckValid();

			Push();
			var clonedThreadRef = L.MakeRefAt(-1);
			Api.lua_pop(L, 1);

			//TODO: currentRef is not cloned to retained copy.
			//  if really want this feature, use a global table to store currentRef of current thread, use threadRef as key

			return new LuaThread { L_ = L, threadRef = clonedThreadRef };
		}


		public void Push()
		{
			var L = CheckValid();
			L.PushRef(threadRef);
		}
		internal void Push(IntPtr L)
		{
			Lua.PushRefInternal(L, threadRef);
		}


		public static LuaThread Create(LuaFunction func)
		{
			var L = func.CheckValid();
			var NL = Api.lua_newthread(L);
			func.Push(NL);
			var threadRef = L.MakeRefAt(-1);
			Api.lua_pop(L, 1); // pop newthread
			return new LuaThread { L_ = L, threadRef = threadRef };
		}

		public static LuaThread CreateAndDispose(LuaFunction func)
		{
			var t = Create(func);
			func.Dispose();
			return t;
		}

		public static LuaThread MakeRefTo(Lua L, int idx)
		{
			Lua.Assert(Api.lua_isthread(L, idx));
			var threadRef = L.MakeRefAt(idx);
			return new LuaThread { L_ = L, threadRef = threadRef };
		}
	}
}