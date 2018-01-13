﻿/*
MIT License

Copyright (c) 2016 xiaobin83

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Reflection;
using System.Linq;

namespace lua
{
	public class LuaEditorGetPathDelegateAttribute : Attribute
	{
		public delegate string[] GetPathDelegate();
		static bool delegateCached = false;
		static GetPathDelegate cachedDelegate;
		public static GetPathDelegate GetDelegate()
		{
			if (delegateCached) return cachedDelegate;
			delegateCached = true;

			System.Collections.Generic.IEnumerable<Type> allTypes = null;
			try
			{
				allTypes = Assembly.Load("Assembly-CSharp").GetTypes().AsEnumerable();
			}
			catch
			{ }
			try
			{
				var typesInPlugins = Assembly.Load("Assembly-CSharp-firstpass").GetTypes();
				if (allTypes != null)
					allTypes = allTypes.Union(typesInPlugins);
				else
					allTypes = typesInPlugins;
			}
			catch
			{ }
			if (allTypes == null)
			{
				Config.LogError("ScritpLoader not found!");
				return null;
			}

			var methods = allTypes.SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public))
				.Where(m => m.GetCustomAttributes(typeof(LuaEditorGetPathDelegateAttribute), false).Length > 0)
				.ToArray();
			if (methods.Length > 0)
			{
				var m = methods[0];
				cachedDelegate = (GetPathDelegate)Delegate.CreateDelegate(typeof(GetPathDelegate), m);
			}
			return cachedDelegate;
		}
	}
}
