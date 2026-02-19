// C# 9.0의 'init' 접근자를 Unity에서 사용하기 위한 폴리필.
// Unity의 .NET 런타임에 IsExternalInit 타입이 없어서 발생하는
// CS0518 에러를 해결한다.

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
