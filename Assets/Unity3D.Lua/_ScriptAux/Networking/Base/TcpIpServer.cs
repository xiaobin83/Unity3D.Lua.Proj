// #define DEBUG_NETWORK
using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Threading;  
using UnityEngine;

namespace networking
{
	public class TcpIpServer 
	{
		// Thread signal.  
		static ManualResetEvent allDone = new ManualResetEvent(false);

		static object socketLock = new object();
		static Socket socket;

		public static void Serve(int port, OnConnected onConnected)
		{
			var entry = Dns.GetHostEntry(Dns.GetHostName());
			var addr = entry.AddressList[0];
			IPEndPoint localEndPoint = new IPEndPoint(addr, port);
			socket = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(localEndPoint);
			socket.Listen(8);
			ThreadPool.QueueUserWorkItem(
				(_) =>
				{
					BeginAccept(onConnected);
				});
		}

		public static void Shutdown()
		{
			lock (socket)
			{
				try
				{
					if (socket != null)
					{
						socket.Close();
					}
				}
				catch (Exception e)
				{
					Debug.LogError(e);
				}
				socket = null;
			}

		}

		class AcceptStateObject
		{
			public OnConnected onConnected;
		} 

		static void BeginAccept(OnConnected onConnected)
		{
			try
			{
				var state = new AcceptStateObject()
				{
					onConnected = onConnected
				};
				while (true)
				{
					// Set the event to nonsignaled state.  
					allDone.Reset();
					// Start an asynchronous socket to listen for connections.  
					Debug.Log("Waiting for a connection...");
					lock (socketLock)
					{
						if (socket != null)
							socket.BeginAccept(new AsyncCallback(AcceptCallback), state);
						else
							break;
					}
					// Wait until a connection is made before continuing.  
					allDone.WaitOne();
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
			Debug.Log("Server stopped.");
		}

		static void AcceptCallback(IAsyncResult ar)
		{
			allDone.Set();
			var s = (AcceptStateObject)ar.AsyncState;
			Socket handler = null;	
			lock (socketLock)
			{
				if (socket != null)
					handler = socket.EndAccept(ar);
			}
			if (handler == null)
			{
				s.onConnected(null);
				return;
			}
			Debug.Log("Client connectioned");
			var chan = new Chan(
				(data) => {
					SocketSendingAndReceiving.Send(handler, data);
				});
			s.onConnected(chan);
			SocketSendingAndReceiving.BeginReceive(
				handler, 
				(data, bytesReceived) =>
				{
					if (bytesReceived > 0)
					{
						chan.HandleRecv(data, bytesReceived);
					}
					else
					{
						chan.Break(Chan.BreakReason.ReceiveZero);
					}
				});

		}
	}
}
