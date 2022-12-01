// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace NymaTypes
{

using global::System;
using global::System.Collections.Generic;
using global::Google.FlatBuffers;

public struct NPortInfo : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_22_9_24(); }
  public static NPortInfo GetRootAsNPortInfo(ByteBuffer _bb) { return GetRootAsNPortInfo(_bb, new NPortInfo()); }
  public static NPortInfo GetRootAsNPortInfo(ByteBuffer _bb, NPortInfo obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public NPortInfo __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public string ShortName { get { int o = __p.__offset(4); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetShortNameBytes() { return __p.__vector_as_span<byte>(4, 1); }
#else
  public ArraySegment<byte>? GetShortNameBytes() { return __p.__vector_as_arraysegment(4); }
#endif
  public byte[] GetShortNameArray() { return __p.__vector_as_array<byte>(4); }
  public string FullName { get { int o = __p.__offset(6); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetFullNameBytes() { return __p.__vector_as_span<byte>(6, 1); }
#else
  public ArraySegment<byte>? GetFullNameBytes() { return __p.__vector_as_arraysegment(6); }
#endif
  public byte[] GetFullNameArray() { return __p.__vector_as_array<byte>(6); }
  public string DefaultDeviceShortName { get { int o = __p.__offset(8); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetDefaultDeviceShortNameBytes() { return __p.__vector_as_span<byte>(8, 1); }
#else
  public ArraySegment<byte>? GetDefaultDeviceShortNameBytes() { return __p.__vector_as_arraysegment(8); }
#endif
  public byte[] GetDefaultDeviceShortNameArray() { return __p.__vector_as_array<byte>(8); }
  public NymaTypes.NDeviceInfo? Devices(int j) { int o = __p.__offset(10); return o != 0 ? (NymaTypes.NDeviceInfo?)(new NymaTypes.NDeviceInfo()).__assign(__p.__indirect(__p.__vector(o) + j * 4), __p.bb) : null; }
  public int DevicesLength { get { int o = __p.__offset(10); return o != 0 ? __p.__vector_len(o) : 0; } }

  public static Offset<NymaTypes.NPortInfo> CreateNPortInfo(FlatBufferBuilder builder,
      StringOffset ShortNameOffset = default(StringOffset),
      StringOffset FullNameOffset = default(StringOffset),
      StringOffset DefaultDeviceShortNameOffset = default(StringOffset),
      VectorOffset DevicesOffset = default(VectorOffset)) {
    builder.StartTable(4);
    NPortInfo.AddDevices(builder, DevicesOffset);
    NPortInfo.AddDefaultDeviceShortName(builder, DefaultDeviceShortNameOffset);
    NPortInfo.AddFullName(builder, FullNameOffset);
    NPortInfo.AddShortName(builder, ShortNameOffset);
    return NPortInfo.EndNPortInfo(builder);
  }

  public static void StartNPortInfo(FlatBufferBuilder builder) { builder.StartTable(4); }
  public static void AddShortName(FlatBufferBuilder builder, StringOffset ShortNameOffset) { builder.AddOffset(0, ShortNameOffset.Value, 0); }
  public static void AddFullName(FlatBufferBuilder builder, StringOffset FullNameOffset) { builder.AddOffset(1, FullNameOffset.Value, 0); }
  public static void AddDefaultDeviceShortName(FlatBufferBuilder builder, StringOffset DefaultDeviceShortNameOffset) { builder.AddOffset(2, DefaultDeviceShortNameOffset.Value, 0); }
  public static void AddDevices(FlatBufferBuilder builder, VectorOffset DevicesOffset) { builder.AddOffset(3, DevicesOffset.Value, 0); }
  public static VectorOffset CreateDevicesVector(FlatBufferBuilder builder, Offset<NymaTypes.NDeviceInfo>[] data) { builder.StartVector(4, data.Length, 4); for (int i = data.Length - 1; i >= 0; i--) builder.AddOffset(data[i].Value); return builder.EndVector(); }
  public static VectorOffset CreateDevicesVectorBlock(FlatBufferBuilder builder, Offset<NymaTypes.NDeviceInfo>[] data) { builder.StartVector(4, data.Length, 4); builder.Add(data); return builder.EndVector(); }
  public static VectorOffset CreateDevicesVectorBlock(FlatBufferBuilder builder, ArraySegment<Offset<NymaTypes.NDeviceInfo>> data) { builder.StartVector(4, data.Count, 4); builder.Add(data); return builder.EndVector(); }
  public static VectorOffset CreateDevicesVectorBlock(FlatBufferBuilder builder, IntPtr dataPtr, int sizeInBytes) { builder.StartVector(1, sizeInBytes, 1); builder.Add<Offset<NymaTypes.NDeviceInfo>>(dataPtr, sizeInBytes); return builder.EndVector(); }
  public static void StartDevicesVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(4, numElems, 4); }
  public static Offset<NymaTypes.NPortInfo> EndNPortInfo(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<NymaTypes.NPortInfo>(o);
  }
  public NPortInfoT UnPack() {
    var _o = new NPortInfoT();
    this.UnPackTo(_o);
    return _o;
  }
  public void UnPackTo(NPortInfoT _o) {
    _o.ShortName = this.ShortName;
    _o.FullName = this.FullName;
    _o.DefaultDeviceShortName = this.DefaultDeviceShortName;
    _o.Devices = new List<NymaTypes.NDeviceInfoT>();
    for (var _j = 0; _j < this.DevicesLength; ++_j) {_o.Devices.Add(this.Devices(_j).HasValue ? this.Devices(_j).Value.UnPack() : null);}
  }
  public static Offset<NymaTypes.NPortInfo> Pack(FlatBufferBuilder builder, NPortInfoT _o) {
    if (_o == null) return default(Offset<NymaTypes.NPortInfo>);
    var _ShortName = _o.ShortName == null ? default(StringOffset) : builder.CreateString(_o.ShortName);
    var _FullName = _o.FullName == null ? default(StringOffset) : builder.CreateString(_o.FullName);
    var _DefaultDeviceShortName = _o.DefaultDeviceShortName == null ? default(StringOffset) : builder.CreateString(_o.DefaultDeviceShortName);
    var _Devices = default(VectorOffset);
    if (_o.Devices != null) {
      var __Devices = new Offset<NymaTypes.NDeviceInfo>[_o.Devices.Count];
      for (var _j = 0; _j < __Devices.Length; ++_j) { __Devices[_j] = NymaTypes.NDeviceInfo.Pack(builder, _o.Devices[_j]); }
      _Devices = CreateDevicesVector(builder, __Devices);
    }
    return CreateNPortInfo(
      builder,
      _ShortName,
      _FullName,
      _DefaultDeviceShortName,
      _Devices);
  }
}

public class NPortInfoT
{
  public string ShortName { get; set; }
  public string FullName { get; set; }
  public string DefaultDeviceShortName { get; set; }
  public List<NymaTypes.NDeviceInfoT> Devices { get; set; }

  public NPortInfoT() {
    this.ShortName = null;
    this.FullName = null;
    this.DefaultDeviceShortName = null;
    this.Devices = null;
  }
}


}
