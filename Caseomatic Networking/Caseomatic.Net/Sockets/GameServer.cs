using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Caseomatic.Net
{
    public class GameServer<TServerPacket, TClientPacket> : Server<TServerPacket, TClientPacket>
        where TServerPacket : IServerPacket where TClientPacket : IClientPacket
    {
        private readonly UdpClient udpClient;
        private readonly IPEndPoint multicastEndPoint;

        public GameServer(int port, IPAddress multicastAddress)
            : base(port)
        {
            multicastEndPoint = new IPEndPoint(multicastAddress, 0);

            udpClient = new UdpClient(port + 1, AddressFamily.InterNetwork); // Change the port number increment
            udpClient.JoinMulticastGroup(multicastEndPoint.Address);
            // udpClient.Ttl = 42; // Default = 32, higher values need more bandwidth
        }
        ~GameServer()
        {
            udpClient.DropMulticastGroup(multicastEndPoint.Address); // Is this really needed?
            udpClient.Close();
        }

        public void SendMulticastPacket(TServerPacket packet)
        {
            var packetBytes = PacketConverter.ToBytes(packet);
            udpClient.Send(packetBytes, packetBytes.Length, multicastEndPoint);
        }
    }
}
