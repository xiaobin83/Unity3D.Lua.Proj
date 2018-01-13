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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

using lua_State = System.IntPtr;

namespace lua
{

	internal class MetaMethod
	{
		static bool IsIndexingClassObject(lua_State L)
		{
			var isIndexingClassObject = false;
			var top = Api.lua_gettop(L);
			if (Api.lua_getmetatable(L, 1) != 0)
			{
				Api.lua_rawgeti(L, -1, 1);
				isIndexingClassObject = Api.lua_toboolean(L, -1);
			}
			Api.lua_settop(L, top);
			return isIndexingClassObject;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int MetaConstructFunction(lua_State L)
		{
			try
			{
				return MetaConstructFunctionInternal(L);
			}
			catch (Exception e)
			{
				Lua.PushErrorObject(L, e.Message);
				return 1;
			}
		}
		static int MetaConstructFunctionInternal(lua_State L)
		{
			var typeObj = Lua.ObjectAtInternal(L, 1);
			Lua.Assert((typeObj != null && (typeObj is System.Type)), "Constructor needs type object.");

			var numArgs = Api.lua_gettop(L);
			int[] luaArgTypes = Lua.luaArgTypes_NoArgs;
			if (numArgs > 1) // the first arg is class itself
			{
				luaArgTypes = new int[numArgs-1];
				for (var i = 2; i <= numArgs; ++i)
				{
					luaArgTypes[i-2] = Api.lua_type(L, i);
				}
			}

			var type = (System.Type)typeObj;

			if (luaArgTypes == Lua.luaArgTypes_NoArgs && type.IsValueType)
			{
				var value = Activator.CreateInstance(type);
				Lua.PushObjectInternal(L, value);
				return 1;
			}

			var mangledName = Lua.CheckHost(L).Mangle("__ctor", luaArgTypes, invokingStaticMethod: true, argStart: 2);
			var mc = Lua.GetMethodFromCache(type, mangledName);
			System.Reflection.ParameterInfo[] parameters = null;
			if (mc == null)
			{
				var constructors = type.GetConstructors();
				System.Reflection.MethodBase selected = null;
				int highScore = int.MinValue;
				List<Exception> pendingExceptions = null;
                for (var i = 0; i < constructors.Length; ++i)
				{
					var method = constructors[i];
					try
					{
						Lua.MatchingParameters(L, 2, method, luaArgTypes, ref highScore, ref selected, ref parameters);
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
				if (selected != null)
				{
					mc = Lua.CacheMethod(type, mangledName, selected, parameters);
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
					throw new Exception(string.Format("No proper constructor available, calling {0}\n{1}", Lua.GetLuaInvokingSigniture("ctor", luaArgTypes), additionalMessage));
				}
			}

			var ctor = (System.Reflection.ConstructorInfo)mc.method;

			IDisposable[] disposableArgs;
			var args = Lua.ArgsFrom(L, mc.parameters, mc.variadicArg, 2, luaArgTypes.Length, out disposableArgs);
			Lua.PushObjectInternal(L, ctor.Invoke(args));
			if (disposableArgs != null)
			{
				foreach (var d in disposableArgs)
				{
					if (d != null) d.Dispose();
				}
			}
			return 1;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int MetaIndexFunction(lua_State L)
		{
			//UnityEngine.Profiling.Profiler.BeginSample("MetaIndexFunction");
			try
			{
				return MetaIndexFunctionInternal(L);
			}
			catch (Exception e)
			{
				Lua.PushErrorObject(L, e.Message);
				return 1;
			}
			finally
			{
				//UnityEngine.Profiling.Profiler.EndSample();
			}
		}

		static int MetaIndexFunctionInternal(lua_State L)
		{
			var isIndexingClassObject = IsIndexingClassObject(L);

			System.Type typeObject = null;
			if (isIndexingClassObject)
			{
				typeObject = (System.Type)Lua.ObjectAtInternal(L, 1);
			}

			object thisObject = null;
			if (!isIndexingClassObject)
			{
				thisObject = Lua.ObjectAtInternal(L, 1);
				typeObject = thisObject.GetType();
			}

			Lua.Assert(typeObject != null, "Should have a type");

			if (Api.lua_isinteger(L, 2))
			{
				if (typeObject != null && typeObject.IsArray)
				{
					var array = (System.Array)thisObject;
					Lua.PushValueInternal(L, array.GetValue((int)Api.lua_tointeger(L, 2)));
					return 1;
				}
				else
				{
					return Lua.IndexObjectInternal(L, thisObject, typeObject, new object[] { (int)Api.lua_tointeger(L, 2) });
				}
			}
			else if (Api.lua_isstring(L, 2))
			{
				return Lua.GetMember(L, thisObject, typeObject, Api.lua_tostring(L, 2), false, null, null);
			}
			else if (Api.lua_istable(L, 2))
			{
				var host = Lua.CheckHost(L);
				using (var p = LuaTable.MakeRefTo(host, 2))
				{
					var isGettingTypeObject = (bool)host.isIndexingTypeObject.Invoke1(p);
					if (isGettingTypeObject)
					{
						Lua.PushObjectInternal(L, typeObject);
						return 1;
					}
					using (var ret = host.testPrivillage.InvokeMultiRet(p))
					{
						var name = (string)ret[1];
						var hasPrivatePrivillage = (bool)ret[2];
						var retrievingNestedType = (bool)ret[5];
						if (retrievingNestedType)
						{
							var flags = System.Reflection.BindingFlags.Public;
							if (hasPrivatePrivillage)
								flags |= System.Reflection.BindingFlags.NonPublic;
							var nestedType = typeObject.GetNestedType(name, flags);
							if (nestedType != null)
							{
								Lua.PushTypeInternal(L, nestedType);
							}
							else
							{
								Api.lua_pushnil(L);
							}
							return 1;
						}
						var exactTypes = (Type[])ret[3];
						var genericTypes = (Type[])ret[4];
						return Lua.GetMember(L, thisObject, typeObject, name, hasPrivatePrivillage, exactTypes, genericTypes);
					}
			
				}
			}
			else
			{
				return Lua.IndexObjectInternal(L, thisObject, typeObject, new object[] { Lua.ValueAtInternal(L, 2) });
			}
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int MetaNewIndexFunction(lua_State L)
		{
			try
			{
				return MetaNewIndexFunctionInternal(L);
			}
			catch (Exception e)
			{
				Lua.PushErrorObject(L, e.Message);
				return 1;
			}
		}

		static int MetaNewIndexFunctionInternal(lua_State L)
		{
			var isIndexingClassObject = IsIndexingClassObject(L);

			System.Type typeObject = null;
			if (isIndexingClassObject)
			{
				typeObject = (System.Type)Lua.ObjectAtInternal(L, 1);
			}

			object thisObject = null;
			if (!isIndexingClassObject)
			{
				thisObject = Lua.ObjectAtInternal(L, 1);
				typeObject = thisObject.GetType();
			}

			Lua.Assert(typeObject != null, "Should has a type.");

			if (Api.lua_isnumber(L, 2))
			{
				if (typeObject != null && typeObject.IsArray)
				{
					var array = (System.Array)thisObject;
					var value = Lua.ValueAtInternal(L, 3);
					var index = (int)Api.lua_tointeger(L, 2);
					array.SetValue(Lua.ConvertTo(value, typeObject.GetElementType()), index);
				}
				else
				{
					Lua.SetValueAtIndexOfObject(L, thisObject, typeObject, new object[] { (int)Api.lua_tointeger(L, 2) }, Lua.ValueAtInternal(L, 3));
				}
			}
			else if (Api.lua_isstring(L, 2))
			{
				Lua.SetMember(L, thisObject, typeObject, Api.lua_tostring(L, 2), Lua.ValueAtInternal(L, 3), hasPrivatePrivillage: false);
			}
			else if (Api.lua_istable(L, 2))
			{
				var host = Lua.CheckHost(L);
				using (var p = LuaTable.MakeRefTo(host, 2))
				{
					using (var ret = host.testPrivillage.InvokeMultiRet(p))
					{
						var name = (string)ret[1];
						Lua.SetMember(L, thisObject, typeObject, name, Lua.ValueAtInternal(L, 3), hasPrivatePrivillage: true);
					}
				}
			}
			else
			{
				Lua.SetValueAtIndexOfObject(L, thisObject, typeObject, new object[] { Lua.ValueAtInternal(L, 2) }, Lua.ValueAtInternal(L, 3));
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int MetaToStringFunction(lua_State L)
		{
			try
			{
				return MetaToStringFunctionInternal(L);
			}
			catch (Exception e)
			{
				Lua.PushErrorObject(L, e.Message);
				return 1;
			}
		}

		static int MetaToStringFunctionInternal(lua_State L)
		{
			var thisObject = Lua.ValueAtInternal(L, 1);
			Api.lua_pushstring(L, thisObject.ToString());
			return 1;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int MetaGcFunction(lua_State L)
		{
			try
			{
				return MetaGcFunctionInternal(L);
			}
			catch (Exception)
			{
				return 0;
			}
		}

		static int MetaGcFunctionInternal(lua_State L)
		{
			var userdata = Api.lua_touserdata(L, 1);
			var ptrToObjHandle = Marshal.ReadIntPtr(userdata);
			var handleToObj = GCHandle.FromIntPtr(ptrToObjHandle);
			handleToObj.Free();
			return 0;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int MetaBinaryOpFunction(lua_State L)
		{
			try
			{
				return MetaBinaryOpFunctionInternal(L);
			}
			catch (Exception e)
			{
				Lua.PushErrorObject(L, e.Message);
				return 1;
			}
		}

		static int MetaBinaryOpFunctionInternal(lua_State L)
		{
			var op = (Lua.BinaryOp)Api.lua_tointeger(L, Api.lua_upvalueindex(2));
			var objectArg = Api.luaL_testudata(L, 1, Lua.objectMetaTable);
			var objectArg2 = Api.luaL_testudata(L, 2, Lua.objectMetaTable);
			if (objectArg == IntPtr.Zero && objectArg2 == IntPtr.Zero)
			{
				throw new LuaException(string.Format("Binary op {0} called on unexpected values.", Api.lua_tostring(L, Api.lua_upvalueindex(1))));
			}
			object obj1 = null, obj2 = null;
			if (objectArg != IntPtr.Zero) obj1 = Lua.UdataToObject(objectArg);
			if (objectArg2 != IntPtr.Zero) obj2 = Lua.UdataToObject(objectArg2);
			if (op == Lua.BinaryOp.op_Equality)
			{
				if ((obj1 == obj2)
					|| (obj1 != null && obj1.Equals(obj2))
					|| (obj2 != null && obj2.Equals(obj1)))
				{
					Api.lua_pushboolean(L, true);
				}
				else
				{
					Api.lua_pushboolean(L, false);
				}
				return 1;
			}
			var obj = obj1;
			if (obj == null)
				obj = obj2;

			var type = obj.GetType();
			var opName = Api.lua_tostring(L, Api.lua_upvalueindex(1));
			var	members	= Lua.GetMembers(type, opName, hasPrivatePrivillage: false);
			if (members.Length == 0)
			{
				throw new LuaException(string.Format("{0} not found in type {1}", opName, type.ToString()));
			}

			long invocationFlags = (long)Lua.InvocationFlags.Static;
			Api.lua_pushinteger(L, invocationFlags); // upvalue 1 --> invocationFlags
			Lua.PushObjectInternal(L, type);// upvalue 2 --> userdata (host of metatable).
			Lua.PushObjectInternal(L, members); // upvalue 3 --> members
			Api.lua_pushcclosure(L, Lua.InvokeMethod, 3);
			Api.lua_pushvalue(L, 1);
			Api.lua_pushvalue(L, 2);
			Lua.CallInternal(L, 2, 1);
			return 1;
		}

		[MonoPInvokeCallback(typeof(Api.lua_CFunction))]
		internal static int MetaUnaryOpFunction(lua_State L)
		{
			try
			{
				return MetaUnaryOpFunctionInternal(L);
			}
			catch (Exception e)
			{
				Lua.PushErrorObject(L, e.Message);
				return 1;
			}
		}

		static int MetaUnaryOpFunctionInternal(lua_State L)
		{
			var objectArg = Api.luaL_testudata(L, 1, Lua.objectMetaTable); // test first one
			if (objectArg == IntPtr.Zero)
				throw new LuaException(string.Format("Binary op {0} called on unexpected values.", Api.lua_tostring(L, Api.lua_upvalueindex(1))));
			var obj = Lua.UdataToObject(objectArg);
			var type = obj.GetType();
			var opName = Api.lua_tostring(L, Api.lua_upvalueindex(1));
			var members = Lua.GetMembers(type, opName, hasPrivatePrivillage: false);
			if (members.Length == 0)
			{
				throw new LuaException(string.Format("{0} not found in type {1}", opName, type.ToString()));
			}

			// upvalue 1 --> invocationFlags
			// upvalue 2 --> userdata (host of metatable).
			// upvalue 3 --> members
			Api.lua_pushinteger(L, (long)Lua.InvocationFlags.Static);
			Lua.PushObjectInternal(L, type);
			Lua.PushObjectInternal(L, members);
			Api.lua_pushcclosure(L, Lua.InvokeMethod, 3);
			Api.lua_pushvalue(L, 1);
			Lua.CallInternal(L, 1, 1);
			return 1;
		}
	}


}
