using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [Header("Game Settings")]
    public int difficulty;
    public GameMode currentMode;

    public float antDigRadius = 1;

    [Header("Main Menu Settings")]
    public float AntFrequencyMax;
    public float AntFrequencyMin;

    public GameObject menuObjectParent;
    public Transform menuCameraFocus;
    public Transform[] menuSpawnSpots;

    private bool MenuRunning;
    private bool GameRunning;

    [Header("Game References")]
    //public PlayerData playerData;
    public Canvas pauseCanvas;

    public Transform playerSpawn;
    public PoolManager poolManager;
    public EnvironmentManager environmentManager;
    public CinemachineVirtualCamera antCamera;
    public Transform IkTargetParent;

    [Header("Creature References")]
    public AntBrain currentPlayerAnt;
    public AntData baseAntData;
    public AntBrain Ant;
    public AntBrain PlayerAnt;
    public List<AntBrain> menuAnts;
    public List<AntBrain> currentAntColony;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
            Destroy(gameObject);
    }

    void Start()
    {
        environmentManager.playerSpawnSpot(playerSpawn);
        setMode(GameMode.MainMenu);
    }

    public void setMode(int modeIndex)
    {
        setMode((GameMode)modeIndex);
    }

    public void setMode(GameMode newMode)
    {
        switch (newMode)
        {
            case GameMode.MainMenu:
                currentMode = GameMode.MainMenu;
                GameRunning = false;
                menuObjectParent.SetActive(true);
                antCamera.Follow = menuCameraFocus;
                environmentManager.ClearMap();
                StartCoroutine(MenuLoop());
                break;
            case GameMode.Maze:
                currentMode = GameMode.Maze;
                MenuRunning = false;
                menuObjectParent.SetActive(false);
                ScreenClear();
                environmentManager.StartMapLoad();
                StartCoroutine(GameLoop());
                break;
            default:
                break;
        }
    }

    public void ScreenClear()
    {
        int antCount = menuAnts.Count;
        for (int i = 0; i < antCount; i++)
        {
            menuAnts[i].antHealth.DamageCreature(1000);
        }
    }

    public void LoadPlayer()
    {
        if (currentPlayerAnt != null)
        {
            Destroy(currentPlayerAnt.gameObject);
        }

        currentPlayerAnt = Instantiate(PlayerAnt, playerSpawn.position, Quaternion.identity);
        antCamera.Follow = currentPlayerAnt.transform;
    }

    public void CallPause()
    {
        if (currentMode == GameMode.Maze)
        {
            if (pauseCanvas.enabled)
            {
                Time.timeScale = 0;
                pauseCanvas.enabled = false;
            }
            else
            {
                Time.timeScale = 1;
                pauseCanvas.enabled = true;
            }
        }
    }

    public void OnApplicationPause(bool pause)
    {
        if (currentMode == GameMode.Maze)
        {
            if (pause)
            {
                Time.timeScale = 0;
                pauseCanvas.enabled = true;
            }
            else
            {
                Time.timeScale = 1;
                pauseCanvas.enabled = false;
            }
        }
    }

    IEnumerator GameLoop()
    {
        GameRunning = true;

        while (GameRunning)
        {
            //Load previous save
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator MenuLoop()
    {
        MenuRunning = true;

        yield return new WaitForSeconds(1);

        while (MenuRunning)
        {
            float freq = Random.Range(AntFrequencyMin, AntFrequencyMax);
            yield return new WaitForSeconds(freq);

            AntBrain newAnt = poolManager.AntSpawn();
            newAnt.ResetData(baseAntData);

            float rand = Random.value;
            //Debug.Log(rand);
            if (rand < 0.5f)
            {
                newAnt.SetDir(menuSpawnSpots[0].right);
                //Spawn Ant
                newAnt.transform.position = menuSpawnSpots[0].position;
                //menuSpawnSpots[0]
            }
            else
            {
                newAnt.SetDir(-menuSpawnSpots[0].right);
                newAnt.transform.position = menuSpawnSpots[1].position;
                //Spawn Ant
            }

            menuAnts.Add(newAnt);
        }
    }
}
