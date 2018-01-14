
local module = {}


local mt = {}
mt.__unm = function(rhs)
	return module.Vector3D(-rhs[1], -rhs[2], -rhs[3])
end
	
mt.__add = function(lhs, rhs)
	return module.Vector3D(lhs[1] + rhs[1], lhs[2] + rhs[2], lhs[3] + rhs[3])
end

mt.__sub = function(lhs, rhs)
	return module.Vector3D(lhs[1] - rhs[1], lhs[2] - rhs[2], lhs[3] - rhs[3])
end

mt.__mul = function(lhs, rhs)
	if type(rhs) == 'number' then
		return module.Vector3D(lhs[1] * rhs, lhs[2] * rhs, lhs[3] * rhs)
	elseif type(lhs) == 'number' then
		return module.Vector3D(lhs * rhs[1], lhs * rhs[2], lhs * rhs[3])
	else
		return module.Vector3D(lhs[1] * rhs[1], lhs[2] * rhs[2], lhs[3] * rhs[3])
	end
end

mt.__div = function(lhs, rhs)
	if type(rhs) == 'number' then
		return module.Vector3D(lhs[1] / rhs, lhs[2] / rhs, lhs[3] / rhs)
	elseif type(lhs) == 'number' then
		return module.Vector3D(lhs / rhs[1], lhs / rhs[2])
	else
		return module.Vector3D(lhs[1] / rhs[1], lhs[1] / rhs[1], lhs[3] / rhs[3])
	end
end

mt.__tostring = function(v)
	return '{'..v[1]..', '..v[2]..', '..v[3]..'}'
end

mt.__eq = function(lhs, rhs)
	return (lhs[1] == rhs[1]) and (lhs[2] == rhs[2]) and (lhs[3] == rhs[3])
end

local funcs = {}
function funcs:Dup() 
	return module.Vector3D(self[1], self[2], self[3])
end

local _sqrt = math.sqrt
function funcs:GetLength() --Return the length of the vector (i.e. the distance from (0,0), see README.md for examples of using this)
	return _sqrt(self[1]^2 + self[2]^2 + self[3]^2)
end

function funcs:GetSquaredLength()
	return self[1]^2 + self[2]^2 + self[3]^2
end

function funcs:Normalize()
	local invLength = 1 / self:GetLength()
	return module.Vector3D(
		self[1] * invLength,
		self[2] * invLength,
		self[3] * invLength)
end

function funcs:Dot(other)
	return self[1]*other[1] + self[2]*other[2] + self[3]*other[3]
end

function funcs:Cross(other)
	local u1 = self[1]	
	local u2 = self[2]
	local u3 = self[3]
	local v1 = other[1]
	local v2 = other[2]
	local v3 = other[3]

	local x = u2*v3 - u3*v2
	local y = u3*v1 - u1*v3
	local z = u1*v2 - u2*v1

	return module.Vector3D(x, y, z)
end

mt.__index = funcs 

module.Vector3D = function(ix, iy, iz)
	return setmetatable({ix, iy, iz}, mt)
end

return module
