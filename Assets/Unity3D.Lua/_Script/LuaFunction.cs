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
using System.Collections.Generic;
using System.Linq;

namespace lua
{
	public class LuaFunction : IDisposable 
	{
		Lua L_;
		int funcRef = Api.LUA_NOREF;
		int refCount = 1;

		class ActionPool
		{
			static List<LuaFunction> toCollect = new List<LuaFunction>();
			class ActionSlot
			{
				public ActionSlot(LuaFunction f)
				{
					func = f.Retain();
				}

				~ActionSlot()
				{
					lock (toCollect)
					{
						toCollect.Add(func);
					}
				}

				LuaFunction func;
				Action action_;
				public Action action
				{
					get
					{
						if (action_ == null)
							action_ = () => func.Invoke();
						return action_;
					}
				}

				UnityAction unityAction_;
				public UnityAction unityAction
				{
					get
					{
						if (unityAction_ == null)
							unityAction_ = () => func.Invoke();
						return unityAction_;
					}
				}

				public Action<T> GetAction<T>()
				{
					return (arg) => func.Invoke(arg);
				}

				public Action<T1, T2> GetAction<T1, T2>()
				{
					return (arg1, arg2) => func.Invoke(arg1, arg2);
				}

				public Action<T1, T2, T3> GetAction<T1, T2, T3>()
				{
					return (arg1, arg2, arg3) => func.Invoke(arg1, arg2, arg3);
				}

				void GenericAction<T>(T arg)
				{
					func.Invoke(arg);
				}

				void GenericAction<T1, T2>(T1 arg1, T2 arg2)
				{
					func.Invoke(arg1, arg2);
				}

				void GenericAction<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
				{
					func.Invoke(arg1, arg2, arg3);
				}

				ReturnType GenericFunc<ReturnType>()
				{
					return (ReturnType)Lua.ConvertTo(func.Invoke1(), typeof(ReturnType));
				}
				ReturnType GenericFunc<T1, ReturnType>(T1 arg1)
				{
					return (ReturnType)Lua.ConvertTo(func.Invoke1(arg1), typeof(ReturnType));
				}

				ReturnType GenericFunc<T1, T2, ReturnType>(T1 arg1, T2 arg2)
				{
					return (ReturnType)Lua.ConvertTo(func.Invoke1(arg1, arg2), typeof(ReturnType));
				}

				ReturnType GenericFunc<T1, T2, T3, ReturnType>(T1 arg1, T2 arg2, T3 arg3)
				{
					return (ReturnType)Lua.ConvertTo(func.Invoke1(arg1, arg2, arg3), typeof(ReturnType));
				}

				static System.Reflection.MemberInfo[] genericActions_;
				static System.Reflection.MemberInfo[] genericActions
				{
					get
					{
						if (genericActions_ == null)
						{
							genericActions_ = typeof(ActionSlot).GetMember("GenericAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
						}
						return genericActions_;
					}
				}

				static System.Reflection.MemberInfo[] genericFuncs_;
				static System.Reflection.MemberInfo[] genericFuncs
				{
					get
					{
						if (genericFuncs_ == null)
						{
							genericFuncs_ = typeof(ActionSlot).GetMember("GenericFunc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
						}
						return genericFuncs_;
					}
				}

				static Dictionary<Type, System.Reflection.MethodInfo> cachedDelegateTypes = new Dictionary<Type, System.Reflection.MethodInfo>();
				static List<Type> types = new List<Type>();
				public System.Delegate GetDelegate(Type delegateType)
				{
					System.Reflection.MethodInfo mi = null;
					if (!cachedDelegateTypes.TryGetValue(delegateType, out mi))
					{
						var invokeMi = delegateType.GetMethod("Invoke");
						var actionOrFunc = invokeMi.ReturnType == typeof(void);
						types.Clear();
						types.AddRange(invokeMi.GetParameters().Select((p) => p.ParameterType));
						if (false == actionOrFunc)
						{
							types.Add(invokeMi.ReturnType);
						}
						var members = actionOrFunc ? genericActions : genericFuncs;
						for (int i = 0; i < members.Length; ++i)
						{
							var m = (System.Reflection.MethodInfo)members[i];
							var gps = m.GetGenericArguments();
							if (gps.Length == types.Count)
							{
								mi = m.MakeGenericMethod(types.ToArray());
								break;
							}
						}
						// cached even null
						cachedDelegateTypes.Add(delegateType, mi);
					}
					return System.Delegate.CreateDelegate(delegateType, this, mi);
				}
			}

			public static Action ToAction(LuaFunction f)
			{
				return (new ActionSlot(f)).action;
			}

			public static UnityAction ToUnityAction(LuaFunction f)
			{
				return (new ActionSlot(f)).unityAction;
			}

			public static Action<T> ToAction<T>(LuaFunction f)
			{
				return (new ActionSlot(f)).GetAction<T>();
			}

			public static Action<T1, T2> ToAction<T1, T2>(LuaFunction f)
			{
				return (new ActionSlot(f)).GetAction<T1, T2>();
			}

			public static Action<T1, T2, T3> ToAction<T1, T2, T3>(LuaFunction f)
			{
				return (new ActionSlot(f)).GetAction<T1, T2, T3>();
			}

			public static System.Delegate ToDelegate(LuaFunction f, Type delegateType)
			{
				return (new ActionSlot(f)).GetDelegate(delegateType);
			}

			public static void Collect()
			{
				lock (toCollect)
				{
					for (int i = 0; i < toCollect.Count; ++i)
					{
						toCollect[i].Dispose();
					}
					toCollect.Clear();
				}
			}
		}

		public static void CollectActionPool()
		{
			ActionPool.Collect();
		}

		public static Action ToAction(LuaFunction f)
		{
			return ActionPool.ToAction(f);
		}

		public static UnityAction ToUnityAction(LuaFunction f)
		{
			return ActionPool.ToUnityAction(f);
		}

		public static Action<T> ToAction<T>(LuaFunction f)
		{
			return ActionPool.ToAction<T>(f);
		}

		public static Action<T1, T2> ToAction<T1, T2>(LuaFunction f)
		{
			return ActionPool.ToAction<T1, T2>(f);
		}

		public static Action<T1, T2, T3> ToAction<T1, T2, T3>(LuaFunction f)
		{
			return ActionPool.ToAction<T1, T2, T3>(f);
		}

		public static System.Delegate ToDelegate(Type type, LuaFunction f)
		{
			return ActionPool.ToDelegate(f, type);
		}

		public void Dispose()
		{
			--refCount;
			if (refCount <= 0)
			{
				if (L_.valid && funcRef != Api.LUA_NOREF)
				{
					L_.Unref(funcRef);
				}
				funcRef = Api.LUA_NOREF;
				L_ = null;
			}
		}

		internal Lua CheckValid()
		{
			if (L_.valid)
				return L_;
			throw new InvalidOperationException("Lua vm already destroyed.");
		}

		public LuaFunction Retain()
		{
			++refCount;
			return this;
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
			Lua.Assert(Api.lua_isfunction(L, idx), "not function");
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
				Api.lua_pushinteger(L, 0); // upvalue 1 --> invocationFlags
				Lua.PushRefInternal(L, refToDelegate); // upvalue 2 --> userdata, first parameter of __index
				var members = Lua.GetMembers(func.GetType(), "Invoke", hasPrivatePrivillage: false);
				Lua.PushObjectInternal(L, members); // upvalue 3 --> members
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
