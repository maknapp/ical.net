//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization;

public sealed class CalendarReader
{
    private byte[] _buffer = new byte[256];
    private int _bufferStart = 0;
    private int _bufferLength = 0;

    private Memory<byte> _contentLine;

    private bool _readNextValueAsBase64 = false;

    private long _lineNumber = 0;

    /// <summary>
    /// The current line number.
    /// </summary>
    public long LineNumber => _lineNumber;

    private readonly Stream _input;

    internal CalendarReader(Stream input)
    {
        _input = input;
    }

    internal bool ReadContentLine()
    {
        // Reset read flags
        _readNextValueAsBase64 = false;

        // Get content line
        var endOfStream = false;

        while (!TryGetContentLine(endOfStream, out _contentLine))
        {
            if (endOfStream)
            {
                return false;
            }

            // Fill buffer
            var count = _buffer.Length - _bufferLength;
            var bytesRead = _input.Read(_buffer, _bufferStart + _bufferLength, count);

            if (bytesRead == 0)
            {
                endOfStream = true;
            }

            // Keep track of buffer data
            _bufferLength += bytesRead;
        }

        return !_contentLine.IsEmpty;
    }

    internal async Task<bool> ReadContentLineAsync(CancellationToken cancellationToken = default)
    {
        // Reset read flags
        _readNextValueAsBase64 = false;

        // Get content line
        var endOfStream = false;

        while (!TryGetContentLine(endOfStream, out _contentLine))
        {
            if (endOfStream)
            {
                return false;
            }

            // Fill buffer
            var offset = _bufferStart + _bufferLength;

#if NET8_0_OR_GREATER
            var bytesRead = await _input
                .ReadAsync(_buffer.AsMemory(offset), cancellationToken);
#else
            var bytesRead = await _input
                .ReadAsync(_buffer, offset, _buffer.Length - offset, cancellationToken)
                .ConfigureAwait(false);
#endif

            if (bytesRead == 0)
            {
                endOfStream = true;
            }

            // Keep track of buffer data
            _bufferLength += bytesRead;
        }

        return !_contentLine.IsEmpty;
    }

#if NET8_0_OR_GREATER
    private static readonly SearchValues<byte> _newLineBytes = SearchValues.Create("\n\r"u8);
#else
    private static readonly byte[] _newLineBytes = [(byte)'\n', (byte)'\r'];
#endif

    /// <summary>
    /// Finds a full content line. Includes unfolding.
    /// </summary>
    private bool TryGetContentLine(bool endOfStream, out Memory<byte> contentLine)
    {
        while (true)
        {
            var bufferedBytes = _buffer.AsSpan(_bufferStart, _bufferLength);

            var endOfLineIndex = IndexOfEndOfContentLine(bufferedBytes);

            if (endOfLineIndex == 0)
            {
                // Skip empty lines
                _bufferStart += 1;
                _bufferLength -= 1;
                continue;
            }

            // Do not require a newline at the end of the stream
            if (endOfLineIndex == -1 && endOfStream)
            {
                endOfLineIndex = _bufferLength;
            }

            if (endOfLineIndex != -1)
            {
                var unfoldedLength = UnfoldInPlace(bufferedBytes, endOfStream, out var lineCount);

                // Keep track of line count
                _lineNumber += lineCount;

                contentLine = _buffer.AsMemory(_bufferStart, unfoldedLength);
                _bufferStart += endOfLineIndex;
                _bufferLength -= endOfLineIndex;
                return true;
            }

            // Buffer does not contain an entire line, buffer more data

            if (_bufferStart > 0 && _bufferLength > 0)
            {
                // At the tail end of the buffer with no line ending.
                // Shift data to start of buffer.
                Array.Copy(_buffer, _bufferStart, _buffer, 0, _buffer.Length - _bufferStart);
                _bufferStart = 0;
            }

            var count = _buffer.Length - _bufferLength;
            if (count == 0)
            {
                Array.Resize(ref _buffer, _buffer.Length * 2);
                count = _buffer.Length - _bufferLength;
            }

            // If all buffered data has been used, make sure
            // to start writing at the start of the buffer.
            if (_bufferLength == 0)
            {
                _bufferStart = 0;
            }

            contentLine = default;
            return false;
        }
    }

    private static int IndexOfEndOfContentLine(
        ReadOnlySpan<byte> buffer)
    {
        var foldStart = 0;

        while (true)
        {
            var relativeEnd = buffer.Slice(foldStart).IndexOfAny(_newLineBytes);

            if (relativeEnd == -1)
            {
                // There is no new line, so no end of content line
                return -1;
            }

            var foldResult = StartsWithLineFold(buffer.Slice(foldStart + relativeEnd), out var foldLength);

            if (foldResult == LineFoldResult.EndOfContentLine)
            {
                return foldStart + relativeEnd;
            }

            if (foldResult == LineFoldResult.NeedsMoreData)
            {
                // If buffer ends on a new line, more data is needed
                // to know if the next byte will indicate a fold or not.
                return -1;
            }

            // Line is folded, continue searching
            foldStart += relativeEnd + foldLength;
        }
    }

    /// <summary>
    /// Unfolds the content line within the buffer.
    /// </summary>
    /// <param name="buffer">A buffer containing an entire content line.</param>
    /// <param name="lineCount">The number of lines before unfolding.</param>
    /// <returns>The length of the unfolded content line.</returns>
    private static int UnfoldInPlace(Span<byte> buffer, bool endOfStream, out int lineCount)
    {
        var foldStart = 0;
        var lineEnd = 0;

        lineCount = 0;

        while (true)
        {
            var relativeEnd = buffer.Slice(foldStart).IndexOfAny(_newLineBytes);

            if (relativeEnd == -1)
            {
                return buffer.Length;
            }

            var foldEnd = foldStart + relativeEnd;

            if (foldStart == 0)
            {
                lineEnd = foldEnd;
            }

            var foldResult = StartsWithLineFold(buffer.Slice(foldEnd), out var foldLength);

            if (foldResult == LineFoldResult.NeedsMoreData)
            {
                // This should only happen at the end of the stream
                // because the buffer should always have an entire
                // content line.
                if (!endOfStream)
                {
                    throw new SerializationException("Unexpected folded content line");
                }

                // At end of stream with no more content,
                // line must be unfolded.
                foldResult = LineFoldResult.EndOfContentLine;
            }

            if (foldStart > 0)
            {
                // Shift folded line backward to "remove" the fold
                var foldedLine = buffer.Slice(foldStart, relativeEnd);
                foldedLine.CopyTo(buffer.Slice(lineEnd));

                // Keep track of end of line
                lineEnd += foldedLine.Length;

                // Keep track of line number
                lineCount++;
            }

            if (foldResult == LineFoldResult.EndOfContentLine)
            {
                // Keep track of line number
                lineCount++;

                // At end of line
                return lineEnd;
            }

            foldStart = foldEnd + foldLength;
        }
    }

    private enum LineFoldResult
    {
        EndOfContentLine,
        Folded,
        NeedsMoreData,
    }

    private static LineFoldResult StartsWithLineFold(
        ReadOnlySpan<byte> endOfLine,
        out int foldLength)
    {
        if (endOfLine.Length < 2)
        {
            foldLength = 0;
            return LineFoldResult.NeedsMoreData;
        }

        // endOfLine[0] is \r or \n

        int foldSpaceIndex;
        if (endOfLine[1] == (byte)'\n')
        {
            if (endOfLine.Length == 2)
            {
                foldLength = 0;

                // Needs another character after the line break
                return LineFoldResult.NeedsMoreData;
            }

            // Line break is \r\n as expected
            foldSpaceIndex = 2;
        }
        else
        {
            // Line break is just \r or \n
            foldSpaceIndex = 1;
        }

        var possibleFoldIndent = endOfLine[foldSpaceIndex];

        var isFold = possibleFoldIndent == (byte) ' ' || possibleFoldIndent == (byte) '\t';

        foldLength = isFold ? 1 + foldSpaceIndex : 0;

        return isFold ? LineFoldResult.Folded : LineFoldResult.EndOfContentLine;
    }

#if NET8_0_OR_GREATER
    private static readonly SearchValues<byte> _nameSeparator = SearchValues.Create(":;"u8);
#else
    private static readonly byte[] _nameSeparator = [(byte)';', (byte)':'];
#endif

    public string ReadName()
    {
        var lineBytes = _contentLine.Span;
        var pos = lineBytes.IndexOfAny(_nameSeparator);

        if (pos == -1)
        {
            throw new SerializationException("Property name missing");
        }

#if NET8_0_OR_GREATER
        var name = Encoding.UTF8.GetString(lineBytes.Slice(0, pos));
#else
        var name = Encoding.UTF8.GetString(lineBytes.Slice(0, pos).ToArray());
#endif

        _contentLine = _contentLine.Slice(pos);

        return name.ToUpperInvariant();
    }

    private const byte ParameterNameSeparator = (byte) '=';

    internal bool TryReadParameterName(
#if NET
        [NotNullWhen(true)]
#endif
        out string? name)
    {
        var lineBytes = _contentLine.Span;

        if (lineBytes.Length == 0 || lineBytes[0] != ';')
        {
            name = null;
            return false;
        }

        lineBytes = lineBytes.Slice(1);

        var pos = lineBytes.IndexOf(ParameterNameSeparator);
        if (pos == -1)
        {
            name = null;
            return false;
        }

#if NET8_0_OR_GREATER
        name = Encoding.UTF8.GetString(lineBytes.Slice(0, pos));
#else
        name = Encoding.UTF8.GetString(lineBytes.ToArray(), 0, pos);
#endif

        // Add 1 for initial prefix byte
        _contentLine = _contentLine.Slice(1 + pos);

        return true;
    }

    internal bool TryReadParameterValue(
#if NET
        [NotNullWhen(true)]
#endif
        out string? value)
    {
        var lineBytes = _contentLine.Span;

        if (lineBytes.Length == 0 || (lineBytes[0] != '=' && lineBytes[0] != ','))
        {
            value = null;
            return false;
        }

        lineBytes = lineBytes.Slice(1);

        var start = 0;
        int pos;

        // Check if value is quoted
        if (lineBytes.Length > 0 && lineBytes[0] == '"')
        {
            start = 1;
            pos = lineBytes.Slice(1).IndexOf((byte) '"');

            if (pos == -1)
            {
                throw new SerializationException($"Unbalanced quotes when reading parameter value at line {LineNumber}");
            }
        }
        else
        {
            pos = lineBytes.IndexOfAny(_nameSeparator);

            if (pos == -1)
            {
                value = null;
                return false;
            }
        }

#if NET8_0_OR_GREATER
        value = Encoding.UTF8.GetString(lineBytes.Slice(start, pos));
#else
        value = Encoding.UTF8.GetString(lineBytes.ToArray(), start, pos);
#endif

        // Add 1 for initial prefix byte
        _contentLine = _contentLine.Slice(1 + (start * 2) + pos);

        return true;
    }

    internal void ReadNextValueAsBase64() => _readNextValueAsBase64 = true;

    public byte[] ReadBinaryValue()
    {
        if (_contentLine.Length == 0)
        {
            return [];
        }

        var lineBytes = _contentLine.Span;

        if (lineBytes.Length == 0 || lineBytes[0] != ':')
        {
            return [];
        }

        lineBytes = lineBytes.Slice(1);

        // A BINARY value is a base64 encoded string
        var result = Base64.DecodeFromUtf8InPlace(lineBytes, out var written);
        if (result != OperationStatus.Done)
        {
            throw new SerializationException("Invalid BINARY data");
        }

        // Entire line is used
        _contentLine = Memory<byte>.Empty;

        return lineBytes.Slice(0, written).ToArray();
    }

    public bool TryGetBoolean(out bool result)
    {
        if (!TryGetValueBytes(out var valueBytes))
        {
            result = default;
            return false;
        }

#if NET
        var strValue = Encoding.UTF8.GetString(valueBytes);
#else
        var strValue = Encoding.UTF8.GetString(valueBytes.ToArray());
#endif

        if (strValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
        {
            result = true;
            return true;
        }

        if (strValue.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
        {
            result = false;
            return true;
        }

        result = default;
        return false;
    }

    private void AdvancePos(int length)
    {
        _contentLine = _contentLine.Slice(length);
    }

    private static readonly string[] _utcOffsetFormats = ["hhmmss", "hhmm", "hh"];


    public bool TryGetUtcOffset(
#if NET
        [NotNullWhen(true)]
#endif
        out UtcOffset? offset)
    {
        if (!TryGetValueBytes(out var valueBytes))
        {
            offset = default;
            return false;
        }

#if NET
        var strValue = Encoding.UTF8.GetString(valueBytes);
#else
        var strValue = Encoding.UTF8.GetString(valueBytes.ToArray());
#endif

        if (!UtcOffset.TryParse(strValue, out offset))
        {
            offset = default;
            return false;
        }

        AdvancePos(1 + valueBytes.Length);

        return true;
    }

    public bool TryGetUri(
#if NET
        [NotNullWhen(true)]
#endif
        out Uri? uri)
    {
        GetRawValueBytes(out var valueBytes);

#if NET
        var strValue = Encoding.UTF8.GetString(valueBytes);
#else
        var strValue = Encoding.UTF8.GetString(valueBytes.ToArray());
#endif

        if (!Uri.TryCreate(strValue, UriKind.RelativeOrAbsolute, out uri))
        {
            return false;
        }

        AdvancePos(1 + valueBytes.Length);

        return true;
    }

    public bool TryGetDateTime(
        string? tzId,
#if NET
        [NotNullWhen(true)]
#endif
        out CalDateTime? dateTime)
    {
        if (!TryGetValueBytes(out var valueBytes))
        {
            dateTime = default;
            return false;
        }

#if NET
        var strValue = Encoding.UTF8.GetString(valueBytes);
#else
        var strValue = Encoding.UTF8.GetString(valueBytes.ToArray());
#endif

        if (!CalDateTime.TryParse(strValue, tzId, out dateTime))
        {
            return false;
        }

        AdvancePos(1 + valueBytes.Length);

        return true;
    }

    public bool TryGetPeriod(
        string? tzId,
#if NET
        [NotNullWhen(true)]
#endif
        out Period? period)
    {
        if (!TryGetValueBytes(out var valueBytes))
        {
            period = default;
            return false;
        }

#if NET
        var strValue = Encoding.UTF8.GetString(valueBytes);
#else
        var strValue = Encoding.UTF8.GetString(valueBytes.ToArray());
#endif

        if (!Period.TryParse(strValue, tzId, out period))
        {
            return false;
        }

        AdvancePos(1 + valueBytes.Length);

        return true;
    }
    public bool TryGetDuration(out Duration duration)
    {
        if (!TryGetValueBytes(out var valueBytes))
        {
            duration = default;
            return false;
        }

        // TODO: Reduce allocation for fixed-size values
        //Span<char> buffer = stackalloc char[256];

#if NET
        var strValue = Encoding.UTF8.GetString(valueBytes);
#else
        var strValue = Encoding.UTF8.GetString(valueBytes.ToArray());
#endif

        if (!Duration.TryParse(strValue, out duration))
        {
            return false;
        }

        AdvancePos(1 + valueBytes.Length);

        return true;
    }

    /// <summary>
    /// Finds the next value in the current line,
    /// handling comma-separated values. Callers must
    /// advance reader position if bytes are used.
    /// </summary>
    /// <returns>A span of the next value.</returns>
    internal bool TryGetValueBytes(out ReadOnlySpan<byte> value)
    {
        var lineBytes = _contentLine.Span;

        if (lineBytes.Length == 0 || (lineBytes[0] != ':' && lineBytes[0] != ','))
        {
            value = default;
            return false;
        }

        lineBytes = lineBytes.Slice(1);

        var endPos = lineBytes.IndexOf((byte)',');

        if (endPos == -1)
        {
            value = lineBytes;
            return true;
        }

        value = lineBytes.Slice(0, endPos);
        return true;
    }

    internal void GetRawValueBytes(out ReadOnlySpan<byte> value)
    {
        var lineBytes = _contentLine.Span;

        if (lineBytes.Length == 0 || lineBytes[0] != ':')
        {
            value = default;
            return;
        }

        value = lineBytes.Slice(1);
    }

    public string GetRecurValue()
    {
        if (_readNextValueAsBase64)
        {
            return ReadValueBase64();
        }

        GetRawValueBytes(out var valueBytes);

#if NET
        var strValue = Encoding.UTF8.GetString(valueBytes);
#else
        var strValue = Encoding.UTF8.GetString(valueBytes.ToArray());
#endif

        return strValue;
    }

    public string GetTextValue()
    {
        if (_readNextValueAsBase64)
        {
            return ReadValueBase64();
        }

        if (!TryReadTextValue(out var textValue))
        {
            return string.Empty;
        }

        return textValue;
    }

    public bool TryGetTextValue(
#if NET
        [NotNullWhen(true)]
#endif
        out string? value)
    {
        if (_readNextValueAsBase64)
        {
            if (_contentLine.IsEmpty)
            {
                value = default;
                return false;
            }

            value = ReadValueBase64();
            return true;
        }

        return TryReadTextValue(out value);
    }

    /// <summary>
    /// Reads a single value from a "structured" text value
    /// delimited by a semicolon.
    /// </summary>
    public bool TryGetStructuredTextValue(
#if NET
        [NotNullWhen(true)]
#endif
        out string? value) => TryReadTextValueCore((byte) ';', out value);

    private bool TryReadTextValue(
#if NET
        [NotNullWhen(true)]
#endif
        out string? textValue) => TryReadTextValueCore((byte) ',', out textValue);

    private bool TryReadTextValueCore(
        byte delimiter,
#if NET
        [NotNullWhen(true)]
#endif
        out string? textValue)
    {
        var lineBytes = _contentLine.Span;

        if (lineBytes.Length == 0 || (lineBytes[0] != ':' && lineBytes[0] != delimiter))
        {
            textValue = null;
            return false;
        }

        lineBytes = lineBytes.Slice(1);

        // Get unescaped bytes. The unescaped length must be smaller
        // than the original length, but the byte length used could be
        // much bigger than needed because it could be multiple values.
        var rentedBytes = ArrayPool<byte>.Shared.Rent(lineBytes.Length);
        UnescapeTextValue(lineBytes, rentedBytes, delimiter, out var writtenBytes, out var readBytes);

#if NET8_0_OR_GREATER
        var unescapedBytes = rentedBytes.AsSpan(0, writtenBytes);
        textValue = Encoding.UTF8.GetString(unescapedBytes);
#else
        textValue = Encoding.UTF8.GetString(rentedBytes, 0, writtenBytes);
#endif

        ArrayPool<byte>.Shared.Return(rentedBytes);

        // Advance reader. Add 1 for prefix ':' or delimiter.
        _contentLine = _contentLine.Slice(1 + readBytes);

        return true;
    }

    private string ReadValueBase64()
    {
        var data = ReadBinaryValue();

        return Encoding.UTF8.GetString(data);
    }

    #region Text unescape

#if NET8_0_OR_GREATER
    private static readonly SearchValues<byte> _escapeOrValueSeparator = SearchValues.Create("\\,"u8);
    private static readonly SearchValues<byte> _escapeOrStructureSeparator = SearchValues.Create("\\;"u8);
#else
    private static readonly byte[] _escapeOrValueSeparator = [(byte)'\\', (byte)','];
    private static readonly byte[] _escapeOrStructureSeparator = [(byte)'\\', (byte)';'];
#endif

    /// <summary>
    /// Unescapes the first value from the source. Stops at the
    /// first unescaped comma.
    /// </summary>
    internal static void UnescapeTextValue(
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        byte delimiter,
        out int written,
        out int idx)
    {
        Debug.Assert(destination.Length >= source.Length);

        var searchValues = delimiter == (byte)';'
            ? _escapeOrStructureSeparator : _escapeOrValueSeparator;

        idx = 0;
        written = 0;

        while (true)
        {
            var remaining = source.Slice(idx);

            var nextUnescapedLength = remaining.IndexOfAny(searchValues);
            if (nextUnescapedLength == -1)
            {
                nextUnescapedLength = remaining.Length;
            }

            // Write unescaped bytes
            remaining.Slice(0, nextUnescapedLength).CopyTo(destination.Slice(written));
            written += nextUnescapedLength;
            idx += nextUnescapedLength;

            // If all source bytes are copied or the
            // delimiter has been reached, then stop.
            if (idx == source.Length || source[idx] == delimiter)
            {
                return;
            }

            Debug.Assert(source[idx] == (byte) '\\');

            destination[written++] = source[++idx] switch
            {
                (byte) 'n' or (byte) 'N' => (byte) '\n',
                (byte) '\\' => (byte) '\\',
                (byte) ';' => (byte) ';',
                (byte) ',' => (byte) ',',

                // Double quotes aren't escaped in RFC2445, but are in Mozilla Sunbird (0.5-)
                (byte) '"' => (byte) '"',

                // Backslash is escaping an invalid character, just
                // include the backslash and leave it unchanged.
                _ => (byte) '\\',
            };

            // If all source bytes are copied or an
            // unescaped comma has been reached, then stop.
            if (++idx == source.Length || source[idx] == delimiter)
            {
                return;
            }
        }
    }
#endregion
}
