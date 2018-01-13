﻿using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Client : Debugable
{

	const int kMaxSingleCmdBufferSize = 2048;

	networking.TcpIpClient client;

	lua.LuaFunction onConnected;
	lua.LuaFunction onRecv;
	List<byte[]> receivedPackets = new List<byte[]>();

	public void Connect(string addr, int port, lua.LuaFunction onConnected, lua.LuaFunction onRecv)
	{
		client = new networking.TcpIpClient();
		this.onConnected = onConnected.Retain();
		this.onRecv = onRecv.Retain();
		client.Connect(addr, port, OnConnected);
	}


	// called in worker thread
	void HandleRecv(byte[] data)
	{
		var received = new byte[data.Length];  // OPT: pool for receieved packet
		Array.Copy(data, received, data.Length);
		lock(receivedPackets)
		{
			receivedPackets.Add(received);
		}
	}

#if UNITY_EDITOR
	object statLock = new object();
	int bytesSend;
	float lastSendSamplingTime;
	int bytesRecv;
	float lastRecvSamplingTime;

	void OnStatRecv(byte[] data)
	{
		lock (statLock)
		{
			bytesRecv = bytesRecv + data.Length;
		}
	}
	void OnStatSend(byte[] data)
	{
		lock (statLock)
		{
			bytesSend = bytesSend + data.Length;
		}
	}

	float GetSendBandwidth()
	{
		lock (statLock)
		{
			var s = bytesSend / (Time.realtimeSinceStartup - lastSendSamplingTime);
			s = s * 8 / 1024;
			bytesSend = 0;
			lastSendSamplingTime = Time.realtimeSinceStartup;
			return s;
		}
	}

	float GetRecvBandwidth()
	{
		lock (statLock)
		{
			var s = bytesRecv / (Time.realtimeSinceStartup - lastRecvSamplingTime);
			s = s * 8 / 1024;
			bytesRecv = 0;
			lastRecvSamplingTime = Time.realtimeSinceStartup;
			return s;
		}
	}


#endif


	// called in worker thread
	void OnConnected(networking.Chan chan)
	{
		chan.recvHandler += HandleRecv;
#if UNITY_EDITOR
		chan.recvHandler += OnStatRecv;
		chan.onSend += OnStatSend;
#endif
		utils.TaskManager.PerformOnMainThread(
			(obj) => {
				Debug.Assert(onConnected != null);
				onConnected.Invoke(chan);

#if UNITY_EDITOR
				bool left = false;
				if (rect.x < Screen.width*0.5f)
				{
					left = true;
				}
				float x;
				var width = 200;
				if (left)
				{
					x = rect.width + 10;
				}
				else
				{
					x = rect.x - 10 - width;
				}
				var y = rect.y;
				var height = 80;
				var func = lua.LuaFunction.CreateDelegate(_Init.luaVm, new Func<float>(() =>
				{
					return GetSendBandwidth();
				}));
				Editor_AddGraph(
					"client_send", "kbps", func, 10, 0.5f,
					x, y, width, height, Color.red);
				func.Dispose();

				y += height + 5;
				func = lua.LuaFunction.CreateDelegate(_Init.luaVm, new Func<float>(() =>
				{
					return GetRecvBandwidth();
				}));
				Editor_AddGraph(
					"client_recv", "kbps", func, 10, 0.5f,
					x, y, width, height, Color.blue);
				func.Dispose();
#endif
			});
	}

	protected override void OnDestroy()
	{
		if (onConnected != null)
		{
			onConnected.Dispose();
			onConnected = null;
		}
		if (onRecv != null)
		{
			onRecv.Dispose();
			onRecv = null;
		}
		if (client != null)
		{
			client.Close();
			client = null;
		}
		base.OnDestroy();
	}

	protected override void Update()
	{
		Debug.Assert(onRecv != null);
		lock (receivedPackets)
		{
			for (int i = 0; i < receivedPackets.Count; ++i)
			{
				var data = receivedPackets[i];
				Profiler.BeginSample("Client.OnRecv");
				onRecv.Invoke(data);
				Profiler.EndSample();
				// OPT: pool for received packet 
			}
			receivedPackets.Clear();
		}
		base.Update();
	}
}