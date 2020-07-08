using System;

namespace Ical.Net.Collections
{
    public sealed class ItemAddedEventArgs<T> : EventArgs
    {
        public T Item { get; }
        public int Index { get; }

        public ItemAddedEventArgs(T item, int index)
        {
            Item = item;
            Index = index;
        }
    }

    public sealed class ItemRemovedEventArgs<T> : EventArgs
    {
        public T Item { get; }
        public int Index { get; }

        public ItemRemovedEventArgs(T item, int index)
        {
            Item = item;
            Index = index;
        }
    }
}
