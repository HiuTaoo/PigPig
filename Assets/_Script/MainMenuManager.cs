using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Script
{
    public class MainMenuManager : MonoBehaviour
    {
        private const string CurrentLevelKey = "CURRENT_LEVEL";

        [SerializeField] private TextMeshProUGUI leveltext;
        private int savedLevel = 1;

        private void Awake()
        {
            savedLevel = PlayerPrefs.GetInt(CurrentLevelKey, 1);

            if (leveltext != null)
            {
                leveltext.text = $"Level {savedLevel}";
            }
        }

        private void Update()
        {
            bool isMobileTouch = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            bool isMouseClick = Input.GetMouseButtonDown(0);

            if (isMobileTouch || isMouseClick)
            {
                LoadGameScene();
            }
        }

        private void LoadGameScene()
        {
            SceneManager.LoadScene(1);
        }
    }
}