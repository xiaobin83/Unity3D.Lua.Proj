local pb = require 'pb'
local ResMgr = csharp.checked_import('utils.ResMgr')
pb.load(ResMgr.LoadBytes('pb/person'))
local PhoneType = pb.findtype('Phone.PHONE_TYPE'):to_enums()
local person = {}
person.id = 1000
person.name = "Alice"
person.email = "Alice@example.com"
person.phones = {
	num = "2147483647",
	type = PhoneType.HOME
}
local data = pb.encode('Person', person)
local msg = pb.decode('Person', data)

return {csharp.as_bytes(data), msg}



