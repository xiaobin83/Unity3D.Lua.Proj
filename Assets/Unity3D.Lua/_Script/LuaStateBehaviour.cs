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