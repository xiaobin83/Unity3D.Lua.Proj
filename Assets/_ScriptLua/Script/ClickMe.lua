local ClickMe = {}
local Button = csharp.checked_import('UnityEngine.UI.Button, UnityEngine.UI')
local Debug = csharp.checked_import('UnityEngine.Debug, UnityEngine')
local WaitForSeconds = csharp.checked_import('UnityEngine.WaitForSeconds, UnityEngine')
local Math = require 'unity.Math'
require 'unity.Debug'

function ClickMe._Init(instance)
    instance.value = 10
end

function ClickMe.StaticMethod()
	Debug.Log('ClickMe.StaticMethod')
end
function ClickMe:Method()
	Debug.Log('ClickMe:Method')
end

function ClickMe:Awake()
	self.StaticMethod()
	self:Method()
--[[
	local btn = self:GetComponent(Button)
	btn.onClick:AddListener(
		function()
			Debug.Log('ClickMe from Lua!')
		end)
--]]
end

function ClickMe:OnClick()
	_LogD(tostring(Math.Vector3(1, 2, 3)))
	Debug.Log('OnClick in Lua ' .. self.value)
	local val = self.value
	local co = coroutine.create(
		function() 
			while val >= 0 do
				Debug.Log('in coroutine ' .. val)
				val = val - 1
				coroutine.yield(WaitForSeconds(1))
			end
			Debug.Log('coroutine ends')
		end)
	Debug.Log(type(co))
	self:StartLuaCoroutine(co)
end

return ClickMe
