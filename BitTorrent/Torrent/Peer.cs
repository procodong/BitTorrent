using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrent;

public class Peer
{
    private readonly TcpClient _client;
    private bool interested = false;
    private bool choked = false;

    public async static Task<Peer> Connect(string address, int port)
    {
        var client = new TcpClient();
        await client.ConnectAsync(address, port);
        var peer = new Peer(client);
        return peer;
    }

    private Peer(TcpClient client)
    {
        _client = client;
    }
}
