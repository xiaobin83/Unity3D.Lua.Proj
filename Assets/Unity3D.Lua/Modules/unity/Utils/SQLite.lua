local SQLite = {}

local sqlite = require 'sqlite'

function SQLite.ConnectToResourceDB(filename)
	return sqlite.ConnectToResourceDB(filename)
end

local xpcall = xpcall
local _MsgHandler = function(err)
	_LogE(msg .. '\n' .. debug.traceback())
end

function SQLite.Query(conn, queryString, readerCallback)
	local func = function(reader) 
		local ok, shouldBreak = xpcall(readerCallback, _MsgHandler, reader)
		if not ok then
			-- break query
			shouldBreak = true
		end
		shouldBreak = shouldBreak or false
		return shouldBreak 
	end
	sqlite.ExecuteReader(conn, queryString, func)
end

function SQLite.Close(conn)
	sqlite.Close(conn)
end


return SQLite
