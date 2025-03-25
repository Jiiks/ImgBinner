# ImgBinner
Simple application to bin a directory of images into a single binary to use with ImGui

# Does not store image size so modify the header and parses to do so until I do

Usage just drop a folder containing your images on the exe and then in your app:

```cs

struct ImgBinEntry {
    public string Name;
    public int Offset;
    public int Length;
}

struct Texture2D {
    public string Name;
    public uint Texture;
}

private static IEnumerable<ImgBinEntry> Parseheader(byte[] bytes) {
    var header = Encoding.ASCII.GetString(bytes);
    var list = new List<ImgBinEntry>();
    int start = 0;
    while ((start = header.IndexOf('[', start)) != -1) {
        int end = header.IndexOf(']', start + 1);
        if (end == -1) break;

        string entry = header.Substring(start + 1, end - start - 1);
        string[] parts = entry.Split('@', '^');
        if (parts.Length == 3) {
            string filename = parts[0];
            int offset = int.Parse(parts[1]);
            int length = int.Parse(parts[2]);
            // Add to the file list
            list.Add(new ImgBinEntry {
                Name = filename,
                Offset = offset,
                Length = length
            });
        }

        start = end + 1;
    }

    return list;
}


public unsafe static IEnumerable<Texture2D> LoadBinary() {
    List<Texture2D> textures = [];
    var idx = Resources.imgidx; // Replace with your index location
    var bytes = Resources.imgbin;  // Replace with your binary location
    var header = Parseheader(idx);

    foreach (var entry in header) {
        var filebytes = new byte[entry.Length];
        Array.Copy(bytes, entry.Offset, filebytes, 0, filebytes.Length);

        fixed (byte* bytePtr = filebytes) {
            var tex = CreateTexture(bytePtr, 128, 128); // Replace with your CreateTexture
            textures.Add(new Texture2D {
                Name = entry.Name,
                Texture = tex
            });
        }

    }

    return textures;
}

```

```cs
// OpenTK CreateTexture
private static unsafe uint CreateTexture(byte* imageData, int width, int height) {
    // Generate a texture ID
    GL.GenTextures(1, out uint texture);

    // Bind the texture to the 2D texture target
    GL.BindTexture(TextureTarget.Texture2D, texture);

    // Upload the texture data
    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                  PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)imageData);

    // Set texture parameters
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

    // Unbind the texture to prevent accidental modifications
    GL.BindTexture(TextureTarget.Texture2D, 0);

    // Return the generated texture ID
    return texture;
}
```