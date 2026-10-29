using System.Buffers.Binary;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Accepts bounded canonical mono PCM clips before passing them to native decoders.</summary>
public static class PcmWave
{
    public static string? Resolve(string dataDirectory, string relativePath)
    {
        if (!AudioPresentationReader.IsWavePath(relativePath)) return null;
        try
        {
            var root = Path.GetFullPath(dataDirectory);
            var path = root;
            foreach (var part in relativePath.Split('/'))
            {
                path = Path.Combine(path, part);
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) return null;
            }
            using var stream = File.OpenRead(path);
            if (stream.Length is < 46 or > 8388608) return null;
            Span<byte> header = stackalloc byte[44];
            stream.ReadExactly(header);
            return header[..4].SequenceEqual("RIFF"u8) && header[8..16].SequenceEqual("WAVEfmt "u8)
                && BinaryPrimitives.ReadUInt32LittleEndian(header[16..]) == 16
                && BinaryPrimitives.ReadUInt16LittleEndian(header[20..]) == 1
                && BinaryPrimitives.ReadUInt16LittleEndian(header[22..]) == 1
                && BinaryPrimitives.ReadUInt32LittleEndian(header[24..]) == 22050
                && BinaryPrimitives.ReadUInt32LittleEndian(header[28..]) == 44100
                && BinaryPrimitives.ReadUInt16LittleEndian(header[32..]) == 2
                && BinaryPrimitives.ReadUInt16LittleEndian(header[34..]) == 16
                && header[36..40].SequenceEqual("data"u8)
                && BinaryPrimitives.ReadUInt32LittleEndian(header[4..]) == stream.Length - 8
                && BinaryPrimitives.ReadUInt32LittleEndian(header[40..]) == stream.Length - 44
                && stream.Length % 2 == 0 ? path : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }
}
