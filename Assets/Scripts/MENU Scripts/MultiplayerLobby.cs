using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class MultiplayerLobby : MonoBehaviour
{
    [Header("Player Boxes")]
    public PlayerBox[] playerBoxes = new PlayerBox[4];
    
    [Header("Car Groups")]
    public CarGroup[] carGroups;
    
    [Header("Main Camera")]
    public Camera lobbyCamera;
    public Transform lobbyCameraPosition;
    
    [Header("Ready/Not Ready Sprites")]
    [Tooltip("Shared Not Ready sprite for all players")]
    public Sprite notReadySprite;
    [Tooltip("Shared Ready sprite for all players")]
    public Sprite readySprite;
    
    [Header("Start Race Button")]
    public Button startRaceButton;
    public Image startButtonImage;
    public TextMeshProUGUI startButtonText;
    
    [Header("Scene Settings")]
    [Tooltip("Fallback scene name if no map is selected")]
    public string fallbackRaceSceneName = "RaceScene";
    
    [Header("Input Settings")]
    public float inputCooldown = 0.3f;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private List<LobbyPlayer> players = new List<LobbyPlayer>();
    private HashSet<InputDevice> usedDevices = new HashSet<InputDevice>();
    private float[] lastInputTimes = new float[4];
    
    private Color notReadyButtonColor = new Color(0.396f, 0.396f, 0.396f); // #656565
    private Color readyButtonColor = Color.white; // #FFFFFF
    
    void Start()
    {
        if (enableDebugLogs) Debug.Log("MultiplayerLobby Start - Press A/Space to join!");
        
        // Make sure SimplifiedGameData exists
        if (SimplifiedGameData.Instance == null)
        {
            GameObject dataObject = new GameObject("SimplifiedGameData");
            dataObject.AddComponent<SimplifiedGameData>();
            if (enableDebugLogs) Debug.Log("Created SimplifiedGameData instance");
        }
        
        InitializePlayerBoxes();
        InitializeCamera();
        InitializeCarGroups();
        
        if (startRaceButton != null)
        {
            startRaceButton.onClick.AddListener(StartRace);
            startRaceButton.interactable = false;
            
            // Set initial button state
            if (startButtonImage != null)
                startButtonImage.color = notReadyButtonColor;
            if (startButtonText != null)
                startButtonText.text = "Players Not Ready";
        }
    }
    
    void InitializePlayerBoxes()
    {
        for (int i = 0; i < playerBoxes.Length; i++)
        {
            playerBoxes[i].Initialize();
            lastInputTimes[i] = 0f;
        }
    }
    
    void InitializeCamera()
    {
        if (lobbyCamera != null && lobbyCameraPosition != null)
        {
            lobbyCamera.transform.position = lobbyCameraPosition.position;
            lobbyCamera.transform.rotation = lobbyCameraPosition.rotation;
        }
    }
    
    void InitializeCarGroups()
    {
        foreach (var carGroup in carGroups)
        {
            foreach (var carModel in carGroup.carModels)
            {
                if (carModel.carGameObject != null)
                    carModel.carGameObject.SetActive(false);
            }
        }
    }
    
    void Update()
    {
        CheckForNewPlayers();
        
        foreach (var player in players)
        {
            HandlePlayerInput(player);
        }
    }
    
    void CheckForNewPlayers()
    {
        foreach (var gamepad in Gamepad.all)
        {
            if (usedDevices.Contains(gamepad)) continue;
            
            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                AddPlayer(gamepad);
            }
        }
        
        if (Keyboard.current != null && !usedDevices.Contains(Keyboard.current))
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || 
                Keyboard.current.enterKey.wasPressedThisFrame)
            {
                AddPlayer(Keyboard.current);
            }
        }
    }
    
    void AddPlayer(InputDevice device)
    {
        int slotIndex = GetNextAvailableSlot();
        if (slotIndex == -1)
        {
            if (enableDebugLogs) Debug.LogWarning("No slots available!");
            return;
        }
        
        LobbyPlayer newPlayer = new LobbyPlayer
        {
            device = device,
            slotIndex = slotIndex,
            selectedCarIndex = 0,
            isReady = false,
            joinTime = Time.time
        };
        
        players.Add(newPlayer);
        usedDevices.Add(device);
        
        playerBoxes[slotIndex].ShowPlayerJoined(carGroups[slotIndex].carModels[0], notReadySprite);
        ShowCarForPlayer(newPlayer);
        
        if (enableDebugLogs) 
            Debug.Log($"Player {slotIndex + 1} joined with {device.displayName}");
        
        UpdateStartButton();
    }
    
    void HandlePlayerInput(LobbyPlayer player)
    {
        if (Time.time - lastInputTimes[player.slotIndex] < inputCooldown) return;
        if (Time.time - player.joinTime < 0.5f) return;
        
        int direction = 0;
        bool confirmPressed = false;
        bool cancelPressed = false;
        
        if (player.device is Gamepad gamepad)
        {
            if (!player.isReady)
            {
                if (gamepad.leftShoulder.wasPressedThisFrame || gamepad.leftTrigger.wasPressedThisFrame)
                    direction = -1;
                if (gamepad.rightShoulder.wasPressedThisFrame || gamepad.rightTrigger.wasPressedThisFrame)
                    direction = 1;
            }
            
            confirmPressed = gamepad.buttonSouth.wasPressedThisFrame;
            cancelPressed = gamepad.buttonEast.wasPressedThisFrame;
        }
        else if (player.device is Keyboard keyboard)
        {
            if (!player.isReady)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                    direction = -1;
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                    direction = 1;
            }
            
            confirmPressed = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;
            cancelPressed = keyboard.backspaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame;
        }
        
        if (direction != 0)
        {
            CycleCar(player, direction);
            lastInputTimes[player.slotIndex] = Time.time;
        }
        
        if (confirmPressed && !player.isReady)
        {
            SetPlayerReady(player);
            lastInputTimes[player.slotIndex] = Time.time;
        }
        
        if (cancelPressed)
        {
            if (player.isReady)
            {
                SetPlayerUnready(player);
                lastInputTimes[player.slotIndex] = Time.time;
            }
            else
            {
                RemovePlayer(player);
            }
        }
    }
    
    void ShowCarForPlayer(LobbyPlayer player)
    {
        CarGroup playerCarGroup = carGroups[player.slotIndex];
        
        foreach (var carModel in playerCarGroup.carModels)
        {
            if (carModel.carGameObject != null)
                carModel.carGameObject.SetActive(false);
        }
        
        if (player.selectedCarIndex < playerCarGroup.carModels.Length)
        {
            CarModel selectedCar = playerCarGroup.carModels[player.selectedCarIndex];
            if (selectedCar.carGameObject != null)
                selectedCar.carGameObject.SetActive(true);
        }
    }
    
    void CycleCar(LobbyPlayer player, int direction)
    {
        CarGroup playerCarGroup = carGroups[player.slotIndex];
        
        player.selectedCarIndex += direction;
        
        if (player.selectedCarIndex < 0)
            player.selectedCarIndex = playerCarGroup.carModels.Length - 1;
        else if (player.selectedCarIndex >= playerCarGroup.carModels.Length)
            player.selectedCarIndex = 0;
        
        playerBoxes[player.slotIndex].ShowCarInfo(playerCarGroup.carModels[player.selectedCarIndex]);
        ShowCarForPlayer(player);
        
        if (enableDebugLogs) 
            Debug.Log($"Player {player.slotIndex + 1} selected car: {player.selectedCarIndex}");
    }
    
    void SetPlayerReady(LobbyPlayer player)
    {
        player.isReady = true;
        playerBoxes[player.slotIndex].SetReadyState(true, readySprite);
        
        if (enableDebugLogs) 
            Debug.Log($"Player {player.slotIndex + 1} is READY");
        
        UpdateStartButton();
    }
    
    void SetPlayerUnready(LobbyPlayer player)
    {
        player.isReady = false;
        playerBoxes[player.slotIndex].SetReadyState(false, notReadySprite);
        
        if (enableDebugLogs) 
            Debug.Log($"Player {player.slotIndex + 1} is NOT READY");
        
        UpdateStartButton();
    }
    
    void RemovePlayer(LobbyPlayer player)
    {
        playerBoxes[player.slotIndex].RemovePlayer();
        
        CarGroup playerCarGroup = carGroups[player.slotIndex];
        foreach (var carModel in playerCarGroup.carModels)
        {
            if (carModel.carGameObject != null)
                carModel.carGameObject.SetActive(false);
            if (carModel.carStatsUI != null)
                carModel.carStatsUI.SetActive(false);
        }
        
        usedDevices.Remove(player.device);
        players.Remove(player);
        
        if (enableDebugLogs) Debug.Log($"Player {player.slotIndex + 1} left");
        
        UpdateStartButton();
    }
    
    void UpdateStartButton()
    {
        if (startRaceButton == null) return;
        
        if (players.Count < 1)
        {
            startRaceButton.interactable = false;
            if (startButtonImage != null)
                startButtonImage.color = notReadyButtonColor;
            if (startButtonText != null)
                startButtonText.text = "Players Not Ready";
            return;
        }
        
        bool allReady = players.All(p => p.isReady);
        
        if (allReady)
        {
            startRaceButton.interactable = true;
            if (startButtonImage != null)
                startButtonImage.color = readyButtonColor;
            if (startButtonText != null)
                startButtonText.text = "Start Race";
        }
        else
        {
            startRaceButton.interactable = false;
            if (startButtonImage != null)
                startButtonImage.color = notReadyButtonColor;
            if (startButtonText != null)
                startButtonText.text = "Players Not Ready";
        }
    }
    
    public void StartRace()
    {
        // Get the selected map scene from MapSelectionManager (saved in PlayerPrefs)
        string sceneToLoad = PlayerPrefs.GetString("SelectedMapSceneName", fallbackRaceSceneName);
        
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            sceneToLoad = fallbackRaceSceneName;
            if (enableDebugLogs) 
                Debug.LogWarning($"No map selected! Using fallback scene: {sceneToLoad}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"Loading selected map scene: {sceneToLoad}");
        }
        
        // Save selected vehicles to InfoVehicle (for race scene)
        if (TS.Generics.InfoVehicle.instance != null)
        {
            TS.Generics.InfoVehicle.instance.listSelectedVehicles.Clear();
            
            foreach (var player in players)
            {
                int carID = player.selectedCarIndex;
                TS.Generics.InfoVehicle.instance.listSelectedVehicles.Add(carID);
                
                if (enableDebugLogs)
                    Debug.Log($"Saved to InfoVehicle - Player {player.slotIndex + 1}: Car ID {carID}");
            }
        }
        else
        {
            Debug.LogError("InfoVehicle.instance is null! Cannot save vehicle selections.");
        }
        
        // Check if SimplifiedGameData exists
        if (SimplifiedGameData.Instance == null)
        {
            Debug.LogError("SimplifiedGameData.Instance is null! Creating one now...");
            GameObject dataObject = new GameObject("SimplifiedGameData");
            dataObject.AddComponent<SimplifiedGameData>();
        }
        
        // Save to SimplifiedGameData
        SimplifiedGameData.Instance.ClearPlayers();
        
        foreach (var player in players)
        {
            CarModel selectedCar = carGroups[player.slotIndex].carModels[player.selectedCarIndex];
            
            SimplifiedGameData.Instance.AddPlayer(
                player.slotIndex,
                player.selectedCarIndex,
                player.device,
                selectedCar.carGameObject
            );
            
            if (enableDebugLogs)
                Debug.Log($"Saved to SimplifiedGameData - P{player.slotIndex + 1}: Car {player.selectedCarIndex}");
        }
        
        // Mark this as multiplayer mode
        PlayerPrefs.SetInt("IsMultiplayer", 1);
        PlayerPrefs.Save();
        
        // Load the selected map scene
        SceneManager.LoadScene(sceneToLoad);
    }
    
    int GetNextAvailableSlot()
    {
        for (int i = 0; i < playerBoxes.Length; i++)
        {
            if (!playerBoxes[i].IsOccupied) return i;
        }
        return -1;
    }
}

[System.Serializable]
public class LobbyPlayer
{
    public InputDevice device;
    public int slotIndex;
    public int selectedCarIndex;
    public bool isReady;
    public float joinTime;
}

[System.Serializable]
public class CarGroup
{
    public CarModel[] carModels;
}

[System.Serializable]
public class CarModel
{
    public GameObject carGameObject;
    public GameObject carStatsUI;
}

[System.Serializable]
public class PlayerBox
{
    [Header("Empty State")]
    public Image playerNumberImage;
    public Image pressToJoinImage;
    
    [Header("Joined State")]
    public GameObject carStatsContainer;
    public Image readyStatusImage;
    
    private bool isOccupied = false;
    
    public bool IsOccupied => isOccupied;
    
    public void Initialize()
    {
        if (playerNumberImage != null) playerNumberImage.gameObject.SetActive(true);
        if (pressToJoinImage != null) pressToJoinImage.gameObject.SetActive(true);
        if (carStatsContainer != null) carStatsContainer.SetActive(false);
        
        if (readyStatusImage != null)
        {
            readyStatusImage.sprite = null;
            readyStatusImage.gameObject.SetActive(false);
        }
        
        isOccupied = false;
    }
    
    public void ShowPlayerJoined(CarModel firstCar, Sprite notReadySprite)
    {
        isOccupied = true;
        
        if (playerNumberImage != null) playerNumberImage.gameObject.SetActive(false);
        if (pressToJoinImage != null) pressToJoinImage.gameObject.SetActive(false);
        
        if (carStatsContainer != null)
        {
            carStatsContainer.SetActive(true);
        }
        
        ShowCarInfo(firstCar);
        
        if (readyStatusImage != null)
        {
            readyStatusImage.gameObject.SetActive(true);
            if (notReadySprite != null)
            {
                readyStatusImage.sprite = notReadySprite;
                readyStatusImage.enabled = false;
                readyStatusImage.enabled = true;
            }
        }
    }
    
    public void ShowCarInfo(CarModel car)
    {
        if (car.carStatsUI != null)
        {
            if (car.carStatsUI.transform.parent != null)
            {
                foreach (Transform sibling in car.carStatsUI.transform.parent)
                {
                    if (sibling.name.StartsWith("Car") && sibling.name.Contains("Stats"))
                    {
                        sibling.gameObject.SetActive(false);
                    }
                }
            }
            
            car.carStatsUI.SetActive(true);
        }
    }
    
    public void SetReadyState(bool ready, Sprite sprite)
    {
        if (readyStatusImage != null && sprite != null)
        {
            readyStatusImage.sprite = sprite;
        }
    }
    
    public void RemovePlayer()
    {
        isOccupied = false;
        if (playerNumberImage != null) playerNumberImage.gameObject.SetActive(true);
        if (pressToJoinImage != null) pressToJoinImage.gameObject.SetActive(true);
        if (carStatsContainer != null) carStatsContainer.SetActive(false);
        if (readyStatusImage != null) readyStatusImage.gameObject.SetActive(false);
    }
}

public class SimplifiedGameData : MonoBehaviour
{
    public static SimplifiedGameData Instance;
    
    private List<PlayerRaceData> players = new List<PlayerRaceData>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void AddPlayer(int slotIndex, int carIndex, InputDevice device, GameObject carPrefab)
    {
        players.Add(new PlayerRaceData
        {
            slotIndex = slotIndex,
            selectedCarIndex = carIndex,
            device = device,
            selectedCarPrefab = carPrefab
        });
    }
    
    public void ClearPlayers()
    {
        players.Clear();
    }
    
    public List<PlayerRaceData> GetPlayers()
    {
        return players;
    }
    
    public int GetPlayerCount()
    {
        return players.Count;
    }
}

[System.Serializable]
public class PlayerRaceData
{
    public int slotIndex;
    public int selectedCarIndex;
    public InputDevice device;
    public GameObject selectedCarPrefab;
}