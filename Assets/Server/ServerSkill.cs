using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using Unity.Netcode;

public class ServerSkill : NetworkBehaviour
{
    [SerializeField] private GameObject skillObject;
    [SerializeField] private ServerSkillEffect skillEffect;
    [SerializeField] private float skillSpeed;

    private Rigidbody rb;
    private bool canforward;    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log($"[Start] {gameObject.name} (ID: {GetInstanceID()})");
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer && canforward)
        {
            rb.linearVelocity = rb.transform.forward * skillSpeed;
        }
    }

    [ClientRpc] 
    public void FireClientRpc()
    {
        canforward = true;
        skillObject.SetActive(true);
        skillEffect.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
        {
            return;
        }

        bool isPlayer = other.CompareTag("Player");
        bool isController = other.CompareTag("GameController");

        if (isPlayer || isController)
        {
            if (isPlayer)
            {
            }

            StopProjectile();

            ApplyHitEffectClientRpc();

            StartCoroutine(EndTimer());
        }
    }

    private void StopProjectile()
    {
        canforward = false;
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    [ClientRpc]
    void ApplyHitEffectClientRpc()
    {
        Debug.Log("Hit");
        skillObject.SetActive(false);
        skillEffect.gameObject.SetActive(true);
        
        if (!IsServer)
        {
            StopProjectile();
        }
    }

    IEnumerator EndTimer()
    {
        yield return new WaitForSeconds(1f);

        if (IsServer)
        {
            ForceDisableClientRpc();

            var networkObj = GetComponent<NetworkObject>();
            if ( networkObj != null && networkObj.IsSpawned)
            {
                networkObj.Despawn(false);
            }
        }
    }

    [ClientRpc]
    void ForceDisableClientRpc()
    {
        gameObject.SetActive(false);
    }
}
