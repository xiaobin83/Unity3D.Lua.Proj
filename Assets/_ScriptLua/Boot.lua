local Boot = {}

local Unity = require 'unity.Unity'

local Color = csharp.checked_import('UnityEngine.Color')
function Boot:Start()
	local Debugable = csharp.checked_import('x600d1dea.lua.utils.Debugable')
	local dbg = self:GetComponent(Debugable)
	dbg:Editor_ToggleGUI()
	dbg:Editor_AddGraph(
		'random', 'unit',
		function()
			return math.random()
		end,
		10, 0.5,
		100, 100, 200, 100, Color.red)
end

return Boot
