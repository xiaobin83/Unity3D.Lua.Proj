local CondExpr = {}

local _M = {}
_M.__index = _M

function CondExpr.New(expr)
	local p = {
		expr = expr,
	}
	return setmetatable(p, _M) 
end

local _strsub = string.sub

local errShouldBeOnlyCondExprOrEqExpr = "should be only cond expr or eq expr"
local errEmptyCondExpr = "empty cond expr"
local errRequireTwoOperand = 'require two operand'
local errNotCompiled = 'not compiled'
local errInvalidOperand = 'invalid operand'

local _buildObject
local _buildCondExpr
local _buildEqExpr
local _buildSubItemsExpr
local _buildBinaryExpr
local _buildUnaryExpr
local _buildOperand


local binaryOp = {
	['$gt'] = {
		Value = function(self, h)
			return self.opr1:Value(h) > self.opr2:Value(h)
		end
	},
	['$lt'] = {
		Value = function(self, h)
			return self.opr1:Value(h) < self.opr2:Value(h)
		end
	},
	['$ge'] = {
		Value = function(self, h)
			return self.opr1:Value(h) >= self.opr2:Value(h)
		end
	},
	['$le'] = {
		Value = function(self, h)
			return self.opr1:Value(h) <= self.opr2:Value(h)
		end
	},
	['$eq'] = {
		Value = function(self, h)
			return self.opr1:Value(h) == self.opr2:Value(h)
		end
	},
	['$add'] = {
		Value = function(self, h)
			return self.opr1:Value(h) + self.opr2:Value(h)
		end
	},
	['$sub'] = {
		Value = function(self, h)
			return self.opr1:Value(h) - self.opr2:Value(h)
		end
	}
}
for k, v in pairs(binaryOp) do
	binaryOp[k].__index = v
end




local _HashedValueMeta = {
	Value = function(self, h)
		return h(self.hash)
	end
}
_HashedValueMeta.__index = _HashedValueMeta

local _HashedValue = function(hash)
	return setmetatable({hash = hash}, _HashedValueMeta)
end

local _ValueMeta = {
	Value = function(self)
		return self.value
	end
}
_ValueMeta.__index = _ValueMeta

local _Value = function(value)
	return setmetatable({value = value}, _ValueMeta)
end

_buildOperand = function(self, opr)
	local typeOpr = type(opr)
	if  typeOpr == 'string'
	and _strsub(opr, 1, 1) == '#' then
		return _HashedValue(opr)
	elseif typeOpr == 'table' then
		return _buildObject(self, opr)
	else
		return _Value(opr)
	end
end

_buildBinaryExpr = function(self, exprObject, meta)
	if exprObject then	
		if #exprObject ~= 2 then
			return nil, errRequireTwoOperand 
		end
		local opr1 = _buildOperand(self, exprObject[1])
		local opr2 = _buildOperand(self, exprObject[2])
		if opr1 and opr2 then
			return setmetatable(
				{opr1 = opr1, opr2 = opr2},
				meta
			)
		else
			return nil, errInvalidOperand
		end
	end
end


-- $and
local _SubItemsBoolValueMeta = {}
_SubItemsBoolValueMeta.__index = _SubItemsBoolValueMeta
function _SubItemsBoolValueMeta:Value(h)
	local items = assert(self.subItems)
	for _, item in pairs(items) do
		if not item:Value(h) then
			return false
		end
	end
	return true
end


-- $or
local _SubItemsAnyBoolValueMeta = {}
_SubItemsAnyBoolValueMeta.__index = _SubItemsAnyBoolValueMeta
function _SubItemsAnyBoolValueMeta:Value(h)
	for index, item in pairs(self.subItems) do
		if item:Value(h) then
			return index 
		end
	end
end

local _SubItemsAllValueMeta = {}
_SubItemsAllValueMeta.__index = _SubItemsAllValueMeta
function _SubItemsAllValueMeta:Value(h)
	local values = {}
	for _, item in pairs(self.subItems) do
		values[#values + 1] = item:Value(h) 
	end
	return table.unpack(values)
end

local _SubItemsSumValueMeta = {}
_SubItemsSumValueMeta.__index = _SubItemsSumValueMeta
function _SubItemsSumValueMeta:Value(h)
	local totalValue = 0
	for _, item in pairs(self.subItems) do
		 totalValue = totalValue + item:Value(h) 
	end
	return totalValue
end

local subItemsOp = {
	['$and'] = _SubItemsBoolValueMeta,
	['$or'] = _SubItemsAnyBoolValueMeta,
	['$all'] = _SubItemsAllValueMeta,
	['$sum'] = _SubItemsSumValueMeta,
}

_buildSubItemsExpr = function(self, exprObject, meta)
	local items = {}
	if exprObject then
		for _, expr in ipairs(exprObject) do
			local item, err = _buildObject(self, expr)
			if err then 
				return nil, err
			end
			items[#items + 1] = item
		end
		return setmetatable({subItems = items}, meta)
	end
end


local unaryOp = {
	['$not'] = {
		Value = function(self, h)
			return not self.opr:Value(h)
		end
	},
	['$minus'] = {
		Value = function(self, h)
			return -self.opr:Value(h)
		end
	},
	['$value'] = {
		Value = function(self, h)
			return self.opr:Value(h)
		end
	}
}
for k, v in pairs(unaryOp) do
	unaryOp[k].__index = v
end


_buildUnaryExpr = function(self, exprObject, meta)
	if exprObject then
		local opr = _buildOperand(self, exprObject)
		if opr then
			return setmetatable( { opr = opr}, meta )
		end
		return nil, errInvalidOperand
	end
end

_buildCondExpr = function(self, exprObject)

	for op, meta in pairs(subItemsOp) do
		local item, err = _buildSubItemsExpr(self, exprObject[op], meta)
		if err then return nil, err end
		if item then return item end 
	end

	for op, meta in pairs(binaryOp) do
		local item, err = _buildBinaryExpr(self, exprObject[op], meta)
		if err then return nil, err end
		if item then return item end 
	end

	for op, meta in pairs(unaryOp) do
		local item, err = _buildUnaryExpr(self, exprObject[op], meta)
		if err then return nil, err end
		if item then return item end
	end

	return nil, errEmptyCondExpr 
end

_buildEqExpr = function(self, exprObject)
	local items = {}
	for opr1, opr2 in pairs(exprObject) do
		local item, err = _buildBinaryExpr(self, {opr1, opr2}, binaryOp['$eq'])
		if err then
			return nil, err
		end
		items[#items + 1] = item
	end
	return setmetatable({subItems = items}, _SubItemsBoolValueMeta)
end

_buildObject = function(self, exprObject)
	local eqExpr
	local condExpr
	for key, obj in pairs(exprObject) do
		if type(key) == 'string' then
			if _strsub(key, 1, 1) == '$' then
				condExpr = true
				if eqExpr then
					return nil, errShouldBeOnlyCondExprOrEqExpr
				end
			else
				eqExpr = true
				if condExpr then
					return nil, errShouldBeOnlyCondExprOrEqExpr
				end
			end
		end
	end
	if condExpr then
		return _buildCondExpr(self, exprObject)
	else
		return _buildEqExpr(self, exprObject)
	end
end

function _M:Compile()
	if not self.compiledExpr then
		local item, err = _buildObject(self, self.expr)
		if err then
			return false, err
		end
		self.compiledExpr = item
	end
	return true
end

function _M:Value(hashedVarFunc)
	self:Compile()
	if self.compiledExpr then
		local h = function(hash)
			if hashedVarFunc then
				return hashedVarFunc(hash)
			end
			return hash
		end
		return self.compiledExpr:Value(h)
	end
	return false, errNotCompiled 
end


return CondExpr
