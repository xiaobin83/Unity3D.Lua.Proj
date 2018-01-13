using System.Collections;
using System;

namespace utils 
{
	public class RingPacket
	{
		const int PACKET_LIMIT = 10 * 1024 * 1024;
		const int INIT_LEN = 102400;

		public class LengthExceededException : Exception
		{
			public LengthExceededException(int len)
				: base(string.Format("required	{0}	bytes but {1} bytes	limited", len, PACKET_LIMIT))
			{
			}
		}

		private int start;
		private int end;
		public byte[] data;
		public int Length
		{
			get
			{
				if (start <= end)
					return end - start;
				else
					return data.Length - start + end;
			}
		}

		public RingPacket(int len = INIT_LEN)
		{
			start = 0;
			end = 0;
			data = new byte[len];
		}

		public void Reset()
		{
			start = 0;
			end = 0;
		}

		private void Extend(int len)
		{
			if (data.Length > PACKET_LIMIT)
				throw new LengthExceededException(data.Length);
			len = (len / data.Length + 2) * data.Length;
			byte[] v = new byte[len];
			int lll = Length;
			ReadBytes_(ref v, Length);
			end = Length;
			start = 0;
			data = v;
		}

		public bool ReadBytes(out byte[] v, int len) // ReadBytes will not move position, call TrimStart if necessary.
		{
			if (Length >= len)
			{
				v = new byte[len];
				return ReadBytes_(ref v, len);
			}
			v = null;
			return false;
		}

		private bool ReadBytes_(ref byte[] v, int len)
		{
			if (Length >= len)
			{
				int left = data.Length - start;
				if (left >= len)
					Array.Copy(data, start, v, 0, len);
				else
				{
					Array.Copy(data, start, v, 0, left);
					Array.Copy(data, 0, v, left, len - left);
				}
				return true;
			}
			return false;
		}

		public void TrimStart(int len)
		{
			start += len;
			if (start >= data.Length)
				start = start - data.Length;
		}

		public void WriteByte(byte v)
		{
			WriteBytes(new byte[] { v });
		}

		public void WriteBytes(byte[] v)
		{
			WriteBytes(v, 0, v.Length);
		}

		public void WriteBytes(byte[] v, int offset)
		{
			WriteBytes(v, offset, v.Length);
		}

		public void WriteBytes(byte[] v, int offset, int length)
		{
			int len = v.Length - offset;
			len = Math.Min(len, length);
			int left = data.Length - Length - 1;
			if (left < len)
				Extend(len);
			int leftToEnd = 0;
			if (start <= end)
				leftToEnd = data.Length - end;
			else
				leftToEnd = start - end;
			if (len <= leftToEnd)
			{
				Array.Copy(v, offset, data, end, len);
				end += len;
				if (end == data.Length)
					end = 0;
			}
			else
			{
				Array.Copy(v, offset, data, end, leftToEnd);
				Array.Copy(v, offset + leftToEnd, data, 0, len - leftToEnd);
				end = len - leftToEnd;
			}
		}
	}
}