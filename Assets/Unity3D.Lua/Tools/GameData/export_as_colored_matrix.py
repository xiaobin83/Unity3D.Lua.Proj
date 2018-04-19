import sys
import cmd
import error
from export_sheet import ExportCell, ShouldSkipRow, errCellError

def getColor(sheet, book, r, c):
	xf_idx = sheet.cell_xf_index(r, c)
	return book.xf_list[xf_idx].background.pattern_colour_index

def ExportGameData(sheet, book, startRow, _params, _context):
	while ShouldSkipRow(sheet, startRow):
		startRow = startRow + 1
	# get colors
	# edge
	edgeColor = getColor(sheet, book, startRow, 0)

	# color name
	colors = {}
	for c in xrange(1, sheet.ncols):
		color = getColor(sheet, book, startRow, c)
		cell = sheet.cell(startRow, c)
		colors[color] = str(cell.value)

	# find edge
	startRow = startRow + 1
	stride = 0
	for c in xrange(0, sheet.ncols):
		color = getColor(sheet, book, startRow, c)
		if color != edgeColor:
			stride = c - 1
			break
	if stride == 0:
		stride = sheet.ncols
	if stride <= 0:
		raise error.Err(sheet.name, 'edge error')

	matrix = []
	comments = {}
	for r in xrange(startRow + 1, sheet.nrows):
		color = getColor(sheet, book, r, 0)
		if color != edgeColor:
			break
		for c in xrange(1, stride):
			color = getColor(sheet, book, r, c)
			if not colors.has_key(color):
				raise error.ErrInvalidFormatAt(sheet.name, r, c)
			matrix.append(color)
			cell = sheet.cell(r, c)
			try:
				cellData = ExportCell(cell, book)
			except Exception, e:
				print >> sys.stderr, e
				raise error.ErrInvalidFormatAt(sheet.name, r, c)
			if cellData == errCellError:
				raise error.ErrInvalidFormatAt(sheet.name, r, c)
			if cellData is not None:
				comments[len(matrix) - 1] = cellData


	return {
		'stride': stride,
		'colors': colors,
		'matrix': matrix,
		'comments': comments
	}





if __name__ == '__main__':
	cmd.Export(sys.modules[__name__])