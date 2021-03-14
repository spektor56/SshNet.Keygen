﻿using System;
using System.IO;
using System.Security.Cryptography;
using Chaos.NaCl;
using Renci.SshNet;
using Renci.SshNet.Security;
using SshNet.Keygen.Extensions;

namespace SshNet.Keygen
{
    public static class SshKey
    {
        public static PrivateGeneratedKey Generate(string path, FileMode mode)
        {
            return Generate(path, mode, new SshKeyGenerateInfo());
        }

        public static PrivateGeneratedKey Generate(string path, FileMode mode, SshKeyGenerateInfo info)
        {
            var key = Generate(info);

            using var file = File.Open(path, mode, FileAccess.Write);
            using var writer = new StreamWriter(file);

            switch (info.KeyFormat)
            {
                case SshKeyFormat.OpenSSH:
                    writer.Write(key.ToOpenSshFormat(info.Encryption));
                    break;
                case SshKeyFormat.PuTTY:
                    writer.Write(key.ToPuttyFormat(info.Encryption));
                    break;
                default:
                    throw new NotSupportedException($"Not supported Key Format {info.KeyFormat}");
            }

            using var pubFile = File.Open($"{path}.pub", mode);
            using var pubWriter = new StreamWriter(pubFile);
            pubWriter.Write(key.ToPublic());
            return key;
        }

        public static PrivateGeneratedKey Generate()
        {
            return Generate(new SshKeyGenerateInfo());
        }

        public static PrivateGeneratedKey Generate(SshKeyGenerateInfo info)
        {
            Key key;
            switch (info.KeyType)
            {
                case SshKeyType.ED25519:
                {
                    using var rngCsp = new RNGCryptoServiceProvider();
                    var seed = new byte[Ed25519.PrivateKeySeedSizeInBytes];
                    rngCsp.GetBytes(seed);
                    Ed25519.KeyPairFromSeed(out var edPubKey, out var edKey, seed);
                    key = new ED25519Key(edPubKey, edKey.Reverse());
                    break;
                }
                case SshKeyType.RSA:
                {
                    using var rsa = CreateRSA(info.KeyLength);
                    var rsaParameters = rsa.ExportParameters(true);

                    key = new RsaKey(
                        rsaParameters.Modulus.ToBigInteger2().ToByteArray().Reverse().ToBigInteger(),
                        rsaParameters.Exponent.ToBigInteger2().ToByteArray().Reverse().ToBigInteger(),
                        rsaParameters.D.ToBigInteger2().ToByteArray().Reverse().ToBigInteger(),
                        rsaParameters.P.ToBigInteger2().ToByteArray().Reverse().ToBigInteger(),
                        rsaParameters.Q.ToBigInteger2().ToByteArray().Reverse().ToBigInteger(),
                        rsaParameters.InverseQ.ToBigInteger2().ToByteArray().Reverse().ToBigInteger()
                    );
                    break;
                }
                case SshKeyType.ECDSA:
                {
#if NETSTANDARD
                    var curve = info.KeyLength switch
                    {
                        256 => ECCurve.CreateFromFriendlyName("nistp256"),
                        384 => ECCurve.CreateFromFriendlyName("nistp384"),
                        521 => ECCurve.CreateFromFriendlyName("nistp521"),
                        _ => throw new CryptographicException("Unsupported KeyLength")
                    };

                    using var ecdsa = ECDsa.Create();
                    if (ecdsa is null)
                        throw new CryptographicException("Unable to generate ECDSA");
                    ecdsa.GenerateKey(curve);
                    var ecdsaParameters = ecdsa.ExportParameters(true);

                    key = new EcdsaKey(
                        ecdsa.EcCurveNameSshCompat(),
                        ecdsaParameters.UncompressedCoords(),
                        ecdsaParameters.D
                    );
#else
                    using var ecdsa = new ECDsaCng(info.KeyLength);
                    var keyBlob = ecdsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
                    using var stream = new MemoryStream(keyBlob);
                    using var reader = new BinaryReader(stream);
                    var magic = (EcdsaExtension.KeyBlobMagicNumber)reader.ReadInt32();
                    var coordLength = reader.ReadInt32();
                    var qx = reader.ReadBytes(coordLength);
                    var qy = reader.ReadBytes(coordLength);
                    var d = reader.ReadBytes(coordLength);

                    key = new EcdsaKey(
                        ecdsa.EcCurveNameSshCompat(magic),
                        ecdsa.UncompressedCoords(qx, qy),
                        d
                    );
#endif
                    break;
                }
                default:
                    throw new NotSupportedException($"Unsupported KeyType: {info.KeyType}");
            }

            key.Comment = info.Comment;
            return new PrivateGeneratedKey(key);
        }

        private static RSA CreateRSA(int keySize)
        {
#if NET40
            var rsa = new RSACryptoServiceProvider(keySize);
            var keySizes = rsa.LegalKeySizes[0];
            if (keySize < keySizes.MinSize || keySize > keySizes.MaxSize)
            {
                throw new CryptographicException($"Illegal Keysize: {keySize}");
            }
            return rsa;
#else
            var rsa = RSA.Create();

            if (rsa is RSACryptoServiceProvider)
            {
                rsa.Dispose();
                return new RSACng(keySize);
            }

            rsa.KeySize = keySize;
            return rsa;
#endif
        }
    }
}