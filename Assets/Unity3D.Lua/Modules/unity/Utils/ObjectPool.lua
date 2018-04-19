local ObjectPool = {}

local Unity = require 'unity.Unity'
local _R = require 'unity.Utils.ResMgr'
local Timer = require 'unity.Utils.Timer'
local Math = require 'unity.Math'

local freeList = {}
local allocList = {}

local kAction_Obtain = 0
local kAction_ObtainFromPrefab = 1
local kAction_Release = 2

function ObjectPool.NativePoolDelegate(action, ...)
	if action == kAction_Obtain then
		return ObjectPool.Obtain(...)
	elseif action == kAction_ObtainFromPrefab then
		return ObjectPool.Obtain('from_prefab', ...)
	elseif action == kAction_Release then
		ObjectPool.Release(...)
	end
end

function ObjectPool.Obtain(type, uri, ...)
	local list = freeList[type]
	if not list then
		list = {}
		freeList[type] = list
	end
	local prefab
	if type == 'from_prefab' then
		prefab = uri
		uri = uri:GetInstanceID()
	end
	local objList = list[uri]
	if not objList then
		objList = {}
		list[uri] = objList 
	end
	local lastObjIdx = #objList
	local obj
	if lastObjIdx > 0 then
		obj = objList[lastObjIdx]
		local posU, rotU = ...
		if posU then
			if not rotU then
				rotU = Math.Quaternion.identityU
			end
			obj.transform.position = posU
			obj.transform.rotation = rotU
		end
		objList[lastObjIdx] = nil
	else
		if prefab then
			obj = Unity.GameObject.Instantiate(prefab, ...)
		else
			obj = _R(type, uri, ...)
			if not obj then return nil end
		end
	end
	obj:SetActive(true)
	allocList[obj:GetInstanceID()] = {obj, type, uri}
	return obj
end


local _Release = function(obj, resetParent)
	if obj == nil then return end

	if resetParent then
		obj.transform:SetParent(nil, false)
	end
	local id = obj:GetInstanceID()
	local objTuple = allocList[id]
	if objTuple then
		local obj_, type, uri = unpack(objTuple)
		assert(obj_ == obj)
		obj:SetActive(false)
		allocList[id] = nil
		local objList = freeList[type][uri] 
		objList[#objList + 1] = obj
	end
end

function ObjectPool.Release(obj, delay, resetParent)
	if obj == nil then return end
	delay = delay or 0
	if delay > 0 then
		if resetParent then
			obj.transform:SetParent(nil)	
		end
		Timer.After(delay, _Release, obj)
	else
		_Release(obj, resetParent)
	end
end

function ObjectPool.CleanUnused()
	for _, list in pairs(freeList) do
		for uri, objList in pairs(list) do
			for _, obj in ipairs(objList) do
				if obj ~= nil then
					Unity.GameObject.Destroy(obj)
				end
			end
			list[uri] = {} 
		end
	end
end

function ObjectPool.Clean()
	ObjectPool.CleanUnused()
	freeList = {}
	for _, objTuple in pairs(allocList) do
		local obj, type = unpack(objTuple)
		if obj ~= nil then
			Unity.Object.Destroy(obj)
		end
	end
	allocList = {}
end

return ObjectPool 
