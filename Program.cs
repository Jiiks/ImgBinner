using Hexa.NET.StbImage;
using System.Runtime.InteropServices;
using System.Text;

public unsafe class ImgBinner {
    public static void Main(string[] args) {
        if (args.Length <= 0) return;
        if(!Directory.Exists(args[0])) {
            Console.WriteLine($"Directory does not exist: {args[0]}");
            Console.ReadKey();
            return;
        }

        var files = Directory.GetFiles(args[0]);
        if(files.Length <= 0) {
            Console.WriteLine("No files");
            Console.ReadKey();
            return;
        }

        string header = "";
        List<byte[]> bytes = new();
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
            header += $"[{fn}@{offset}^{data.Length}]";
            offset += data.Length;
        }

        byte[] headerBytes = Encoding.UTF8.GetBytes(header);
        byte[] merged = new byte[tSize];

        offset = 0;
        foreach (var barr in bytes) {
            Array.Copy(barr, 0, merged, offset, barr.Length);
            offset += barr.Length;
        }
        Console.WriteLine("Writing all bytes to img.bin and img.idx");
        File.WriteAllBytes("img.bin", merged);
        File.WriteAllBytes("img.idx", headerBytes);
        Console.WriteLine("All done!");
        Console.ReadKey();
    }
}
