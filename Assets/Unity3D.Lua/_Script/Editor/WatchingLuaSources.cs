using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace lua
{
	public class WatchingLuaSources : AssetPostprocessor 
	{
		enum Status 
		{
			Ok,
			Reimported,
		};
		static Dictionary<string, Status> luaSourceStatus = new Dictionary<string, Status>();

		public static event System.Action onSourceChanged;

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
		{
			var sourceChanged = false;
			foreach (string str in importedAssets)
			{
				if (str.EndsWith(".lua"))
				{
					luaSourceStatus[str] = Status.Reimported;
					sourceChanged = true;
				}
			}
			foreach (string str in deletedAssets) 
			{
				if (str.EndsWith(".lua"))
				{
					luaSourceStatus.Remove(str);
					sourceChanged = true;
				}
			}	

			for (int i=0; i<movedAssets.Length; i++)
			{
				var movedAsset = movedAssets[i];
				var movedFromAssetPath = movedFromAssetPaths[i];
				if (movedAsset.EndsWith(".lua"))
				{
					luaSourceStatus.Remove(movedFromAssetPath);
					luaSourceStatus[movedAsset] = Status.Reimported;
					sourceChanged = true;
				}
			}

			if (sourceChanged && onSourceChanged != null)
			{
				onSourceChanged();
				SceneView.RepaintAll();
			}
		}

		public static bool IsReimported(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;

			var rootPath = new System.Uri(Application.dataPath);
			var scriptPath = new System.Uri(path);
			var relativeUri = rootPath.MakeRelativeUri(scriptPath);
			Status s;
			if (luaSourceStatus.TryGetValue(relativeUri.ToString(), out s))
			{
				return s == Status.Reimported;
			}
			return false;
		}

		public static void SetProcessed(string path)
		{
			if (string.IsNullOrEmpty(path)) return;

			var rootPath = new System.Uri(Application.dataPath);
			var scriptPath = new System.Uri(path);
			var relativeUri = rootPath.MakeRelativeUri(scriptPath);
			var pathToCheck = relativeUri.ToString();
			if (luaSourceStatus.ContainsKey(pathToCheck))
			{
				luaSourceStatus[pathToCheck] = Status.Ok;
			}
		}

	}
}

