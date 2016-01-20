using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Caseomatic.Net
{
    public interface ICommunicationModule
    {
        T ConvertReceive<T>(byte[] bytes) where T : IPacket;
        byte[] ConvertSend<T>(T packet) where T : IPacket;
    }
}
