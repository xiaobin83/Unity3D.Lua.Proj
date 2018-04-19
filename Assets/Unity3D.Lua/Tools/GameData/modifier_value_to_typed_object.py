import types
import error

def to_typed_object(value):
	if isinstance(value, types.IntType):
		return {'intvalue': value}
	elif isinstance(value, types.FloatType):
		return {'floatvalue': value}
	elif isinstance(value, types.StringTypes):
		return {'stringvalue': value}
	elif isinstance(value, types.BooleanType):
		return {'boolvalue': value}
	raise error.Err('modifier_value_to_typed_object', 'unsupported ' +  str(type(value)))

def NewModifier(params):
	checkName = len(params) > 0
	def modifier(name, value):
		if checkName:
			if name in params:
				return to_typed_object(value)
			return value
		return to_typed_object(value)
	return modifier
