using System.Collections.Generic;

namespace Ical.Net.Collections
{
    // TODO: MultiLinkedList<T> class appears to be obsolete.

    public sealed class MultiLinkedList<TType> :
        List<TType>,
        IMultiLinkedList<TType>
    {
        private IMultiLinkedList<TType> _previous;

        public void SetPrevious(IMultiLinkedList<TType> previous)
        {
            _previous = previous;
        }

        public int StartIndex => _previous?.ExclusiveEnd ?? 0;

        public int ExclusiveEnd => Count > 0 ? StartIndex + Count : StartIndex;
    }
}
