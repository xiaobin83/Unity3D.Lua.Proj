local Debug = {}

local serpent = require 'serpent'
local Unity = require 'unity.Unity'

Debug.LL_ERROR = 1
Debug.LL_WARNING = 2
Debug.LL_INFO = 3
Debug.LL_DEBUG = 4
Debug.LL_TRIVIAL = 5 

Debug.logLevel = Debug.LL_DEBUG
-- turn off stacktrace for Unity.Debug.Log 
function Debug.DisableLogStackTrace()
	Unity.Application.SetStackTraceLogType(3, 0)
end

local timeString_ = csharp.timeString
local timeString = function()
	return '[' .. timeString_() .. ']'
end

-- global functions
function _LogE(msg)
	if Debug.logLevel >= Debug.LL_ERROR then
		Unity.Debug.LogError(timeString() .. msg)
	end
end
function _LogW(msg)
	if Debug.logLevel >= Debug.LL_WARNING then
		Unity.Debug.LogWarning(timeString() .. msg)
	end
end
function _LogI(msg)
	if Debug.logLevel >= Debug.LL_INFO then
		Unity.Debug.Log(timeString() .. msg)
	end
end
function _LogD(msg)
	if Debug.logLevel >= Debug.LL_DEBUG then
		Unity.Debug.Log(timeString() .. msg)
	end
end
function _LogT(msg)
	if Debug.logLevel >= Debug.LL_TRIVIAL then
		Unity.Debug.Log(timeString() .. msg)
	end
end

function _ToString(value)
	return serpent.block(value)
end

function Debug.GetLogFuncs(prefix)
	return 
		function(s)
			s = tostring(s)
			_LogT(prefix..s)
		end,
		function(s)
			s = tostring(s)
			_LogD(prefix..s)
		end,
		function(s)
			s = tostring(s)
			_LogI(prefix..s)
		end,
		function(s)
			s = tostring(s)
			_LogW(prefix..s)
		end,
		function(s)
			s = tostring(s)
			_LogE(prefix..s)
		end
end

return Debug
