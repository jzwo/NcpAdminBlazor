using System.Buffers;
using System.Text;

namespace NcpAdminBlazor.Client.HttpClientServices;

public static class SseDataDecoder
{
    // FastEndpoints Send.EventStreamAsync serializes payloads as JSON.
    // For strings, that means quoted JSON strings, escaped where needed.
    // This helper unwraps JSON string tokens back into plain text.
    public static string Decode(ReadOnlySpan<byte> utf8Data)
    {
        if (utf8Data.IsEmpty) return string.Empty;

        // If it looks like a JSON string ("...") try to parse it.
        if (utf8Data.Length >= 2 && utf8Data[0] == (byte)'\"' && utf8Data[^1] == (byte)'\"')
        {
            try
            {
                // System.Text.Json can parse a JSON string from UTF8 without allocations beyond the result.
                return System.Text.Json.JsonSerializer.Deserialize<string>(utf8Data) ?? string.Empty;
            }
            catch
            {
                // Fall back to raw decode.
            }
        }

        return Encoding.UTF8.GetString(utf8Data);
    }

    public static string Decode(ReadOnlySequence<byte> data)
    {
        if (data.IsEmpty) return string.Empty;

        if (data.IsSingleSegment)
        {
            return Decode(data.FirstSpan);
        }

        // Small allocation fallback for multi-segment.
        var bytes = data.ToArray();
        return Decode(bytes);
    }
}
