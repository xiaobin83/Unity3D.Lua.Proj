using System;
using System.Runtime.InteropServices;

namespace lua
{
	public class CModules
	{
#if UNITY_EDITOR || UNITY_ANDROID
		public const string PROTOBUF = "pb";
#elif !UNITY_EDITOR || UNITY_IPHONE
		public const string PROTOBUF = "__Internal";
#endif
		[DllImport(PROTOBUF, EntryPoint = "luaopen_pb", CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaopen_pb(IntPtr L);
	}

}
