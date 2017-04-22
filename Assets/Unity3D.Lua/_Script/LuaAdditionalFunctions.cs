using UnityEngine;
using System.Collections;

namespace lua
{
	public class LuaAdditionalFunctions
	{
		const string kLuaStub_Math_Clamp01 = "function(v) return (v > 1 and 1) or (v < 0 and 0 or v) end";

		struct FuncReg
		{
			public string name;
			public LuaFunction f;
			public FuncReg(string name, LuaFunction f)
			{
				this.name = name;
				this.f = f;
			}
		};

		static void RegisterAndDisposeFuncs(LuaTable t, FuncReg[] regs)
		{
			foreach (var r in regs)
			{
				t[r.name] = r.f;
				r.f.Dispose();
			}
		}

		public static void Open(Lua L)
		{
			// math	lib
			Api.lua_getglobal(L, "math");
			using (var t = LuaTable.MakeRefTo(L, -1))
			{
				var funcs = new FuncReg[]
				{
					new FuncReg("clamp01", LuaFunction.NewFunction(
						L, 
						"function(v) return	(v > 1 and 1) or (v	< 0	and	0 or v)	end")),
					new	FuncReg("lerp", LuaFunction.NewFunction(
						L,
						"function(a, b, f) return (a-b)*f + b end"))
				};
				RegisterAndDisposeFuncs(t, funcs);
			}
			Api.lua_pop(L, 1);

		}
	}
}
