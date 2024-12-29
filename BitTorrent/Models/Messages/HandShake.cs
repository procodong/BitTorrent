using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Messages;
public readonly record struct HandShake(string Protocol, byte[] InfoHash, byte[] PeerId);