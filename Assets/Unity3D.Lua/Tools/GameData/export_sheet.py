import xlrd
import sys
import cmd
import error
import datetime
import json

errCellError = object()


def GetFieldChecker(name, sheetname, r, c, type_restrict_checker):
	def checker(value):
		if type_restrict_checker is None:
			return value
		return type_restrict_checker(name, value, sheetname, r, c)
	return checker

def allPassChcker(value):
	return value

def ExportCell(cell, book, checker=None):
	if checker is None:
		checker = allPassChcker
	if cell.ctype == xlrd.XL_CELL_TEXT:
		if len(cell.value) > 0:
			if cell.value.startswith('$json'):
				return checker(json.loads(cell.value[5:]))
			return checker(cell.value)
	elif cell.ctype == xlrd.XL_CELL_BLANK or cell.ctype == xlrd.XL_CELL_EMPTY:
		return checker(None)
	elif cell.ctype == xlrd.XL_CELL_ERROR:
		return errCellError
	elif cell.ctype == xlrd.XL_CELL_NUMBER:
		if cell.value.is_integer():
			return checker(int(cell.value))
		return cell.value
	elif cell.ctype == xlrd.XL_CELL_DATE:
		date = datetime.datetime(*xlrd.xldate_as_tuple(cell.value, book.datemode))
		return checker(str(date))
	elif cell.ctype == xlrd.XL_CELL_BOOLEAN:
		return checker(cell.value == 1)


def ShouldSkipRow(sheet, row):
	cell = sheet.cell(row, 0)
	if cell.ctype == xlrd.XL_CELL_TEXT:
		if cell.value.startswith(':'):
			print >> sys.stdout, 'skipping row {}'.format(cell.value)
			return True
	return False

def ExportGameData(sheet, book, startRow, params, context):
	while ShouldSkipRow(sheet, startRow):
		startRow = startRow + 1

	if sheet.cell(startRow, 0).value != 'ID':
		raise error.ErrInvalidFormat
	col = -1
	keysIndex = {}
	for c in sheet.row(startRow):
		col = col + 1
		if c.ctype == xlrd.XL_CELL_BLANK or c.ctype == xlrd.XL_CELL_EMPTY:
			break
		if c.ctype != xlrd.XL_CELL_TEXT:
			raise error.ErrInvalidFormatAt(sheet.name, 0, col)
		keysIndex[col] = c.value

	data = {}

	lowerKeys = context['lower_keys']
	field_checker = context['field_checker']
	modifier = context['modifier']
	for r in xrange(startRow + 1, sheet.nrows):
		if not ShouldSkipRow(sheet, r):
			cell = sheet.cell(r, 0)
			if cell.ctype != xlrd.XL_CELL_TEXT:
				raise error.ErrInvalidFormatAt(sheet.name, r, 0)
			checker = GetFieldChecker(keysIndex[0], sheet.name, r, 0, field_checker)
			try:
				key = ExportCell(cell, book, checker)
			except Exception, e:
				print >> sys.stderr, e
				raise error.ErrInvalidFormatAt(sheet.name, r, 0)
			if data.has_key(key):
				raise error.ErrDuplicatedID(key, sheet.name, r)

			obj = {}
			data[key] = obj
			for c in xrange(0, sheet.ncols):
				if not keysIndex.has_key(c):
					break
				cellName = keysIndex[c]
				cell = sheet.cell(r, c)
				checker = GetFieldChecker(cellName, sheet.name, r, c, field_checker)
				try:
					cellData = modifier(cellName, ExportCell(cell, book, checker))
				except Exception, e:
					print >> sys.stderr, e
					raise error.ErrInvalidFormatAt(sheet.name, r, c)
				if cellData == errCellError:
					raise error.ErrInvalidFormatAt(sheet.name, r, c)
				if cellData is not None:
					if lowerKeys:
						obj[cellName.lower()] = cellData
					else:
						obj[cellName] = cellData
	return data


if __name__ == '__main__':
	cmd.Export(sys.modules[__name__])
