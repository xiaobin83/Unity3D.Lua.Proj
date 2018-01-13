using UnityEngine;
using System.Collections;

namespace utils 
{
	// notice, these interface is used for those class dosen't extend from monobehaviour. 
	public interface IAppPauseDelegate
	{
		void OnAppPause(bool pauseStatus);
	}
	public interface IAppUpdateDelgate
	{
		void OnAppUpdate();
	}
	public interface IAppLateUpdateDelegate
	{
		void OnAppLateUpdate();
	}


	// Do not attach this behavior to any gameobject, it managed by TaskManager 
	// And keep the script execute order of project setting in mind
	public class ApplicationController : MonoBehaviour
	{
		public IAppPauseDelegate appPauseDelegate;
		public IAppUpdateDelgate appUpdateDelegate;
		public IAppLateUpdateDelegate appLateUpdateDelegate;

		void OnApplicationPause(bool pause)
		{
			if (appPauseDelegate != null)
			{
				appPauseDelegate.OnAppPause(pause);
			}
		}

		void Update()
		{
			if (appUpdateDelegate != null)
			{
				appUpdateDelegate.OnAppUpdate();
			}
		}

		void LateUpdate()
		{
			if (appLateUpdateDelegate != null)
			{
				appLateUpdateDelegate.OnAppLateUpdate();
			}
		}
	}
}
