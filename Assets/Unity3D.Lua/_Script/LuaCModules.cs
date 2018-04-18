using System;
using System.Runtime.InteropServices;

namespace lua
{
	public class CModules
	{
#if UNITY_EDITOR || UNITY_ANDROID
		public const string PROTOBUF = "pb";
		public const string RAPIDJSON = "rapidjson";
		public const string BSON = "bson";
#elif !UNITY_EDITOR || UNITY_IPHONE
		public const string PROTOBUF = "__Internal";
		public const string RAPIDJSON = "__Internal";
		public const string BSON = "__Internal";
#endif
		[DllImport(PROTOBUF, EntryPoint = "luaopen_pb", CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaopen_pb(IntPtr L);

		[DllImport(RAPIDJSON, EntryPoint = "luaopen_rapidjson", CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaopen_rapidjson(IntPtr L);

		[DllImport(BSON, EntryPoint = "luaopen_bson", CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaopen_bson(IntPtr L);
	}

}
