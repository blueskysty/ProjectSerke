using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

// 네트워크 연결(Host/Client) 및 접속자 수 표시를 관리하는 클래스입니다.
public class NetworkUI : NetworkBehaviour
{
    [SerializeField] private Button hostButton;     // 호스트(서버+클라이언트) 시작 버튼
    [SerializeField] private Button clientButton;   // 클라이언트 시작 버튼

    [SerializeField] private TextMeshProUGUI playerCountText;   // 접속자 수 표시 텍스트

    // [NetworkVariable] 모든 클라이언트가 읽을 수 있는 동기화된 접속자 수 데이터
    private NetworkVariable<int> playersNumber = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);

    private void Awake()
    {
        // 호스트 시작: 서버 역할과 플레이어 역할을 동시에 수행
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });

        // 클라이언트 시작: 이미 실행 중인 서버에 접속
        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });
    }

    private void Update()
    {
        // UI 갱신: 현재 동기화된 NetworkVariable 값을 화면에 출력
        // (참고: OnValueChanged 콜백으로 최적화 가능)
        playerCountText.text = $"Players : {playersNumber.Value}";

        // 오직 서버(Host 포함)만 접속자 수를 계산하여 값을 갱신함
        if (!IsServer)
        {
            return;
        }

        // 현재 연결된 클라이언트 리스트의 개수를 네트워크 변수에 할당 (동기화 발생)
        playersNumber.Value = NetworkManager.Singleton.ConnectedClients.Count;
    }
}
