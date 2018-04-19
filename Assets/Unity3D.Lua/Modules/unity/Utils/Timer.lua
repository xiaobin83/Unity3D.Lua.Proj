
-- https://blog.acolyer.org/2015/11/23/hashed-and-hierarchical-timing-wheels/
-- Scheme 6

local Timer = {}

local Time = require 'unity.Utils.Time'

local kMaxIntervalShift = 4  --> 1 << kMaxIntervalShift seconds
local kMaxSlots = 1 << kMaxIntervalShift
local kSlotsMask = kMaxSlots - 1
local origin = Time.RS()
local pointer = 0
local round = 0
local slots = {}
for i = 1, kMaxSlots do
	slots[i] = {}
end

local toArrange = {}

local _floor = math.floor
local _ipairs = ipairs

function Timer.After(seconds, event, obj)
	toArrange[#toArrange + 1] = {Time.RS() + seconds, event, obj}
end

function Timer.Update()
	local cur = Time.RS() 
	if #toArrange > 0 then
		for _, a in _ipairs(toArrange) do
			local expire = a[1]
			local s = _floor(expire - origin)
			if s < 0 then s = 0 end
			local slot = (pointer + s) & kSlotsMask
			local events = slots[slot+1]
			events[#events + 1] = a
		end
		toArrange = {}
	end
	local advance = _floor(cur - origin)
	for i = 0, advance do
		local slot = (pointer + i) & kSlotsMask
		local events = slots[slot + 1]
		if #events > 0 then 
			local eventsNotFired = {}
			for _, evt in _ipairs(events) do
				if cur + i >= evt[1] then
					evt[2](evt[3])
				else
					-- not fired
					eventsNotFired[#eventsNotFired + 1] = evt
				end
			end
			slots[slot + 1] = eventsNotFired
		end
	end
	origin = origin + advance
	pointer = pointer + advance
end

return Timer
