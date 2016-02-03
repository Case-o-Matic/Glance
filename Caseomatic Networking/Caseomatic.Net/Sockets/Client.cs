﻿using Caseomatic.Net.Utility;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

namespace Caseomatic.Net
{
    public class Client<TClientPacket, TServerPacket> where TClientPacket : IClientPacket
        where TServerPacket : IServerPacket
    {
        public delegate void OnReceivePacketHandler(TServerPacket packet);
        public event OnReceivePacketHandler OnReceivePacket;

        public delegate void OnConnectionLostHandler();
        public event OnConnectionLostHandler OnConnectionLost;

        private Socket socket;
        private Thread receivePacketsThread;

        private byte[] packetReceivingBuffer;
        private object packetReceivingLock;
        private readonly int port;

        private readonly ConcurrentStack<TServerPacket> receivePacketsSynchronizationStack;

        private ICommunicationModule communicationModule;
        public ICommunicationModule CommunicationModule
        {
            get { return communicationModule; }
            set { communicationModule = value; }
        }

        private bool isConnected;
        public bool IsConnected
        {
            get { return isConnected; }
        }

        public Client(int port)
        {
            this.port = port;
            packetReceivingLock = new object();

            receivePacketsSynchronizationStack = new ConcurrentStack<TServerPacket>();
            communicationModule = new DefaultCommunicationModule();
        }
        ~Client()
        {
            Disconnect();
        }

        public void Connect(IPEndPoint serverEndPoint)
        {
            if (!isConnected)
            {
                OnConnect(serverEndPoint);
            }
        }
        public void Connect(string hostName, int port)
        {
            var serverIpAddresses = Dns.GetHostAddresses(hostName);
            foreach (var serverIpAddress in serverIpAddresses)
            {
                if (serverIpAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    var serverIpEndPoint = new IPEndPoint(serverIpAddress, port);
                    Connect(serverIpEndPoint);

                    return;
                }
            }

            Console.WriteLine("Resolving the hostname \"" + hostName+  "\" not possible: No address of type inter-network v4 found.");
        }

        public void Disconnect()
        {
            if (isConnected)
            {
                OnDisconnect();
            }
        }

        public void FireEvents()
        {
            var events = receivePacketsSynchronizationStack.PopAll();
            for (int i = 0; i < events.Length; i++)
                OnReceivePacket(events[i]);
        }

        public bool HeartbeatConnection(bool repairIfBroken)
        {
            if (isConnected)
            {
                var isReallyConnected = socket.IsConnectionValid();
                if (!isReallyConnected)
                {
                    Console.WriteLine("The server shows no heartbeat" + (repairIfBroken ?
                        ", trying to repair connection" : ", disconnecting"));

                    Disconnect();
                    if (OnConnectionLost != null)
                        OnConnectionLost();

                    if (repairIfBroken)
                        RepairConnection();
                }

                return isReallyConnected;
            }
            else
                return false;
        }


        protected virtual void OnConnect(IPEndPoint serverEndPoint)
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Any, port));

                socket.ConfigureInitialSocket();

                socket.Connect(serverEndPoint);
                packetReceivingBuffer = new byte[socket.ReceiveBufferSize];

                isConnected = true;

                receivePacketsThread = new Thread(ReceivePacketsLoop);
                receivePacketsThread.Start();
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Connecting to " + serverEndPoint.ToString() + " resulted in a problem: " + ex.SocketErrorCode +
                    "\n" + ex.Message);
                Disconnect();
            }
        }
        protected virtual void OnDisconnect()
        {
            try
            {
                isConnected = false;
                receivePacketsThread.Join();

                socket.Close(); // Or use socket.Disconnect(true) instead of close/null?
                Console.WriteLine("Disconnected from the server");
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Disconnecting resulted in a problem: " + ex.SocketErrorCode);
            }
        }

        public void SendPacket(TClientPacket packet)
        {
            try
            {
                if (isConnected)
                {
                    var packetBytes = communicationModule.ConvertSend<TClientPacket>(packet);
                    var sentBytes = socket.Send(packetBytes);

                    if (sentBytes == 0)
                    {
                        Console.WriteLine("Sending to server resulted in a problem: Peer not reached");
                        HeartbeatConnection(false);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Sending to the server resulted in a problem: " + ex.SocketErrorCode +
                    "\n" + ex.Message);
                HeartbeatConnection(true);
            }
        }

        #region Requests
        public TServerAnswer SendRequest<TClientRequest, TServerAnswer>(TClientRequest requestPacket)
            where TClientRequest : TClientPacket, IPacketRequestable where TServerAnswer : TServerPacket
        {
            if (isConnected)
            {
                SendPacket(requestPacket);
                var answerPacket = ReceivePacket();

                return answerPacket != null ?
                    (TServerAnswer)answerPacket : default(TServerAnswer);
            }
            else
                return default(TServerAnswer);
        }

        public bool TrySendRequest<TClientRequest, TServerAnswer>(TClientRequest requestPacket, out TServerAnswer answerPacket)
            where TClientRequest : TClientPacket, IPacketRequestable where TServerAnswer : TServerPacket
        {
            answerPacket = SendRequest<TClientRequest, TServerAnswer>(requestPacket);
            return !answerPacket.Equals(default(TServerAnswer));
        }

        public TServerAnswer SendRequestAsync<TClientRequest, TServerAnswer>(TClientRequest requestPacket)
            where TClientRequest : TClientPacket, IPacketRequestable where TServerAnswer : TServerPacket
        {
            TServerAnswer serverAnswerPacket = default(TServerAnswer);
            object lockObj = new object();
            var rcvThread = new Thread(() =>
            {
                lock (lockObj)
                    serverAnswerPacket = (TServerAnswer)ReceivePacket();
            });
            rcvThread.Start();

            lock (lockObj)
                return serverAnswerPacket;
        }

        public bool TrySendRequestAsync<TClientRequest, TServerAnswer>(TClientRequest requestPacket, out TServerAnswer answerPacket) where TClientRequest : TClientPacket, IPacketRequestable
            where TServerAnswer : TServerPacket
        {
            answerPacket = SendRequestAsync<TClientRequest, TServerAnswer>(requestPacket);
            return answerPacket.Equals(default(TServerAnswer));
        }
        #endregion

        private void ReceivePacketsLoop()
        {
            try
            {
                while (isConnected)
                {
                    var serverPacket = ReceivePacket();

                    if (serverPacket != null)
                    {
                        receivePacketsSynchronizationStack.Push(serverPacket);
                    }
                    // Else: Information about corruption already printed in ReceivePacket()
                    //    Console.WriteLine("The received packet is corrupt.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Receiving in the client loop resulted in an exception: " + ex.Message);
                HeartbeatConnection(true);
            }
        }

        private TServerPacket ReceivePacket()
        {
            try
            {
                lock (packetReceivingLock)
                {
                    var receivedBytes = socket.Receive(packetReceivingBuffer);

                    if (receivedBytes == 0)
                    {
                        Console.WriteLine("Receiving from server resulted in a problem: 0 bytes received");
                        Disconnect();

                        return default(TServerPacket);
                    }
                    else
                    {
                        var packetBuffer = new byte[receivedBytes];
                        Buffer.BlockCopy(packetReceivingBuffer, 0, packetBuffer, 0, receivedBytes);

                        return communicationModule.ConvertReceive<TServerPacket>(packetBuffer);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Receiving from server resulted in a problem: " + ex.SocketErrorCode +
                    "\n" + ex.Message);
                HeartbeatConnection(true);

                return default(TServerPacket);
            }
        }

        private void RepairConnection()
        {
            // The endpoint the client is currently connected to
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            Disconnect();

            var ping = new Ping();
            var pReply = ping.Send(remoteEndPoint.Address, 250);

            if (pReply.Status == IPStatus.Success)
            {
                Console.WriteLine("Reconnecting to server");

                Connect(remoteEndPoint);
                HeartbeatConnection(false);
            }
            else
            {
                Console.WriteLine("IP not pingable. Result: " + pReply.Status + ", dropping off");
            }
        }
    }
}
