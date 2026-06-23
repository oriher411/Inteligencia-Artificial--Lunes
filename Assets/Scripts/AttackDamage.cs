using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackDamage : MonoBehaviour {

    public LayerMask layer;
    public float radius = 1f;
    public float damage = 1f;
	
	void Update () {

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, layer);

        if (hits.Length > 0) {
            HealthScript health = hits[0].GetComponent<HealthScript>();

            if (health != null) {
                health.ApplyDamage(damage);
                gameObject.SetActive(false);
            }
        }

	}



} // class

























