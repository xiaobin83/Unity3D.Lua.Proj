local module = {}

local mt = {}

mt.__unm = function(rhs)
	return module.Vector4D(-rhs.x, -rhs.y, -rhs.z, -rhs.w)
end
	
mt.__add = function(lhs, rhs)
	return module.Vector4D(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w)
end

mt.__sub = function(lhs, rhs)
	return module.Vector4D(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w)
end

mt.__mul = function(lhs, rhs)
	if type(rhs) == 'number' then
		return module.Vector4D(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs)
	elseif type(lhs) == 'number' then
		return module.Vector4D(lhs * rhs.x, lhs * rhs.y, lhs * rhs.z, lhs * rhs.w)
	else
		return module.Vector4D(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z, lhs.w * rhs.w)
	end
end

mt.__div = function(lhs, rhs)
	if type(rhs) == 'number' then
		return module.Vector4D(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w/rhs)
	elseif type(lhs) == 'number' then
		return module.Vector4D(lhs / rhs.x, lhs / rhs.y, lhs / rhs.z, lhs / rhs.w)
	else
		return module.Vector4D(lhs.x / rhs.x, lhs.x / rhs.x, lhs.z / rhs.z, lhs.w / rhs.w)
	end
end

mt.__tostring = function(v)
	return "[(X:".. v.x .."),(Y:".. v.y .."),(Z:".. v.z .."),(W:"..v.w..")]"
end

mt.__eq = function(lhs, rhs)
	return (lhs.x == rhs.x) and (lhs.y == rhs.y) and (lhs.z == rhs.z) and (lhs.w == rhs.w)
end

local funcs = {}
function funcs:Dup() 
	return module.Vector4D(self.x, self.y, self.z, self.w)
end

mt.__index = funcs

module.Vector4D = function(ix, iy, iz, iw)
	return setmetatable({x=ix, y=iy, z=iz, w=iw}, mt)
end

return module

