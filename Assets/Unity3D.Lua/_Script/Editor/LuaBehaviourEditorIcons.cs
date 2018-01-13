using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace lua
{
	[InitializeOnLoad]
	public class LuaBehaviourEditorIcons
	{
		static GUIContent guiIcon;
		static LuaBehaviourEditorIcons()
		{
			var paths = AssetDatabase.FindAssets("LuaBehaviour 16x16 Icon");
			if (paths.Length > 0)
			{
				var p = AssetDatabase.GUIDToAssetPath(paths[0]);
				var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
				if (icon != null)
				{
					guiIcon = new GUIContent(icon);
					EditorApplication.hierarchyWindowItemOnGUI += HierachyItemCB;
				}
			}
		}

		static void HierachyItemCB(int instanceID, Rect selectionRect)
		{
			var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
			if (go == null) return;
			var r = new Rect(selectionRect);
			r.x = 0;
			var comp = go.GetComponent<LuaBehaviour>();
			if (comp != null)
			{
				GUILayout.BeginArea(r);
				GUILayout.Label(guiIcon);
				GUILayout.EndArea();
			}
		}
	}
}
