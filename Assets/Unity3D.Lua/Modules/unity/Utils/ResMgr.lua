local Unity = require 'unity.Unity'
local resmgr = require 'resmgr'

local cache = {}

local _R

local _lower = string.lower
_R = function(t, uri, ...)
	if t == ':clearcache' then
		cache = {}	
		return
	elseif t == ':unloadunused' then
		resmgr.UnloadUnused()
		return
	elseif t == ':setcryptokey' then
		resmgr.SetCryptoKey(uri)
		return
	end

	if t == "gameobject" then
		local obj = _R('object', uri)
		return Unity.GameObject.Instantiate(obj, ...)
	end

	local c = cache[t]
	if not c then
		c = {}
		cache[t] = c
	end

	uri = _lower(uri)	
	local r = c[uri]
	if r then
		return r
	end
	
	if t == 'sprite' then
		r = resmgr.LoadSprite(uri)
	elseif t == 'sprites' then
		r = resmgr.LoadSprites(uri)
	elseif t == 'bytes' then
		r = resmgr.LoadBytes(uri)
	elseif t == 'encrypted_bytes' then
		r = resmgr.LoadBytes(uri, true)
	elseif t == 'text' then
		r = resmgr.LoadText(uri)
	elseif t == 'encrypted_text' then
		r = resmgr.LoadText(uri, true)
	elseif t == 'object' then
		r = resmgr.LoadObject(uri)
	elseif t == 'objects' then
		r = resmgr.LoadObjects(uri)
	elseif t == 'texture2d' then
		r = resmgr.LoadTexture2D(uri)
	end

	c[uri] = r

	return r
end

return _R
