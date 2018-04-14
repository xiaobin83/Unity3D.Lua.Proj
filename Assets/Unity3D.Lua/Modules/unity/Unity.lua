
local Unity = {} 
local invalidModule = {
	__name = 'invalid unity module'
}
local cached = {}
setmetatable(Unity, { __index = function(tbl, name)
	local m = cached[name]
	if not m then
		m = csharp.checked_import('UnityEngine.'..name)
		if not m then
			m = invalidModule
		end
		cached[name] = m
	end
	return m
end})

Unity.lua = {
	LuaBehaviour = csharp.checked_import('lua.LuaBehaviour'),
	GetLBT = function(gameObject)
		local lb = gameObject:GetComponent(LuaBehaviour)
		if lb then
			return lb:GetBehaviourTable()
		end
	end,
}


return Unity

