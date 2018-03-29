using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace lua
{
	internal class ListPool<T>
	{
		static List<List<T>> freeList = new List<List<T>>();

		public static List<T> Alloc()
		{
			if (freeList.Count == 0)
			{
				return new List<T>();
			}
			else
			{
				var list  = freeList[freeList.Count - 1];
				freeList.RemoveAt(freeList.Count - 1);
				return list;
			}
		}

		public static void Release(List<T> list)
		{
			list.Clear();
			freeList.Add(list);
		}


	}
}
