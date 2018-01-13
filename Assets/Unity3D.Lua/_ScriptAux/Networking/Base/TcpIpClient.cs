// #define DEBUG_NETWORK
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace networking 
{
    public class TcpIpClient
    {
		const int kWorkingBufferSize = 1024;

        Socket clientSocket = null;

		class ConnStateObject
		{
			public Socket workSocket;
			public OnConnected onConnected;
		}
		public void Connect(string addr, int port, OnConnected onConnected)
		{
			try
			{
				string newIP = addr;
#if UNITY_IOS && !UNITY_EDITOR
				Regex reg =	new	Regex("^\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}$");
				Match match	= reg.Match(ep.ip);
				if (match.Success)
				{
					common.IPv6.ResolveAddress(ep.ip, out newIP, out type);
				}
#endif
				Debug.Log("Start connect " + newIP + ":" + port);
				IPAddress ipAddress;
				if (!IPAddress.TryParse(newIP, out ipAddress))
				{
					if (newIP == "localhost")
					{
						newIP = Dns.GetHostName();
					}
					ipAddress = Dns.GetHostEntry(newIP).AddressList[0];
				}
				clientSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
				var connStateObject = new ConnStateObject()
				{
					workSocket = clientSocket,
					onConnected = onConnected
				};
				var ret = clientSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), connStateObject);
				if (ret == null)
				{
					onConnected(null);
				}
			}
			catch (Exception e)
			{
				Debug.Log("Exception on connecting " + e.Message);
				onConnected(null);
			}
		}

		public void Close()
		{
			if (clientSocket == null)
				return;

			Debug.Log("Close connect");
			try
			{
				clientSocket.Shutdown(SocketShutdown.Both);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to shutdown socket, " + e.Message);
			}
			try
			{
				clientSocket.Close();
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to close socket, " + e.Message);
			}
			clientSocket = null;
		}

		static void ConnectCallback(IAsyncResult ar)
		{
			var s = (ConnStateObject)ar.AsyncState;
			try
			{
				s.workSocket.EndConnect(ar);
				if (s.workSocket.Connected)
				{
					var chan = new Chan(
						(data) => {
							SocketSendingAndReceiving.Send(s.workSocket, data);
						});
					s.onConnected(chan);
					SocketSendingAndReceiving.BeginReceive(
						s.workSocket,
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
				else
				{
					Debug.Log("Client not connected!");
					s.onConnected(null);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Exception on connecting callback " + e.Message);
				s.onConnected(null);
			}
		}
	}
}
