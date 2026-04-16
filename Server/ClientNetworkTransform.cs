using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities.ClientAuthority
{
    // 클라이언트 권한(Client Authority) 기반의 네트워크 트랜스폼.
    // 기본 NetworkTransform은 서버가 위치 권한을 가지지만, 
    // 이 클래스를 상속받으면 플레이어 본인이 자신의 위치를 직접 제어합니다.

    [DisallowMultipleComponent] // 한 오브젝트에 중복으로 붙이는 것을 방지
    public class ClientNetworkTransform: NetworkTransform
    {
        // 해당 트랜스폼이 서버 권한인지 여부를 결정하는 메소드입니다.
        // false를 리턴함으로써 서버가 아닌 '소유자 클라이언트(Owner)'가 권한을 갖게 합니다.
        /// <returns>false: 클라이언트 권한 / true: 서버 권한</returns>
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}