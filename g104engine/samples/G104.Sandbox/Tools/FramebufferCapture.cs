using System.IO.Compression;
using System.Buffers.Binary;
using OpenTK.Graphics.OpenGL4;

namespace G104.Sandbox.Tools;

internal static class FramebufferCapture
{
    public static void Save(string path,int width,int height)
    {
        byte[] pixels=new byte[checked(width*height*4)];
        GL.PixelStore(PixelStoreParameter.PackAlignment,1);
        GL.ReadPixels(0,0,width,height,PixelFormat.Rgba,PixelType.UnsignedByte,pixels);
        using var raw=new MemoryStream();
        using(var compressed=new ZLibStream(raw,CompressionLevel.Fastest,true))
            for(int y=height-1;y>=0;y--) { compressed.WriteByte(0); compressed.Write(pixels,y*width*4,width*4); }
        using var output=new MemoryStream(); output.Write(new byte[]{137,80,78,71,13,10,26,10});
        byte[] header=new byte[13]; BinaryPrimitives.WriteInt32BigEndian(header,width); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4),height);
        header[8]=8;header[9]=6; Chunk(output,"IHDR",header);Chunk(output,"IDAT",raw.ToArray());Chunk(output,"IEND",[]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path,output.ToArray());
    }
    private static void Chunk(Stream stream,string tag,byte[] data)
    {
        Span<byte> size=stackalloc byte[4]; BinaryPrimitives.WriteInt32BigEndian(size,data.Length); stream.Write(size);
        byte[] name=System.Text.Encoding.ASCII.GetBytes(tag);stream.Write(name);stream.Write(data);
        uint crc=uint.MaxValue;
        foreach(byte value in name.Concat(data)) { crc^=value;for(int i=0;i<8;i++) crc=(crc>>1)^((crc&1)!=0 ? 0xedb88320u : 0); }
        BinaryPrimitives.WriteUInt32BigEndian(size,~crc);stream.Write(size);
    }
}
