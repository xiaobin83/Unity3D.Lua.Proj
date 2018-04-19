local module = {}

local _sqrt = math.sqrt
local _type = type

local mt = {}
mt.__unm = function(rhs)
	return module.Vector2D(-rhs.x, -rhs.y)
end
	
mt.__add = function(lhs, rhs)
	return module.Vector2D(lhs.x + rhs.x, lhs.y + rhs.y)
end

mt.__sub = function(lhs, rhs)
	return module.Vector2D(lhs.x - rhs.x, lhs.y - rhs.y)
end

mt.__mul = function(lhs, rhs)
	if _type(rhs) == 'number' then
		return module.Vector2D(lhs.x * rhs, lhs.y * rhs)
	elseif _type(lhs) == 'number' then
		return module.Vector2D(lhs * rhs.x, lhs * rhs.y)
	else
		return module.Vector2D(lhs.x * rhs.x, lhs.y * rhs.y)
	end
end

mt.__div = function(lhs, rhs)
	if _type(rhs) == 'number' then
		return module.Vector2D(lhs.x / rhs, lhs.y / rhs)
	elseif _type(lhs) == 'number' then
		return module.Vector2D(lhs / rhs.x, lhs / rhs.y)
	else
		return module.Vector2D(lhs.x / rhs.x, lhs.y / rhs.y)
	end
end

mt.__tostring = function(v)
	--tostring handler for Vector2D
	return "[(X:".. v.x .."),(Y:".. v.y ..")]"
end

--Comparisons

mt.__eq = function(lhs, rhs)
	--Equal To operator for vector2Ds
	return (lhs.x == rhs.x) and (lhs.y == rhs.y)
end


local funcs = {}
function funcs:Dup() 
	return module.Vector2D(self.x, self.y)
end

function funcs:GetLength() --Return the length of the vector (i.e. the distance from (0,0), see README.md for examples of using this)
	return _sqrt(self.x^2 + self.y^2) 
end

function funcs:GetSquaredLength()
	return self.x^2 + self.y^2 + self.z^2
end

function funcs:Normalized()
	local sqLen = self:GetSquaredLength() 
	if sqLen > 0 then
		local invLength = 1 / _sqrt(sqLen) 
		return module.Vector2D(
			self.x * invLength,
			self.y * invLength)
	end
end

function funcs:Normalize()
	local sqLen = self:GetSquaredLength() 
	if sqLen > 0 then
		local invLength = 1 / _sqrt(sqLen) 
		self.x = self.x * invLength
		self.y = self.y * invLength
	end
end

function funcs:Dot(other)
	return self.x*other.x + self.y*other.y + self.z*other.z
end

function funcs:Cross(other)
	local u1 = self.x	
	local u2 = self.y
	local v1 = other.x
	local v2 = other.y
	return u1*v2 - u2*v1
end

mt.__index = funcs


module.Vector2D = function(ix, iy)
	return setmetatable({x=ix, y=iy}, mt)
end

return module
