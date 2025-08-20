namespace ASRClientCore.Models.Enums
{
    public enum ResponseStatus : uint
    {
        WriteError,
        ReadError,
        InvalidOrUnknown,
        Okey = 0x59454B4F, //"OKEY"
        Fail = 0x4C494146, //"FAIL"
    }
}
