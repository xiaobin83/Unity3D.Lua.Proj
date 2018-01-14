local serpent = require 'serpent' 

local module = {}

local mt = {}
mt.__unm = function(rhs)
	return module.Vector2D(-rhs[1], -rhs[2])
end
	
mt.__add = function(lhs, rhs)
	return module.Vector2D(lhs[1] + rhs[1], lhs[2] + rhs[2])
end

mt.__sub = function(lhs, rhs)
	return module.Vector2D(lhs[1] - rhs[1], lhs[2] - rhs[2])
end

mt.__mul = function(lhs, rhs)
	if type(rhs) == 'number' then
		return module.Vector2D(lhs[1] * rhs, lhs[2] * rhs)
	elseif type(lhs) == 'number' then
		return module.Vector2D(lhs * rhs[1], lhs * rhs[2])
	else
		return module.Vector2D(lhs[1] * rhs[1], lhs[2] * rhs[2])
	end
end

mt.__div = function(lhs, rhs)
	if type(rhs) == 'number' then
		return module.Vector2D(lhs[1] / rhs, lhs[2] / rhs)
	elseif type(lhs) == 'number' then
		return module.Vector2D(lhs / rhs[1], lhs / rhs[2])
	else
		return module.Vector2D(lhs[1] / rhs[1], lhs[2] / rhs[2])
	end
end

mt.__tostring = function(v)
	return '{'..v[1]..', '..v[1] ..'}'
end

--Comparisons

mt.__eq = function(lhs, rhs)
	--Equal To operator for vector2Ds
	return (lhs[1] == rhs[1]) and (lhs[2] == rhs[2])
end

local funcs = {}
function funcs:Dup() 
	return module.Vector3D(self[1], self[2])
end

local _sqrt = math.sqrt
function funcs:GetLength() --Return the length of the vector (i.e. the distance from (0,0), see README.md for examples of using this)
	return _sqrt(self[1]^2 + self[2]^2)
end

function funcs:GetSquaredLength()
	return self[1]^2 + self[2]^2 
end

function funcs:Normalize()
	local invLength = 1 / self:GetLength()
	return module.Vector2D(
		self[1] * invLength,
		self[2] * invLength)
end

function funcs:Dot(other)
	return self[1]*other[1] + self[2]*other[2] + self[3]*other[3]
end

mt.__index = funcs


module.Vector2D = function (ix, iy)
	return setmetatable({ix, iy}, mt)
end

return module
