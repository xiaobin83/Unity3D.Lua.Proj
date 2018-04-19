
local Unity = setmetatable({}, { __index = function(tbl, name)
	local m = csharp.checked_import('UnityEngine.'..name)
	rawset(tbl, name, m)
	return m
end})

Unity.UI = setmetatable({}, { __index = function(tbl, name)
	local m = csharp.checked_import('UnityEngine.UI.'..name)
	rawset(tbl, name, m)
	return m
end})

Unity.game = setmetatable({}, { __index = function(tbl, name)
	local m = csharp.checked_import(name)
	rawset(tbl, name, m)
	return m
end})

Unity.lua = setmetatable({
	GetLBT = function(gameObject)
		local lb = gameObject:GetComponent(Unity.lua.LuaBehaviour)
		if lb then
			return lb:GetBehaviourTable()
		end
	end},

	{ __index = function(tbl, name)
		local m = csharp.checked_import('lua.'..name)
		rawset(tbl, name, m)
		return m
	end}
)

return Unity

