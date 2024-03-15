using System.Collections.Generic;
using System.Linq;
using Renci.SshNet;
using Renci.SshNet.Security;

namespace SshNet.Keygen
{
    public class PrivateGeneratedKey : IPrivateKeySource
    {
        private readonly List<HostAlgorithm> _hostAlgorithms = new();

        public IReadOnlyCollection<HostAlgorithm> HostKeyAlgorithms => _hostAlgorithms;

        public Key Key { get; }

        public HostAlgorithm HostKey => _hostAlgorithms.First();

        public PrivateGeneratedKey(Key key)
        {
            Key = key;
            _hostAlgorithms.Add(new KeyHostAlgorithm(key.ToString(), key));
        }
    }
}