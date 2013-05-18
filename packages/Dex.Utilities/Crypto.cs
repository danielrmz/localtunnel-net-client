using System;
using System.IO;

namespace Dex.Utilities
{
    using RSA = OpenSSL.Crypto.RSA;
    using Buffer = Tamir.SharpSsh.jsch.Buffer;
    using Encoding = System.Text.Encoding;
    using PemReader = Org.BouncyCastle.OpenSsl.PemReader;
    using RsaKeyParameters = Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters;

    /// <summary>
    /// Simple utility class for obtaining a compatible set of keys
    /// between OpenSSL .NET and SSH
    /// 
    /// Currently it has a dependency with BouncyCastle's PemReader
    /// and SharpSSH Buffer class, included.
    /// </summary>
    public static class Cyrpto
    {
        /// <summary>
        /// Contains required information if needed.
        /// </summary>
        public struct RsaKeyPair
        {
            public string PrivateKeyAsPEM;
            public string PublicKeyAsPEM;
            public string PublicSSHKey;
        }

        /// <summary>
        /// Writes a pair of files representing the Public Key in ssh-required format
        /// and the Private key in PEM format.
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="comment"></param>
        /// <param name="privFileName"></param>
        public static void WriteRsaKeyPair(int bits,  string comment, string privFileName)
        {
            RsaKeyPair pair = GenerateRsaKeyPair(bits, comment);
            TextWriter tw;

            tw = new StreamWriter(privFileName, false);
            tw.WriteLine(pair.PrivateKeyAsPEM);
            tw.Close();
            
            tw = new StreamWriter(privFileName + ".pub", false);
            tw.WriteLine(pair.PublicSSHKey);
            tw.Close();
        }

        /// <summary>
        /// Writes the public key from the private key specified.
        /// </summary>
        /// <param name="privateKeyFile"></param>
        public static void WriteRsaKeyPair(string privateKeyFile, string comment)
        {   
            RsaKeyPair pair = GenerateRsaKeyPair(privateKeyFile, comment);

            // We only need to write the public key since we already got the private one.
            TextWriter tw = new StreamWriter(privateKeyFile + ".pub", false);
            tw.WriteLine(pair.PublicSSHKey);
            tw.Close();
        }

        /// <summary>
        /// Parses the private key to obtain both.
        /// </summary>
        /// <param name="privateKeyFile">The filename of the private key</param>
        /// <returns></returns>
        public static RsaKeyPair GenerateRsaKeyPair(string privateKeyFile, string comment)
        {
            if (!File.Exists(privateKeyFile))
            {
                throw new FileNotFoundException("Private key file not found");
            }

            RSA rsa = RSA.FromPrivateKey(new OpenSSL.Core.BIO(Encoding.Default.GetBytes(File.ReadAllText(privateKeyFile))));
            
            return GetRsaKeyPair(rsa, comment);
        }

        /// <summary>
        /// Generates the Rsa keys accordingly.
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static RsaKeyPair GenerateRsaKeyPair(int bits, string comment)
        {
            
            // Generate keys using OpenSSL RSA generator.
            using (RSA rsa = new RSA())
            {
                rsa.GenerateKeys(bits, 65537, null, null);

                return GetRsaKeyPair(rsa, comment);
            }
        }

        private static RsaKeyPair GetRsaKeyPair(RSA rsa, string comment)
        {
            const string sshrsa = "ssh-rsa";
            
            // Use Bouncy castle's pem reader to get the modulus and exponent as a byte array
            PemReader reader = new PemReader(new StringReader(rsa.PublicKeyAsPEM));
            RsaKeyParameters r = (RsaKeyParameters)reader.ReadObject();

            byte[] sshrsa_bytes = Encoding.Default.GetBytes(sshrsa);
            byte[] n = r.Modulus.ToByteArray();
            byte[] e = r.Exponent.ToByteArray();

            Buffer buf = new Buffer(sshrsa_bytes.Length + 4 + e.Length + 4 + n.Length + 4);

            // Add the bytes
            buf.add(sshrsa_bytes, e, n);

            // Encode in Base64
            string buffer64 = buf.AsBase64String();

            // Set the base DTO
            RsaKeyPair pair = new RsaKeyPair()
            {
                PublicSSHKey = string.Format("ssh-rsa {0} {1}", buffer64, comment),
                PublicKeyAsPEM = rsa.PublicKeyAsPEM,
                PrivateKeyAsPEM = rsa.PrivateKeyAsPEM,
                // Later add parameters if required.
            };

            return pair;
        }
    }
}
