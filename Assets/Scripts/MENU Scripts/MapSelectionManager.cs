using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MapSelectionManager : MonoBehaviour
{
    [Header("Map Data")]
    public MapSelectionData[] maps;
    
    [Header("Navigation Buttons")]
    public Button backButton;
    public Button nextButton;
    
    [Header("Initial Selection")]
    [Tooltip("The map icon button to select first when opening (leave empty for first map)")]
    public Button firstSelectedMapButton;
    
    [Header("Selection Colors")]
    public Color selectedBorderColor = Color.cyan;
    public Color unselectedBorderColor = Color.white;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Events for external controllers (optional)
    public System.Action OnBackPressed;
    public System.Action OnNextPressed;
    
    private int selectedMapIndex = 0;
    private bool isActive = false;
    private float lastInputTime = 0f;
    private float inputCooldown = 0.2f;
    private EventSystem eventSystem;
    
    void Start()
    {
        eventSystem = EventSystem.current;
        
        if (eventSystem == null)
        {
            Debug.LogError("No EventSystem found!");
            return;
        }
        
        SetupMapSelection();
        
        // Determine initial map index
        int initialMapIndex = GetInitialMapIndex();
        
        // Start with the specified map selected
        if (maps.Length > 0)
        {
            SelectMap(initialMapIndex);
        }
        
        // Setup button listeners
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);
        if (nextButton != null)
            nextButton.onClick.AddListener(ProceedWithSelectedMap);
        
        if (enableDebugLogs) Debug.Log($"MapSelectionManager initialized, first selected: Map {initialMapIndex}");
    }
    
    void Update()
    {
        if (!isActive) return;
        if (Time.time - lastInputTime < inputCooldown) return;
        
        HandleConfirmInput();
    }
    
    void HandleConfirmInput()
    {
        // Only handle confirm - navigation is done by Unity's explicit navigation system
        if (IsConfirmPressed())
        {
            // Check which button is currently selected and click it
            if (eventSystem.currentSelectedGameObject != null)
            {
                Button currentButton = eventSystem.currentSelectedGameObject.GetComponent<Button>();
                if (currentButton != null && currentButton.interactable)
                {
                    // Check if it's a map icon button
                    for (int i = 0; i < maps.Length; i++)
                    {
                        if (maps[i].iconButton != null)
                        {
                            Button iconButton = maps[i].iconButton.GetComponent<Button>();
                            if (iconButton == currentButton)
                            {
                                SelectMap(i);
                                lastInputTime = Time.time;
                                return;
                            }
                        }
                    }
                    
                    // If not a map button, click whatever is selected
                    currentButton.onClick.Invoke();
                    lastInputTime = Time.time;
                }
            }
        }
    }
    
    bool IsConfirmPressed()
    {
        // Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || 
                Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;
        }
        
        // Gamepad
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;
        }
        
        return false;
    }
    
    int GetInitialMapIndex()
    {
        // If no button specified, default to first map
        if (firstSelectedMapButton == null)
            return 0;
        
        // Find which map has this button
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].iconButton == null) continue;
            
            Button iconButton = maps[i].iconButton.GetComponent<Button>();
            if (iconButton == firstSelectedMapButton)
            {
                if (enableDebugLogs) Debug.Log($"Found firstSelectedMapButton at index {i}");
                return i;
            }
        }
        
        // If button not found, default to first map
        Debug.LogWarning("firstSelectedMapButton not found in maps array, defaulting to first map");
        return 0;
    }
    
    void SetupMapSelection()
    {
        for (int i = 0; i < maps.Length; i++)
        {
            int mapIndex = i;
            
            if (maps[i].mapName == "New Map" || string.IsNullOrEmpty(maps[i].mapName))
            {
                maps[i].mapName = $"Map {i + 1}";
            }
            
            if (maps[i].iconButton == null)
            {
                Debug.LogWarning($"Map {i} ({maps[i].mapName}) has no icon button assigned!");
                continue;
            }
            
            Button iconButton = maps[i].iconButton.GetComponent<Button>();
            
            if (iconButton == null)
            {
                Debug.LogError($"Map {i} ({maps[i].mapName}) icon doesn't have a Button component!");
                continue;
            }
            
            // Set up explicit navigation on buttons - do this in Unity Inspector
            // The script just handles the click
            iconButton.onClick.AddListener(() => SelectMap(mapIndex));
            
            if (enableDebugLogs) Debug.Log($"Map {i} ({maps[i].mapName}) setup complete");
        }
    }
    
    public void Activate()
    {
        isActive = true;
        
        // Reset to the first selected map
        int initialMapIndex = GetInitialMapIndex();
        selectedMapIndex = initialMapIndex;
        SelectMap(selectedMapIndex);
        
        // Select the first map button visually
        if (maps.Length > 0 && maps[selectedMapIndex].iconButton != null)
        {
            Button iconButton = maps[selectedMapIndex].iconButton.GetComponent<Button>();
            if (iconButton != null)
            {
                iconButton.Select();
            }
        }
        
        if (enableDebugLogs) Debug.Log($"MapSelectionManager activated, starting at Map {selectedMapIndex}");
    }
    
    public void Deactivate()
    {
        isActive = false;
        
        if (enableDebugLogs) Debug.Log("MapSelectionManager deactivated");
    }
    
    public void SelectMap(int mapIndex)
    {
        if (mapIndex < 0 || mapIndex >= maps.Length)
        {
            Debug.LogError($"Invalid map index: {mapIndex}");
            return;
        }
        
        selectedMapIndex = mapIndex;
        UpdateBorderColors();
        UpdateMapInfoVisibility();
        
        if (enableDebugLogs) Debug.Log($"Selected Map: {maps[mapIndex].mapName}");
    }
    
    void UpdateBorderColors()
    {
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].border == null) continue;
            
            Image borderImage = maps[i].border.GetComponent<Image>();
            if (borderImage != null)
            {
                borderImage.color = (i == selectedMapIndex) ? selectedBorderColor : unselectedBorderColor;
            }
        }
    }
    
    void UpdateMapInfoVisibility()
    {
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].mapInfoFolder == null) continue;
            maps[i].mapInfoFolder.SetActive(i == selectedMapIndex);
        }
    }
    
    void OnBackButtonClicked()
    {
        OnBackPressed?.Invoke();
        
        if (enableDebugLogs) Debug.Log("Back button clicked");
    }
    
    void ProceedWithSelectedMap()
    {
        SaveSelectedMap();
        OnNextPressed?.Invoke();
        
        if (enableDebugLogs) Debug.Log("Next button clicked");
    }
    
    void SaveSelectedMap()
    {
        MapSelectionData selectedMap = maps[selectedMapIndex];
        PlayerPrefs.SetString("SelectedMapName", selectedMap.mapName);
        PlayerPrefs.SetInt("SelectedMapIndex", selectedMapIndex);
        PlayerPrefs.SetString("SelectedMapSceneName", selectedMap.sceneToLoad);
        PlayerPrefs.Save();
        
        if (enableDebugLogs) Debug.Log($"Saved map selection: {selectedMap.mapName}");
    }
    
    public MapSelectionData GetSelectedMap() => maps[selectedMapIndex];
    public int GetSelectedMapIndex() => selectedMapIndex;
    public string GetSelectedMapName() => maps[selectedMapIndex].mapName;
    
    void OnDestroy()
    {
        Deactivate();
    }
}

[System.Serializable]
public class MapSelectionData
{
    [Header("Map Info")]
    public string mapName = "New Map";
    
    [Header("UI Elements")]
    public GameObject iconButton;
    public GameObject border;
    public GameObject mapInfoFolder;
    
    [Header("Game Data")]
    public string sceneToLoad = "GameScene";
    public int maxPlayers = 4;
}