
using BencodeNET.IO;
using BencodeNET.Torrents;

var parser = new TorrentParser();
var file = File.OpenRead("d");

var data = parser.Parse(file);