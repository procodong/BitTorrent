using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public class AtomicBool(bool value)
{
    private volatile int _value = value ? 1 : 0;

    public static implicit operator bool(AtomicBool value) => value._value == 1;

    public bool CompareExchange(bool value, bool comparand)
    {
        return Interlocked.CompareExchange(ref _value, value ? 1 : 0, comparand ? 1 : 0) == 1;
    }

    public bool Exchange(bool value)
    {
        return Interlocked.Exchange(ref _value, value ? 1 : 0) == 1;
    }
}
