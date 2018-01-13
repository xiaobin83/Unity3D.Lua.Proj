using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Threading;

namespace utils
{
	public class TaskManager
	{
		// could be	used for class don't extends from MonoBehavior
		protected class AppEventDelegate : IAppPauseDelegate, IAppUpdateDelgate, IAppLateUpdateDelegate
		{
			public event System.Action<bool> onAppPause;
			public event System.Action onAppUpdate;
			public event System.Action onAppLateUpdate;

			public void OnAppPause(bool pauseStatus)
			{
				Debug.Log("App " + (pauseStatus ? "Pause" : "Resume"));
				if (onAppPause != null)
					onAppPause(pauseStatus);
			}

			public void OnAppUpdate()
			{
				if (onAppUpdate != null)
					onAppUpdate();
			}

			public void OnAppLateUpdate()
			{
				if (onAppLateUpdate != null)
					onAppLateUpdate();
			}
		}

		public static int mainThreadId
		{
			get; private set;
		}

		static MonoBehaviour appBehaviour_;
		protected static MonoBehaviour appBehaviour
		{
			get
			{
				CheckCreate();
				return appBehaviour_;
			}
		}

		static MonoBehaviour unbreakableAppBehaviour_;
		protected static MonoBehaviour unbreakableAppBehaviour
		{
			get
			{
				CheckCreate();
				return unbreakableAppBehaviour_;
			}
		}

		static ApplicationController appController_;
		protected static ApplicationController appController
		{
			get
			{
				CheckCreate();
				return appController_;
			}
		}

		static AppEventDelegate appEventDelegate_;
		protected static AppEventDelegate appEventDelegate
		{
			get
			{
				CheckCreate();
				return appEventDelegate_;
			}
		}

		static GameObject goCommonAppController;
		static void CheckCreate()
		{
			if (!Application.isPlaying)
			{
				throw new System.InvalidOperationException("CheckCreate can only be called at play mode.");
			}

			if (goCommonAppController == null)
			{
				mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

				var go = new GameObject("_TaskManager");
				goCommonAppController = go;

				appBehaviour_ = go.AddComponent<DummyMonoBehaviour>();
				unbreakableAppBehaviour_ = go.AddComponent<DummyMonoBehaviour>();
				appController_ = go.AddComponent<ApplicationController>();
				appEventDelegate_ = new AppEventDelegate();

				appController_.appPauseDelegate = appEventDelegate_;
				appController_.appUpdateDelegate = appEventDelegate_;
				appController_.appLateUpdateDelegate = appEventDelegate_;

				appEventDelegate_.onAppUpdate += Update;

				GameObject.DontDestroyOnLoad(go);
			}
		}

		// can only	be called at main thread without check
		// if Init called on a worker thread, all things will go wrong.
		public static void Init()
		{
			CheckCreate();
		}

		public static event System.Action<bool> onAppPause
		{
			add
			{
				appEventDelegate.onAppPause += value;
			}
			remove
			{
				// should not use appEventDelegate.onAppPause, it may CheckCreate in a Finalize call (remove event listener in dtor).
				if (appEventDelegate_ != null)
					appEventDelegate_.onAppPause -= value;
			}
		}

		public static event System.Action onAppUpdate
		{
			add
			{
				appEventDelegate.onAppUpdate += value;
			}
			remove
			{
				if (appEventDelegate_ != null)
					appEventDelegate_.onAppUpdate -= value;
			}
		}

		public static event System.Action onAppLateUpdate
		{
			add
			{
				appEventDelegate.onAppLateUpdate += value;
			}
			remove
			{
				if (appEventDelegate_ != null)
					appEventDelegate_.onAppLateUpdate -= value;
			}
		}

		public static void StartCoroutine(IEnumerator routine, MonoBehaviour behaviour = null)
		{
			if (behaviour == null)
			{
				behaviour = appBehaviour;
			}
			behaviour.StartCoroutine(routine);
		}

		public static void StopCoroutine(IEnumerator routine)
		{
			// you may want	to stop	coroutine in a dtor, so i shoud not use appBehaviour.FUNC, 
			// it calls CheckCreate.
			if (appBehaviour_ != null)
				appBehaviour_.StopCoroutine(routine);
		}

		public static void StopAllCoroutines()
		{
			if (appBehaviour_ != null)
				appBehaviour_.StopAllCoroutines();
		}

		public static void StartUnbreakableCoroutine(IEnumerator routine)
		{
			unbreakableAppBehaviour.StartCoroutine(routine);
		}

		public static IEnumerator WaitForFrames(int frame)
		{
			while (frame > 0)
			{
				--frame;
				yield return null;
			}
		}

		public static void DelayExecute(float delay, System.Action action, MonoBehaviour behaviour = null)
		{
			StartCoroutine(DelayExecute_(delay, action), behaviour);
		}

		static IEnumerator DelayExecute_(float delay, System.Action action)
		{
			yield return new WaitForSeconds(delay);
			action();
		}

		public static void ExecuteIf(
			System.Func<bool> pred,	System.Action action, MonoBehaviour	behaviour =	null,
			float timeout =	Mathf.Infinity,	System.Action timeoutAction	= null)
		{
			WarningIfNotAtMainThread();

			if (pred())
			{
				action();
			}
			else
			{
				StartCoroutine(ExecuteIf_(pred, action, timeout, timeoutAction), behaviour);
			}
		}

		public static void ExecuteUnbreakableIf(System.Func<bool> pred, System.Action action, float timeout = Mathf.Infinity, System.Action timeoutAction = null)
		{
			WarningIfNotAtMainThread();

			if (pred())
			{
				action();
			}
			else
			{
				// wait	for	pred()
				StartUnbreakableCoroutine(ExecuteIf_(pred, action, timeout, timeoutAction));
			}
		}

		static IEnumerator ExecuteIf_(System.Func<bool> pred, System.Action action, float timeout, System.Action actionTimeout)
		{
			float timeoutTime = Mathf.Infinity;
			if (timeout != Mathf.Infinity)
				timeoutTime = Time.realtimeSinceStartup + timeout;
			bool bTimeout = false;
			while (!pred())
			{
				if (Time.realtimeSinceStartup < timeoutTime)
				{
					yield return null;
				}
				else
				{
					bTimeout = true;
					break;
				}
			}
			if (!bTimeout)
				action();
			else
			{
				if (actionTimeout != null)
					actionTimeout();
			}
		}

		public static void ExecuteAtEndOfFrame(System.Action action, MonoBehaviour behaviour = null)
		{
			StartCoroutine(ExecuteAtEndOfFrame_(action), behaviour);
		}

		static IEnumerator ExecuteAtEndOfFrame_(System.Action action)
		{
			yield return new WaitForEndOfFrame();
			action();
		}

		// task

		struct Task
		{
			public WaitCallback callback;
			public object state;
		}
		static List<Task> taskListOnMainThread = new List<Task>();

		public static void PerformAsync(WaitCallback callback, object state = null, WaitCallback complete = null)
		{
			ThreadPool.QueueUserWorkItem((object s) =>
			{
				try
				{
					Debug.Assert(callback != null);
					callback(state);
					if (complete != null)
						PerformOnMainThread(complete, state);
				}
				catch (System.Exception e)
				{
					Debug.LogError("Error in worker thread, " + e.ToString());
					if (complete != null)
						PerformOnMainThread(complete, e);
				}
			}, state);
		}

		public static void PerformOnMainThread(WaitCallback callback, object state = null)
		{
			if (isMainThread) // if at manthread, run it directly
			{
				callback(state);
			}
			else
			{
				lock (taskListOnMainThread)
				{
					Task task = new Task();
					task.callback = callback;
					task.state = state;
					taskListOnMainThread.Add(task);
				}
			}
		}

		public static void ConsumeAllTaskOnMainThread()
		{
			AssertMainThread();
			lock (taskListOnMainThread)
			{
				foreach (var t in taskListOnMainThread)
				{
					t.callback(t.state);
				}
				taskListOnMainThread.Clear();
			}
		}

		public static void CancelAllTasksOnMainThread()
		{
			lock (taskListOnMainThread)
			{
				taskListOnMainThread.Clear();
			}
		}

		public static void AssertMainThread()
		{
			Debug.Assert(isMainThread);
		}

		public static bool isMainThread
		{
			get
			{
				return mainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId;
			}
		}

		public static void WarningIfNotAtMainThread()
		{
			if (!isMainThread)
			{
				Debug.LogError("Method called at worker thread!");
			}
		}

		const float executionTimePerFrame = 0.1f;
		static void Update()
		{
			AssertMainThread();
			lock (taskListOnMainThread)
			{
				float executionDueTime = Time.realtimeSinceStartup + executionTimePerFrame;
				while (taskListOnMainThread.Count > 0 && Time.realtimeSinceStartup < executionDueTime)
				{
					Task task = taskListOnMainThread[0];
					taskListOnMainThread.RemoveAt(0);
					Debug.Assert(task.callback != null);
					task.callback(task.state);
				}
			}
		}

	}
}
