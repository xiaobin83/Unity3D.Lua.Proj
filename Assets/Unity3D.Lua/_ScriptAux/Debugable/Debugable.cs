using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace utils
{
	public class Debugable : MonoBehaviour
	{
		protected Rect rect = new Rect(Screen.width - 400, Screen.height - 400, 400, 400);

		protected bool show;

		string title;
		class Button
		{
			public string name;
			public lua.LuaFunction func;
			public int width = 100;
		}
		List<Button> buttons = new List<Button>();
		List<Button> toolbarButtons = new List<Button>();

		class StatusString
		{
			public string name;
			public lua.LuaFunction func;
		}
		List<StatusString> statusStrings = new List<StatusString>();


		class Graph
		{
			public string name;
			public string unitName;
			public lua.LuaFunction func;
			public float time;
			public float sampleStepDuration;
			public Rect rect;
			public Color color;

			List<float> values = new List<float>();
			float nextSampleTime = 0;

			int totalStepCount;



			public void Initialize()
			{
				totalStepCount = Mathf.CeilToInt(time / sampleStepDuration);
			}

			public void Sample()
			{
				if (Time.realtimeSinceStartup > nextSampleTime)
				{
					nextSampleTime = Time.realtimeSinceStartup + sampleStepDuration;
					var value = (float)(double)func.Invoke1();
					values.Add(value);
					if (value > maxValue)
						maxValue = value;
					if (value < minValue)
						minValue = value;
					if (values.Count > totalStepCount * 2)
					{
						values.RemoveRange(0, totalStepCount);
					}
				}
			}


			float maxValue = -Mathf.Infinity;
			float minValue = Mathf.Infinity;
			public void Draw()
			{
				GUI.Box(rect, name + ": cur / avg " + unitName);

				// x pos in graph
				var padding = 20;
				var x = rect.x + padding;
				var y = rect.y + padding;
				var width = rect.width - 2 * padding;
				var height = rect.height - 2 * padding;

				int stepSize = Mathf.CeilToInt(width / totalStepCount);




				if (values.Count == 0)
					return;
				var valueIndex = Mathf.Max(0, values.Count - totalStepCount);
				var numValuesToDraw = values.Count - valueIndex;

				var posX = x + width - stepSize * numValuesToDraw;
				var valueHeight = maxValue - minValue;
				if (Mathf.Approximately(valueHeight, 0))
					valueHeight = 0;

				float posY = y + height;
				if (valueIndex > 0 && valueHeight > 0)
				{
					posY = y + height * (1 - ((values[valueIndex - 1] - minValue) / valueHeight));
				}
				Vector2 lastPos = new Vector2(posX, posY);
				float total = 0;
				int count = 0;
				for (int i = valueIndex; i < values.Count; ++i)
				{
					var v = values[i];
					total = total + v;
					count = count + 1;
					if (valueHeight != 0)
					{
						posY = y + height * (1 - ((v - minValue) / valueHeight));
					}
					var pos = new Vector2(posX, posY);
					Drawing.DrawLine(lastPos, pos, color, 1, false);
					lastPos = pos;
					posX += stepSize;
				}

				var r = rect;
				r.y = r.y + r.height - padding;
				r.height = padding;
				string lastValueString;
				if (values.Count > 0)
				{
					lastValueString = string.Format("{0:0.00} / {1:0.00} {2}", values[values.Count - 1], total / count, unitName);
				}
				else
				{
					lastValueString = "---" + unitName;
				}
				GUI.Box(r, lastValueString);

			}
		}
		List<Graph> graphs = new List<Graph>();

		lua.LuaFunction cmdHandler;
		string cmdString;

		string popUpContent = "";
		bool showPopUp;


		static lua.Lua luaVm;

		public static void SetLua(lua.Lua luaVm)
		{
			Debugable.luaVm = luaVm;
		}

		protected virtual void Update()
		{
#if UNITY_EDITOR
			foreach (var g in graphs)
			{
				g.Sample();
			}
#endif
		}

		Vector2 scrollPosition = Vector2.zero;
#if UNITY_EDITOR
		void OnGUI()
		{
			if (show)
			{
				if (!string.IsNullOrEmpty(title))
				{
					GUILayout.BeginArea(rect, title);
					GUILayout.Space(15);
				}
				else
				{
					GUILayout.BeginArea(rect);
				}

				if (toolbarButtons.Count > 0)
				{
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					foreach (var b in toolbarButtons)
					{
						if (GUILayout.Button(b.name, GUILayout.Width(b.width)))
						{
							b.func.Invoke();
						}
					}
					GUILayout.EndHorizontal();
				}

				if (showPopUp)
				{

					GUILayout.TextArea(popUpContent, GUILayout.Height(100));
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Clear"))
					{
						popUpContent = "";
					}
					if (GUILayout.Button("Close"))
					{
						showPopUp = false;
					}
					GUILayout.EndHorizontal();
				}

				if (cmdHandler != null)
				{
					cmdString = GUILayout.TextArea(cmdString, GUILayout.Height(80));
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Execute", GUILayout.Width(100)))
					{
						cmdHandler.Invoke(cmdString);
					}
					GUILayout.EndHorizontal();
				}
				scrollPosition = GUILayout.BeginScrollView(scrollPosition);
				var oldColor = GUI.color;
				foreach (var s in statusStrings)
				{
					var statsString = (string)s.func.Invoke1();
					GUI.color = Color.blue;
					GUILayout.Label(s.name);
					GUI.color = oldColor;
					GUILayout.TextArea(statsString);
				}

				foreach (var b in buttons)
				{
					if (GUILayout.Button(b.name, GUILayout.Width(b.width)))
					{
						b.func.Invoke();
					}
				}
				GUILayout.EndScrollView();
				GUILayout.EndArea();

				if (Event.current.type == EventType.Repaint)
				{
					foreach (var g in graphs)
					{
						g.Draw();
					}
				}
			}
		}
#endif

		public void Editor_SetArea(float x, float y, float width, float height)
		{
			rect = new Rect(x, y, width, height);
		}

		public void Editor_SetTitle(string title)
		{
			this.title = title;
		}

		public void Editor_AddButton(string name, lua.LuaFunction func, int width = 100)
		{
			buttons.Add(
				new Button()
				{
					name = name,
					func = func.Retain()
				});
		}
		public void Editor_AddToolbarButton_Native(string name, System.Action func, int width = 100)
		{
			var luaFunc = lua.LuaFunction.CreateDelegate(luaVm, func);
			Editor_AddToolbarButton(name, luaFunc, width);
			luaFunc.Dispose();
		}

		public void Editor_AddToolbarButton(string name, lua.LuaFunction func, int width = 100)
		{
			toolbarButtons.Add(
				new Button()
				{
					name = name,
					func = func.Retain(),
					width = width,
				});
		}

		public void Editor_AddStatsString(string name, lua.LuaFunction func)
		{
			statusStrings.Add(
				new StatusString()
				{
					name = name,
					func = func.Retain(),
				});

		}

		public void Editor_AddGraph_Native(
			string name, string unitName, System.Func<float> func,
			float time, float duration, float x, float y, float w, float h, Color color)
		{
			var luaFunc = lua.LuaFunction.CreateDelegate(luaVm, func);
			Editor_AddGraph(name, unitName, luaFunc, time, duration, x, y, w, h, color);
			luaFunc.Dispose();
		}

		public void Editor_AddGraph(
			string name,
			string unitName,
			lua.LuaFunction func,
			float time,
			float duration,
			float x, float y, float w, float h, Color color)
		{
			var g = new Graph()
			{
				name = name,
				unitName = unitName,
				func = func.Retain(),
				time = time,
				sampleStepDuration = duration,
				rect = new Rect(x, y, w, h),
				color = color
			};
			g.Initialize();
			graphs.Add(g);
		}

		public void Editor_SetCmdHandler(lua.LuaFunction func)
		{
			cmdHandler = func.Retain();
		}

		public void Editor_ToggleGUI()
		{
			show = !show;
		}

		public void Editor_PopUp(string content)
		{
			popUpContent += content;
			showPopUp = true;
		}

		public void Editor_TogglePopUp()
		{
			showPopUp = !showPopUp;
		}

		protected virtual void OnDestroy()
		{
			foreach (var g in graphs)
			{
				g.func.Dispose();
			}
			graphs.Clear();
			foreach (var s in statusStrings)
			{
				s.func.Dispose();
			}
			statusStrings.Clear();
			foreach (var b in toolbarButtons)
			{
				b.func.Dispose();
			}
			toolbarButtons.Clear();
			foreach (var b in buttons)
			{
				b.func.Dispose();
			}
			buttons.Clear();
		}
	}
}
