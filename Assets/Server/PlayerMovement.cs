using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField]    private float moveSpeed = 8;
    [SerializeField]    private float rotationSpeed = 500;
    [SerializeField]    private float positionRange = 5;

    private Animator animator;

    [SerializeField] private GameObject serverSkill;
    [SerializeField] private NetworkSkillPool skillPool;
     


    void Start()
    {
        skillPool = FindFirstObjectByType<NetworkSkillPool>();
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        float x = Random.Range(positionRange, -positionRange);
        float z = Random.Range(positionRange, -positionRange);

        transform.position = new Vector3(x, 0, z);

        Vector3 dir = Camera.main.transform.position- transform.position;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        float h_input = Input.GetAxis("Horizontal");
        float v_input = Input.GetAxis("Vertical");

        Vector3 moveDir = new Vector3(h_input, 0, v_input);
        moveDir.Normalize();

        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

        if(moveDir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, rotationSpeed * Time.deltaTime);
        }

        animator.SetFloat("run", moveDir.magnitude);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            RequestFireServerRpc();
        }
    }

    [ServerRpc]
    void RequestFireServerRpc()
    {
        NetworkObject netObj = skillPool.Instantiate(OwnerClientId, transform.position + transform.rotation * new Vector3(0, 1, 2), transform.rotation);

        if (!netObj.IsSpawned)
        {
            netObj.Spawn();
        }

        netObj.GetComponent<ServerSkill>().FireClientRpc();
    }
}
