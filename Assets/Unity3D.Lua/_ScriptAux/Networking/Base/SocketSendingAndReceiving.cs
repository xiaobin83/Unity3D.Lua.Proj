using System;  
using System.Net.Sockets;  
using UnityEngine;

namespace networking
{
	public static class SocketSendingAndReceiving
	{
		public delegate void RecvHandler(byte[] data, int bytesReceived);

		class StateObject
		{
			public Socket workSocket;
			public const int BufferSize = 1024;
			public byte[] buffer = new byte[BufferSize];
			public RecvHandler onRecv;
		}

		public static void BeginReceive(Socket handler, RecvHandler onRecv)
		{
			var stateObject = new StateObject()
			{
				workSocket = handler,
				onRecv = onRecv
			};
			try
			{
				handler.BeginReceive(
					stateObject.buffer, 0, StateObject.BufferSize, 0,
					new AsyncCallback(ReadCallback), stateObject);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				onRecv(null, 0);
			}
		}

		static void ReadCallback(IAsyncResult ar)
		{
			var state = (StateObject)ar.AsyncState;
			var onRecv = state.onRecv;
			var handler = state.workSocket;
			int bytesRead = handler.EndReceive(ar);
			if (bytesRead > 0)
			{
				onRecv(state.buffer, bytesRead);
				try
				{
					handler.BeginReceive(
						state.buffer, 0, StateObject.BufferSize, 0,
						new AsyncCallback(ReadCallback), state);
				}
				catch (Exception e)
				{
					Debug.LogError(e.Message);
					onRecv(null, 0);
				}
			}
			else
			{
				onRecv(null, 0);
			}
		}

		public static void Send(Socket handler, byte[] data)
		{
			try
			{
				handler.BeginSend(data, 0, data.Length, 0,
					new AsyncCallback(SendCallback), handler);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		static void SendCallback(IAsyncResult ar)
		{
			try
			{
				Socket handler = (Socket)ar.AsyncState;
				handler.EndSend(ar);
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

	}
}
