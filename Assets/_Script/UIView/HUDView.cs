using System;
using System.Collections.Generic;
using System.Linq;
using _Script.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Script.UI
{
    public struct HUDViewData
    {
        public int Level;
        public List<Color> Colors;

        public HUDViewData(int level, List<Color> colors)
        {
            this.Level = level;
            this.Colors = colors;
        }
    }

    public class HUDView : UIBaseView
    {
        public TextMeshProUGUI levelText;
        public GameObject colorUIParent;
        public GameObject healthUIParent;

        public Button backButton;
        public Button settingButton;
        public Button toolButton1;
        public Button toolButton2;
        public Button toolButton3;

        // Lưu trữ các Icon Image theo mã Hex của màu gốc
        private readonly Dictionary<string, Image> colorIconMap = new Dictionary<string, Image>();
        private Queue<GameObject> healthIcons = new Queue<GameObject>();

        public override void OnInit()
        {
            base.OnInit();
            backButton.onClick.AddListener(OnBackButtonClick);
            settingButton.onClick.AddListener(OnSettingsButtonClick);
            toolButton1.onClick.AddListener(OnToolButton1Click);
            toolButton2.onClick.AddListener(OnToolButton2Click);
            toolButton3.onClick.AddListener(OnToolButton3Click);
        }

        public override void OnOpen(object args = null)
        {
            base.OnOpen(args);

            if (args is HUDViewData data)
            {
                if (levelText != null) levelText.text = $"{data.Level}";

                // 1. Trả toàn bộ icon cũ về Pool và dọn dẹp map
                ClearActiveIcons();

                // 2. Tạo icon mới và lưu vào Dictionary
                if (data.Colors != null)
                {
                    foreach (var color in data.Colors)
                    {
                        var colorIcon = ObjectPooler.Instance.Get(PrefabConfig.Instance.colorUIIcon,
                            colorUIParent.transform.position,
                            Quaternion.identity, colorUIParent.transform);

                        var colorImage = colorIcon.GetComponent<Image>();
                        colorImage.color = color;

                        // Chuyển mã màu sang Hex string làm Key an toàn
                        string hexKey = ColorUtility.ToHtmlStringRGB(color);
                        colorIconMap[hexKey] = colorImage;
                    }
                }

                var listIcon = LevelManager.Instance.SpawnHealthUI(healthUIParent);
                foreach (var icon in listIcon)
                {
                    healthIcons.Enqueue(icon);
                }
            }
        }

        /// <summary>
        /// Tìm icon theo màu gốc và đổi sang một màu mới
        /// </summary>
        /// <param name="originalColor">Màu ban đầu lúc khởi tạo icon</param>
        /// <param name="newColor">Màu muốn gán đè lên</param>
        public bool TryUpdateIconColor(Color originalColor, Color newColor)
        {
            string hexKey = ColorUtility.ToHtmlStringRGB(originalColor);
            if (colorIconMap.TryGetValue(hexKey, out Image targetImage))
            {
                if (targetImage != null)
                {
                    targetImage.color = newColor;
                    return true;
                }
            }

            Debug.LogWarning($"[HUDView] Không tìm thấy Icon với màu: #{hexKey}");
            return false;
        }

        /// <summary>
        /// Lấy component Image của Icon theo màu
        /// </summary>
        public Image GetIconByColor(Color color)
        {
            string hexKey = ColorUtility.ToHtmlStringRGB(color);
            colorIconMap.TryGetValue(hexKey, out Image targetImage);
            return targetImage;
        }

        private void ClearActiveIcons()
        {
            Transform targetTransform = colorUIParent.transform;
            for (int i = targetTransform.childCount - 1; i >= 0; i--)
            {
                ObjectPooler.Instance.Return(targetTransform.GetChild(i).gameObject);
            }
            colorIconMap.Clear();
            
            Transform healthTransform = healthUIParent.transform;
            for (int i = healthTransform.childCount - 1; i >= 0; i--)
            {
                ObjectPooler.Instance.Return(healthTransform.GetChild(i).gameObject);
            }
            healthIcons.Clear();
        }

        public override void OnClose()
        {
            ClearActiveIcons();
            base.OnClose();
        }

        private void OnBackButtonClick() => GameManager.Instance.BackToMainMenu();
        private void OnSettingsButtonClick() => UIManager.Instance.OpenView<SettingPopupView>(UIID.SettingsPopup);
        private void OnToolButton1Click() => LevelManager.Instance.Tool_FindSingleValidCell();
        private void OnToolButton2Click() => LevelManager.Instance.Tool_MarkInvalidCellsFromPigOrSuggest();
        private void OnToolButton3Click() => LevelManager.Instance.Tool_Mark3InvalidCellsNearValidPlacements();

        private void OnCellCorrectClicked(CellCorrectClickEvent cellCorrectClickEvent)
        {
            TryUpdateIconColor(LevelManager.Instance.GetColorByRegionId(cellCorrectClickEvent.ClickedNode.colorRegionID), Color.white);
        }

        private void OnCellInCorrectClicked(CellInCorrectClickEvent cellInCorrectClickEvent)
        {
            ObjectPooler.Instance.Return(healthIcons.Dequeue());
        }

        private void OnEnable()
        {
            EventManager.Subscribe<CellCorrectClickEvent>(OnCellCorrectClicked);
            EventManager.Subscribe<CellInCorrectClickEvent>(OnCellInCorrectClicked);
        }
        
        private void OnDisable()
        {
            EventManager.Unsubscribe<CellCorrectClickEvent>(OnCellCorrectClicked);
            EventManager.Unsubscribe<CellInCorrectClickEvent>(OnCellInCorrectClicked);
        }

        private void OnDestroy()
        {
            backButton.onClick.RemoveListener(OnBackButtonClick);
            settingButton.onClick.RemoveListener(OnSettingsButtonClick);
            toolButton1.onClick.RemoveListener(OnToolButton1Click);
            toolButton2.onClick.RemoveListener(OnToolButton2Click);
            toolButton3.onClick.RemoveListener(OnToolButton3Click);
        }
        
    }
}