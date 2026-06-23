using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    private Transform target;

    [SerializeField]
    private Vector3 offsetPosition;
	
	void Awake () {
        // En lugar de buscar por Tag (que puede confundirse con enemigos mal tageados),
        // buscamos específicamente el componente del jugador (Warrior).
        PlayerMove playerScript = Object.FindFirstObjectByType<PlayerMove>();
        
        if(playerScript != null) {
            target = playerScript.transform;
        } else {
            // Un plan B en caso de emergencia
            target = GameObject.FindGameObjectWithTag(Tags.PLAYER_TAG).transform;
        }
	}
	
	void LateUpdate () {
        FollowPlayer();
    }

    void FollowPlayer() {

        transform.position = target.TransformPoint(offsetPosition);

        transform.rotation = target.rotation;

    }


} // class

























