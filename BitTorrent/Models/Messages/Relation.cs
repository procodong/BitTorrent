using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Messages;
public enum Relation
{
    Choke,
    Unchoke,
    Interested,
    NotInterested,
}
