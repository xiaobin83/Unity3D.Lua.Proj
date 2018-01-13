using System;

namespace networking
{
	public interface IChanModule : IDisposable 
	{
		// main thread
		void Init(Chan chan);

		// worker thread
		void ProcessSendingBytes(ref byte[] data);
		// worker thread
		void ProcessReceivedBytes(ref byte[] data);
	}
}
