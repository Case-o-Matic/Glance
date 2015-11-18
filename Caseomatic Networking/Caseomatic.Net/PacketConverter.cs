using NetSerializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Caseomatic.Net
{
    public static class PacketConverter
    {
        private static Serializer serializer;
        public static void Initialize(Assembly packetDefinitionAssembly)
        {
            var packetTypes = packetDefinitionAssembly.GetTypes()
                .Where(t => t.GetInterface(typeof(IPacket).Name) != null);

            serializer = new Serializer(packetTypes);
        }

        public static byte[] ToBytes<T>(T packet) where T : IPacket
        {
            using (var mStream = new MemoryStream())
            {
                serializer.Serialize(mStream, packet);
                return mStream.ToArray();
            }
        }

        public static T ToPacket<T>(byte[] bytes) where T : IPacket
        {
            using (var mStream = new MemoryStream(bytes))
            {
                var deserializedObj = serializer.Deserialize(mStream);
                return deserializedObj != null ? (T)deserializedObj : default(T);
            }
        }

        // --- Flexibility, to be standardized in next versions ---
        // What it does?
        // Initially the packet argument is serialized into a byte array.
        // Then a new buffer is created, one byte bigger, the serialized byte array is copied into the new buffer with the first index slot free.
        // This first slot is used to store a vector byte, containing information about the encryption and compression of the packet sent.
        // Flags:
        //  1. Huffman-compress
        //  2. Encrypt
        public static byte[] ToFlexBytes<T>(T packet, bool compress, bool encrypt) where T : IPacket
        {
            var bytes = ToBytes(packet);

            // Encrypt first, then compress
            if (encrypt)
            {
                bytes = ToEncryptedBytes(bytes);
            }
            if (compress)
            {
                bytes = ToCompressedBytes(bytes);
            }

            // After encrypting and or compressing, the flex byte is inserted in front of the first index
            var flexBytes = new byte[bytes.Length + 1];
            flexBytes[0] = new VectorByte( // The info byte
                encrypt,   // 1: Huffman-compress
                compress);     // 2: Encrypt

            Buffer.BlockCopy(bytes, 0, flexBytes, 1, bytes.Length);

            return flexBytes;
        }
        public static T ToFlexPacket<T>(byte[] flexBytes) where T : IPacket
        {
            // Before decrypting and or decompressing we need to know which of these techniques have been applied, by reading the first byte array index
            var infoByte = new VectorByte(flexBytes[0]);

            var bytes = new byte[flexBytes.Length - 1];
            Buffer.BlockCopy(flexBytes, 1, bytes, 0, bytes.Length);

            // Reversed: decompress first, then decrypt
            if (infoByte[0])
            {
                bytes = ToDecompressedBytes(bytes);
            }
            if (infoByte[1])
            {
                bytes = ToDecryptedBytes(bytes);
            }

            return ToPacket<T>(bytes);
        }

        private static byte[] ToCompressedBytes(byte[] bytes)
        {
            var compressedBytes = new byte[bytes.Length];
            HuffmanCompressor.Compress(bytes, out compressedBytes); // Do something with the return value?

            return compressedBytes;
        }
        private static byte[] ToDecompressedBytes(byte[] compressedBytes)
        {
            var decompressedBytes = new byte[compressedBytes.Length];
            HuffmanCompressor.Decompress(compressedBytes, out decompressedBytes);

            return compressedBytes;
        }

        // TODO: Implement crypto
        private static byte[] ToEncryptedBytes(byte[] encryptedBytes)
        {
            return encryptedBytes;
        }
        private static byte[] ToDecryptedBytes(byte[] decryptedBytes)
        {
            return decryptedBytes;
        }
    }
}
