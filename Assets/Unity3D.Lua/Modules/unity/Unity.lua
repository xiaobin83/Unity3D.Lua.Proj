
local Unity = {

	UI = {
		Button = csharp.checked_import('UnityEngine.UI.Button'),
		Image = csharp.checked_import('UnityEngine.UI.Image'),
		Text = csharp.checked_import('UnityEngine.UI.Text'),
		ScrollRect = csharp.checked_import('UnityEngine.UI.ScrollRect'),
		Toggle = csharp.checked_import('UnityEngine.UI.Toggle'),
		ToggleGroup = csharp.checked_import('UnityEngine.UI.ToggleGroup'),
	},

	AI = {
		NavMesh = csharp.checked_import('UnityEngine.AI.NavMesh'),
		NavMeshAgent = csharp.checked_import('UnityEngine.AI.NavMeshAgent'),
		NavMeshPath = csharp.checked_import('UnityEngine.AI.NavMeshPath'),
		NavMeshPathStatus = csharp.checked_import('UnityEngine.AI.NavMeshPathStatus'),
	},

	Profiling = {
		Profiler = _UNITY['EDITOR'] and csharp.checked_import('UnityEngine.Profiling.Profiler')
									or { BeginSample = function() end, EndSample = function() end }
	},

	GameObject = csharp.checked_import('UnityEngine.GameObject'),
	Camera = csharp.checked_import('UnityEngine.Camera'),
	RenderTexture = csharp.checked_import('UnityEngine.RenderTexture'),
	Screen = csharp.checked_import('UnityEngine.Screen'),
	Texture2D = csharp.checked_import('UnityEngine.Texture2D'),
	CanvasGroup = csharp.checked_import('UnityEngine.CanvasGroup'),
	Animator = csharp.checked_import('UnityEngine.Animator'),
	PlayerPrefs = csharp.checked_import("UnityEngine.PlayerPrefs"),
	Application = csharp.checked_import("UnityEngine.Application"),
	SystemLanguage = csharp.checked_import("UnityEngine.SystemLanguage"),
	WaitForSeconds = csharp.checked_import('UnityEngine.WaitForSeconds'),

	Time = csharp.checked_import('UnityEngine.Time'),

	Debug = csharp.checked_import('UnityEngine.Debug'),

	Vector2 = csharp.checked_import('UnityEngine.Vector2'),
	Vector3 = csharp.checked_import('UnityEngine.Vector3'),
	Color = csharp.checked_import("UnityEngine.Color"),

	Input = csharp.checked_import('UnityEngine.Input'),
	Physics = csharp.checked_import('UnityEngine.Physics'),
	LayerMask = csharp.checked_import('UnityEngine.LayerMask'),

	RectTransformUtility = csharp.checked_import('UnityEngine.RectTransformUtility'),

	Gizmos = csharp.checked_import('UnityEngine.Gizmos'),
	
}

local LuaBehaviour = csharp.checked_import('lua.LuaBehaviour')
Unity.lua = {
	LuaBehaviour = LuaBehaviour,
	GetLBT = function(gameObject)
		local lb = gameObject:GetComponent(LuaBehaviour)
		if lb then
			return lb:GetBehaviourTable()
		end
	end,
}


return Unity

