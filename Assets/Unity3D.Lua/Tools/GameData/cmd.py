import argparse
import json
import os
import codecs 
import xlrd

def exportGameDataFromFile(filename, exportFn, **kwargs):
	wb = xlrd.open_workbook(filename)
	exported = {}
	for sheet in wb.sheets():
		data = exportFn(sheet, wb, 0)
		exported[sheet.name] = data
	return exported

def Export(m, exportFromFileFn=None):
	if exportFromFileFn is None:
		exportFromFileFn = exportGameDataFromFile
	parser = argparse.ArgumentParser()
	parser.add_argument('-i', '--input', required=True)
	parser.add_argument('-o', '--output')
	parser.add_argument('-c', '--command')
	args = parser.parse_args()
	filename = args.input
	output = args.output
	if not output:
		output = os.path.splitext(filename)[0] + '.json'
	if os.path.exists(filename):
		exported = exportFromFileFn(filename, m.ExportGameData, command=args.command)
		f = codecs.open(output, mode='w', encoding='utf-8')
		f.write(json.dumps(exported, ensure_ascii=False))
		f.close()