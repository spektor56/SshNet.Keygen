﻿using System;
using System.IO;
using Renci.SshNet;
using SshNet.Keygen.Extensions;

namespace SshNet.Keygen.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var key = SshKey.Generate();
            var privateKey = key.ToOpenSshFormat("Generated by SshNet.Keygen");
            var publicKey = key.ToOpenSshPublicFormat("Generated by SshNet.Keygen");

            Console.WriteLine("Add this to your .ssh/authorized_keys of the SSH Server: {0}", publicKey);
            Console.ReadLine();

            File.WriteAllText("test.key", privateKey);
            var connectionInfo = new ConnectionInfo("ssh.foo.com", "root", new PrivateKeyAuthenticationMethod("root", new PrivateKeyFile("test.key")));
            using (var client = new SshClient(connectionInfo))
            {
                client.Connect();
                Console.WriteLine(client.RunCommand("hostname").Result);
            }
        }
    }
}