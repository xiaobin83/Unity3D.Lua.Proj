using UnityEngine;
using System.Collections;

namespace lua
{
	public class LuaStateBehaviour : MonoBehaviour
	{
		public static LuaStateBehaviour Create()
		{
			var go = new GameObject("_LuaStateObject");
			var be = go.AddComponent<LuaStateBehaviour>();
			DontDestroyOnLoad(go);
			return be;
		}

		void Awake()
		{
			Load();
		}

		public void Unload()
		{
			Debug.Assert(luaVm != null);

			if (Api.LUA_TFUNCTION == Api.lua_getglobal(luaVm, "_atexit"))
			{
				try
				{
					luaVm.Call(0, 0);
				}
				catch (System.Exception e)
				{
					Debug.LogError(e);
				}
			}
			else
			{
				Api.lua_pop(luaVm, 1);
			}
			luaVm.Dispose();
			luaVm = null;
		}

		public void Load()
		{
			Debug.Assert(luaVm == null);
			luaVm = new Lua();
			LuaBehaviour.SetLua(luaVm);
		}

		public Lua luaVm { get; private set; }
		void OnDestroy()
		{
			if (luaVm != null)
			{
				Unload();
			}
		}
	}
}