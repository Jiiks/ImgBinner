# ImgBinner
Simple application to bin a directory of images into a single binary to use with ImGui

Creates an index file with the following format:
```
[<filename>@<width>x<height>x<channels>|<offset>^<length>]
```

# Usage:
Just drop a folder containing your images on the exe and then in your app:

```cs

struct ImgBinEntry {
    public string Name;
    public int Width;
    public int Height;
    public int Channels;
    public int Offset;
    public int Length;
}

struct Texture2D {
    public string Name;
    public int Width;
    public int Height;
    public int Channels;
    public uint Texture;
}

private static IEnumerable<ImgBinEntry> Parseheader(byte[] bytes) {
    var header = Encoding.ASCII.GetString(bytes);
    var list = new List<ImgBinEntry>();
    var start = 0;
    while ((start = header.IndexOf('[', start)) != -1) {
        var end = header.IndexOf(']', start + 1);
        if (end == -1) break;

        var entry = header.Substring(start + 1, end - start - 1);

        var parts = entry.Split('@', '|', '^');

        //Extract filename, dimensions, start, and length
        var filename = parts[0];
        var dimensions = parts[1].Split('x');
        var width = int.Parse(dimensions[0]);
        var height = int.Parse(dimensions[1]);
        var channels = int.Parse(dimensions[2]);
        var dataStart = int.Parse(parts[2]);
        var dataLength = int.Parse(parts[3]);
        
        // Add to the list as a FileEntry object
        list.Add(new ImgBinEntry {
            Name = filename,
            Width = width,
            Height = height,
            Channels = channels,
            Offset = dataStart,
            Length = dataLength
        });

        start = end + 1;
    }

    return list;
}


public unsafe static IEnumerable<Texture2D> LoadBinary() {
    List<Texture2D> textures = [];
    var idx = Resources.imgidx; // Replace with your index location
    var bytes = Resources.imgbin; // Replace with your binary location
    var header = Parseheader(idx);

    foreach (var entry in header) {
        var filebytes = new byte[entry.Length];
        Array.Copy(bytes, entry.Offset, filebytes, 0, filebytes.Length);

        fixed (byte* bytePtr = filebytes) {
            var tex = CreateTexture(bytePtr, entry.Width, entry.Height); // Replace with your create texture
            textures.Add(new Texture2D {
                Name = entry.Name,
                Width = entry.Width,
                Height = entry.Height,
                Channels = entry.Channels,
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

```cs
// Silk CreateTexture
private static unsafe uint CreateTexture(byte* imageData, int width, int height) {
    _gl.GenTextures(1, out uint texture);
    _gl.BindTexture(GLEnum.Texture2D, texture);
    _gl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba, (uint)width, (uint)height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, imageData);
    _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
    _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
    _gl.BindTexture(GLEnum.Texture2D, 0);
    return texture;
}
```
