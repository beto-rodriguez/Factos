namespace Factos;

[Flags]
public enum ProtocolType
{
    None = 0,
    Tcp = 1 << 0,
    Http = 1 << 1
}