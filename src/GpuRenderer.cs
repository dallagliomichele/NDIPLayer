using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using MapMode = SharpDX.Direct3D11.MapMode;

namespace ImageNdi {
// Offscreen D3D11 renderer. NDI still needs a BGRA byte array, so every result is read back.
internal sealed class GpuRenderer : IDisposable {
 const string Shader = @"
cbuffer Parameters : register(b0) { float4 rect; float4 options; };
Texture2D first : register(t0);
Texture2D second : register(t1);
SamplerState linearSampler : register(s0);
float4 VS(uint id : SV_VertexID) : SV_POSITION {
 float2 p = float2((id << 1) & 2, id & 2);
 return float4(p.x * 2 - 1, 1 - p.y * 2, 0, 1);
}
float4 PS(float4 position : SV_POSITION) : SV_Target {
 int2 xy = int2(position.xy);
 if (options.x < 0.5) {
  float2 uv = (position.xy - rect.xy) / rect.zw;
  if (any(uv < 0) || any(uv >= 1)) return 0;
  return first.Sample(linearSampler, uv);
 }
 float4 a = first.Load(int3(xy, 0));
 float4 b = second.Load(int3(xy, 0));
 float aa = a.a * (1 - options.y);
 float ba = b.a * options.y;
 float alpha = aa + ba;
 return alpha > 0 ? floor(float4((a.rgb * aa + b.rgb * ba) / alpha, alpha) * 255 + 0.5) / 255 : 0;
}";
 readonly Device device;
 readonly DeviceContext context;
 readonly VertexShader vertex;
 readonly PixelShader pixel;
 readonly SamplerState sampler;
 readonly Buffer parameters;
 public readonly string AdapterName;

 public GpuRenderer(){
  using(var factory=new Factory1()){
   var candidates=new List<Adapter1>();
   try {
    foreach(var adapter in factory.Adapters1){
     if((adapter.Description1.Flags & AdapterFlags.Software)!=0){adapter.Dispose();continue;}
     candidates.Add(adapter);
    }
    candidates.Sort((a,b)=>((long)b.Description1.DedicatedVideoMemory).CompareTo((long)a.Description1.DedicatedVideoMemory));
    Exception last=null;
    foreach(var adapter in candidates){
     try {device=new Device(adapter,DeviceCreationFlags.BgraSupport,FeatureLevel.Level_11_0,FeatureLevel.Level_10_1,FeatureLevel.Level_10_0);AdapterName=adapter.Description1.Description;break;}
     catch(Exception e){last=e;}
    }
    if(device==null)throw new InvalidOperationException("Nessuna GPU Direct3D 11 disponibile.",last);
   } finally {foreach(var adapter in candidates)adapter.Dispose();}
  }
  try {
   context=device.ImmediateContext;
   using(var vs=ShaderBytecode.Compile(Shader,"VS","vs_4_0"))vertex=new VertexShader(device,vs);
   using(var ps=ShaderBytecode.Compile(Shader,"PS","ps_4_0"))pixel=new PixelShader(device,ps);
   sampler=new SamplerState(device,new SamplerStateDescription{Filter=Filter.MinMagMipLinear,AddressU=TextureAddressMode.Clamp,AddressV=TextureAddressMode.Clamp,AddressW=TextureAddressMode.Clamp,ComparisonFunction=Comparison.Never,MinimumLod=0,MaximumLod=float.MaxValue});
   parameters=new Buffer(device,32,ResourceUsage.Default,BindFlags.ConstantBuffer,CpuAccessFlags.None,ResourceOptionFlags.None,0);
  } catch {Dispose();throw;}
 }
 static Texture2D MakeTexture(Device device,byte[] bytes,int width,int height){
  var desc=new Texture2DDescription{Width=width,Height=height,MipLevels=1,ArraySize=1,Format=Format.B8G8R8A8_UNorm,SampleDescription=new SampleDescription(1,0),Usage=ResourceUsage.Immutable,BindFlags=BindFlags.ShaderResource,CpuAccessFlags=CpuAccessFlags.None,OptionFlags=ResourceOptionFlags.None};
  var pin=GCHandle.Alloc(bytes,GCHandleType.Pinned);
  try{return new Texture2D(device,desc,new DataRectangle(pin.AddrOfPinnedObject(),width*4));}
  finally{pin.Free();}
 }
 static byte[] Pixels(Image image){
  using(var bitmap=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppArgb)){
   using(var graphics=Graphics.FromImage(bitmap)){graphics.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;graphics.DrawImage(image,0,0,image.Width,image.Height);}
   var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try{var result=new byte[bitmap.Width*bitmap.Height*4];for(int y=0;y<bitmap.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),result,y*bitmap.Width*4,bitmap.Width*4);return result;}
   finally{bitmap.UnlockBits(data);}
  }
 }
 public byte[] Render(Image image,int width,int height,Placement placement){
  if(image==null)return new byte[width*height*4];
  var rect=Renderer.Fit(image.Width,image.Height,width,height,placement);
  rect=Rectangle.Round(rect);
  using(var source=MakeTexture(device,Pixels(image),image.Width,image.Height))return Draw(source,null,width,height,new[]{rect.X,rect.Y,rect.Width,rect.Height,0f,0f,0f,0f});
 }
 public void Blend(byte[] a,byte[] b,double t,byte[] result,int width,int height){
  using(var first=MakeTexture(device,a,width,height))using(var second=MakeTexture(device,b,width,height)){
   var pixels=Draw(first,second,width,height,new[]{0f,0f,0f,0f,1f,(float)t,0f,0f});
   System.Buffer.BlockCopy(pixels,0,result,0,result.Length);
  }
 }
 byte[] Draw(Texture2D first,Texture2D second,int width,int height,float[] constants){
  var outputDescription=new Texture2DDescription{Width=width,Height=height,MipLevels=1,ArraySize=1,Format=Format.B8G8R8A8_UNorm,SampleDescription=new SampleDescription(1,0),Usage=ResourceUsage.Default,BindFlags=BindFlags.RenderTarget,CpuAccessFlags=CpuAccessFlags.None,OptionFlags=ResourceOptionFlags.None};
  var stagingDescription=outputDescription;stagingDescription.Usage=ResourceUsage.Staging;stagingDescription.BindFlags=BindFlags.None;stagingDescription.CpuAccessFlags=CpuAccessFlags.Read;
  using(var output=new Texture2D(device,outputDescription))using(var staging=new Texture2D(device,stagingDescription))using(var target=new RenderTargetView(device,output))using(var viewA=new ShaderResourceView(device,first))using(var viewB=second==null?null:new ShaderResourceView(device,second)){
   context.OutputMerger.SetRenderTargets(target);
   context.Rasterizer.SetViewport(0,0,width,height);
   context.InputAssembler.PrimitiveTopology=PrimitiveTopology.TriangleList;
   context.VertexShader.Set(vertex);
   context.PixelShader.Set(pixel);
   context.PixelShader.SetShaderResource(0,viewA);
   context.PixelShader.SetShaderResource(1,viewB);
   context.PixelShader.SetSampler(0,sampler);
   context.UpdateSubresource(constants,parameters);
   context.PixelShader.SetConstantBuffer(0,parameters);
   context.Draw(3,0);
   context.OutputMerger.SetRenderTargets((RenderTargetView)null);
   context.PixelShader.SetShaderResources(0,null,null);
   context.CopyResource(output,staging);
   var data=context.MapSubresource(staging,0,MapMode.Read,SharpDX.Direct3D11.MapFlags.None);
   try{var result=new byte[width*height*4];for(int y=0;y<height;y++)Marshal.Copy(IntPtr.Add(data.DataPointer,y*data.RowPitch),result,y*width*4,width*4);return result;}
   finally{context.UnmapSubresource(staging,0);}
  }
 }
 public void Dispose(){if(parameters!=null)parameters.Dispose();if(sampler!=null)sampler.Dispose();if(pixel!=null)pixel.Dispose();if(vertex!=null)vertex.Dispose();if(context!=null)context.Dispose();if(device!=null)device.Dispose();}
}
}
