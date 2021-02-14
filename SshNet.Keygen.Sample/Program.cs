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
            var key = SshKey.Generate("test.key", FileMode.Create, new SshKeyEncryptionAes256("12345"));
            var publicKey = key.ToOpenSshPublicFormat("Generated by SshNet.Keygen");
            var fingerprint = key.Fingerprint("Generated by SshNet.Keygen");

            Console.WriteLine("Fingerprint: {0}", fingerprint);
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