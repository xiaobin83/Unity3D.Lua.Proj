using UnityEngine;
using System.Collections;
using System.IO;
using x600d1dea.lua;
using x600d1dea.stubs.utils;

public class _Init : MonoBehaviour {

	const string kTagInternalInit = "_InternalInit";

	class _InternalInit : MonoBehaviour
	{
		public LuaStateBehaviour luaState;
		void Awake()
		{
			TaskManager.Init();

			Config.Log = (msg) => UnityEngine.Debug.Log("[lua]" + msg);
			Config.LogWarning = (msg) => UnityEngine.Debug.LogWarning("[lua]" + msg);
			Config.LogError = (msg) => UnityEngine.Debug.LogError("[lua]" + msg);

			luaState = LuaStateBehaviour.Create();

			Lua.scriptLoader = LuaScriptLoader.ScriptLoader;
			Lua.typeLoader = LuaScriptLoader.TypeLoader;

#if UNITY_EDITOR
			Lua.editorGetPathDelegate = LuaScriptLoader.Editor_GetPath;
			luaState.luaVm.Editor_UpdatePath();
#endif
		}

	}

	static _InternalInit _internalInit;
	public static Lua luaVm
	{
		get
		{
			return _internalInit.luaState.luaVm;
		}
	}

	void Awake()
	{
		var go = GameObject.FindGameObjectWithTag(kTagInternalInit);
		if (go == null)
		{
			go = new GameObject(kTagInternalInit);
			go.tag = kTagInternalInit;
			GameObject.DontDestroyOnLoad(go);
			_internalInit = go.AddComponent<_InternalInit>();
		}
	}

}
