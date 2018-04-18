import error

'''
restrict = {
    'id': {'required':True}, 
    'type': {'required':True},
    'bundle': {'type':'object'},
    'exchange':{'type':'object'},
    'tradable':{'type':'bool'},
    'stackable':{'type':'bool'},
    'tags':{'type':'object'},
    'hidden': {'type':'bool'},
    'quality': {'type':'int'},
    'properties': {'type':'object'},
    'always_merge': {'type': 'bool'},
    'dont_autoremove' : {'type':'bool'}
}
'''


def GetChecker(content):
	if not isinstance(content, dict):
		raise error.Err('type_restrict', 'content should be dict')
	restrict = content
	def Checker(name, value, sheetname, row, col):
		if not restrict.has_key(name):
			return value
		r = restrict[name]
		if r.has_key('required'):
			if r['required']:
				if value is None:
					raise error.ErrInvalidFormatAt(sheetname, row, col)
		if r.has_key('type'):
			requiredType = r['type']
			if requiredType == 'object':
				if value is not None and not isinstance(value, (list, dict)):
					raise error.ErrInvalidFormatAt(sheetname, row, col)
			elif requiredType == 'int':
				if value is not None and not isinstance(value, int):
					raise error.ErrInvalidFormatAt(sheetname, row, col)
			elif requiredType == 'number':
				if value is not None and not isinstance(value, (int, float)):
					raise error.ErrInvalidFormatAt(sheetname, row, col)
			elif requiredType == 'bool':
				if value is not None and not isinstance(value, bool):
					raise error.ErrInvalidFormatAt(sheetname, row, col)
		return value
	return Checker

def AllPassChecker(_1, value, _3, _4, _5):
	return value
