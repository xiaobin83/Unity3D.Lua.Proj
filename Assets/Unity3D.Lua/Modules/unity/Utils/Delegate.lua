return function()
	local methods = {} 
	local meta = {
		__add = function(delegates, func)
			methods[#methods + 1] = func
			return delegates
		end,
		__sub = function(delegates, func)
			for i, f in ipairs(methods) do
				if f == func then
					table.remove(methods, i)
				end
			end
			return delegates
		end,
		__call = function(delegates, ...)
			for _, f in ipairs(methods) do
				f(...)
			end
		end
	}
	return setmetatable({}, meta)
end
