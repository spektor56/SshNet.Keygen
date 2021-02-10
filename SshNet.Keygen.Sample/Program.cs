﻿using System;
using System.IO;
using Renci.SshNet;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;

namespace SshNet.Keygen.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            SshKey.Generate("test.key", new SshKeyEncryptionAes256("12345"));
            var key = new PrivateKeyFile("test.key", "12345");
            var publicKey = key.ToOpenSshPublicFormat("Generated by SshNet.Keygen");

            Console.WriteLine("Fingerprint: {0}", key.Fingerprint("Generated by SshNet.Keygen"));
            Console.WriteLine("Add this to your .ssh/authorized_keys of the SSH Server: {0}", publicKey);
            Console.ReadLine();

            var connectionInfo = new ConnectionInfo("ssh.foo.com", "root", new PrivateKeyAuthenticationMethod("root", key));
            using (var client = new SshClient(connectionInfo))
            {
                client.Connect();
                Console.WriteLine(client.RunCommand("hostname").Result);
            }
        }
    }
}