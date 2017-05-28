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
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace lua
{

	/*
	 * 
	 *  -- A LuaBehaviour script, MyLuaBehaviour.lua
	 *  
	 *  local MyLuaBehaviour = {
	 * 		staticValue = 10 -- a `static' variable
	 *  }
	 *
	 *  -- _Init function for new behaviour instance
	 *  function MyLuaBehaviour._Init(instance) -- notice, it use dot `.' to define _Init function (`static' function)
	 * 		-- values put in instance table used as self below
	 * 		instance.value0 = 32
	 * 		instance.value1 = 'abc'
	 * 
	 * 		local Vector3 = csharp.import('UnityEngine.Vector3, UnityEngine') -- import a type from C#
	 * 		instance.value2 = Vector3(1, 2, 3)
	 *  end
	 * 
	 *  -- When a new GameObject which has LuaBehaviour component with MyLuaBehaviour.lua attached Awake
	 *  -- from deserialized data, it will create an empty table as instance of this Lua component (aka behaviour table).
	 *  -- The instance (behaviour table) is passed to _Init function for initialization. 
	 *  -- All values set to instance *in _Init function* can be serialized with GameObject. In fact, the function itself 
	 *  -- is serialized.
	 *  -- And those values will show in Inspector. Any tweaking on those values also can be serialized as new _Init
	 *  -- function.
	 *  -- When Awake from deserialized data, _Init function is loaded and 'hides' the original _Init function. 
	 *	-- The your version of _Init function is executed to restore values serialized.
	 *	-- And behaviour table all so can be used to save local value at run-time.
	 * 
	 * 
	 *  -- called at the end of host LuaBehaviour.Awake. 
	 *  function MyLuaBehaviour:Awake()
	 * 		-- awake
	 * 		self.some_value = self:FunctionFromCsharp() -- calling function defined in C# side and store return value to behaviour table.
	 * 		MyLuaBehaviour.staticValue = 20
	 *  end
	 * 
	 * 	-- called at LuaBehaviour.Update
	 *  function MyLuaBehaviour:Update()
	 * 		-- update
	 *  end
	 * 
	 *  -- You can also have other messages which defined in enum LuaBehaviour.Message.
	 *  -- For performance reason, those *Update messages are combined in different components, LuaInstanceBehaviour*
	 * 
	 * 	return MyLuaBehaviour -- Important! Return the `class' to host, and becoming the `meta-class' of instance.
	 */

	public class LuaBehaviour : MonoBehaviour
	{
		static Lua L;
		public static void SetLua(Lua luaVm)
		{
			if (luaVm == L) return;
			if (L != null)
			{
				Debug.LogWarning("Lua state chagned, LuaBehaviour will run in new state.");
			}
			L = luaVm;
		}

		public static void UnloadAll()
		{
			var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
			foreach (var obj in rootObjects)
			{
				obj.BroadcastMessage("UnloadLuaScript", SendMessageOptions.DontRequireReceiver);
			}
			L = null;
		}

		public static void ReloadAll()
		{
			var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
			foreach (var obj in rootObjects)
			{
				obj.BroadcastMessage("ReloadLuaScript", SendMessageOptions.DontRequireReceiver);
			}
		}

		public string scriptName;
#if UNITY_EDITOR
		[NonSerialized]
		public string scriptPath;
#endif

		[SerializeField]
		[HideInInspector]
		string[] keys;
		[SerializeField]
		[HideInInspector]
		GameObject[] gameObjects;


		[SerializeField]
		[HideInInspector]
		string[] eventKeys;

		[Serializable]
		public class Event : UnityEngine.Events.UnityEvent<string, object>
		{
		}
		[SerializeField]
		[HideInInspector]
		Event[] events;



		List<LuaInstanceBehaviour0> instanceBehaviours = new List<LuaInstanceBehaviour0>();

		public enum Message
		{
			Awake = 0,
			Start,
			OnDestroy,

			OnEnable,
			OnDisable,

			Update,
			FixedUpdate,
			LateUpdate,

			OnCollisionEnter,
			OnCollisionExit,
			OnCollisionStay,

			OnCollisionEnter2D,
			OnCollisionExit2D,
			OnCollisionStay2D,

			OnTriggerEnter,
			OnTriggerExit,
			OnTriggerStay,

			OnTriggerEnter2D,
			OnTriggerExit2D,
			OnTriggerStay2D,

			Event_PointerClick,
			Event_PointerDown,
			Event_PointerUp,
			Event_PointerEnter,
			Event_PointerExit,

			_Count
		}

		int[] messageRef_ = null;
		int[] messageRef
		{
			get
			{
				if (messageRef_ == null)
				{
					messageRef_ = new int[(int)Message._Count];
					for (int i = 0; i < messageRef_.Length; ++i)
					{
						messageRef_[i] = Api.LUA_NOREF;
					}
				}
				return messageRef_;
			}
		}

		int messageFlag = 0;
		static int MakeFlag(Message m)
		{
			return 1 << (int)m;
		}

		int handleToThis = Api.LUA_NOREF;
		bool scriptLoaded_ = false;
		bool scriptLoaded
		{
			get
			{
				return L != null && scriptLoaded_;
			}
		}
		int luaBehaviourRef = Api.LUA_NOREF;

		public void LoadScript(string scriptName)
		{
			if (!scriptLoaded)
			{
				this.scriptName = scriptName;
				Awake();
			}
			else
			{
				Debug.LogWarning("script already loaded.");
			}
		}

		public LuaTable GetBehaviourTable()
		{
			if (luaBehaviourRef != Api.LUA_NOREF)
			{
				Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);
				var t = LuaTable.MakeRefTo(L, -1);
				Api.lua_pop(L, 1);
				return t;
			}
			return null;
		}

		void Awake()
		{
			if (L == null || !L.valid)
			{
				Debug.LogError("Call LuaBehaviour.SetLua first.");
				return;
			}

			if (string.IsNullOrEmpty(scriptName))
			{
				Debug.LogWarning("LuaBehaviour with empty scriptName.");
				return;
			}

			// make	instance
			handleToThis = L.MakeRefTo(this);

			Api.lua_newtable(L); 
			// stack: instance table
			luaBehaviourRef = L.MakeRefAt(-1);

			// meta
			Api.lua_createtable(L, 1, 1);
			// stack: instance table, meta

			L.PushRef(handleToThis);
			Api.lua_rawseti(L, -2, 1); // meta[1] = behaviour (this)

			// TODO: can be	opt, each Script take its own meta for LuaBehaviour
			// load	class
			Api.luaL_requiref(L, scriptName, Lua.LoadScript1, 0);
			// stack: instance table, meta, script table

			if (Api.lua_istable(L, -1)) // set metatable and bind messages
			{
				// stack: behaviour table, meta, script table
				L.DoString("return function(be, script)\n" +
							"  return function(t, key)\n" +
							"    local val = script[key]\n"	+
							"    if not val then val = be[key] end\n" +
							"    return val\n" +
							"  end\n" +
							"end", 1, "LuaBehaviour_GetMetaIndexFunction");
				L.PushRef(handleToThis);
				Api.lua_pushvalue(L, -3);
				L.Call(2, 1);
				// stack: instance table, meta, script table, index function
				Api.lua_setfield(L, -3, "__index");
				// stack: instance table, meta, script table
				Api.lua_insert(L, -3);
				// stack: script table, instance table, meta
				Api.lua_setmetatable(L, -2);
				// stack: script table, instance table

				// check message
				for (Message i = Message.Awake; i < Message._Count; ++i)
				{
					Api.lua_pushstring(L, i.ToString());
					// stack: script table, instance table, key
					Api.lua_rawget(L, -3);
					// stack: script table, instance table, function?
					if (Api.lua_isfunction(L, -1))
					{
						messageFlag = messageFlag | MakeFlag(i);
						messageRef[(int)i] = Api.luaL_ref(L, -2); // func pops, and make ref in behaviour table
					}
					else
					{
						Api.lua_pop(L, 1); // pop field
					}
					// stack: script table,	instance table
				}

				// choose script
				int flag = messageFlag & (MakeFlag(Message.Update) | MakeFlag(Message.FixedUpdate) | MakeFlag(Message.LateUpdate));
				var componentType = Type.GetType("lua.LuaInstanceBehaviour" + flag.ToString());
				instanceBehaviours.Add(gameObject.AddComponent(componentType) as LuaInstanceBehaviour0);

				flag = messageFlag & (MakeFlag(Message.OnCollisionEnter) | MakeFlag(Message.OnCollisionExit));
				if (flag != 0)
				{
					instanceBehaviours.Add(gameObject.AddComponent<LuaCollisionBehaviour>());
				}
				flag = messageFlag & MakeFlag(Message.OnCollisionStay);
				if (flag != 0)
				{
					instanceBehaviours.Add(gameObject.AddComponent<LuaCollisionStayBehaviour>());
				}

				flag = messageFlag & (MakeFlag(Message.OnCollisionEnter2D) | MakeFlag(Message.OnCollisionExit2D));
				if (flag != 0)
				{
					instanceBehaviours.Add(gameObject.AddComponent<LuaCollisionBehaviour2D>());
				}
				flag = messageFlag & MakeFlag(Message.OnCollisionStay2D);
				if (flag !=	0)
				{
					instanceBehaviours.Add(gameObject.AddComponent<LuaCollisionStayBehaviour2D>());
				}

				flag = messageFlag & (MakeFlag(Message.OnTriggerEnter) | MakeFlag(Message.OnTriggerEnter));
				if (flag != 0)
				{
					instanceBehaviours.Add(gameObject.AddComponent<LuaTriggerBehaviour>());
				}
				flag = messageFlag & MakeFlag(Message.OnTriggerStay);
				if (flag != 0)
				{
					instanceBehaviours.Add(gameObject.AddComponent<LuaTriggerStayBehaviour>());
				}


				flag = messageFlag & (MakeFlag(Message.OnTriggerEnter2D) | MakeFlag(Message.OnTriggerEnter2D));
				if (flag != 0)
				{
					instanceBehaviours.Add(gameObject.AddComponent<LuaTriggerBehaviour2D>());
				}
				flag = messageFlag & MakeFlag(Message.OnTriggerStay2D);
				if (flag != 0)
				{
					instanceBehaviours.Add(gameObject.AddComponent<LuaTriggerStayBehaviour2D>());
				}

				flag = messageFlag 
					& (MakeFlag(Message.Event_PointerClick)
					| MakeFlag(Message.Event_PointerUp)
					| MakeFlag(Message.Event_PointerDown)
					| MakeFlag(Message.Event_PointerEnter)
					| MakeFlag(Message.Event_PointerExit));
				if (flag != 0)
				{
					instanceBehaviours.Add(gameObject.AddComponent<LuaPointerEventHander>());
				}

				Api.lua_pop(L, 2); // pop instance table, script table

				scriptLoaded_ = true;
			}
			else
			{
				Api.lua_pop(L, 3); // pop instance table, meta, script table
			}

			// stack: (*empty)

			if (scriptLoaded_)
			{
				// load	_Init from serialized version
				LoadInitFuncToInstanceTable(L);
				RunInitFuncOnInstanceTable(L);

				// Awake Lua script
				for (int i = 0; i < instanceBehaviours.Count; ++i)
				{
					instanceBehaviours[i].SetLuaBehaviour(this);
				}
			}
			else
			{
				Debug.LogWarningFormat("No Lua script running with {0}.", gameObject.name);
			}
		}

		void RunInitFuncOnInstanceTable(Lua L)
		{
			Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);
			// Behaviour._Init hides Script._Init
			Api.lua_getfield(L, -1, "_Init");
			if (Api.lua_isfunction(L, -1))
			{
				Api.lua_pushvalue(L, -2);
				try
				{
					L.Call(1, 0);
				}
				catch (Exception e)
				{
					Debug.LogErrorFormat("{0}._Init failed: {1}.", scriptName, e.Message);
				}
			}
			else
			{
				Api.lua_pop(L, 1); // pop non-function
			}
			Api.lua_pop(L, 1); // pop behaviour table
		}

		void Start()
		{
			SendLuaMessage(Message.Start);
		}

		void OnEnable()
		{
			for (int i = 0; i < instanceBehaviours.Count; ++i)
			{
				instanceBehaviours[i].enabled = true;
			}
			SendLuaMessage(Message.OnEnable);
		}

		void OnDisable()
		{
			SendLuaMessage(Message.OnDisable);
			for (int i = 0; i < instanceBehaviours.Count; ++i)
			{
				instanceBehaviours[i].enabled = false;
			}
		}

		void OnDestroy()
		{
			SendLuaMessage(Message.OnDestroy);
			if (L != null && L.valid)
			{
				for (int i = 0; i < messageRef.Length; ++i)
				{
					messageRef[i] = Api.LUA_NOREF; // since referenced in luaBehaviourRef, here is no need to unref
				}
				messageFlag = 0;

				Api.luaL_unref(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);

				if (handleToThis != Api.LUA_NOREF)
					L.Unref(handleToThis);
				luaBehaviourRef = Api.LUA_NOREF;
				handleToThis = Api.LUA_NOREF;
				scriptLoaded_ = false;
			}
		}

		public object InvokeLuaMethod(string method, object[] args)
		{
			if (!scriptLoaded) return null;

			Debug.Log("InvokeLuaMethod " + method);

			var top = Api.lua_gettop(L);
			try
			{
				Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);
				if (Api.lua_getfield(L, -1, method) == Api.LUA_TFUNCTION)
				{
					Api.lua_pushvalue(L, -2);
					int argsLength = 0;
					if (args != null && args.Length > 0)
					{
						foreach (var arg in args)
						{
							L.PushValue(arg);
						}
						argsLength = args.Length;
					}
					L.Call(1 + argsLength, 1);
					var ret = L.ValueAt(-1);
					Api.lua_settop(L, top);
					return ret;
				}
				Api.lua_settop(L, top);
			}
			catch (Exception e)
			{
				Api.lua_settop(L, top);
				Debug.LogErrorFormat("Invoke {0}.{1} failed: {2}", scriptName, method, e.Message);
			}
			return null;
		}

		public void SendLuaMessage2(string message)
		{
			if (!scriptLoaded || string.IsNullOrEmpty(message)) return;
			var funcAndParams = message.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (funcAndParams.Length >= 1)
			{
				Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);
				Api.lua_getfield(L, -1, message);
				Api.lua_pushvalue(L, -2);
				for (int i = 1; i < funcAndParams.Length; ++i)
				{
					Api.lua_pushstring(L, funcAndParams[i]);
				}
				try
				{
					L.Call(1 + funcAndParams.Length - 1, 0);
				}
				catch (Exception e)
				{
					Debug.LogErrorFormat("Invoke {0}.{1} failed: {2}", scriptName, message, e.Message);
				}
				Api.lua_pop(L, 1);
			}
		}
		
		public void SendLuaMessage(string message, object obj)
		{
			if (!scriptLoaded) return;

			Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);
			Api.lua_getfield(L, -1, message);
			Api.lua_pushvalue(L, -2);
			L.PushObject(obj);
			try
			{
				L.Call(2, 0);
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("Invoke {0}.{1} failed: {2}", scriptName, message, e.Message);
			}
			Api.lua_pop(L, 1); // pop behaviour table
		}

		public void SendLuaMessage(string message, string parameter)
		{
			if (!scriptLoaded) return;

			Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);
			Api.lua_getfield(L, -1, message);
			Api.lua_pushvalue(L, -2);
			Api.lua_pushstring(L, parameter);
			try
			{
				L.Call(2, 0);
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("Invoke {0}.{1} failed: {2}", scriptName, message, e.Message);
			}
			Api.lua_pop(L, 1); // pop behaviour table
		}

		public void SendLuaMessage(string message)
		{
			if (!scriptLoaded) return;

			Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);
			if (Api.lua_getfield(L, -1, message) == Api.LUA_TFUNCTION)
			{
				Api.lua_pushvalue(L, -2);
				try
				{
					L.Call(1, 0);
				}
				catch (Exception e)
				{
					Debug.LogErrorFormat("Invoke {0}.{1} failed: {2}", scriptName, message, e.Message);
				}
			}
			Api.lua_pop(L, 1); // pop behaviour table
		}

		IEnumerator LuaCoroutine(LuaThread thread)
		{
			while (thread.Resume())
			{
				if (thread.hasYields)
				{
					yield return thread.current[1];
				}
				yield return null;
			}
			thread.Dispose();
		}

		public void StartLuaCoroutine(LuaThread thread)
		{
			StartCoroutine(LuaCoroutine(thread.Retain()));
		}


		public void SendLuaMessage(Message message)
		{
			if (L == null || !L.valid) return;

			if (!scriptLoaded) return;

			if ((messageFlag & MakeFlag(message)) == 0) return; // no message defined

			Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);
			// get message func	from instance table
			if (Api.lua_rawgeti(L, -1, messageRef[(int)message]) == Api.LUA_TFUNCTION)
			{
				Api.lua_pushvalue(L, -2);
				// stack: func, instance table
				try
				{
					L.Call(1, 0);
				}
				catch (Exception e)
				{
					Debug.LogErrorFormat("Invoke {0}.{1} failed: {2}", scriptName, message, e.Message);
				}
			}
			Api.lua_pop(L, 1); // pop behaviour table
		}

		public void SendLuaMessage(Message message, params object[] objs)
		{
			if (L == null || !L.valid) return;

			if (!scriptLoaded) return;

			if ((messageFlag & MakeFlag(message)) == 0) return; // no message defined

			Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);
			// get message func	from instance table
			if (Api.lua_rawgeti(L, -1, messageRef[(int)message]) == Api.LUA_TFUNCTION)
			{
				Api.lua_pushvalue(L, -2);
				// stack: func, instance table
				var numParams = 0;
				if (objs != null)
				{
					for (int i = 0; i < objs.Length; ++i)
					{
						L.PushValue(objs[i]);
						++numParams;
					}
				}
				try
				{
					L.Call(numParams + 1, 0);
				}
				catch (Exception e)
				{
					Debug.LogErrorFormat("Invoke {0}.{1} failed: {2}", scriptName, message, e.Message);
				}
			}
			Api.lua_pop(L, 1); // pop behaviour table
		}

		[SerializeField]
		[HideInInspector]
		byte[] _InitChunk;
		bool LoadInitFuncToInstanceTable(Lua L)
		{
			if (_InitChunk == null || _InitChunk.Length == 0) return false;
			try
			{
				Api.lua_rawgeti(L, Api.LUA_REGISTRYINDEX, luaBehaviourRef);
				L.LoadChunk(_InitChunk, scriptName + "_Init");
				L.Call(0, 1); // run loaded chunk
				Api.lua_setfield(L, -2, "_Init");
				Api.lua_pop(L, 1);  // pop behaviour table
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
			return false;
		}

		// non-throw
		public GameObject FindGameObject(string key)
		{
			var index = System.Array.FindIndex(keys, (k) => k == key);
			if (index != -1)
				return GetGameObjectAtIndex(index);
			return null;
		}

		// non-throw
		public GameObject GetGameObjectAtIndex(int index)
		{
			if (index >= 0 && index < gameObjects.Length)
			{
				return gameObjects[index];
			}
			return null;
		}

		public Event FindEvent(string key)
		{
			var index = System.Array.FindIndex(eventKeys, (k) => k == key);
			if (index != -1)
				return GetEventAtIndex(index);
			return null;
		}

		public Event GetEventAtIndex(int index)
		{
			if (index >= 0 && index < events.Length)
			{
				return events[index];
			}
			return null;
		}


		// https://docs.unity3d.com/Manual/ExecutionOrder.html
		void UnloadLuaScript()
		{
			L.DoString("package.loaded['" + scriptName + "'] = nil");
			StopAllCoroutines();
			OnDisable();
			OnDestroy();
			for (int i = 0; i < instanceBehaviours.Count; ++i)
			{
				Destroy(instanceBehaviours[i]);
			}
			instanceBehaviours.Clear();
		}

		void ReloadLuaScript()
		{
			Awake();
			OnEnable();
			Start();
		}

#if UNITY_EDITOR
		public static System.Action debuggeePoll;
		static int debuggeeUpdatedFrameCount = 0;

		void LateUpdate()
		{
			if (debuggeeUpdatedFrameCount != Time.frameCount)
			{
				if (debuggeePoll != null)
					debuggeePoll();
				debuggeeUpdatedFrameCount = Time.frameCount;
			}
		}

		public bool IsInitFuncDumped()
		{
			return !string.IsNullOrEmpty(scriptName) && _InitChunk != null && _InitChunk.Length > 0;
		}

		public byte[] GetInitChunk()
		{
			return _InitChunk;
		}

		public void SetInitChunk(byte[] chunk)
		{
			_InitChunk = chunk;
			if (Application.isPlaying)
			{
				if (scriptLoaded)
				{
					LoadInitFuncToInstanceTable(L);
					RunInitFuncOnInstanceTable(L);
				}
			}
		}

		public void Reload()
		{
			if (Application.isPlaying)
			{
				UnloadLuaScript();
				ReloadLuaScript();
			}
		}
#endif
	}
}