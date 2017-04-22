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
using UnityEngine.Events;
using System;
using AOT;

namespace lua
{
	public class LuaFunction : IDisposable 
	{
		Lua L_;
		int funcRef = Api.LUA_NOREF;

		public static implicit operator Action(LuaFunction f)
		{
			return ToAction(f);
		}

		public static implicit operator UnityAction(LuaFunction f)
		{
			return ToUnityAction(f);
		}

		public static Action ToAction(LuaFunction f)
		{
			f = f.Retain();
			Action action = () => {
				f.Invoke();
				f.Dispose();
			};
			return action;
		}

		public static UnityAction ToUnityAction(LuaFunction f)
		{
			f = f.Retain();
			UnityAction action = () => {
				f.Invoke();
				f.Dispose();
			};
			return action;
		}


		public static Action<T> ToAction<T>(LuaFunction f)
		{
			f = f.Retain();
			Action<T> action = (arg) => {
				f.Invoke(null, arg);
				f.Dispose();
			};
			return action;
		}

		public static Action<T1, T2> ToAction<T1, T2>(LuaFunction f)
		{
			f = f.Retain();
			Action<T1, T2> action = (arg1, arg2) => {
				f.Invoke(null, arg1, arg2);
				f.Dispose();
			};
			return action;
		}

		public static Action<T1, T2, T3> ToAction<T1, T2, T3>(LuaFunction f)
		{
			f = f.Retain();
			Action<T1, T2, T3> action = (arg1, arg2, arg3) => {
				f.Invoke(null, arg1, arg2, arg3);
				f.Dispose();
			};
			return action;
		}

		public void Dispose()
		{
			if (L_.valid && funcRef != Api.LUA_NOREF)
			{
				L_.Unref(funcRef);
			}
			funcRef = Api.LUA_NOREF;
			L_ = null;
		}

		internal Lua CheckValid()
		{
			if (L_.valid)
				return L_;
			throw new InvalidOperationException("Lua vm already destroyed.");
		}

		public LuaFunction Retain()
		{
			var L = CheckValid();
			Push();
			var ret = new LuaFunction {L_ = L, funcRef = L.MakeRefAt(-1) };
			Api.lua_pop(L, 1);
			return ret;
		}

		public void Push()
		{
			var L = CheckValid();
			L.PushRef(funcRef);
		}

		internal void Push(IntPtr L)
		{
			Lua.PushRefInternal(L, funcRef);
		}

		public void Invoke(LuaTable target, params object[] args)
		{
			InvokeInternal(target, 0, args);
		}

		public void Invoke(params object[] args)
		{
			Invoke(null, args);
		}

		public object Invoke1(LuaTable target, params object[] args)
		{
			return InvokeInternal(target, 1, args);
		}

		public object Invoke1(params object[] args)
		{
			return Invoke1(null, args);
		}

		public LuaTable InvokeMultiRet(LuaTable target, params object[] args)
		{
			return (LuaTable)InvokeInternal(target, Api.LUA_MULTRET, args);
		}

		public LuaTable InvokeMultiRet(params object[] args)
		{
			return InvokeMultiRet(null, args);
		}

		object InvokeInternal(LuaTable target, int nrets, params object[] args)
		{
			var L = CheckValid();
			var top = Api.lua_gettop(L);
			try
			{
				Push();
				int self = 0;
				if (target != null)
				{
					target.Push();
					self = 1;
				}
				for (int i = 0; i < args.Length; ++i)
				{
					L.PushValue(args[i]);
				}
				L.Call(self + args.Length, nrets);
				if (nrets == 0)
				{
					return null;
				}
				else if (nrets == 1)
				{
					var ret = L.ValueAt(-1);
					Api.lua_settop(L, top);
					return ret;
				}
				else
				{
					nrets = Api.lua_gettop(L) - top;
					LuaTable ret = null;
					
					Api.lua_createtable(L, nrets, 0);
					for (int i = 0; i < nrets; ++i)
					{
						Api.lua_pushvalue(L, top + i + 1);
						Api.lua_seti(L, -2, i + 1);
					}
					ret = LuaTable.MakeRefTo(L, -1);
					Api.lua_settop(L, top);
					return ret;
				}
			}
			catch (Exception e)
			{
				Api.lua_settop(L, top);
				throw e;
			}
		}

		public static LuaFunction MakeRefTo(Lua L, int idx)
		{
			Lua.Assert(Api.lua_isfunction(L, idx));
			return new LuaFunction { L_ = L, funcRef = L.MakeRefAt(idx) };
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int LuaDelegate(IntPtr L)
		{
			try
			{
				return LuaDelegateInternal(L);
			}
			catch (Exception e)
			{
				Lua.PushErrorObject(L, string.Format("{0}\nnative stack traceback:\n{1}", e.Message, e.StackTrace));
				return 1;
			}
		}

		static int LuaDelegateInternal(IntPtr L)
		{
			var func = (Delegate)Lua.ObjectAtInternal(L, Api.lua_upvalueindex(1));
			var numArgs = Api.lua_gettop(L);
			var refToDelegate = Lua.MakeRefToInternal(L, func);
			try
			{

				bool isInvokingFromClass = false;
				Api.lua_pushboolean(L, isInvokingFromClass);      // upvalue 1 --> isInvokingFromClass
				Lua.PushRefInternal(L, refToDelegate);            // upvalue 2 --> userdata, first parameter of __index
				Api.lua_pushstring(L, "Invoke");                  // upvalue 3 --> member name
				Api.lua_pushcclosure(L, Lua.InvokeMethod, 3);
				Lua.PushRefInternal(L, refToDelegate);
				for (int i = 1; i <= numArgs; ++i)
				{
					Api.lua_pushvalue(L, i);
				}
				Lua.CallInternal(L, numArgs + 1, 1);
				Lua.UnrefInternal(L, refToDelegate);
			}
			catch (Exception e)
			{
				Lua.UnrefInternal(L, refToDelegate);
				throw e;
			}
			return 1;
		}

		public static LuaFunction NewFunction(Lua L, string luaFunctionScript, string name = null)
		{
			L.DoString(string.Format("return {0}", luaFunctionScript), 1, null);
			var func = MakeRefTo(L, -1);
			Api.lua_pop(L, 1);
			return func;
		}

		public static LuaFunction CreateDelegate(Lua L, Delegate func)
		{
			L.PushObject(func);
			Api.lua_pushcclosure(L, LuaDelegate, 1);
			var f = MakeRefTo(L, -1);
			Api.lua_pop(L, 1);
			return f;
		}

	}
}
