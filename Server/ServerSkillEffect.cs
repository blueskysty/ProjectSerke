using UnityEngine;

public class ServerSkillEffect : MonoBehaviour
{
    /* * [향후 구현 예정 기능]
     * 1. 파티클 시스템(Particle System) 재생 및 제어
     * 2. 스킬 적중/폭발 시 사운드 효과음(AudioSource) 출력
     * 3. 카메라 흔들림(Camera Shake) 등의 화면 연출 신호 전달
     * 4. 적중 지점의 데칼(Decal) 생성 또는 바닥 타격 효과
     */
    void Start()
    {
        // 오브젝트가 활성화될 때 연출을 시작하도록 구성할 예정입니다.
    }

    void Update()
    {
        // 연출의 시간에 따른 변화(크기 조절, 페이드 아웃 등)가 필요할 경우 사용합니다.
    }

    // 연출이 끝난 후 이펙트 오브젝트를 정리하거나 초기화하는 로직을 담습니다.
    public void ResetEffect()
    {
        // 파티클 정지 및 컴포넌트 초기화
    }
}
