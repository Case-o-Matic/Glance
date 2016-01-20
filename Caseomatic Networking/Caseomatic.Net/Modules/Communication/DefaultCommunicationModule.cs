using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Caseomatic.Net
{
    public class DefaultCommunicationModule : ICommunicationModule
    {
        public T ConvertReceive<T>(byte[] bytes) where T : IPacket
        {
            return PacketConverter.ToPacket<T>(bytes);
        }

        public byte[] ConvertSend<T>(T packet) where T : IPacket
        {
            return PacketConverter.ToBytes<T>(packet);
        }
    }
}
