using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullScreenDebugable : Debugable
{
	static Debugable instance_;
	public static Debugable instance
	{
		get
		{
			if (instance_ == null)
			{
				instance_ = GameObject.FindObjectOfType<FullScreenDebugable>();
			}
			if (instance_ == null)
			{
				var go = new GameObject("_FullScreenDebugable");
				DontDestroyOnLoad(go);
				instance_ = go.AddComponent<FullScreenDebugable>();
			}
			return instance_;
		}
	}

	void Awake()
	{
		Editor_SetArea(2 * Screen.width / 3, 0, Screen.width / 3, Screen.height);
		show = true;
		var f = lua.LuaFunction.CreateDelegate(_Init.luaVm, new System.Action(Editor_TogglePopUp));
		Editor_AddToolbarButton("PopUp", f);
		f.Dispose();
	}

}

