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
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

namespace lua
{
	public class LuaDebugging
	{
		static LuaFunction debuggeePoll;
		[MenuItem("Lua/Start Debugging (debug debuggee) ...")]
		static void StartDebugging_DumpCommunication()
		{
			StartDebuggingWithOption(debugDebuggee:true);
		}

		[MenuItem("Lua/Start Debugging ...")]
		public static void StartDebugging()
		{
			StartDebuggingWithOption(debugDebuggee:false);
		}

		static void StartDebuggingWithOption(bool debugDebuggee = false)
		{
			if (Application.isPlaying)
			{
				var type = Type.GetType("lua.LuaBehaviour, Assembly-CSharp-firstpass");
				Debug.Assert(type != null);
				var field = type.GetField("L", BindingFlags.Static | BindingFlags.NonPublic);
				var L = (Lua)field.GetValue(null);
				Debug.Assert(L != null);
				var top = Api.lua_gettop(L);

				try
				{
					Api.luaL_dostring(L,
						"return function()" +
						"  local Debug = csharp.import('UnityEngine.Debug, UnityEngine')\n"	+
						"  local json =	require 'json'\n" +
						"  local debuggee =	require	'vscode-debuggee'\n" +
						"  local startResult, startType = debuggee.start(json, { dumpCommunication = " + (debugDebuggee ? "true" : "false" ) + "})\n" +
						"  Debug.Log('start debuggee ' .. startType .. ' ' .. tostring(startResult))\n" +
						"  return debuggee.poll\n" + 
						"end");
					L.Call(0, 2);
					if (debuggeePoll != null)
					{
						LuaBehaviour.debuggeePoll = null;
						debuggeePoll.Dispose();
						debuggeePoll = null;
					}
					debuggeePoll = LuaFunction.MakeRefTo(L, -2);
					LuaBehaviour.debuggeePoll = delegate ()
					{
						try
						{
							debuggeePoll.Invoke();
						}
						catch (Exception e)
						{
							Debug.LogError("debug session is ended unexpected: " + e.Message);
							LuaBehaviour.debuggeePoll = null;
							debuggeePoll.Dispose();
							debuggeePoll = null;
						}
					};
				}
				catch (Exception e)
				{
					Debug.LogErrorFormat("Cannot start debuggee {0}", e.Message);
				}
				Api.lua_settop(L, top);
			}
		}
	}
}
#endif
