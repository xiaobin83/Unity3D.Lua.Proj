
local module = {}

local _sqrt = math.sqrt
local _type = type

local mt = {}
mt.__unm = function(rhs)
	return module.Vector3D(-rhs.x, -rhs.y, -rhs.z)
end
	
mt.__add = function(lhs, rhs)
	return module.Vector3D(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z)
end

mt.__sub = function(lhs, rhs)
	return module.Vector3D(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z)
end

mt.__mul = function(lhs, rhs)
	if _type(rhs) == 'number' then
		return module.Vector3D(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs)
	elseif _type(lhs) == 'number' then
		return module.Vector3D(lhs * rhs.x, lhs * rhs.y, lhs * rhs.z)
	else
		return module.Vector3D(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z)
	end
end

mt.__div = function(lhs, rhs)
	if _type(rhs) == 'number' then
		return module.Vector3D(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs)
	elseif _type(lhs) == 'number' then
		return module.Vector3D(lhs / rhs.x, lhs / rhs.y)
	else
		return module.Vector3D(lhs.x / rhs.x, lhs.x / rhs.x, lhs.z / rhs.z)
	end
end

mt.__tostring = function(v)
	return "[(X:".. v.x .."),(Y:".. v.y .."),(Z:".. v.z ..")]"
end

mt.__eq = function(lhs, rhs)
	return (lhs.x == rhs.x) and (lhs.y == rhs.y) and (lhs.z == rhs.z)
end

local funcs = {}
function funcs:Dup() 
	return module.Vector3D(self.x, self.y, self.z)
end

function funcs:GetLength() --Return the length of the vector (i.e. the distance from (0,0), see README.md for examples of using this)
	return _sqrt(self.x^2 + self.y^2 + self.z^2)
end

function funcs:GetSquaredLength()
	return self.x^2 + self.y^2 + self.z^2
end

function funcs:Normalized()
	local sqLen = self:GetSquaredLength()
	if sqLen > 0 then
		local invLength = 1 / _sqrt(sqLen) 
		return module.Vector3D(
			self.x * invLength,
			self.y * invLength,
			self.z * invLength)
	end
end

function funcs:Normalize()
	local sqLen = self:GetSquaredLength() 
	if sqLen > 0 then
		local invLength = 1 / _sqrt(sqLen) 
		self.x = self.x * invLength
		self.y = self.y * invLength
		self.z = self.z * invLength
	end
end

function funcs:Dot(other)
	return self.x*other.x + self.y*other.y + self.z*other.z
end

function funcs:Cross(other)
	local u1 = self.x	
	local u2 = self.y
	local u3 = self.z
	local v1 = other.x
	local v2 = other.y
	local v3 = other.z

	local x = u2*v3 - u3*v2
	local y = u3*v1 - u1*v3
	local z = u1*v2 - u2*v1

	return module.Vector3D(x, y, z)
end

mt.__index = funcs

module.Vector3D = function(ix, iy, iz)
	return setmetatable({x=ix, y=iy, z=iz}, mt)
end

return module
