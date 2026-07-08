using System.Runtime.CompilerServices;

// 런타임에서는 숨긴 MT19937 내부 구현을 NUH 테스트 어셈블리만 직접 검증할 수 있게 허용합니다.
[assembly: InternalsVisibleTo("FlatVenture.NUH.Tests.EditMode")]
