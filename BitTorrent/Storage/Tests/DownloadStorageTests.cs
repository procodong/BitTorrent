using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Storage.Tests;
[TestClass]
public class DownloadStorageTests
{
    [TestMethod]
    public void TestStream()
    {
        /*
        var files = new List<StreamData>();
        for (int i = 0; i < 10; i++)
        {
            var stream = new MemoryStream();
            files.Add(new(stream, i * 100, new(1, 1)));
        }
        var storage = new DownloadStorage(10, files);
        var data = storage.GetStream(11, 0, 100);
        Assert.Equals(data.Length, 100);
        Assert.Equals(data.Current.StreamData.ByteOffset, 100);
        Assert.Equals(data.Current.Position, 10);
        Assert.Equals(data.Current.Length, 90);
        */
    }
}
