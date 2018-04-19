import xlrd
import sys
import cmd
import error
from export_sheet import ExportCell, ShouldSkipRow, errCellError, GetFieldChecker

def ExportGameData(sheet, book, startRow, params, context):
	while ShouldSkipRow(sheet, startRow):
		startRow = startRow + 1
	col = -1
	keysIndex = {}
	for c in sheet.row(startRow):
		col = col + 1
		if c.ctype != xlrd.XL_CELL_TEXT:
			raise error.ErrInvalidFormatAt(sheet.name, startRow, col)
		keysIndex[col] = c.value

	data = []

	modifier = context['modifier']
	field_checker = context['field_checker']
	for r in xrange(startRow + 1, sheet.nrows):
		if not ShouldSkipRow(sheet, r):
			obj = {}
			data.append(obj)
			for c in xrange(0, sheet.ncols):
				cellName = keysIndex[c]
				cell = sheet.cell(r, c)
				checker = GetFieldChecker(keysIndex[c], sheet.name, r, c, field_checker)
				try:
					cellData = modifier(cellName, ExportCell(cell, book, checker))
				except Exception, e:
					print >> sys.stderr, e
					raise error.ErrInvalidFormatAt(sheet.name, r, c)
				if cellData == errCellError:
					return error.ErrInvalidFormatAt(sheet.name, r, c)
				if cellData is not None:
					obj[cellName] = cellData
		
	return data


if __name__ == '__main__':
	cmd.Export(sys.modules[__name__])