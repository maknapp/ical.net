using System.Collections.Generic;
using System.Text;

namespace Ical.Net.Serialization
{
    internal class EncodingStack
    {
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;
        private readonly Stack<Encoding> _stack;

        public EncodingStack()
        {
            _stack = new Stack<Encoding>();
        }

        public Encoding Current => _stack.Count > 0
            ? _stack.Peek()
            : DefaultEncoding;

        public void Push(Encoding encoding)
        {
            if (encoding != null)
            {
                _stack.Push(encoding);
            }
        }

        public Encoding Pop() => _stack.Count > 0
            ? _stack.Pop()
            : null;
    }
}