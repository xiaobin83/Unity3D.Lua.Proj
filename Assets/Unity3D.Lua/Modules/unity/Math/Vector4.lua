local serpent = require 'serpent' 

local module = {}

local mt = {}

mt.__unm = function(rhs)
	return module.Vector4D(-rhs[1], -rhs[2], -rhs[3], -rhs[4])
end
	
mt.__add = function(lhs, rhs)
	return module.Vector4D(lhs[1] + rhs[1], lhs[2] + rhs[2], lhs[3] + rhs[3], lhs[4] + rhs[4])
end

mt.__sub = function(lhs, rhs)
	return module.Vector4D(lhs[1] - rhs[1], lhs[2] - rhs[2], lhs[3] - rhs[3], lhs[4] - rhs[4])
end

mt.__mul = function(lhs, rhs)
	if type(rhs) == 'number' then
		return module.Vector4D(lhs[1] * rhs, lhs[2] * rhs, lhs[3] * rhs, lhs[4] * rhs)
	elseif type(lhs) == 'number' then
		return module.Vector4D(lhs * rhs[1], lhs * rhs[2], lhs * rhs[3], lhs * rhs[4])
	else
		return module.Vector4D(lhs[1] * rhs[1], lhs[2] * rhs[2], lhs[3] * rhs[3], lhs[4] * rhs[4])
	end
end

mt.__div = function(lhs, rhs)
	if type(rhs) == 'number' then
		return module.Vector4D(lhs[1] / rhs, lhs[2] / rhs, lhs[3] / rhs, lhs[4]/rhs)
	elseif type(lhs) == 'number' then
		return module.Vector4D(lhs / rhs[1], lhs / rhs[2], lhs / rhs[3], lhs / rhs[4])
	else
		return module.Vector4D(lhs[1] / rhs[1], lhs[1] / rhs[1], lhs[3] / rhs[3], lhs[4] / rhs[4])
	end
end

mt.__tostring = function(v)
	return '{'..v[1]..', '..v[1]..', '..v[3] .. ', '..v[4]..'}'
end

mt.__eq = function(lhs, rhs)
	return (lhs[1] == rhs[1]) and (lhs[2] == rhs[2]) and (lhs[3] == rhs[3]) and (lhs[4] == rhs[4])
end

local funcs = {}
function funcs:Dup() 
	return module.Vector4D(self[1], self[2], self[3], self[4])
end

mt.__index = funcs

module.Vector4D = function (ix, iy, iz, iw)
	return setmetatable({ix, iy, iz, iw}, mt)
end

return module

