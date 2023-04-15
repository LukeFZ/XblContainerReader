using System.Text;

namespace LibXblContainer;

internal static class Extensions
{
    internal static string ReadUnicode(this BinaryReader reader, int? maxLength = null)
    {
        var length = reader.ReadInt32(); // actually uint
        if (length > maxLength)
            throw new InvalidDataException($"String too long. Max length: {maxLength}, got length: {length}");

        return length == 0 
            ? string.Empty
            : Encoding.Unicode.GetString(reader.ReadBytes(length * 2));
    }

    internal static void WriteUnicode(this BinaryWriter writer, string str, int? maxLength = null)
    {
        var length = str.Length;
        if (length > maxLength)
            throw new InvalidDataException($"String too long. Max length: {maxLength}, got length: {length}");

        writer.Write(length);
        if (length > 0) 
            writer.Write(Encoding.Unicode.GetBytes(str));
    }

    internal static string ToStringWithoutDashes(this Guid guid)
        => guid.ToString().Replace("-", string.Empty);
}