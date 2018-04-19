using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class GameTools : EditorWindow {

	const string kLastGameDataPath = "_unity3d_lua_gametools_lastgamedatapath";

	[MenuItem("Unity3D.Lua/Game Tools")]
	static void ShowGameTools()
	{
		var gameTools = EditorWindow.GetWindow<GameTools>();
		gameTools.ShowUtility();
	}

	class Config : ScriptableObject
	{
		public DefaultAsset gameDataAsset;	
	}

	Config config;
	
	void OnEnable()
	{
		if (config == null)
		{
			config = (Config)AssetDatabase.LoadAssetAtPath("Assets/Unity3D.Lua.GameTools.asset", typeof(Config));
			if (config == null)
			{
				config = ScriptableObject.CreateInstance<Config>();
				AssetDatabase.CreateAsset(config, "Assets/Unity3D.Lua.GameTools.asset");
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}
	}

	
	string CheckCreatePath(string path)
	{
		var s = path.Split('/', '\\');
		var pa = string.Empty;
		foreach (var p in s)
		{
			pa = Path.Combine(pa, p);
			if (!Directory.Exists(pa))
			{
				Directory.CreateDirectory(pa);
			}
		}
		return path;	
	}
	string CheckPath(string subPath)
	{
		return CheckCreatePath(Path.Combine("Assets/_Generated/Resources", subPath));
	}

	void RefreshAssetDatabase()
	{
		CustomEditorApp.AddTask(() => AssetDatabase.Refresh());
	}

	void OnGUI()
	{
		GUILayout.BeginHorizontal();
		config.gameDataAsset = (DefaultAsset)EditorGUILayout.ObjectField(config.gameDataAsset, typeof(Object), allowSceneObjects:false);
		if (GUILayout.Button("Export GameData ...", GUILayout.ExpandWidth(false)))
		{
			if (config.gameDataAsset != null)
			{
				var path = AssetDatabase.GetAssetPath(config.gameDataAsset.GetInstanceID());
				var scripts = AssetDatabase.FindAssets("export_gamedata");
				if (scripts.Length > 0)
				{
					var scriptName = AssetDatabase.GUIDToAssetPath(scripts[0]);
					var outputPath = CheckPath(string.Empty);
					Cmd.Execute("python " + scriptName + " -i " + path + " -o " + Path.Combine(outputPath, "GameData.json"), () => RefreshAssetDatabase());
				}
				
			}
		}
		GUILayout.EndHorizontal();
	}

}
