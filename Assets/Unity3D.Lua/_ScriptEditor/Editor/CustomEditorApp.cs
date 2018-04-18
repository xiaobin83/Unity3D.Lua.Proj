using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class CustomEditorApp {

	static object taskLock = new object();
	static List<System.Action> tasks = new List<System.Action>();

	static CustomEditorApp()
	{
		EditorApplication.update += Update;
	}
	


	static void Update()
	{
		lock(taskLock)
		{
			if (tasks.Count > 0)
			{
				foreach (var t in tasks)
				{
					t();
				}
				tasks.Clear();
			}
		}
	}

	public static void AddTask(System.Action action)
	{
		lock(taskLock)
		{
			tasks.Add(action);
		}
	}
}
