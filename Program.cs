﻿using Hexa.NET.StbImage;
using System.Runtime.InteropServices;
using System.Text;

namespace ImgBinner;

public struct ImgBinEntry {
    public string Name;
    public int Index;
    public int Width;
    public int Height;
    public int Channels;
    public int Offset;
    public int Length;
}

public struct Texture2D {
    public string Name;
    public int Width;
    public int Height;
    public int Channels;
    public uint Texture;
}


public static class Tests {
    public static IEnumerable<ImgBinEntry> Parseheader(byte[] bytes) {
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

    public static byte[] ExtractHeader(byte[] bytes) {
        var headerEnd = Array.IndexOf(bytes, (byte)'$');

        if (headerEnd == -1) {
            return [];
        }

        var header = new byte[headerEnd + 1];
        Array.Copy(bytes, 0, header, 0, headerEnd + 1);

        return header;
    }

    public static byte[] ExtractBody(byte[] bytes) {
        var headerEnd = Array.IndexOf(bytes, (byte)'$');

        if (headerEnd == -1 || headerEnd >= bytes.Length - 1) {
            return [];
        }

        var bodyLength = bytes.Length - (headerEnd + 1);
        var body = new byte[bodyLength];

        Array.Copy(bytes, headerEnd + 1, body, 0, bodyLength);

        return body;
    }
}

public unsafe class ImgBinner {
    public const bool DEBUG = true;
    public static void Main(string[] args) {
        if(args.Length <= 0) {
            Console.WriteLine("No directory supplied");
            Console.ReadKey();
            return;
        }
        var wd = args[0];

        if (!Directory.Exists(wd)) {
            Console.WriteLine($"Directory does not exist: {wd}");
            Console.ReadKey();
            return;
        }

        var files = Directory.GetFiles(wd);
        if(files.Length <= 0) {
            Console.WriteLine("No files");
            Console.ReadKey();
            return;
        }

        var wn = new DirectoryInfo(wd).Name;
        
        var header = "";

        List<byte[]> bytes = [];
        int tSize = 0;
        int offset = 0;
        foreach (var file in files) {
            string fn = Path.GetFileNameWithoutExtension(file);
            Console.WriteLine($"Processing: {file}");
            int x, y;
            int channelsInFile = 0;

            byte* image = StbImage.Load(file, &x, &y, ref channelsInFile, 4);
            byte[] data = new byte[x * y * 4];
            Marshal.Copy((IntPtr)image, data, 0, data.Length);
            tSize += data.Length;
            bytes.Add(data);
            header += $"[{fn}@{x}x{y}x{channelsInFile}|{offset}^{data.Length}]";
            offset += data.Length;
        }
        header += '$';
        byte[] headerBytes = Encoding.UTF8.GetBytes(header);
        byte[] merged = new byte[headerBytes.Length + tSize];

        offset = headerBytes.Length;
        Array.Copy(headerBytes, 0, merged, 0, headerBytes.Length);
        foreach (var barr in bytes) {
            Array.Copy(barr, 0, merged, offset, barr.Length);
            offset += barr.Length;
        }

#if DEBUG
        var extractedHeaderBytes = Tests.ExtractHeader(merged);
        var entries = Tests.Parseheader(extractedHeaderBytes);
        foreach (var entry in entries) {
            Console.WriteLine($"{entry.Name} {entry.Width}x{entry.Height}x{entry.Channels} {entry.Offset}-{entry.Offset + entry.Length}[{entry.Length}]");
        }

        var extractedBody = Tests.ExtractBody(merged);
        for(var i = 0; i < 16; i++) {
            Console.Write($"{extractedBody[i]}");
            if (i != 15) Console.Write("-");
        }
        Console.Write('\n');
        Console.WriteLine($"Writing all bytes to {wn}.bin");
        File.WriteAllBytes($"{wn}.bin", merged);
#else
        Console.WriteLine($"Writing all bytes to {wn}.bin");
        File.WriteAllBytes($"{wn}.bin", merged);
#endif

        Console.WriteLine("All done!");
        Console.ReadKey();
    }
}
