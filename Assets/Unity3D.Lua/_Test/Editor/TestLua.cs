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
﻿using UnityEngine;
using System.Collections;
using NUnit.Framework;
using AOT;
using System.Linq;

namespace lua.test
{
	struct TestStruct
	{
		public float floatValue;
		public int intValue;
		public string stringValue;
	}

	class TestRetValue
	{
		public string value = "A Test String";

		~TestRetValue()
		{
			Debug.Log("TestRetValue destroyed.");
		}
	}

	class TestClass
	{
		public int test = 42;
		public int TestMethod()
		{
			return test;
		}
		public static int TestStaticMethod()
		{
			return 43;
		}

		public string TestMethodWithParam(int p0, string p1)
		{
			return "ret_" + p1 + p0;
		}

		public TestRetValue GetRetValue()
		{
			return new TestRetValue();
		}

		public int TestOutParam(int k, out int t1, out Vector3 t2, ref int t3)
		{
			t1 = 11;
			t2 = new Vector3(1, 2, 3);
			t3 = t1 + t3;
			return k+10;
		}

		public int TestOverloading(int a)
		{
			return test;
		}
		public int TestOverloading(int a, string b)
		{
			return test;
		}
	}

	class TestDerivedClass : TestClass
	{

	}

	[TestFixture]
	public class TestLua
	{
		Lua L;
		TestClass obj;
		int objRef;



		[TestFixtureSetUp]
		public void SetUp()
		{
			L = new Lua();
			obj = new TestClass();
			try
			{
				objRef = L.MakeRefTo(obj);
			}
			catch (System.Exception e)
			{
				Debug.LogError(e);
				throw e;
			}
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			L.Unref(objRef);
			Lua.CleanMethodCache();
			L.Dispose();
		}

		[Test]
		public void PreTest()
		{
			Api.lua_settop(L, 0);

			Debug.Log(Lua.DebugStack(L));
			Api.lua_pushnumber(L, 1);
			Api.lua_pushnumber(L, 2);
			Api.lua_pushnumber(L, 3);
			Api.lua_pushnumber(L, 4);
			var top  = Api.lua_gettop(L) - 3;
			Api.lua_pushnumber(L, 5);
			Api.lua_insert(L, top);
			Debug.Log(Lua.DebugStack(L));
			Api.lua_pop(L, 4);
			Api.lua_pushboolean(L, true);
			Api.lua_remove(L, top);
			Debug.Log(Lua.DebugStack(L));

			Api.lua_pop(L, 1);



			var type = typeof(System.Int32);
			double t = 10.0;
			var converted = System.Convert.ChangeType(t, type);
			Assert.True(converted is int);
			Assert.AreEqual(10, (int)converted);
		}

		[Test]
		public void TestAccessFieldFromLua()
		{
			Api.lua_settop(L, 0);

			Api.luaL_dostring(L, 
				"function Test(obj)\n" + 
				"  return obj.test\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			Assert.AreEqual(Api.LUA_TFUNCTION, Api.lua_type(L, -1));
			L.PushRef(objRef);
			L.Call(1, 1);
			Assert.AreEqual(Api.LUA_TNUMBER, Api.lua_type(L, -1));
			Assert.AreEqual(obj.test, Api.lua_tointeger(L, -1));
			Api.lua_pop(L, 1);

			Assert.AreEqual(0, Api.lua_gettop(L));

		}

		[Test]
		public void TestCallMethodFromLua()
		{

			Api.lua_settop(L, 0);

			Api.luaL_dostring(L, 
				"return function(obj)\n" + 
				"  return obj:TestMethod()\n" + 
				"end");
			for (int i = 0; i < 1000; ++i)
			{
				Api.lua_pushvalue(L, -1);
				L.PushRef(objRef);
				L.Call(1, 1);
				Assert.AreEqual(Api.LUA_TNUMBER, Api.lua_type(L, -1));
				Assert.AreEqual(obj.test, (int)Api.lua_tointeger(L, -1));
				Api.lua_pop(L, 1);
			}

			Api.lua_pop(L, 1);
			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		[ExpectedException(typeof(lua.LuaException))]
		public void TestCallMethodFromLua_IncorrectSyntax()
		{
			Api.lua_settop(L, 0);
			Api.luaL_dostring(L, 
				"return function(obj)\n" + 
				"  return obj.TestMethod()\n" + 
				"end");
			L.PushRef(objRef);
			string thisMessage = string.Empty;
			Config.LogError = (message) => thisMessage = message;
			try
			{
				L.Call(1, 1);
			}
			catch (System.Exception e)
			{
				Assert.True(thisMessage.IndexOf("invoking non-static method Int32 TestMethod() with incorrect syntax") > 0);
				Assert.Greater(thisMessage.Length, 0);
				Config.LogError = null;
				Api.lua_settop(L, 0);
				throw e;
			}
			Assert.Fail("never get here");
		}

		[Test]
		public void TestCallStaticMethodFromLua()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			Api.luaL_dostring(L, 
				"function Test(obj)\n" + 
				"  return obj.TestStaticMethod()\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			Assert.AreEqual(Api.LUA_TFUNCTION, Api.lua_type(L, -1));
			L.PushRef(objRef);
			L.Call(1, 1);
			Assert.AreEqual(Api.LUA_TNUMBER, Api.lua_type(L, -1));
			Assert.AreEqual(43, Api.lua_tointeger(L, -1));
			Api.lua_pop(L, 1);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));


			Assert.AreEqual(0, Api.lua_gettop(L));

		}

		[Test]
		[ExpectedException(exceptionType: typeof(lua.LuaException))]
		public void TestCallStaticMethodFromLua_IncorrectSyntax()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			Api.luaL_dostring(L, 
				"function Test(obj)\n" + 
				"  return obj:TestStaticMethod()\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			Assert.AreEqual(Api.LUA_TFUNCTION, Api.lua_type(L, -1));
			L.PushRef(objRef);
			string thisMessage = string.Empty;
			Config.LogError = (message) => thisMessage = message;
			try
			{
				L.Call(1, 1);
			}
			catch (System.Exception e)
			{
				Assert.True(thisMessage.IndexOf("invoking static method Int32 TestStaticMethod() with incorrect syntax") > 0);
				Assert.Greater(thisMessage.Length, 0);
				Config.LogError = null;
				Api.lua_settop(L, stackTop);
				throw e;
			}
			Assert.Fail("never get here");
		}

		[Test]
		public void TestCallMethodWithParamFromLua()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			Api.luaL_dostring(L, 
				"function Test(obj)\n" + 
				"  return obj:TestMethodWithParam(11, 'TestString')\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			Assert.AreEqual(Api.LUA_TFUNCTION, Api.lua_type(L, -1));
			L.PushRef(objRef);
			L.Call(1, 1);
			Assert.AreEqual(Api.LUA_TSTRING, Api.lua_type(L, -1));
			Assert.AreEqual("ret_TestString11", Api.lua_tostring(L, -1));
			Api.lua_pop(L, 1);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		void SetupTestMethodCache()
		{
			Api.luaL_dostring(L, 
				"function Test(obj)\n" + 
				"  return obj:TestMethodWithParam(10, 'TestString')\n" + 
				"end");
		}

		void MethodCacheTestLoop()
		{
			var top = Api.lua_gettop(L);
			Api.lua_getglobal(L, "Test");
			for (int i = 0; i < 10000; ++i)
			{
				var innerTop = Api.lua_gettop(L);
				Assert.AreEqual(1, Api.lua_gettop(L));

				Api.lua_pushvalue(L, -1); // push function
				if (!Api.lua_isfunction(L, -1))
				{
					Debug.LogError(Lua.DebugStack(L));
					Assert.Fail("Not a func? " + i);
				}
				L.PushRef(objRef); // push obj
				Assert.True(Api.lua_isuserdata(L, -1));
				L.Call(1, 1); // call obj:func
				Api.lua_pop(L, 1); // pop ret
				if (innerTop != Api.lua_gettop(L))
				{
					Debug.LogError(Lua.DebugStack(L));
				}
				Assert.AreEqual(innerTop, Api.lua_gettop(L));
			}
			Api.lua_pop(L, 1);
			Assert.AreEqual(top, Api.lua_gettop(L));
		}

		[Test]
		public void TestMethodCache()
		{
			Api.lua_settop(L, 0);
			SetupTestMethodCache();
			MethodCacheTestLoop();
			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		public void TestAccessingObjectReturned()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			Api.luaL_dostring(L, 
				"function Test(obj)\n" + 
				"  return obj:GetRetValue().value\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			Assert.AreEqual(Api.LUA_TFUNCTION, Api.lua_type(L, -1));
			L.PushRef(objRef);
			L.Call(1, 1);
			Assert.AreEqual(Api.LUA_TSTRING, Api.lua_type(L, -1));
			var ret = new TestRetValue();
			Assert.AreEqual(ret.value, Api.lua_tostring(L, -1));
			Api.lua_pop(L, 1);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		public void TestStructValue()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);

			var value = new TestStruct();
			value.floatValue = 10f;
			value.intValue = 20;
			value.stringValue = "30";

			Api.luaL_dostring(L, 
				"function Test(obj)\n" + 
				"  return obj.floatValue .. obj.intValue .. obj.stringValue\n" + 
				"end");

			Api.lua_getglobal(L, "Test");
			Assert.AreEqual(Api.LUA_TFUNCTION, Api.lua_type(L, -1));
			L.PushValue(value);
			L.Call(1, 1);
			Assert.AreEqual(Api.LUA_TSTRING, Api.lua_type(L, -1));
			Assert.AreEqual("10.02030", Api.lua_tostring(L, -1));
			Api.lua_pop(L, 1);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		public void TestSetStructValue()
		{
			string thisMessage = string.Empty;
			Config.LogError = (msg) => thisMessage = msg;
			using (var f = LuaFunction.NewFunction(L, "function(st) st.floatValue = 42 end"))
			{
				var st = new TestStruct();
				st.floatValue = 1f;
				f.Invoke(null, st);
				Assert.AreEqual(1f, st.floatValue);
			}
			Config.LogError = null;
			Assert.True(string.IsNullOrEmpty(thisMessage), thisMessage);
		}

		[Test]
		public void TestSettingField()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			Api.luaL_dostring(L, 
				"function Test(obj)\n" + 
				"  obj.test = 13\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			Assert.AreEqual(Api.LUA_TFUNCTION, Api.lua_type(L, -1));
			var obj = new TestClass();
			L.PushValue(obj);
			L.Call(1, 0);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));
			Assert.AreEqual(13, obj.test);

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		public void TestArray_GetElement()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			var testArray = new TestClass[10];
			for (int i = 0; i < testArray.Length; ++i)
			{
				testArray[i] = new TestClass();
				testArray[i].test = i;
			}
			Api.luaL_dostring(L,
				"function Test(obj)\n" + 
				"  return obj[4]\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			L.PushValue(testArray);
			L.Call(1, 1);
			Assert.True(Api.lua_isuserdata(L, -1));
			var obj = L.ObjectAt(-1);
			Assert.AreEqual(testArray[4], obj);
			Assert.AreEqual(4, ((TestClass)obj).test);
			Api.lua_pop(L, 1);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		public void TestArray_SetElement()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			var testArray = new int[10];
			for (int i = 0; i < testArray.Length; ++i)
			{
				testArray[i] = i;
			}
			Api.luaL_dostring(L,
				"function Test(obj)\n" + 
				"  obj[4] = 42\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			L.PushValue(testArray);
			L.Call(1, 0);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));
			Assert.AreEqual(42, testArray[4]);

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		public void TestCreateCsharpObjectFromLua()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			Api.luaL_dostring(L,
				"Vector3 = csharp.import('UnityEngine.Vector3, UnityEngine')\n" +
				"function TestCreateVector3()\n" +
				"  return Vector3(1, 2, 3)\n" + 
				"end");
			Api.lua_getglobal(L, "TestCreateVector3");
			L.Call(0, 1);
			var v = (Vector3)L.ObjectAt(-1);
			Assert.AreEqual(new Vector3(1f, 2f, 3f), v);
			Api.lua_pop(L, 1);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		public void TestImportShouldExecuteOnlyOnce()
		{
			Api.lua_settop(L, 0);

			Api.luaL_dostring(L,
				"return function()\n" +
				"  Vector3 = csharp.import('UnityEngine.Vector3, UnityEngine')\n" +
				"  return Vector3(1, 2, 3)\n" + 
				"end");
			for (int i = 0; i < 10000; ++i)
			{
				Api.lua_pushvalue(L, -1);
				L.Call(0, 1);
				Api.lua_pop(L, 1);
			}
			Api.lua_pop(L, 1);
			Assert.AreEqual(0, Api.lua_gettop(L));
		}


		[Test]
		public void TestCreateCsharpObjectFromLua_AndAccessIt()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			Api.luaL_dostring(L,
				"Vector3 = csharp.import('UnityEngine.Vector3, UnityEngine')\n" +
				"function TestCreateVector3()\n" +
				"  local v = Vector3(1, 2, 3)\n" + 
				"  return v.x + v.y\n" + 
				"end");
			Api.lua_getglobal(L, "TestCreateVector3");
			L.Call(0, 1);
			var v = Api.lua_tonumber(L, -1);
			Assert.AreEqual(3.0, v);
			Api.lua_pop(L, 1);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		class DefaultCtor
		{
			public int t = 10;
		}


		[Test]
		public void TestCreateCsharpObjectWithDefaultCtor()
		{
			L.Import(typeof(DefaultCtor), "DefaultCtor");
			using (var f = LuaFunction.NewFunction(
				L,
				"function()\n" +
				"  local c = DefaultCtor()\n" +
				"  return c.t\n" +
				"end"))
			{
				var ret = f.Invoke1();
				Assert.AreEqual(10, ret);
			}
		}


		public class MyClass
		{
			public static int value = 20;
		}

		[Test]
		public void TestSetStaticFieldOfClass()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			Api.luaL_dostring(L,
				"MyClass = csharp.import('lua.test.TestLua+MyClass, Assembly-CSharp-Editor')\n" +
				"function Test()\n" +
				"  MyClass.value = 42\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			L.Call(0, 0);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));
			Assert.AreEqual(42, MyClass.value);

			Assert.AreEqual(0, Api.lua_gettop(L));
		}


		[Test]
		public void TestSetStaticFieldOfClass_ImportGlobal()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			L.Import(typeof(MyClass), "Global_MyClass");
			Api.luaL_dostring(L,
				"function Test()\n" +
				"  Global_MyClass.value = 42\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			L.Call(0, 0);
			Assert.AreEqual(stackTop, Api.lua_gettop(L));
			Assert.AreEqual(42, MyClass.value);

			Assert.AreEqual(0, Api.lua_gettop(L));
		}


		[Test]
		public void TestDebugLog()
		{
			Api.lua_settop(L, 0);

			L.Import(typeof(UnityEngine.Debug), "UnityDebug");
			Api.luaL_dostring(L,
				"function Test()\n" +
				"  csharp.check_error(UnityDebug.Log('Hello UnityDebug'))\n" + 
				"  csharp.check_error(UnityDebug.Log(42))\n" + 
				"end");
			Api.lua_getglobal(L, "Test");
			L.Call(0, 0);

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		public void TestDumpAndLoad()
		{
			Api.lua_settop(L, 0);

			L.Import(typeof(UnityEngine.Debug), "UnityDebug");
			Api.luaL_dostring(L,
				"return function()\n" +
//				"  UnityDebug.Log('Hello UnityDebug')\n" +
//				"  UnityDebug.Log(42)\n" +
				"  return 10\n"	+
				"end");

			var chunk = L.DumpChunk();
			Assert.True(chunk != null && chunk.Length > 0);
			L.LoadChunk(chunk, "Test_LoadFromChunk");
			Assert.True(Api.lua_isfunction(L, -1));
			L.Call(0, 1);
			Assert.AreEqual(10, Api.lua_tonumber(L, -1));
			Api.lua_pop(L, 1);

			
			for (int i = 0; i < 10000; ++i)
			{
				chunk = L.DumpChunk();
				L.LoadChunk(chunk, "Test_LoadFromChunk");
				L.Call(0, 1);
				Api.lua_pop(L, 1);
			}

			Api.lua_pop(L, 1);
			Assert.AreEqual(0, Api.lua_gettop(L));

		}

//*
		[Test]
		public void TestCallFunctionOutParams()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);
			Api.luaL_dostring(L,
				"return function(obj)\n" +
				"  ret, t1, t2, t3 = obj:TestOutParam(10, nil, nil, 10)\n" + 
				"  return ret, t1, t2, t3\n" +
				"end");

			L.PushRef(objRef);
			L.Call(1, Api.LUA_MULTRET);

			Assert.AreEqual(20.0, Api.lua_tonumber(L, -4));
			Assert.AreEqual(11.0, Api.lua_tonumber(L, -3));
			var value = (Vector3)L.ValueAt(-2);
			Assert.AreEqual(1f, value.x);
			Assert.AreEqual(2f, value.y);
			Assert.AreEqual(3f, value.z);
			// t3 =	t1 + t3
			Assert.AreEqual(21.0, Api.lua_tonumber(L, -1));
			Api.lua_pop(L, 4);

			Assert.AreEqual(stackTop, Api.lua_gettop(L));
		}
//*/

		public class SomeClass
		{
			public int MeCallYou(lua.LuaFunction complete)
			{
				return (int)(long)complete.Invoke1();
			}

			public int MeCallYou2(lua.LuaFunction complete)
			{
				return (int)(long)complete.Invoke1(null, "called in MeCallYou2");
			}

			public int Call(int i)
			{
				return i;
			}

			~SomeClass()
			{
			}
		}
//*
		[Test]
		public void TestCallNativeFuncWithLuaCallback()
		{
			Api.lua_settop(L, 0);
			var inst = new SomeClass();
			var re = L.MakeRefTo(inst);
			Api.luaL_dostring(
				L,
				"function Test(obj)\n" +
				" return obj:MeCallYou(function() \n" +
//				"     local Debug = csharp.import('UnityEngine.Debug, UnityEngine')\n" +
//				"     Debug.Log('HERE')\n" +
				"	  return 10\n" +
				"	end)\n"	+
				"end");
			Assert.AreEqual(0, Api.lua_gettop(L));
			Api.lua_getglobal(L, "Test");
			for (int i = 0; i < 10000; ++i)
			{
				Api.lua_pushvalue(L, -1);
				L.PushRef(re);
				L.Call(1, 1);
				Assert.AreEqual(10.0, Api.lua_tonumber(L, -1));
				Api.lua_pop(L, 1);
			}

			Api.lua_pop(L, 1);
			Assert.AreEqual(0, Api.lua_gettop(L));
		}

//*/


//*

//*/

//*
		[Test]
		public void TestRunScript()
		{
			Api.lua_settop(L, 0);

			using (var runMe = (lua.LuaTable)L.RunScript1("RunMe"))
			{
				Assert.AreEqual(0, Api.lua_gettop(L));

				using (var ret = (LuaTable)runMe.InvokeMultiRet("MyFunc", "Hello"))
				{
					Assert.AreEqual(0, Api.lua_gettop(L));

					Assert.AreEqual(4, ret.Length);
					Assert.AreEqual(1, (long)ret[1]);
					Assert.AreEqual(2, (long)ret[2]);
					Assert.AreEqual(3, (long)ret[3]);
					Assert.AreEqual("Hello", (string)ret[4]);

					// set value in runMe, and re RunScript on RunMe, the value should lost
					runMe["TestValue"] = "Test Value";
					runMe[25] = 7788;
					Assert.AreEqual("Test Value", runMe["TestValue"]);
					Assert.AreEqual(7788, runMe[25]);
				}
			}


			using (var runMe = (lua.LuaTable)L.RunScript1("RunMe"))
			{
				Assert.AreEqual(0, Api.lua_gettop(L));
				Assert.AreEqual(null, runMe["TestValue"]);
				Assert.AreEqual(null, runMe[25]);
			}

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		public void TestRequire()
		{
			Api.lua_settop(L, 0);

			using (var runMe = (lua.LuaTable)L.Require("RunMe"))
			{
				Assert.AreEqual(0, Api.lua_gettop(L));

				using (var ret = (LuaTable)runMe.InvokeMultiRet("MyFunc", "Hello"))
				{
					Assert.AreEqual(0, Api.lua_gettop(L));

					Assert.AreEqual(4, ret.Length);

					Assert.AreEqual(0, Api.lua_gettop(L));

					Assert.AreEqual(1, (long)ret[1]);
					Assert.AreEqual(2, (long)ret[2]);
					Assert.AreEqual(3, (long)ret[3]);

					Assert.AreEqual(0, Api.lua_gettop(L));

					Assert.AreEqual("Hello", (string)ret[4]);

					Assert.AreEqual(0, Api.lua_gettop(L));


					// set value in runMe, and re Require on RunMe, the value should be there
					runMe["TestValue"] = "Test Value";
					runMe[25] = 7788;
					Assert.AreEqual("Test Value", runMe["TestValue"]);
				}
			}

			using (var runMe = (lua.LuaTable)L.Require("RunMe"))
			{
				Assert.AreEqual("Test Value", runMe["TestValue"]);
				Assert.AreEqual(7788, runMe[25]);
			}

			Assert.AreEqual(0, Api.lua_gettop(L));
		}
//*/

		[Test]
		public void TestPushBytesAsLuaString()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);

			var bytes = new byte[30];
			for (int i = 0; i < bytes.Length; ++i)
			{
				bytes[i] = (byte)Random.Range(0, 255);
			}
			bytes[0] = 0;

			Api.luaL_dostring(L, 
				"return function(s)\n" +
				"  return s\n" +
				"end");
			Assert.True(Api.lua_isfunction(L, -1));
			L.PushValue(bytes);
			Assert.True(Api.lua_isstring(L, -1));
			L.Call(1, 1);
			var outBytes = Api.lua_tobytes(L, -1);
			Assert.AreEqual(30, outBytes.Length);
			for (int i = 0; i < bytes.Length; ++i)
			{
				Assert.AreEqual(bytes[i], outBytes[i]);
			}
			Api.lua_settop(L, stackTop);
		}

		[Test]
		public void TestPushBytesAsLuaString_UsePushArray()
		{
			Api.lua_settop(L, 0);

			var stackTop = Api.lua_gettop(L);

			var bytes = new byte[30];
			for (int i = 0; i < bytes.Length; ++i)
			{
				bytes[i] = (byte)Random.Range(0, 255);
			}
			bytes[0] = 0;

			Api.luaL_dostring(L, 
				"return function(s)\n" +
				"  return s\n" +
				"end");
			Assert.True(Api.lua_isfunction(L, -1));
			L.PushArray(bytes);
			Assert.True(Api.lua_isstring(L, -1));
			L.Call(1, 1);
			var outBytes = Api.lua_tobytes(L, -1);
			Assert.AreEqual(30, outBytes.Length);
			for (int i = 0; i < bytes.Length; ++i)
			{
				Assert.AreEqual(bytes[i], outBytes[i]);
			}
			Api.lua_settop(L, stackTop);
		}

		[Test]
		public void TestOperatorEquality()
		{
			Api.lua_settop(L, 0);

			Api.luaL_dostring(L, "return function(a, b) return a == b end");
			Api.lua_pushvalue(L, -1);
			L.PushValue(new Vector3(1, 2, 3));
			Api.lua_pushvalue(L, -1);
			L.Call(2, 1);
			var bret = (bool)L.ValueAt(-1);
			Assert.True(bret);
			Api.lua_pop(L, 2);


			Api.luaL_dostring(L, "return function(a, b) return a == b end");
			Api.lua_pushvalue(L, -1);
			L.PushValue(new Vector3(1, 2, 3));
			L.PushValue(new Vector3(1, 2, 3));
			L.Call(2, 1);
			bret = (bool)L.ValueAt(-1);
			Assert.True(bret);
			Api.lua_pop(L, 2);

			Assert.AreEqual(0, Api.lua_gettop(L));
		}

		[Test]
		public void TestOperator()
		{
			Api.lua_settop(L, 0);

			Api.luaL_dostring(L, "return function(a, b) return a + b end");
			Api.lua_pushvalue(L, -1);
			L.PushValue(new Vector3(1, 2, 3));
			L.PushValue(new Vector3(2, 3, 4));
			L.Call(2, 1);
			var ret = (Vector3)L.ValueAt(-1);
			Assert.AreEqual(3f, ret.x);
			Assert.AreEqual(5f, ret.y);
			Assert.AreEqual(7f, ret.z);
			Api.lua_pop(L, 2);


			Api.luaL_dostring(L, "return function(a, b) return a * b end");
			Api.lua_pushvalue(L, -1);
			L.PushValue(new Vector3(1, 2, 3));
			L.PushValue(2f);
			L.Call(2, 1);
			ret = (Vector3)L.ValueAt(-1);
			Assert.AreEqual(2f, ret.x);
			Assert.AreEqual(4f, ret.y);
			Assert.AreEqual(6f, ret.z);
			Api.lua_pop(L, 2);

			Api.luaL_dostring(L, "return function(a, b) return a * b end");
			Api.lua_pushvalue(L, -1);
			L.PushValue(2f);
			L.PushValue(new Vector3(1, 2, 3));
			L.Call(2, 1);
			ret = (Vector3)L.ValueAt(-1);
			Assert.AreEqual(2f, ret.x);
			Assert.AreEqual(4f, ret.y);
			Assert.AreEqual(6f, ret.z);
			Api.lua_pop(L, 2);

			Api.luaL_dostring(L, "return function(a, b) return a == b end");
			Api.lua_pushvalue(L, -1);
			L.PushValue(new Vector3(1, 2, 3));
			Api.lua_pushvalue(L, -1);
			L.Call(2, 1);
			var bret = (bool)L.ValueAt(-1);
			Assert.True(bret);
			Api.lua_pop(L, 2);

			Api.luaL_dostring(L, "return function(a, b) return a - b end");
			Api.lua_pushvalue(L, -1);
			L.PushValue(new Vector3(1, 2, 3));
			L.PushValue(new Vector3(2, 3, 4));
			L.Call(2, 1);
			ret = (Vector3)L.ValueAt(-1);
			Assert.AreEqual(-1f, ret.x);
			Assert.AreEqual(-1f, ret.y);
			Assert.AreEqual(-1f, ret.z);
			Api.lua_pop(L, 2);

			Api.luaL_dostring(L, "return function(a) return -a end");
			Api.lua_pushvalue(L, -1);
			L.PushValue(new Vector3(1, 2, 3));
			L.Call(1, 1);
			ret = (Vector3)L.ValueAt(-1);
			Assert.AreEqual(-1f, ret.x);
			Assert.AreEqual(-2f, ret.y);
			Assert.AreEqual(-3f, ret.z);
			Api.lua_pop(L, 2);


			Assert.AreEqual(0, Api.lua_gettop(L));

		}

		[Test]
		public void TestEnumBitwiseOp()
		{
			L.Import(typeof(System.Reflection.BindingFlags), "flags");
			using (var f = LuaFunction.NewFunction(L,
				"function() return flags.NonPublic | flags.Instance end"))
			{
				var r = f.Invoke1();
				Assert.AreEqual((int)(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance), r);
			}
		}

		[Test]
		public void TestEnumAsArgument()
		{
			using (var f = LuaFunction.NewFunction(L,
				"function(v) return type(v) == 'number' end"))
			{
				var r = f.Invoke1(System.Reflection.BindingFlags.Instance);
				Assert.AreEqual(true, (bool)r);
			}
		}

		[Test]
		public void TestToEnum()
		{
			L.Import(typeof(System.Reflection.BindingFlags), "flags");
			using (var f = LuaFunction.NewFunction(L,
				"function() return csharp.to_enum(flags, 36) end"))
			{
				var r = (System.Reflection.BindingFlags)f.Invoke1();
				Assert.AreEqual(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, r);
			}
		}

		int c = 20;
		int HaveFun(int a, int b)
		{
			return a + b + c;
		}



		[Test]
		public void TestSetDelegateToTable()
		{
			using (var table = new LuaTable(L))
			{
				table.SetDelegate("Test", new System.Func<int, int, int>(HaveFun));
				var ret = table.InvokeStatic1("Test", 1, 2);
				Assert.AreEqual(23, (long)ret);
			}
		}

		int HaveFun2(LuaTable self, int a, int b)
		{
			return (int)(a + b + c + (long)self["somevalue"]);
		}

		[Test]
		public void TestSetDelegateToTableWithSelf()
		{
			using (var table = new LuaTable(L))
			{
				table["somevalue"] = 20;
				table.SetDelegate("Test", new System.Func<LuaTable, int, int, int>(HaveFun2));
				var ret = table.Invoke1("Test", 1, 2);
				Assert.AreEqual(43, (long)ret);

				Api.luaL_dostring(L, 
					"return function(t) return t:Test(3, 4) end");
				table.Push();
				L.Call(1, 1);
				var val = Api.lua_tonumber(L, -1);
				Assert.AreEqual(47, (long)val);
			}
		}

		double HaveFun3(LuaTable self, int a, int b, LuaFunction func)
		{
			return (long)func.Invoke1(null, (a + b + c + (long)self["somevalue"]));
		}

		[Test]
		public void TestSetDelegateToTableWithFunc()
		{
			using (var table = new LuaTable(L))
			{
				table["somevalue"] = 20;
				table.SetDelegate("Test", new System.Func<LuaTable, int, int, LuaFunction, double>(HaveFun3));
				Api.luaL_dostring(L, 
					"return function(t) return t:Test(3, 4, function(k) return k + 5 end) end");
				table.Push();
				L.Call(1, 1);
				var val = Api.lua_tonumber(L, -1);
				Assert.AreEqual(52.0, (double)val);
			}
		}


		void FuncThrowError()
		{
			throw new System.Exception("K");
		}

		[Test]
		[ExpectedException(typeof(LuaException))]
		public void TestCatchErrorInLua()
		{
			string thisMessage = string.Empty;
			try
			{

				lua.Config.LogError = (message) => thisMessage = message;
				using (var f = LuaFunction.NewFunction(L,
					"function(f) csharp.check_error(f()) end"))
				{
					f.Invoke(null, new System.Action(FuncThrowError));
				}

			}
			catch (System.Exception e)
			{
				Assert.True(thisMessage.IndexOf("csharp.check_error") > 0);
				Debug.Log("Catched Error: " + thisMessage);
				Assert.Greater(thisMessage.Length, 0);
				lua.Config.LogError = null;
				throw e;
			}

		}

		void FuncNoError()
		{
			Debug.Log("I don't have problem");
		}

		[Test]
		public void TestCatchErrorInLua_NoError()
		{
			using (var f = LuaFunction.NewFunction(L,
				"function(f) csharp.check_error(f()) end"))
			{
				f.Invoke(null, new System.Action(FuncNoError));
			}
		}

		[Test]
		public void TestReturnBytesFromLua()
		{
			using (var f = LuaFunction.NewFunction(L,
				"function() return csharp.as_bytes(string.pack('BBBB', 1, 2, 3, 4)) end"))
			{
				var ret = (byte[])f.Invoke1();
				Assert.AreEqual(1, ret[0]);
				Assert.AreEqual(2, ret[1]);
				Assert.AreEqual(3, ret[2]);
				Assert.AreEqual(4, ret[3]);
			}

		}


		[Test]
		public void TestHexDumpInBytesObject()
		{
			using (var f = LuaFunction.NewFunction(L,
				"function()\n" +
				" local Debug = csharp.import('UnityEngine.Debug, UnityEngine')\n" +
				" local b = csharp.as_bytes('asldjflaksdjfl;aksdjf;alskfjda;s')\n"+
				" Debug.Log(tostring(b))\n" +
				" return b\n" +
				"end"))
			{
				f.Invoke1();
			}

		}

		[Test]
		public void Test_luaL_teststring()
		{
			Api.lua_pushinteger(L, 1);
			string str;
			bool ret = Api.luaL_teststring_strict(L, -1, out str);
			Assert.False(ret);

			Assert.True(Api.lua_isinteger(L, -1));

			Api.lua_pushstring(L, "test");
			ret = Api.luaL_teststring_strict(L, -1, out str);
			Assert.True(ret);
			Assert.AreEqual("test", str);

			Api.lua_pop(L, 2);
		}

		public int VariadicFunc(int a, params object[] args)
		{
			return a + (args != null ? (int)args.Sum(o => (long)o) : 0);
		}

		[Test]
		public void TestVariadicParams()
		{
			using (var f = LuaFunction.NewFunction(
				L, "function(t)	return t:VariadicFunc(10) end"))
			{
				var ret = (long)f.Invoke1(null, this);
				Assert.AreEqual(10, ret);
			}

			using (var f = LuaFunction.NewFunction(
				L, "function(t)	return t:VariadicFunc(10, 1, 2, 3, 4, 5) end"))
			{
				var ret = (long)f.Invoke1(null, this);
				Assert.AreEqual(25, ret);
			}
		}

		public int VariadicFuncNoParam(params int[] args)
		{
			return 10;
		}

		[Test]
		public void TestVariadicParams_NoParam()
		{
			using (var f = LuaFunction.NewFunction(
				L, "function(t)	return t:VariadicFuncNoParam() end"))
			{
				var ret = (long)f.Invoke1(null, this);
				Assert.AreEqual(10, ret);
			}
		}

		[Test]
		[ExpectedException(typeof(LuaException))]
		public void TestVariadicParams_IncorrectArgCount()
		{
			string thisMessage = string.Empty;
			try
			{
				Config.LogError = (message) => thisMessage = message;
				using (var f = LuaFunction.NewFunction(
					L, "function(t)	return t:VariadicFunc() end"))
				{
					f.Invoke1(null, this);
				}
			}
			catch (System.Exception e)
			{
				Assert.True(thisMessage.IndexOf("no corresponding csharp method") > 0);
				Debug.Log("Catched Error: " + thisMessage);
				Assert.Greater(thisMessage.Length, 0);
				Config.LogError = null;
				throw e;
			}
		}

		[Test]
		[ExpectedException(typeof(LuaException))]
		public void TestVariadicParams_IncorrectRequiredArgType()
		{
			string thisMessage = string.Empty;
			try
			{
				Config.LogError = (message) => thisMessage = message;
				using (var f = LuaFunction.NewFunction(
					L, "function(t)	return t:VariadicFunc('incorrect') end"))
				{
					f.Invoke1(null, this);
				}
			}
			catch (System.Exception e)
			{
				Assert.True(thisMessage.IndexOf("no corresponding csharp method") > 0);
				Debug.Log("Catched Error: " + thisMessage);
				Assert.Greater(thisMessage.Length, 0);
				Config.LogError = null;
				throw e;
			}
		}

		class TestOverloading
		{
			public string Func(string a, params object[] args)
			{
				throw new System.Exception("Should not match this");
			}

			public string Func(string a, string b, params object[] args)
			{
				var sb = new System.Text.StringBuilder();
				sb.Append(a);
				sb.Append(b);
				if (args != null)
				{
					foreach (var arg in args)
					{
						sb.Append((string)arg);
					}
				}
				return sb.ToString();
			}

			public string Func(string a, string b, int c)
			{
				return "Func3";
			}
		}

		[Test]
		public void TestFuncOverloading()
		{
			var t = new TestOverloading();
			using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:Func('a', 'b')) end"))
			{
				f.Invoke(null, t);
			}
		}

		[Test]
		public void TestFuncOverloading1()
		{
			var t = new TestOverloading();
			using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:Func('a', 'b', 10)) end"))
			{
				var k = (string)f.Invoke1(null, t);
				Assert.AreEqual("Func3", k);
			}
		}

		class TestOverloading2
		{
			public int Func(object a, object b, object c)
			{
				throw new System.Exception("not this");
			}
			public int Func(string a, object b, object c)
			{
				throw new System.Exception("not this");
			}
			public int Func(string a, string b, int c)
			{
				return 10;
			}
		}

		[Test]
		public void TestFuncOverloading_ExactlyMatchIsBetterThanConvert()
		{
			var t = new TestOverloading2();
			using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:Func('a', 'b', 10)) end"))
			{
				f.Invoke(null, t);
			}
		}

		class TestOverloading3
		{
			public int Func(string a, object b, object c)
			{
				throw new System.Exception("not this");
			}
			public int Func(string a, object b, int c)
			{
				return 10;
			}
		}

		[Test]
		public void TestFuncOverloading_ExactlyMatchIsBetterThanConvert2()
		{
			var t = new TestOverloading3();
			using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:Func('a', 'b', 10)) end"))
			{
				f.Invoke(null, t);
			}
		}

		class TestOverloading4
		{
			public int Func(string a, string b, object c)
			{
				return 10;
			}
			public int Func(string a, object b, int c)
			{
				throw new System.Exception("not this");
			}
		}

		[Test]
		[ExpectedException(typeof(LuaException))]
		public void TestFuncOverloading_Ambiguous()
		{
			string thisMessage = string.Empty;
			try
			{
				Config.LogError = (message) => thisMessage = message;
				var t = new TestOverloading4();
				//t.Func("a", "b", 10); 
				// C# cannot decide which one is it. 
				// In Lua.cs, these two func has same score on parameters passed below
				// throw exception about this
				using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:Func('a', 'b', 10)) end"))
				{
					f.Invoke(null, t);
				}
			}
			catch (System.Exception e)
			{
				Assert.True(thisMessage.IndexOf("ambiguous") > 0);
				throw e;
			}
		}

		[Test]
		public void TestFuncOverloading_Ambiguous_ExactMatch()
		{
			var t = new TestOverloading4();
			using (var f = LuaFunction.NewFunction(L, "function(d) return d[{csharp.p_exact('string', 'string', 'object'), 'Func'}](d, 'a', 'b', 20) end"))
			{
				Assert.AreEqual(10, f.Invoke1(t));
			}
		}

		class TestOverloading5
		{
			public int Func(int a, int b , int c)  // 30
			{
				return 10;
			}
			public int Func(params int[] args) // 27
			{
				throw new System.Exception("not this");
			}
			public int Func(params object[] args) // 15
			{
				throw new System.Exception("not this");
			}
			public int Func(int a, params object[] args) // 20
			{
				throw new System.Exception("not this");
			}
			public int Func(int a, int b, params object[] args) // 25
			{
				throw new System.Exception("not this");
			}
		}

		[Test]
		public void TestFuncOverloading_NoAmbiguous()
		{
			var t = new TestOverloading5();
			t.Func(10, 10, 10);
			using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:Func(10, 10, 10)) end"))
			{
				f.Invoke(null, t);
			}
		}

		class TestOverloading6
		{
			public int Func(int a, int b , int c)  // scores 30
			{
				return 10;
			}
			public int Func(int a, int b, params int[] args) // scores 29
			{
				throw new System.Exception("not this");
			}
		}
		[Test]
		public void TestFuncOverloading_NoAmbiguous2()
		{
			var t = new TestOverloading5();
			t.Func(10, 10, 10);
			using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:Func(10, 10, 10)) end"))
			{
				f.Invoke(null, t);
			}
		}


		class TestOverloading7
		{
			public int Func(int a, int b , object c)  // scores 25
			{
				throw new System.Exception("not this");
			}
			public int Func(int a, int b, params int[] args) // scores 29
			{
				return 20;
			}
		}
		[Test]
		public void TestFuncOverloading_NoAmbiguous3()
		{
			var t = new TestOverloading7();
			t.Func(10, 10, 10);
			using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:Func(10, 10, 10)) end"))
			{
				f.Invoke(null, t);
			}
		}

		class A { }
		class B { }
		class TestOverloading8
		{
			public void Func(A _1, A _2)
			{
			}
			public void Func(B _1, B _2)
			{
			}
		}
		[Test]
		public void TestFuncOverloading_NoAmbiguous4()
		{
			var t = new TestOverloading8();
			L.Import(typeof(A), "A");
			L.Import(typeof(B), "B");
			using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:Func(A(), A())) end"))
			{
				f.Invoke(null, t);
			}
		}

		struct C { }

		class TestNilParam
		{
			public void Func(A a)
			{
				Assert.AreEqual(null, a);
			}
			public void Func(C a)
			{
				throw new System.Exception("not this");
			}
			public void FuncNonValueType(C a)
			{
			}
		}

		[Test]
		public void TestNilParam_ValueType()
		{
			var t = new TestNilParam();
			using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:Func(nil)) end"))
			{
				f.Invoke(null, t);
			}			
		}

		[Test]
		[ExpectedException(typeof(LuaException))]
		public void TestNilParam_NonValueType()
		{
			string thisMessage = string.Empty;
			try
			{
				Config.LogError = (message) => thisMessage = message;
				var t = new TestNilParam();
				using (var f = LuaFunction.NewFunction(L, "function(t) return csharp.check_error(t:FuncNonValueType(nil)) end"))
				{
					f.Invoke(t);
				}
			}
			catch (System.Exception e)
			{
				Config.LogError = null;
				Assert.True(thisMessage.IndexOf("no corresponding") > 0);
				throw e;
			}
		}

		class Issue33
		{
			public int Func(A _1, A _2)
			{
				return 10;
			}

			public int Func(B _1, B _2)
			{
				return 20;
			}
		}


		[Test]
		public void Test_Issue33_SameSignatureForNonPrimitiveParameter()
		{
			var t = new Issue33();
			L.Import(typeof(A), "A");
			L.Import(typeof(B), "B");
			using (var f = LuaFunction.NewFunction(L, "function(t) return t:Func(A(), A()) end"))
			{
				var ret = f.Invoke1(null, t);
				Assert.AreEqual(10, ret);
			}

			using (var f = LuaFunction.NewFunction(L, "function(t) return t:Func(B(), B()) end"))
			{
				var ret = f.Invoke1(null, t);
				Assert.AreEqual(20, ret);
			}
		}

		class IssueTypeAsParameter
		{
			public object Func(System.Type t)
			{
				return System.Activator.CreateInstance(t);
			}
		}

		[Test]
		public void Test_Issue_TypeShouldBeAbleToBePassedAsParameter()
		{
			var top = Api.lua_gettop(L);
			L.Import(typeof(A), "A");
			using (var f = LuaFunction.NewFunction(L, "function(t) return t:Func(A) end"))
			{
				var r = f.Invoke1(null, new IssueTypeAsParameter());
				Assert.AreEqual(typeof(A), r.GetType());
			}
			Api.lua_settop(L, top);
		}

		[Test]
		public void TestLuaCoroutine()
		{
			var top = Api.lua_gettop(L);
			var t = LuaThread.CreateAndDispose(
				LuaFunction.NewFunction(
					L,
					"function()\n" + 
					"  coroutine.yield(1)\n" + 
					"  coroutine.yield(2)\n" + 
					"  coroutine.yield(3)\n" + 
					"  coroutine.yield(4)\n" + 
					"  return 5\n" +
					"end\n"));
			Assert.AreEqual(top, Api.lua_gettop(L));
			long i = 1;
			while (t.Resume())
			{
				Assert.NotNull(t.current);
				Assert.AreEqual(i, (long)t.current[1]);
				++i;
			}
			Assert.AreEqual(5, i);
			Assert.AreEqual(5, t.current[1]);
			t.Dispose();
			Assert.AreEqual(top, Api.lua_gettop(L));
		}

		[Test]
		public void TestLuaCoroutine_ResumeWithArg()
		{
			var top = Api.lua_gettop(L);
			var t = LuaThread.CreateAndDispose(
				LuaFunction.NewFunction(
					L,
					"function()\n" + 
					"  return coroutine.yield()\n" + 
					"end\n"));
			t.Resume(); // first yield
			Assert.IsNull(t.current);
			t.Resume(4, 5, "hello");
			Assert.NotNull(t.current);
			Assert.AreEqual(4, (long)t.current[1]);
			Assert.AreEqual(5, (long)t.current[2]);
			Assert.AreEqual("hello", (string)t.current[3]);
			t.Dispose();
			Assert.AreEqual(top, Api.lua_gettop(L));
		}

		[Test]
		[ExpectedException(typeof(LuaException))]
		public void TestLuaCoroutine_ResumeDeadCoroutine()
		{
			var top = Api.lua_gettop(L);
			var t = LuaThread.CreateAndDispose(
				LuaFunction.NewFunction(
					L,
					"function()\n" + 
					"  return coroutine.yield()\n" + 
					"end\n"));
			t.Resume(); // first yield
			Assert.IsNull(t.current);
			t.Resume(4, 5, "hello");
			Assert.NotNull(t.current);
			Assert.AreEqual(4, (long)t.current[1]);
			Assert.AreEqual(5, (long)t.current[2]);
			Assert.AreEqual("hello", (string)t.current[3]);

			try
			{
				t.Resume();
			}
			catch (LuaException e)
			{
				Assert.True(e.Message.Contains("dead coroutine"));
				Assert.AreEqual(top, Api.lua_gettop(L));
				t.Dispose();
				throw e;
			}
		}

		LuaThread ResumeCoroutine(LuaThread t)
		{
			t.Resume(10);
			return t.Retain(); // this function is called from lua, so t will be disposed. Retain it before return
		}

		[Test]
		public void TestLuaCoroutine_CoroutineAsParameterFromLua()
		{
			var f = LuaFunction.CreateDelegate(L, new System.Func<LuaThread, LuaThread>(ResumeCoroutine));
			L.SetGlobal("my_resume", f);
			f.Dispose();
			f = LuaFunction.NewFunction(
				L, 
				"function()\n" +
				"  local c = coroutine.create(\n" +
				"    function(k0)\n" + 
				"      local k1 = coroutine.yield('hello')\n" +
				"      return 5 + k0 + k1\n" +
				"    end)\n" + 
				"  return my_resume(c)\n" +
				"end");
			var th = (LuaThread)f.Invoke1();
			th.Resume(20);
			Assert.AreEqual(35, (long)th.current[1]);
		}

		class TestData
		{
		}

		class K
		{
			public string Xoo(bool t)
			{
				return "Xoo_t";
			}

			public string Xoo(object d)
			{
				return "Xoo_d";
			}

			public object GetTestData()
			{
				return new TestData();
			}

			public string Cool(string a)
			{
				return "Cool_a";
			}

			public string Cool(string a, params string[] b)
			{
				return "Cool_ab";
			}

			public string Foo(string p0, params string[] ps)
			{
				return Foo(p0, "_abcd", ps);
			}

			public string Foo(string p0, string p1, params string[] ps)
			{
				var sb = new System.Text.StringBuilder();
				sb.Append(p0);
				sb.Append(p1);
				foreach (var p in ps)
				{
					sb.Append(p);
				}
				return sb.ToString();
			}

			public void Bar(System.Action action)
			{
				action();
			}

			public System.Action action;
			public void DoAction()
			{
				if (action != null)
				{
					action();
				}
			}

			public System.Action[] actions = new System.Action[1];
			public void DoAction2()
			{
				if (actions[0] != null)
				{
					actions[0]();
				}
			}
		}

		[Test]
		public void TestOverloadingVaArg()
		{
			using (var f = LuaFunction.NewFunction(
				L,
				"function(t) return t:Cool('a') end"))
			{
				var inst = new K();
				var retCsharp = inst.Cool("b");
				var ret = (string)f.Invoke1(null, inst);
				Assert.AreEqual("Cool_a", ret);
				Assert.AreEqual(retCsharp, ret);
            }
		}

		[Test]
		public void Bugfix_AmbiguousOverrloadingOnUserDataToBoolean()
		{
			using (var f = LuaFunction.NewFunction(
				L,
				"function(t) return t:Xoo(t:GetTestData()) end"))
			{
				var inst = new K();
				var ret = (string)f.Invoke1(null, inst);
				Assert.AreEqual("Xoo_d", ret);
			}
		}
		[Test]
		public void Bugfix_AmbiguousOverrloadingOnUserDataToBoolean2()
		{
			using (var f = LuaFunction.NewFunction(
				L,
				"function(t) return t:Xoo(false) end"))
			{
				var inst = new K();
				var ret = (string)f.Invoke1(null, inst);
				Assert.AreEqual("Xoo_t", ret);
			}
		}

		[Test]
		public void TestCallingMethodWithDefaultParameter()
		{

			using (var f = LuaFunction.NewFunction(
				L,
				"function(t) return t:Foo('a') end"))
			{
				var inst = new K();
				var ret = (string)f.Invoke1(null, inst);
				Assert.AreEqual("a_abcd", ret);
			}
		}

		[Test]
		public void TestCallingMethodWithDefaultParameter2()
		{

			using (var f = LuaFunction.NewFunction(
				L,
				"function(t) return t:Foo('a', 'b') end"))
			{
				var inst = new K();
				var retCsharp = inst.Foo("a", "b");
				var ret = (string)f.Invoke1(null, inst);
				Assert.AreEqual("ab", ret);
				Assert.AreEqual(retCsharp, ret);
			}
		}


		[Test]
		public void TestCallingMethodWithDefaultParameter_WithVariadicParams()
		{

			using (var f = LuaFunction.NewFunction(
				L,
				"function(t) return t:Foo('a', 'b', 'kkk', 'ggg') end"))
			{
				var inst = new K();
				var ret = (string)f.Invoke1(inst);
				Assert.AreEqual("abkkkggg", ret);
			}
		}

		[Test]
		public void TestConvertLuaFunctionToSystemAction()
		{
			var f = LuaFunction.NewFunction(L, "function() _G['test1234'] = 20 end");
			var action = LuaFunction.ToAction(f);
			action();
			f.Dispose();

			Api.lua_getglobal(L, "test1234");
			var ret = Api.lua_tointeger(L, -1);
			Api.lua_pop(L, 1);

			Assert.AreEqual(20, ret);
		}

		[Test]
		public void TestConvertLuaFunctionToSystemAction2()
		{
			var f = LuaFunction.NewFunction(L, "function(k) k:Bar(function() _G['test1234'] = 80 end) end");
			f.Invoke(new K());
			f.Dispose();

			Api.lua_getglobal(L, "test1234");
			var ret = Api.lua_tointeger(L, -1);
			Api.lua_pop(L, 1);
			Assert.AreEqual(80, ret);
		}

		[Test]
		public void TestConvertLuaFunctionToSystemAction_op_assignment()
		{
			var k = new K();
			var f = LuaFunction.NewFunction(L, "function(k) k.action = function() _G['test1234'] = 'hello' end end");
			f.Invoke(k);
			k.DoAction();

			Api.lua_getglobal(L, "test1234");
			var ret = Api.lua_tostring(L, -1);
			Api.lua_pop(L, 1);
			Assert.AreEqual("hello", ret);
		}

		[Test]
		public void TestConvertLuaFunctionToSystemAction_op_assignment_array()
		{
			var k = new K();
			var f = LuaFunction.NewFunction(L, "function(k) k.actions[0] = function() _G['test1234'] = 'hello2' end end");
			f.Invoke(k);
			k.DoAction2();

			Api.lua_getglobal(L, "test1234");
			var ret = Api.lua_tostring(L, -1);
			Api.lua_pop(L, 1);
			Assert.AreEqual("hello2", ret);
		}

		class Base
		{
			public static int Foo()
			{
				return 10;
			}
			public int Bar()
			{
				return 20;
			}
			public virtual int Bar2()
			{
				return 30;
			}
		}

		class Derived : Base
		{
			public override int Bar2()
			{
				return 40;
			}
		}

		[Test]
		public void Test_Issue_CallStaticFunctionInParentClass()
		{
			L.Import(typeof(Derived), "Derived");
			using (var f = LuaFunction.NewFunction(L, "function() return Derived.Foo() end"))
			{
				Assert.AreEqual(10, f.Invoke1());
			}
		}

		[Test]
		public void Test_Issue_CallFunctionInParentClass()
		{
			var d = new Derived();
			L.Import(typeof(Derived), "Derived");
			using (var f = LuaFunction.NewFunction(L, "function(d) return d:Bar() end"))
			{
				Assert.AreEqual(20, f.Invoke1(d));
			}
		}


		[Test]
		public void Test_Issue_CallFunctionInParentClass2()
		{
			var d = new Derived();
			L.Import(typeof(Derived), "Derived");
			using (var f = LuaFunction.NewFunction(L, "function(d) return d:Bar2() end"))
			{
				Assert.AreEqual(40, f.Invoke1(d));
			}
		}

		public class TestObj
		{
		}

		class TestArrayArg
		{
			public string Foo(string[] arr)
			{
				return arr[0] + arr[1] + arr[2];
			}
			public string Foo(TestObj[] arr)
			{
				return "testobj";
            }
			public string Foo(ulong[] arr)
			{
				return "ulong";
            }
        }

		[Test]
		public void Test_ArrayArgFromLua()
		{
			var d = new TestArrayArg();
			L.Import(typeof(TestArrayArg), "TestArrayArg");
			L.DoString("String = csharp.checked_import('System.String')");
            using (var f = LuaFunction.NewFunction(L, 
				"function(d) return d:Foo(csharp.as_array(String, {'1', '2', '3'})) end"))
			{
				Assert.AreEqual("123", f.Invoke1(d));
			}
		}

		[Test]
		public void Test_ArrayArgFromLua2()
		{
			var d = new TestArrayArg();
			L.Import(typeof(TestArrayArg), "TestArrayArg");
            using (var f = LuaFunction.NewFunction(L, 
				"function(d) return d:Foo(csharp.as_array('string', {'1', '2', '3'})) end"))
			{
				Assert.AreEqual("123", f.Invoke1(d));
			}
		}

		[Test]
		public void Test_ArrayArgFromLua3()
		{
			var d = new TestArrayArg();
			var m = new TestObj();
			L.Import(typeof(TestArrayArg), "TestArrayArg");
            using (var f = LuaFunction.NewFunction(L, 
				"function(d, m) return d:Foo(csharp.as_array('lua.test.TestLua+TestObj,Assembly-CSharp-Editor', {m, m, m})) end"))
			{
				Assert.AreEqual("testobj", f.Invoke1(d, m));
			}
		}

		[Test]
		public void Test_ArrayArgFromLua4()
		{
			var d = new TestArrayArg();
			L.Import(typeof(TestArrayArg), "TestArrayArg");
            using (var f = LuaFunction.NewFunction(L, "function(d) return d:Foo(csharp.as_array('ulong', {1, 1, 1})) end"))
			{
				Assert.AreEqual("ulong", f.Invoke1(d));
			}
		}

		public class TestAction
		{
			public System.Action action;
			public void Foo()
			{
				action();
			}
		}


		[Test]
		public void Test_issue_LuaFunctionToActionDisposedAfterBeingCalled()
		{
			var t = new TestAction();
			using (var f = LuaFunction.NewFunction(L, "function(d) d.action = function() end end"))
			{
				f.Invoke(t);
			}
			t.Foo();
			t.Foo();
			LuaFunction.CollectActionPool();
			t.Foo();
			t.Foo();
			t.action = null;
			System.GC.Collect();
			LuaFunction.CollectActionPool();
		}

		class PrivatePrivillage
		{
			int Foo()
			{
				return 42;
			}
			int Bar = 84;
		}

		[Test]
		public void TestPrivatePrivillage()
		{
			var t = new PrivatePrivillage();
			using (var f = LuaFunction.NewFunction(L, "function(d) return d[{csharp.p_private(), 'Bar'}] end"))
			{
				Assert.AreEqual(84, f.Invoke1(t));
			}
		}

		[Test]
		public void TestPrivatePrivillage2()
		{
			var t = new PrivatePrivillage();
			using (var f = LuaFunction.NewFunction(L, "function(d) return d[{csharp.p_private(), 'Foo'}](d) end"))
			{
				Assert.AreEqual(42, f.Invoke1(t));
			}
		}


		[Test]
		public void TestTypeOf()
		{
			L.Import(typeof(int), "SystemInt");
			using (var f = LuaFunction.NewFunction(L, "function() return csharp.typeof(SystemInt) end"))
			{
				Assert.AreEqual(typeof(int), f.Invoke1());
			}
		}

		class TestR
		{
			int Foo()
			{
				return 42;
			}
		}

		[Test]
		public void TestReflectionInLua()
		{
			L.Import(typeof(TestR), "TestR");
			L.Import(typeof(System.Reflection.BindingFlags), "BindingFlags");
			using (var f = LuaFunction.NewFunction(L,
				"function(d)\n" +
				"  local mi = csharp.typeof(TestR):GetMethod('Foo', csharp.to_enum(BindingFlags, 36))\n" +
				"  return mi:Invoke(d, csharp.make_array('System.Object', 0))\n" + 
				"end"))
			{
				Assert.AreEqual(42, f.Invoke1(new TestR()));
			}
		}

		[Test]
		public void TestTypeOf2()
		{
			L.Import(typeof(TestR), "TestR");
			using (var f = LuaFunction.NewFunction(L,
				"function(d)\n"	+
				"  return d:GetType() == csharp.typeof(TestR) and csharp.typeof(d) == csharp.typeof(TestR)\n" + 
				"end"))
			{
				Assert.AreEqual(true, f.Invoke1(new TestR()));
			}
		}


		class TestA
		{
			public T Foo<T>(double a)
			{
				return (T)System.Convert.ChangeType(a, typeof(T));
			}

			public int Foo(int a)
			{
				return a;
			}

			public int Foo(decimal a)
			{
				return (int)a;
			}
		}


		[Test]
		public void TestCallingGenericMethod()
		{
			using (var f = LuaFunction.NewFunction(L, "function(d) return d[{csharp.p_generic('int'), 'Foo'}](d, 20.0) end"))
			{
				Assert.AreEqual(20, f.Invoke1(new TestA()));
			}
		}



		class TestB
		{
			public int Foo(System.Action<int, string> complete)
			{
				complete(10, "a");
				return 42;
			}

			public int Bar(System.Func<int, int, int> func)
			{
				return func(10, 32);
			}
		}

		[Test]
		public void TestGenericActionPre()
		{
			L.Import(typeof(UnityEngine.Debug), "Debug");
			using (var f = LuaFunction.NewFunction(L,
				"function(d, f)\n"	+
				"  return d:Foo(f)\n" + 
				"end"))
			{
				System.Action<int, string> k = (a, b) => { Debug.Log(a.ToString() + b); };
				Assert.AreEqual(42, f.Invoke1(new TestB(), k));
			}
		}

		[Test]
		public void TestGenericAction()
		{
			L.Import(typeof(UnityEngine.Debug), "Debug");
			using (var f = LuaFunction.NewFunction(L,
				"function(d)\n"	+
				"  return d:Foo(function(a, b) Debug.Log(tostring(a) .. b) end)\n" + 
				"end"))
			{
				Assert.AreEqual(42, f.Invoke1(new TestB()));
			}
		}

		[Test]
		public void TestGenericFunc()
		{
			L.Import(typeof(UnityEngine.Debug), "Debug");
			using (var f = LuaFunction.NewFunction(L,
				"function(d)\n"	+
				"  return d:Bar(function(a, b) return a+b end)\n" + 
				"end"))
			{
				Assert.AreEqual(42, f.Invoke1(new TestB()));
			}
		}


		class TestIntPtr
		{
			public int Foo(System.IntPtr p)
			{
				return 22 + p.ToInt32();
			}
			public System.IntPtr Bar()
			{
				return new System.IntPtr(20);
			}
		}
		[Test]
		public void TestIntPtrAsArgument()
		{
			var p = new TestIntPtr();
			using (var f = LuaFunction.NewFunction(L,
				"function(p) return p:Foo(p:Bar()) end"))
			{
				Assert.AreEqual(42, f.Invoke1(p));
			}
		}
	}
}
