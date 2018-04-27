using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using stubs;

public class GameTools : EditorWindow {

	[MenuItem("Unity3D.Lua/Game Tools")]
	static void ShowGameTools()
	{
		var gameTools = EditorWindow.GetWindow<GameTools>("Game Tools");
		gameTools.ShowUtility();
	}

	class Config : ScriptableObject
	{
		public DefaultAsset gameDataAsset;	
		public bool encryptGameData;
		public string pathExportGameData;
		public string pathPiker;
		public int cryptoKey = 0x600d1dea;
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

	string FindExportGameDataScript()
	{
		if (!string.IsNullOrEmpty(config.pathExportGameData))
		{
			if (File.Exists(config.pathExportGameData))
				return config.pathExportGameData;
			config.pathExportGameData = null;
		}
		var scripts = AssetDatabase.FindAssets("export_gamedata");
		foreach (var s in scripts)
		{
			var scriptName = AssetDatabase.GUIDToAssetPath(s);
			var otherScriptName = Path.Combine(Path.GetDirectoryName(scriptName), "export_sheet.py");
			if (File.Exists(otherScriptName))
			{
				config.pathExportGameData = scriptName;
				return scriptName;
			}
		}
		return null;
	}

	string FindPiker()
	{
		if (!string.IsNullOrEmpty(config.pathPiker))
		{
			if (File.Exists(config.pathPiker))
				return config.pathPiker;
			config.pathPiker = null;
		}
		var files = AssetDatabase.FindAssets("Piker");
		foreach (var s in files)
		{
			var piker = AssetDatabase.GUIDToAssetPath(s);
			var otherFilename = Path.Combine(Path.GetDirectoryName(piker), "Pike.dll");
			if (File.Exists(otherFilename))
			{
				config.pathPiker = piker;
				return piker;
			}
		}
		return null;
	}

	void OnGUI()
	{
		GUILayout.Label("Game Data");

		EditorGUI.indentLevel++;

		config.encryptGameData = EditorGUILayout.Toggle("Encrypt?", config.encryptGameData);
		if (config.encryptGameData)
		{
			EditorGUI.indentLevel++;
			config.cryptoKey = EditorGUILayout.IntField("Crypto Key", config.cryptoKey);
			EditorGUI.indentLevel--;
		}
		
		GUILayout.BeginHorizontal();
		config.gameDataAsset = (DefaultAsset)EditorGUILayout.ObjectField(config.gameDataAsset, typeof(Object), allowSceneObjects:false);
		if (GUILayout.Button("Export", GUILayout.ExpandWidth(false)))
		{
			if (config.gameDataAsset != null)
			{
				var path = AssetDatabase.GetAssetPath(config.gameDataAsset.GetInstanceID());
				var scriptName = FindExportGameDataScript();
				if (!string.IsNullOrEmpty(scriptName))
				{
					var outputPath = CheckPath(string.Empty);
					var outputName = Path.GetFileNameWithoutExtension(path);
					var jsonOutput = Path.Combine(outputPath, outputName + ".json");
					Cmd.Execute("python " + scriptName + " -i " + path + " -o " + jsonOutput);
					Debug.Assert(File.Exists(jsonOutput));
					if (config.encryptGameData)
					{
						var piker = FindPiker();
						if (!string.IsNullOrEmpty(piker))
						{
							var encryptOutput = Path.Combine(outputPath, outputName + ".bytes");
							Cmd.Execute(piker + " -i " + jsonOutput + " -o " + encryptOutput + " -k " + config.cryptoKey.ToString());
							Debug.Assert(File.Exists(encryptOutput));
							AssetDatabase.DeleteAsset(jsonOutput);
							AssetDatabase.Refresh();
						}
					}
				}
				else
				{
					Debug.LogError("Piker not found.");
				}
			}
			else
			{
				Debug.LogError("GameData export script 'export_gamedata' not found!");
			}
		}
		GUILayout.EndHorizontal();
		
	}

}
