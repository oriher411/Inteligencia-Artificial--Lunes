using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackInput : MonoBehaviour {

    private CharacterAnimations playerAnimation;

    public GameObject attackPoint;

    private PlayerShield shield;

    private CharacterSoundFX soundFX;

	void Awake () {
        playerAnimation = GetComponent<CharacterAnimations>();
        shield = GetComponent<PlayerShield>();

        soundFX = GetComponentInChildren<CharacterSoundFX>();
	}
	
	void Update () {
	
        // defend when J pressed DOWN or Right Click
        if(Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(1)) {

            if(playerAnimation != null) playerAnimation.Defend(true);
            if(shield != null) shield.ActivateShield(true);

        }

        // release Defence when J Is released or Right Click Released
        if(Input.GetKeyUp(KeyCode.J) || Input.GetMouseButtonUp(1)) {

            if(playerAnimation != null) {
                playerAnimation.UnFreezeAnimation();
                playerAnimation.Defend(false);
            }

            if(shield != null) shield.ActivateShield(false);

        }

        // Attack when K is pressed or Left Click
        if(Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(0)) {

            if(Random.Range(0, 2) > 0) {

                if(playerAnimation != null) playerAnimation.Attack_1();
                if(soundFX != null) soundFX.Attack_1();

            } else {

                if(playerAnimation != null) playerAnimation.Attack_2();
                if(soundFX != null) soundFX.Attack_2();

            }

        }

	}

    void Activate_AttackPoint() {
        attackPoint.SetActive(true);
    }

    void Deactivate_AttackPoint() {
        if(attackPoint.activeInHierarchy) {
            attackPoint.SetActive(false);
        }
    }

} // class































