using UnityEngine;
using System.Collections;
using System.IO;

public class _Init : MonoBehaviour {

	const string kTagInternalInit = "_InternalInit";

	class _InternalInit : MonoBehaviour
	{
		public lua.LuaStateBehaviour luaState;
		void Awake()
		{
			utils.TaskManager.Init();

			lua.Config.Log = (msg) => UnityEngine.Debug.Log("[lua]" + msg);
			lua.Config.LogWarning = (msg) => UnityEngine.Debug.LogWarning("[lua]" + msg);
			lua.Config.LogError = (msg) => UnityEngine.Debug.LogError("[lua]" + msg);

			luaState = lua.LuaStateBehaviour.Create();
			lua.Api.luaL_requiref(luaState.luaVm, "pb", lua.CModules.luaopen_pb, 0);
			utils.Debugable.SetLua(luaState.luaVm);
			utils.WebRequest2_Lua.SetLua(luaState.luaVm);

			lua.Lua.scriptLoader = LuaScriptLoader.ScriptLoader;
			lua.Lua.typeLoader = LuaScriptLoader.TypeLoader;

#if UNITY_EDITOR
			lua.Lua.editorGetPathDelegate = LuaScriptLoader.Editor_GetPath;
			luaState.luaVm.Editor_UpdatePath();
#endif
		}

	}

	static _InternalInit _internalInit;
	public static lua.Lua luaVm
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
