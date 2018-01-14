local v2d = require 'unity.Math.Vector2'
local v3d = require 'unity.Math.Vector3'
local v4d = require 'unity.Math.Vector4'
local UnityVector2 = csharp.checked_import('UnityEngine.Vector2')
local UnityVector3 = csharp.checked_import('UnityEngine.Vector3')
local UnityVector4 = csharp.checked_import('UnityEngine.Vector4')
local UnityQuaternion = csharp.checked_import('UnityEngine.Quaternion')
local Mathf = csharp.checked_import('UnityEngine.Mathf')

local Math = {}

local kEpsilon = 1e-3


Math.infinity = Mathf.Infinity
Math.pi = 3.14159
Math.invPi = 1 / Math.pi
Math.arc2Deg = 180*Math.invPi
Math.epsilon = kEpsilon


---------------------
local _abs = math.abs
local _floor = math.floor

local _Smooth01_M5 = function(p)
	return p * p * p * (p * (p * 6 - 15) + 10)
end
local _Lerp = function(a, b, t)
	return a - (a - b) * t
end

local _Clamp = function(v, a, b)
	if a and v < a then 
		v = a
	elseif b and v > b then
		v = b
	end
	return v
end


local _Approximate = function(a, b, epsilon)
	epsilon = epsilon or kEpsilon
	return _abs(a - b) < epsilon
end

local _Round = function(x)
	return _floor(x + 0.5)
end

local _random = math.random
local _RandomInUnit = function()
	return -1 + 2 * _random 
end
---------------------


Math.Vector2 = {}
Math.Vector2.zero = v2d.Vector2D(0, 0)
Math.Vector2.one = v2d.Vector2D(1, 1)
setmetatable(
	Math.Vector2, 
	{ 
		__call = function(t, x, y)
			return v2d.Vector2D(x, y)
		end
	})

function Math.Vector2.ToUnity(v)
	return UnityVector2(unpack(v))
end

function Math.Vector2.FromUnity(v)
	return Math.Vector2(v.x, v.y)
end

---------------------

Math.Vector3 = {}
Math.Vector3.zero = v3d.Vector3D(0, 0, 0)
Math.Vector3.zero_Unity = UnityVector3.zero
Math.Vector3.one = v3d.Vector3D(1, 1, 1)
Math.Vector3.one_Unity = UnityVector3.one
Math.Vector3.up = v3d.Vector3D(0, 1, 0)
Math.Vector3.up_Unity = UnityVector3.up
Math.Vector3.right = v3d.Vector3D(1, 0, 0)
Math.Vector3.right_Unity = UnityVector3.right
Math.Vector3.forward = v3d.Vector3D(0, 0, 1)
Math.Vector3.forward_Unity = UnityVector3.forward

Math.Vector3.InsideUnitCircleXZ = function()
	return Math.Vector3(_RandomInUnit(), 0, _RandomInUnit())
end

Math.Vector3.InsideUnitSphere = function()
	return Math.Vector3(_RandomInUnit(), _RandomInUnit(), _RandomInUnit())
end

Math.Vector3.OnUnitSphere = function()
	return Math.Vector3.InsideUnitSphere():Normalize()
end

Math.Vector3.SmoothDampU = function(posU, targetPosU, smoothTime, maxSpeed)
	return UnityVector3.SmoothDamp(posU, targetPosU, nil, smoothTime, maxSpeed or Math.infinity)
end

setmetatable(
	Math.Vector3,
	{
		__call = function(t, x, y, z)
			return v3d.Vector3D(x, y, z)
		end
	})

function Math.Vector3.ToUnity(v)
	return UnityVector3(unpack(v))
end

function Math.Vector3.FromUnity(v)
	return Math.Vector3(v.x, v.y, v.z)
end

function Math.Vector3.FromUnity2(v)
	return Math.Vector3(v.x, v.y, 0)
end

local TU = Math.Vector3.ToUnity
local FU = Math.Vector3.FromUnity

function Math.Vector3.SignedAngle(a, b, axis)
	return UnityVector3.SignedAngle(TU(a), TU(b), TU(axis))
end

function Math.Vector3.Approximate(a, b, epsilon)
	epsilon = epsilon or kEpsilon 
	return  _Approximate(a[1], b[1], epsilon)
		and _Approximate(a[2], b[2], epsilon)
		and _Approximate(a[3], b[3], epsilon)
end
------------------------

Math.Vector4 = {}

setmetatable(
	Math.Vector4, 
	{
		__call = function(t, x, y, z, w)
			return v4d.Vector4D(x, y, z, w)
		end
	}
)

function Math.Vector4.ToUnity(v)
	return UnityVector4(unpack(v))
end

function Math.Vector4.FromUnity(v)
	return Math.Vector4(v.x, v.y, v.z, v.w)
end


function Math.Vector4.Approximate(a, b, epsilon)
	epsilon = epsilon or kEpsilon 
	return  _Approximate(a[1], b[1], epsilon)
		and _Approximate(a[2], b[2], epsilon)
		and _Approximate(a[3], b[3], epsilon)
		and _Approximate(a[4], b[4], epsilon)
end

Math.Vector4.quaternionIdentity = Math.Vector4.FromUnity(UnityQuaternion.identity)

------------------------
Math.Quaternion = {}
Math.Quaternion.identity_Unity = UnityQuaternion.identity
local TU = Math.Vector3.ToUnity
function Math.Quaternion.FromToRotation(from, to)
	return UnityQuaternion.FromToRotation(TU(from), TU(to))
end

function Math.Quaternion.LookRotation(forward, up)
	if up then
		return UnityQuaternion.LookRotation(TU(forward), TU(up))
	else
		return UnityQuaternion.LookRotation(TU(forward))
	end
end

function Math.Quaternion.ToUnity(q)
	return UnityQuaternion(unpack(q)) 
end

function Math.Quaternion.SlerpU(a, b, t)
	return UnityQuaternion.Slerp(a, b, t)
end

function Math.Quaternion.Slerp(a, b, t)
	return UnityQuaternion.Slerp(TU(a), TU(b), t)
end

function Math.Quaternion.AngleAxis(angle, axis)
	return UnityQuaternion.AngleAxis(angle, TU(axis))
end

--------------------------

Math.Lerp = _Lerp
Math.Clamp = _Clamp
Math.Approximate = _Approximate
Math.Round = _Round
Math.Smooth01_M5 = _Smooth01_M5

return Math