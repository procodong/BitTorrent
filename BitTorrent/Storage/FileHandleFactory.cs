using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Storage;
public class FileHandleFactory : IStreamHandleFactory
{
    private readonly string _path;
    private readonly long _size;
    
    public FileHandleFactory(string path, long size)
    {
        _path = path;
        _size = size;
    }

    public StreamHandle CreateStream()
    {
        var createdFile = File.Open(_path, new FileStreamOptions()
        {
            Access = FileAccess.ReadWrite,
            Share = FileShare.ReadWrite,
            Mode = FileMode.OpenOrCreate,
            Options = FileOptions.Asynchronous,
        });
        if (createdFile.Length != _size)
        {
            createdFile.SetLength(_size);
        }
        return new(new(1, 1), createdFile);
    }
}
