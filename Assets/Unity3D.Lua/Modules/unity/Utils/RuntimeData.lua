local RuntimeData = {}
local Unity = require 'unity.Unity'
local WatchDog = require 'unity.Utils.WatchDog'
local json = require 'rapidjson'


local all = {}

function RuntimeData.CreateOrObtainMonitored(name)
	local d = all[name]
	if not d then
		d = WatchDog.WatchTable({})
		all[name] = d
	end
	return d
end

function RuntimeData.CreateOrObtain(name)
	local d = all[name]
	if not d then
		d = { __mode = 'bare' }
		all[name] = d
	end
	return d
end

function RuntimeData.CreateOrObtainSaved(name)
	local d = all[name]
	if not d then
		d = WatchDog.WatchTable({})
		-- read from playerprefs
		local savename = '_rd_'..name
		local s = Unity.PlayerPrefs.GetString(savename, '{}')
		local restored = json.decode(s)
		for k, v in pairs(restored) do
			d[k] = v
		end
		d.onValueChanged = d.onValueChanged + function()
			Unity.PlayerPrefs.SetString(savename, json.encode(d.__value))
		end
		all[name] = d
	end
	return d
end


return RuntimeData
