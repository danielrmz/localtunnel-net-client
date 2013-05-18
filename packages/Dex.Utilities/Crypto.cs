using System;
using System.IO;

namespace Dex.Utilities
{
    using RSA = OpenSSL.Crypto.RSA;
    using Buffer = Tamir.SharpSsh.jsch.Buffer;
    using Encoding = System.Text.Encoding;
    using PemReader = Org.BouncyCastle.OpenSsl.PemReader;
    using RsaKeyParameters = Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters;
    using System.Security.Cryptography;

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
            public RSAParameters RSAParameters;
        }

        /// <summary>
        /// Writes a pair of files representing the Public Key in ssh-required format
        /// and the Private key in PEM format.
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="comment"></param>
        /// <param name="privFileName"></param>
        public static RsaKeyPair WriteRsaKeyPair(int bits, string comment, string privFileName)
        {
            RsaKeyPair pair = GenerateRsaKeyPair(bits, comment);
            
            using(TextWriter tw = new StreamWriter(privFileName, false), 
                             twpub = new StreamWriter(privFileName + ".pub", false)) {

                tw.WriteLine(pair.PrivateKeyAsPEM);
                twpub.WriteLine(pair.PublicSSHKey); 
            }

            return pair;
        }
        
        /// <summary>
        /// Generates the Rsa keys accordingly.
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static RsaKeyPair GenerateRsaKeyPair(int bits, string comment)
        {
            const string sshrsa = "ssh-rsa";
            
            var generatedKey = RSAKeyGenerator.Generate(bits, comment);

            PemReader reader = new PemReader(new StringReader(generatedKey.PublicKeyAsPEM));
            RsaKeyParameters r = (RsaKeyParameters)reader.ReadObject();
            
            byte[] sshrsa_bytes = Encoding.Default.GetBytes(sshrsa);
            byte[] n = r.Modulus.ToByteArray();
            byte[] e = r.Exponent.ToByteArray();

            Buffer buf = new Buffer(sshrsa_bytes.Length + 4 + e.Length + 4 + n.Length + 4);

            // Add the bytes
            buf.add(sshrsa_bytes, e, n);

            // Encode in Base64
            string buffer64 = buf.AsBase64String();

            generatedKey.PublicSSHKey = string.Format("{0} {1} {2}", sshrsa, buffer64, comment);

            return generatedKey;
        }
        
    }
}
