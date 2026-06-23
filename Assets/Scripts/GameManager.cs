using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    private int enemiesDefeated;
    private int totalEnemies;
    private bool gameEnded;

    void Start() {
        HealthScript[] healthScripts = Object.FindObjectsByType<HealthScript>(FindObjectsSortMode.None);

        foreach (HealthScript healthScript in healthScripts) {
            if (!healthScript.isPlayer) {
                totalEnemies++;
            }
        }
    }

    public void RegisterEnemyDefeated() {
        if (gameEnded) {
            return;
        }

        enemiesDefeated++;

        if (enemiesDefeated >= totalEnemies) {
            TriggerWin();
        }
    }

    void TriggerWin() {
        gameEnded = true;
        Time.timeScale = 0f;

        PlayerMove playerMove = Object.FindFirstObjectByType<PlayerMove>();
        if (playerMove != null) {
            playerMove.enabled = false;
        }

        PlayerAttackInput playerAttack = Object.FindFirstObjectByType<PlayerAttackInput>();
        if (playerAttack != null) {
            playerAttack.enabled = false;
        }

        EnemyAIHelper.DisableAllEnemyAI();

        ShowWinScreen();
    }

    void ShowWinScreen() {
        GameObject panel = new GameObject("Win Screen");
        panel.transform.SetParent(transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image overlay = panel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.6f);
        overlay.raycastTarget = true;

        GameObject textObject = new GameObject("Win Text");
        textObject.transform.SetParent(panel.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(800f, 120f);

        Text winText = textObject.AddComponent<Text>();
        winText.text = "YOU WIN";
        winText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        winText.fontSize = 72;
        winText.fontStyle = FontStyle.Bold;
        winText.alignment = TextAnchor.MiddleCenter;
        winText.color = Color.white;
    }

}
