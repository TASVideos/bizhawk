// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace NymaTypes
{

public enum NInputExtra : byte
{
  NONE = 0,
  Button = 1,
  Axis = 2,
  Switch = 3,
  Status = 4,
};

public class NInputExtraUnion {
  public NInputExtra Type { get; set; }
  public object Value { get; set; }

  public NInputExtraUnion() {
    this.Type = NInputExtra.NONE;
    this.Value = null;
  }

  public T As<T>() where T : class { return this.Value as T; }
  public NymaTypes.NButtonInfoT AsButton() { return this.As<NymaTypes.NButtonInfoT>(); }
  public static NInputExtraUnion FromButton(NymaTypes.NButtonInfoT _button) { return new NInputExtraUnion{ Type = NInputExtra.Button, Value = _button }; }
  public NymaTypes.NAxisInfoT AsAxis() { return this.As<NymaTypes.NAxisInfoT>(); }
  public static NInputExtraUnion FromAxis(NymaTypes.NAxisInfoT _axis) { return new NInputExtraUnion{ Type = NInputExtra.Axis, Value = _axis }; }
  public NymaTypes.NSwitchInfoT AsSwitch() { return this.As<NymaTypes.NSwitchInfoT>(); }
  public static NInputExtraUnion FromSwitch(NymaTypes.NSwitchInfoT _switch) { return new NInputExtraUnion{ Type = NInputExtra.Switch, Value = _switch }; }
  public NymaTypes.NStatusInfoT AsStatus() { return this.As<NymaTypes.NStatusInfoT>(); }
  public static NInputExtraUnion FromStatus(NymaTypes.NStatusInfoT _status) { return new NInputExtraUnion{ Type = NInputExtra.Status, Value = _status }; }

  public static int Pack(Google.FlatBuffers.FlatBufferBuilder builder, NInputExtraUnion _o) {
    switch (_o.Type) {
      default: return 0;
      case NInputExtra.Button: return NymaTypes.NButtonInfo.Pack(builder, _o.AsButton()).Value;
      case NInputExtra.Axis: return NymaTypes.NAxisInfo.Pack(builder, _o.AsAxis()).Value;
      case NInputExtra.Switch: return NymaTypes.NSwitchInfo.Pack(builder, _o.AsSwitch()).Value;
      case NInputExtra.Status: return NymaTypes.NStatusInfo.Pack(builder, _o.AsStatus()).Value;
    }
  }
}


}
