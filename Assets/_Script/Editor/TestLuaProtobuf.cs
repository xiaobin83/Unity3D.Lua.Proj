using UnityEngine;
using System.Collections;
using NUnit.Framework;

namespace lua.test
{
	[TestFixture]
	public class TestLuaProtobuf
	{
		Lua L;

		[OneTimeSetUp]
		public void SetUp()
		{
			L = new Lua();
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			Lua.CleanMethodCache();
			Lua.CleanMemberCache();
			LuaFunction.CollectActionPool();
			L.Dispose();
		}

		[Test]
		public void TestPersonProto()
		{
			var ret = (LuaTable)L.RunScript1("TestProto");
			var data = (byte[])ret[1];
			var person = (LuaTable)ret[2];
			Assert.AreEqual(1000, person["id"]);
			Assert.AreEqual("Alice", person["name"]);
			Assert.AreEqual("Alice@example.com", person["email"]);
			person.Dispose();
		}
	}
}