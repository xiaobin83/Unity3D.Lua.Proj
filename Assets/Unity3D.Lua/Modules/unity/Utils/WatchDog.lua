local WatchDog = {}

local Delegate = require 'unity.Utils.Delegate'

function WatchDog.WatchTable(tbl)
	local proxy = {
		__value = tbl,
		onValueChanged = Delegate(),
	}
	proxy.Reset	= function(tbl)
		proxy.__value = tbl or {}
		proxy.onValueChanged = Delegate()
	end

	return setmetatable(proxy, {
		__index = function(t, name)
			return t.__value[name]
		end,
		__newindex = function(t, name, newValue) 
			local old = t.__value[name]
			if old ~= newValue then
				t.__value[name] = newValue
				t.onValueChanged(name, newValue)
			end
		end
	})
end



return WatchDog
