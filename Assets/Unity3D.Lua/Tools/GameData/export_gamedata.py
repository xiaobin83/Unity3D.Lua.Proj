import os
import sys
import subprocess
import xlrd
import error
import cmd
import type_restrict
import export_sheet
import imp
import modifier_trival



def ExportGameData(_1, _2):
	pass

def exportSheet(sheet, wb, filename, disableColoredMatrix, command):
	context = {
		'field_checker': type_restrict.AllPassChecker,
		'modifier': modifier_trival.NewModifier(),
		'lower_keys': False
	}
	for r in xrange(0, sheet.nrows):
		firstCell = sheet.cell(r, 0)
		if firstCell.ctype != xlrd.XL_CELL_TEXT:
			raise error.ErrInvalidFormat
		firstCellStr = str(firstCell.value)
		if firstCellStr.startswith(':'):
			# is command
			scriptName = firstCellStr[1:]
			if scriptName == 'export_as_colored_matrix':
				if disableColoredMatrix:
					raise error.Err(sheet.name, 'requires export_as_colored_matrix but feature disabled')
			params = []
			for c in xrange(1, sheet.ncols):
				cell = sheet.cell(r, c)
				try:
					cellData = export_sheet.ExportCell(cell, wb)
				except Exception, e:
					print >> sys.stderr, e
					raise error.ErrInvalidFormatAt(sheet.name, r, c)
				if cellData == export_sheet.errCellError:
					raise error.ErrInvalidFormatAt(sheet.name, 0, c)
				if cellData is not None:
					params.append(cellData)
			if scriptName == 'type_restrict':
				if len(params) != 1:
					raise error.Err('export_gamedata.exportSheet', 'type_restrict requires restrict content')
				context['field_checker'] = type_restrict.GetChecker(params[0])
			elif scriptName == 'modifier':
				context['modifier'] = __import__(scriptName + '_' + params[0]).NewModifier(params[1:])
			elif scriptName == 'lower_keys':
				context['lower_keys'] = True
			else:
				exportScript = __import__(scriptName)
				return exportScript.ExportGameData(sheet, wb, r+1, params, context)
		elif firstCellStr == 'ID':
			return export_sheet.ExportGameData(sheet, wb, r, None, context)
		elif command is not None:
			subprocess.call([command, filename, sheet.name])
			return {}
		else:
			raise error.ErrInvalidOperation

def exportGameDataFromFile(filename, _, **kwargs):
	disableColoredMatrix = False
	try:
		wb = xlrd.open_workbook(filename, formatting_info=True)
	except:
		print >> sys.stderr, 'WARNING: do not support formatting_info, export_as_colored_matrix will be disabled. please use .xls instead of .xlsx.'
		disableColoredMatrix = True
		wb = xlrd.open_workbook(filename)
	exported = {}
	command = None
	if kwargs.has_key('command'):
		command = kwargs['command']
	for sheet in wb.sheets():
		data = exportSheet(sheet, wb, filename, disableColoredMatrix, command)
		if data is not None:
			exported[sheet.name] = data
	return exported


class add_path():
	def __init__(self, path):
		self.path = path

	def __enter__(self):
		sys.path.insert(0, self.path)

	def __exit__(self, exc_type, exc_value, traceback):
		sys.path.remove(self.path)

def Export(filename, **kwargs):
	with add_path(os.path.dirname(__file__)):
		return exportGameDataFromFile(filename, None, **kwargs)

if __name__ == '__main__':
	cmd.Export(sys.modules[__name__], exportGameDataFromFile)
