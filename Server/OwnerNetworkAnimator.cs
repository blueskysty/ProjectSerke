using UnityEngine;
using Unity.Netcode.Components;

public class OwnerNetworkAnimator : NetworkAnimator
{
    // 소유자 권한(Owner Authority) 기반의 네트워크 애니메이터입니다.
    // 기본 NetworkAnimator와 달리, 서버가 아닌 '내 캐릭터'가 애니메이션 상태를 주도합니다.

    protected override bool OnIsServerAuthoritative()
    {
        // 애니메이션 동기화 권한이 서버에 있는지 여부를 결정합니다.
        // false를 반환하여 소유자(Owner) 클라이언트가 애니메이션 파라미터를 변경할 권한을 갖도록 설정합니다.
        // <returns>false: 소유자 클라이언트 권한 / true: 서버 권한</returns>
        return false;
    }  
}
