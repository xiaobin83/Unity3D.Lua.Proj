require 'unity.Debug'
setmetatable(_G, {__newindex = function(t, k, v)
	_LogE(debug.traceback(string.format('setting global variable is disabled. key = %s', k), 2))
end
})