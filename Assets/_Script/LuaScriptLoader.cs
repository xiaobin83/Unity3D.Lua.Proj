using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;


public class LuaScriptLoader {


#if	UNITY_EDITOR
	static Dictionary<string, List<string>> allLuaScripts;
	static void RefreshAllLuaScripts()
	{
		var pathScripts = new System.Uri(Path.Combine(Application.dataPath, "_ScriptLua/"));
		var pathModules = new System.Uri(Path.Combine(Application.dataPath, "Unity3D.Lua/Modules/"));

		var scripts = 
			Directory.GetFiles(pathScripts.AbsolutePath, "*.lua", SearchOption.AllDirectories)
			.Select(p => new KeyValuePair<string, string>(pathScripts.MakeRelativeUri(new System.Uri(p)).ToString(), p))
			.Union(
				Directory.GetFiles(pathModules.AbsolutePath, "*.lua", SearchOption.AllDirectories)
				.Select(p => new KeyValuePair<string, string>(pathModules.MakeRelativeUri(new System.Uri(p)).ToString(), p)));

		allLuaScripts = new Dictionary<string, List<string>>();
		foreach (var kv in scripts)
		{
			var basename = kv.Key;
			basename = basename.ToLower();
			basename = basename.Replace(".lua", "");
			basename = basename.Replace("/", ".");
			List<string> flist = null;
			if (!allLuaScripts.TryGetValue(basename, out flist))
			{
				flist = new List<string>();
				allLuaScripts[basename] = flist;
			}
			flist.Add(kv.Value);
		}
	}
	static bool watchingLuaSources = false;

	static string GetScriptPath(string scriptName)
	{
		if (allLuaScripts == null || !watchingLuaSources)
		{
			RefreshAllLuaScripts();
			// a little	bit	hacky
			var type = System.Type.GetType("lua.WatchingLuaSources, Assembly-CSharp-Editor-firstpass");
			if (type != null)
			{
				var evt = type.GetEvent("onSourceChanged", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
				if (evt != null)
				{
					watchingLuaSources = true;
					evt.AddEventHandler(null, new System.Action(RefreshAllLuaScripts));
				}
				else
				{
					Debug.LogWarning("Not watching lua sources, will performance issue when in Editor.");
				}
			}
		}

		var name = scriptName.ToLower();
		List<string> flist;
		if (allLuaScripts.TryGetValue(name, out flist))
		{
			if (flist.Count > 1)
			{
				var sb = new System.Text.StringBuilder();
				foreach (var f in flist)
				{
					sb.AppendLine(f);
				}
				Debug.LogWarningFormat("found duplicated script:\n{0}", sb.ToString());
			}
			return flist[0];
		}
		return string.Empty;
	}
#endif


	static byte[] LoadLuaScriptInEditor(string scriptName, out string scriptPath)
	{
#if UNITY_EDITOR
		scriptPath = GetScriptPath(scriptName);
		if (System.IO.File.Exists(scriptPath))
		{
			return System.IO.File.ReadAllBytes(scriptPath);
		}
#endif
		scriptPath = string.Empty;
		return null;
	}

	static byte[] LoadLuaScriptRunTime(string scriptName, out string scriptPath)
	{
		if (System.IntPtr.Size > 4)
		{
			scriptName = scriptName + "_64";
		}
		else
		{
			scriptName = scriptName + "_32";
		}
		scriptName = scriptName.ToLower();

		var s = ResMgr.LoadBytes("_LuaRoot/" + scriptName);
		if (s != null)
		{
			scriptPath = scriptName;
			return s;
		}
		scriptPath = string.Empty;
		return null;
	}

	[lua.LuaScriptLoader]
	public static byte[] ScriptLoader(string scriptName, out string scriptPath)
	{
		if (Application.isEditor)
		{
			// debugging in	editor
			return LoadLuaScriptInEditor(scriptName, out scriptPath);
		}
		else
		{
			return LoadLuaScriptRunTime(scriptName, out scriptPath);
		}
	}

	[lua.LuaTypeLoader]
	public static Type TypeLoader(string typename)
	{
		Type type = Type.GetType(typename);
		if (type == null && typename.IndexOf(',') == -1)
		{
			// try other dlls
			type = type != null ? type : Type.GetType(string.Format("{0}, Assembly-CSharp", typename));
			type = type != null ? type : Type.GetType(string.Format("{0}, Assembly-CSharp-firstpass", typename));
			type = type != null ? type : Type.GetType(string.Format("{0}, UnityEngine", typename));
			type = type != null ? type : Type.GetType(string.Format("{0}, UnityEngine.UI", typename));
		}
		return type;
	}

	[lua.LuaEditorGetPathDelegate]
	public static string[] Editor_GetPath()
	{
		return new string[] {
			"Unity3D.Lua",
		};
	}

}
