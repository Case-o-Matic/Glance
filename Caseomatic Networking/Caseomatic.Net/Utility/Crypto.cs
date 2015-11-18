using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Caseomatic.Net.Utility
{
    public static class Crypto
    {
        public static RSAParameters rsaParams;

        public static byte[] Encrypt(byte[] bytes, bool isOAEP)
        {
            try
            {
                byte[] encryptedData;
                using (var rsa = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This only needs
                    //to include the public key information.
                    rsa.ImportParameters(rsaParams);

                    //Encrypt the passed byte array and specify OAEP padding.
                    encryptedData = rsa.Encrypt(bytes, isOAEP);
                }
                return encryptedData;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public static byte[] Decrypt(byte[] encrypedBytes, bool isOAEP)
        {
            try
            {
                byte[] decryptedData;
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This needs
                    //to include the private key information.
                    RSA.ImportParameters(rsaParams);

                    //Decrypt the passed byte array and specify OAEP padding.
                    decryptedData = RSA.Decrypt(encrypedBytes, isOAEP);
                }
                return decryptedData;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }
    }
}
