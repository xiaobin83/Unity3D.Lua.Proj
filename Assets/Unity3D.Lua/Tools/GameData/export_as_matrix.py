import xlrd
import sys
import cmd
import error
from export_sheet import ExportCell, ShouldSkipRow, errCellError


def findChecker(params, exportAsSparseMatrix, sheet, book):
	if len(params) > 0:
		if params[0] == 'int':
			def checkInt(cell, r, c):
				if cell.ctype == xlrd.XL_CELL_NUMBER:
					if cell.value.is_integer():
						return int(cell.value)
				elif cell.ctype == xlrd.XL_CELL_EMPTY or cell.ctype == xlrd.XL_CELL_BLANK:
					return 0
				raise error.ErrInvalidFormatAt(sheet.name, r, c)
			return checkInt
		elif params[0] == 'number':
			def checkNumber(cell, r, c):
				if cell.ctype == xlrd.XL_CELL_NUMBER:
					return cell.value
				elif cell.ctype == xlrd.XL_CELL_EMPTY or cell.ctype == xlrd.XL_CELL_BLANK:
					return 0 
				raise error.ErrInvalidFormatAt(sheet.name, r, c)
			return checkNumber
		elif params[0] == 'string':
			def checkString(cell, r, c):
				try:
					cellData = ExportCell(cell, book)
				except Exception, e:
					print >> sys.stderr, e
					raise error.ErrInvalidFormatAt(sheet.name, r, c)
				if cellData == errCellError:
					raise error.ErrInvalidFormatAt(sheet.name, r, c)
				if not exportAsSparseMatrix:
					if cellData is None:
						cellData = ''
				return str(cellData)
			return checkString
		elif params[0] == 'bool':
			def checkBoolean(cell, r, c):
				try:
					cellData = ExportCell(cell, book)
				except Exception, e:
					print >> sys.stderr, e
					raise error.ErrInvalidFormatAt(sheet.name, r, c)
				if cellData == errCellError or not isinstance(cellData, bool):
					raise error.ErrInvalidFormatAt(sheet.name, r, c)
				return cellData
			return checkBoolean
	if exportAsSparseMatrix:
		def checkNonError(cell, r, c):
			try:
				cellData = ExportCell(cell, book)
			except Exception, e:
				print >> sys.stderr, e
				raise error.ErrInvalidFormatAt(sheet.name, r, c)
			if cellData == errCellError:
				raise error.ErrInvalidFormatAt(sheet.name, r, c)
			return cellData
		return checkNonError

	raise error.Err(sheet.name, 'non-sparse matrix have no type restrict')


def ExportGameData(sheet, book, startRow, params, context):
	while ShouldSkipRow(sheet, startRow):
		startRow = startRow + 1
	exportAsSparseMatrix = False
	if len(params) > 0:
		if 'sparse' == params[0]:
			exportAsSparseMatrix = True
			params = params[1:]

	ch = findChecker(params, exportAsSparseMatrix, sheet, book)
	modifier = context['modifier']
	def checker(cell, r, c):
		value = ch(cell, r, c)
		return modifier(None, context['field_checker']('$any', value, sheet.name, r, c))

	if exportAsSparseMatrix:
		matrix = {}
		for r in xrange(startRow, sheet.nrows):
			if not matrix.has_key(r):
				matrix[r] = {}
			for c in xrange(0, sheet.ncols):
				cellData = checker(sheet.cell(r, c), r, c)
				if cellData is not None:
					matrix[r][c] = cellData
	else:
		matrix = []
		for r in xrange(startRow, sheet.nrows):
			for c in xrange(0, sheet.ncols):
				matrix.append(checker(sheet.cell(r, c), r, c))
		matrix = {
			'stride': sheet.ncols,
			'matrix': matrix
		}
	return matrix


if __name__ == '__main__':
	cmd.Export(sys.modules[__name__])
