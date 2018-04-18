
def rowName(r):
	return r + 1

def colName(c):
	n = ''
	ordA = ord('A')
	while True:
		n = chr(ordA + c % 26) + n
		c = c/26 - 1
		if c < 0:
			break
	return n


ErrInvalidFormat = Exception('invalid format')

def ErrInvalidFormatAt(sheetName, row, col):
	return Exception('invalid format at {} index {}-{}'.format(sheetName, rowName(row), colName(col)))


ErrInvalidOperation = Exception('invalid operation')

def ErrDuplicatedID(ID, sheetName, row):
	return Exception('duplicated ID {} at {} row {}'.format(ID, sheetName, rowName(row)))

def Err(sheetName, msg):
	return Exception('{} at {}'.format(msg, sheetName))
