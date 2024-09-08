using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models;
public record class Config(
    ulong MaxDownload,
    ulong MaxUpload
    );