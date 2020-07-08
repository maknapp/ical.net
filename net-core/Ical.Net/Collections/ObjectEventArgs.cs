using System;

namespace Ical.Net.Collections
{
    public sealed class ObjectEventArgs<TFirst, TSecond> : EventArgs
    {
        public TFirst First { get; }
        public TSecond Second { get; }

        public ObjectEventArgs(TFirst first, TSecond second)
        {
            First = first;
            Second = second;
        }
    }
}
