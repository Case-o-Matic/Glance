using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Caseomatic.Net
{
    public class NetDebugCommunicationModule : ICommunicationModule
    {
        private static Random random = new Random(DateTime.Now.Millisecond);
        private readonly ICommunicationModule underlyingCommModule;

        private NetDebugProperties properties;
        public NetDebugProperties Properties
        {
            get { return properties; }
        }

        public NetDebugCommunicationModule(ICommunicationModule underlyingCommModule)
        {
            this.underlyingCommModule = underlyingCommModule;
            properties = new NetDebugProperties();
        }

        public T ConvertReceive<T>(byte[] bytes) where T : IPacket
        {
            bytes = ApplyReceiveProperties(bytes);

            var packet = underlyingCommModule.ConvertReceive<T>(bytes);
            Log("Received packet of type " + packet.GetType().FullName);

            return packet;
        }

        public byte[] ConvertSend<T>(T packet) where T : IPacket
        {
            //packet = ApplySendProperties(packet);

            Log("Sending packet of type " + packet.GetType().FullName);
            return underlyingCommModule.ConvertSend(packet);
        }

        private byte[] ApplyReceiveProperties(byte[] bytes)
        {
            if (properties.simulateLag)
                Thread.Sleep(properties.lagIntensitivity);

            if (properties.simulatePacketDrop)
            {
                var chance = random.Next(0, 100);
                if (chance <= properties.packetDropIntensitivityPercentage)
                    return null;
            }

            return bytes;
        }
        private T ApplySendProperties<T>(T packet) where T : IPacket
        {
            return packet;
        }

        private void Log(string text)
        {

        }
    }

    public class NetDebugProperties
    {
        public bool fullLog;

        public bool simulateLag;
        public int lagIntensitivity = 100;

        public bool simulateDuplicates;
        public int duplicateIntensitivityPercentage = 50;

        public bool simulatePacketDrop;
        public int packetDropIntensitivityPercentage = 20;
    }
}
