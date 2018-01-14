using UnityEngine;

namespace utils
{
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
			Editor_AddToolbarButton_Native("PopUp", Editor_TogglePopUp);
		}

	}
}
