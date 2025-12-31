using System;
using System.Buffers;
using System.Text;
using NcpAdminBlazor.Client.HttpClientServices;
using Xunit;

namespace NcpAdminBlazor.Client.Tests;

public class SseDataDecoderTests
{
    [Fact]
    public void Decode_WhenPlainUtf8_ReturnsString()
    {
        var bytes = Encoding.UTF8.GetBytes("你好！");

        var text = SseDataDecoder.Decode(bytes);

        Assert.Equal("你好！", text);
    }

    [Fact]
    public void Decode_WhenJsonStringToken_ReturnsUnquoted()
    {
        var bytes = Encoding.UTF8.GetBytes("\"你好\"");

        var text = SseDataDecoder.Decode(bytes);

        Assert.Equal("你好", text);
    }

    [Fact]
    public void Decode_WhenJsonStringTokenContainsEscapes_Unescapes()
    {
        var bytes = Encoding.UTF8.GetBytes("\"line1\\nline2\"");

        var text = SseDataDecoder.Decode(bytes);

        Assert.Equal("line1\nline2", text);
    }

    [Fact]
    public void Decode_WhenMultiSegmentJsonStringToken_ReturnsUnquoted()
    {
        var seg1 = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("\"你"));
        var seg2 = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("好\""));
        var sequence = CreateSequence(seg1, seg2);

        var text = SseDataDecoder.Decode(sequence);

        Assert.Equal("你好", text);
    }

    private static ReadOnlySequence<byte> CreateSequence(ReadOnlyMemory<byte> first, ReadOnlyMemory<byte> second)
    {
        var firstSegment = new BufferSegment(first);
        var secondSegment = firstSegment.Append(second);
        return new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, second.Length);
    }

    private sealed class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public BufferSegment Append(ReadOnlyMemory<byte> next)
        {
            var segment = new BufferSegment(next)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;
            return segment;
        }
    }
}
