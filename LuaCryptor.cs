namespace LuaEditor;

/// <summary>
/// XOR encrypt/decrypt for Elsword .lua bytecode files.
/// Keys sourced from X2KomFileViewer/Config.lua (AddEncryptionKey calls).
/// Algorithm: FileCrypt::FileEncrypt&lt;int&gt; in FileCrypt.h - cyclic 4-byte XOR.
/// </summary>
internal static class LuaCryptor
{
    // From Config.lua: 3338185218, 642231260, 2550184943
    private static readonly uint[] Keys = { 0xC6F8AA02u, 0x2647ABDCu, 0x9800BBEFu };

    // Elsword-modified LuaJIT magic: \x1b KL \x81  (vs standard \x1b LJ \x01)
    private static readonly byte[] ElswordMagic = { 0x1B, 0x4B, 0x4C, 0x81 };

    /// <summary>
    /// Encrypt or decrypt (symmetric XOR). Operates 4 bytes at a time with
    /// cycling key index, matching FileCrypt::FileEncrypt/FileDecrypt exactly.
    /// </summary>
    public static byte[] Crypt(byte[] data)
    {
        var result = new byte[data.Length];
        int keyIdx = 0;

        for (int i = 0; i < data.Length; )
        {
            int remaining = data.Length - i;
            int blockSize = Math.Min(4, remaining);

            // Load up to 4 bytes as little-endian uint32, zero-padded
            uint block = 0;
            for (int j = 0; j < blockSize; j++)
                block |= (uint)data[i + j] << (j * 8);

            uint xored = block ^ Keys[keyIdx];

            // Write only blockSize bytes back
            for (int j = 0; j < blockSize; j++)
                result[i + j] = (byte)(xored >> (j * 8));

            i += blockSize;

            // Key advances only for full 4-byte blocks (matches C++ break-before-increment)
            if (blockSize == 4)
                keyIdx = (keyIdx + 1) % Keys.Length;
        }

        return result;
    }

    public static bool HasElswordMagic(byte[] data)
        => data.Length >= 4
        && data[0] == ElswordMagic[0]
        && data[1] == ElswordMagic[1]
        && data[2] == ElswordMagic[2]
        && data[3] == ElswordMagic[3];
}
