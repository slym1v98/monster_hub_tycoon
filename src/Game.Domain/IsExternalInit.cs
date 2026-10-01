// Polyfill để dùng record/init trên netstandard2.1 (C# 9). Không đổi hành vi, chỉ để compiler chấp nhận.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
