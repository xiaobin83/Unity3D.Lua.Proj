using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace networking
{
	public class Chan
	{
		public enum BreakReason
		{
			ReceiveZero,
			Exception,
			ChanModule,
		}

		public delegate void DataHandler(byte[] data);
		public delegate void ErrorHandler(BreakReason reason, object obj);

		public string name = "chan";

		DataHandler sendHandler;
		public event DataHandler onSend;
		public event DataHandler recvHandler;
		public event ErrorHandler errorHandler;

		utils.RingPacket ringPacket = new utils.RingPacket();
		IChanModule chanModule;

		public Chan(DataHandler sendHandler)
		{
			this.sendHandler = sendHandler;
		}

		public void HandleRecv(byte[] data, int size)
		{
			//Debug.LogFormat("[CHAN]{0} received {1} bytes", name, size);
			ringPacket.WriteBytes(data, 0, size);
			while (true)
			{
				byte[] completeData = Packet.TryGetCompleteData(ringPacket);
				if (completeData != null)
				{
					lock (this)
					{
						if (chanModule != null)
						{
							 chanModule.ProcessReceivedBytes(ref completeData);
						}
						if (recvHandler != null)
						{
							recvHandler(completeData);
						}
					}
				}
				else
				{
					// no complete packet
					break;
				}
			}

		}


		public void Send(byte[] data)
		{
			//Debug.LogFormat("[CHAN]{0} send {1} bytes", name, data.Length);
			if (chanModule != null)
			{
				chanModule.ProcessSendingBytes(ref data);
			}
			if (sendHandler != null)
			{
				sendHandler(data);
				if (onSend != null)
					onSend(data);
			}
		}


		public void Break(BreakReason reason, object obj = null)
		{
			Debug.LogWarning("Chan broken: " + reason.ToString());
			if (errorHandler != null)
			{
				errorHandler(reason, obj);
			}
		}

		public void SetChanModule(IChanModule cm)
		{
			if (chanModule != null)
				chanModule.Dispose();
			chanModule = cm;
			chanModule.Init(this);
		}


	}

	public delegate void OnConnected(Chan chan);
}
