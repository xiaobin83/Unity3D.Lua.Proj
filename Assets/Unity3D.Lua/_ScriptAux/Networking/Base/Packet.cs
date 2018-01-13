using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace networking
{
	public class Packet
	{
		public static byte[] TryGetCompleteData(utils.RingPacket ringPacket)
		{
			byte[] data = null;
			if (ringPacket.Length < 4)
				return null;

			byte[] bytesLen = null;
			if (!ringPacket.ReadBytes(out bytesLen, 4))
				return null;

			int len = 
				  ((int)bytesLen[0] << 24)
				+ ((int)bytesLen[1] << 16)
				+ ((int)bytesLen[2] << 8 )
				+ ((int)bytesLen[3]      );
			if (len == 0)
			{
				data = bytesLen;
				ringPacket.TrimStart(4);
				return data;
			}
			else
			{
				if (ringPacket.Length < len + 4)
					return null;
				if (!ringPacket.ReadBytes(out data, len + 4))
					return null;
				ringPacket.TrimStart(len + 4);
				return data;
			}
		}

	}
}
