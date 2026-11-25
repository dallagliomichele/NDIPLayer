using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = System.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using MapMode = SharpDX.Direct3D11.MapMode;

internal static class HapGpuBench {
    const string Shader = @"
Texture2D source : register(t0);
float4 VS(uint id : SV_VertexID) : SV_POSITION {
    float2 p = float2((id << 1) & 2, id & 2);
    return float4(p.x * 2 - 1, 1 - p.y * 2, 0, 1);
}
float4 Hap(float4 p : SV_POSITION) : SV_Target {
    return float4(source.Load(int3(int2(p.xy), 0)).rgb, 1);
}
float4 HapQ(float4 p : SV_POSITION) : SV_Target {
    float4 sample = source.Load(int3(int2(p.xy), 0));
    float2 chroma = (sample.rg - 128.0 / 255.0) / (1 + sample.b * (255.0 / 8.0));
    return float4(sample.a + float3(chroma.x - chroma.y, chroma.y, -chroma.x - chroma.y), 1);
}";

    static bool Read(Stream input, byte[] buffer, int count, bool allowEof) {
        int offset = 0;
        while (offset < count) {
            int n = input.Read(buffer, offset, count - offset);
            if (n == 0) {
                if (allowEof && offset == 0) return false;
                throw new EndOfStreamException("Incomplete Hap section.");
            }
            offset += n;
        }
        return true;
    }

    static int UInt(byte[] data, ref int at, int end, int bytes) {
        if (bytes > end - at) throw new InvalidDataException("Truncated Hap or Snappy data.");
        uint value = 0;
        for (int i = 0; i < bytes; i++) value |= (uint)data[at++] << (8 * i);
        if (value > Int32.MaxValue) throw new InvalidDataException("Hap or Snappy value too large.");
        return (int)value;
    }

    static int Section(byte[] data, ref int at, int end, out int type) {
        int length = UInt(data, ref at, end, 3);
        type = UInt(data, ref at, end, 1);
        if (length == 0) length = UInt(data, ref at, end, 4);
        return length;
    }

    static int UnpackChunk(byte[] data, int at, int size, int compressor, byte[] blocks, int output) {
        if (compressor == 10) {
            if (size > blocks.Length - output) throw new InvalidDataException("Hap texture overflow.");
            Buffer.BlockCopy(data, at, blocks, output, size);
            return output + size;
        }
        if (compressor != 11) throw new InvalidDataException("Unsupported Hap compressor.");

        int end = at + size, length = 0, shift = 0;
        int value;
        do {
            value = UInt(data, ref at, end, 1);
            if (shift > 28 || shift == 28 && value > 7) throw new InvalidDataException("Invalid Snappy length.");
            length |= (value & 127) << shift;
            shift += 7;
        } while ((value & 128) != 0);
        if (length > blocks.Length - output) throw new InvalidDataException("Snappy texture overflow.");
        int first = output, limit = output + length;
        while (at < end) {
            int tag = data[at++], kind = tag & 3, count = (tag >> 2) + 1;
            if (kind == 0) {
                if (count > 60) count = checked(UInt(data, ref at, end, count - 60) + 1);
                if (count > end - at || count > limit - output) throw new InvalidDataException("Invalid Snappy literal.");
                Buffer.BlockCopy(data, at, blocks, output, count);
                at += count;
            } else {
                int distance;
                if (kind == 1) {
                    count = ((tag >> 2) & 7) + 4;
                    distance = ((tag & 224) << 3) | UInt(data, ref at, end, 1);
                } else {
                    distance = UInt(data, ref at, end, kind == 2 ? 2 : 4);
                }
                if (distance <= 0 || distance > output - first || count > limit - output)
                    throw new InvalidDataException("Invalid Snappy back-reference.");
                for (int i = 0; i < count; i++) blocks[output + i] = blocks[output + i - distance];
            }
            output += count;
        }
        if (output != limit) throw new InvalidDataException("Incomplete Snappy chunk.");
        return output;
    }

    static void UnpackFrame(byte[] data, int size, int compressor, byte[] blocks) {
        int output = 0;
        if (compressor != 12) {
            output = UnpackChunk(data, 0, size, compressor, blocks, 0);
        } else {
            int at = 0, type;
            int instructionsLength = Section(data, ref at, size, out type);
            if (type != 1 || instructionsLength > size - at) throw new InvalidDataException("Invalid Hap chunk instructions.");
            int payload = at + instructionsLength;
            int compressors = -1, sizes = -1, offsets = -1, count = 0, sizesLength = 0, offsetsLength = 0;
            while (at < payload) {
                int length = Section(data, ref at, payload, out type);
                if (length > payload - at) throw new InvalidDataException("Truncated Hap chunk table.");
                switch (type) {
                    case 2: compressors = at; count = length; break;
                    case 3: sizes = at; sizesLength = length; break;
                    case 4: offsets = at; offsetsLength = length; break;
                }
                at += length;
            }
            if (compressors < 0 || sizes < 0 || count < 1 || (long)count * 4 != sizesLength ||
                (offsets >= 0 && sizesLength != offsetsLength))
                throw new InvalidDataException("Invalid Hap chunk tables.");
            int sequential = 0;
            for (int i = 0; i < count; i++) {
                int length = UInt(data, ref sizes, payload, 4);
                int offset = offsets >= 0 ? UInt(data, ref offsets, payload, 4) : sequential;
                if (offset > size - payload || length > size - payload - offset)
                    throw new InvalidDataException("Invalid Hap chunk bounds.");
                output = UnpackChunk(data, payload + offset, length, data[compressors + i], blocks, output);
                sequential = offset + length;
            }
        }
        if (output != blocks.Length) throw new InvalidDataException("Incomplete Hap texture.");
    }

    static int Main(string[] args) {
        try {
            int width = Int32.Parse(args[2]), height = Int32.Parse(args[3]);
            bool hapQ = args[4] == "hap-q";
            var start = new ProcessStartInfo(args[0], "-hide_banner -loglevel error -nostdin -i \"" +
                args[1] + "\" -map 0:v:0 -c copy -f data pipe:1") {
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            };
            using (var demuxer = Process.Start(start)) {
                try {
                    Stream output = args.Length > 5 ? Stream.Null : Console.OpenStandardOutput();
                    int frames = Run(demuxer.StandardOutput.BaseStream, width, height, hapQ, output);
                    Console.Error.WriteLine("frames={0}", frames);
                    demuxer.WaitForExit();
                    return demuxer.ExitCode;
                } finally {
                    if (!demuxer.HasExited) demuxer.Kill();
                }
            }
        } catch (Exception e) {
            Console.Error.WriteLine(e);
            return 1;
        }
    }

    static int Run(Stream input, int width, int height, bool hapQ, Stream output) {
        int frames = 0;
        int pitch = width / 4 * (hapQ ? 16 : 8);
        var blocks = new byte[pitch * (height / 4)];
        var header = new byte[4];
        var section = new byte[0];
        var pixels = new byte[width * height * 4];
        var description = new Texture2DDescription {
            Width = width, Height = height, MipLevels = 1, ArraySize = 1,
            SampleDescription = new SampleDescription(1, 0), Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource, Format = hapQ ? Format.BC3_UNorm : Format.BC1_UNorm
        };
        using (var device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport))
        using (var context = device.ImmediateContext)
        using (var vertexCode = ShaderBytecode.Compile(Shader, "VS", "vs_4_0"))
        using (var pixelCode = ShaderBytecode.Compile(Shader, hapQ ? "HapQ" : "Hap", "ps_4_0"))
        using (var vertex = new VertexShader(device, vertexCode))
        using (var pixel = new PixelShader(device, pixelCode))
        using (var texture = new Texture2D(device, description))
        using (var view = new ShaderResourceView(device, texture)) {
            description.Format = Format.B8G8R8A8_UNorm;
            description.BindFlags = BindFlags.RenderTarget;
            using (var target = new Texture2D(device, description))
            using (var targetView = new RenderTargetView(device, target)) {
                description.Usage = ResourceUsage.Staging;
                description.BindFlags = BindFlags.None;
                description.CpuAccessFlags = CpuAccessFlags.Read;
                using (var staging = new Texture2D(device, description)) {
                    context.OutputMerger.SetRenderTargets(targetView);
                    context.Rasterizer.SetViewport(0, 0, width, height);
                    context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                    context.VertexShader.Set(vertex);
                    context.PixelShader.Set(pixel);
                    var pin = GCHandle.Alloc(blocks, GCHandleType.Pinned);
                    try {
                        while (Read(input, header, 4, true)) {
                            int size = header[0] | header[1] << 8 | header[2] << 16;
                            int type = header[3];
                            if (size == 0) {
                                Read(input, header, 4, false);
                                size = BitConverter.ToInt32(header, 0);
                            }
                            if ((type & 15) != (hapQ ? 15 : 11) || size <= 0 || size > 6L * blocks.Length + 4096)
                                throw new InvalidDataException("Invalid Hap texture section.");
                            if ((type >> 4) == 10 && size == blocks.Length) {
                                Read(input, blocks, size, false);
                            } else {
                                if (section.Length < size) Array.Resize(ref section, size);
                                Read(input, section, size, false);
                                UnpackFrame(section, size, type >> 4, blocks);
                            }
                            context.UpdateSubresource(new DataBox(pin.AddrOfPinnedObject(), pitch, 0), texture, 0);
                            context.PixelShader.SetShaderResource(0, view);
                            context.Draw(3, 0);
                            context.PixelShader.SetShaderResource(0, null);
                            context.CopyResource(target, staging);
                            var data = context.MapSubresource(staging, 0, MapMode.Read, MapFlags.None);
                            try {
                                for (int y = 0; y < height; y++)
                                    Marshal.Copy(IntPtr.Add(data.DataPointer, y * data.RowPitch), pixels, y * width * 4, width * 4);
                            } finally {
                                context.UnmapSubresource(staging, 0);
                            }
                            output.Write(pixels, 0, pixels.Length);
                            frames++;
                        }
                    } finally {
                        pin.Free();
                    }
                }
            }
        }
        return frames;
    }
}
