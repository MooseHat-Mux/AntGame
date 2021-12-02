using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Environment Settings")]
    public string EnvironmentSeed;
    public int mapWidth;
    public int mapHeight;
    public float size = 2f;

    public int voxelResolution = 8;
    public int chunkResolution = 2;

    public GroundGrid voxelGridPrefab;

    private bool loadingMap;
    private VoxelTile[,] voxGrid;
    private GroundGrid[] chunks;
    private float chunkSize, voxelSize, halfSize;

    [Range(0, 100)]
    public int randomCaveFillPercent;

    [Range(0, 100)]
    public int randomResourcePercent;

    public List<TileBase> AllTiles;
    private int minDepth;
    private int[,] grid;
    private readonly Dictionary<TileType, TileBase> tileBaseDictionary = new Dictionary<TileType, TileBase>();

    [Header("Loading References")]
    public TextMeshProUGUI loadingText;
    public Slider loadingBar;
    public Canvas LoadingCanvas;

    [Header("Environment References")]
    public Tilemap groundTileMap;
    private VoxelStencil activeStencil = new VoxelStencil();

    void Awake()
    {
        if (string.IsNullOrEmpty(EnvironmentSeed))
        {
            EnvironmentSeed = GenerateNewSeed();
        }

        minDepth = mapHeight - mapHeight / 5;

        halfSize = size * 0.5f;
        chunkSize = size / chunkResolution;
        voxelSize = chunkSize / voxelResolution;

        chunks = new GroundGrid[chunkResolution * chunkResolution];
        //int chunksX = chunkResolution;
        //if (mapWidth > mapHeight)
        //{
        //    chunksX *= mapWidth / mapHeight;
        //}
        ////Debug.Log(chunksX);

        //voxGrid = new VoxelTile[chunksX * voxelResolution, voxelResolution * chunkResolution];

        //int chunkCount = chunks.Length;
        //for (int i = 0; i < chunkCount; i++)
        //{
        //    int voxCount = chunks[i].voxels.Length;
        //    for (int j = 0; j < voxCount; j++)
        //    {
        //        //int x = chunks[i].voxels[j].x + (int)(i * chunkSize);
        //        //int y = chunks[i].voxels[j].y;
        //        //voxGrid[x, y] = chunks[i].voxels[j];
        //        //voxGrid[x, y].state = TileType.Dirt;

        //        chunks[i].voxels[j].state = TileType.Dirt;
        //    }
        //}

        //for (int i = 0; i < chunkCount; i++)
        //{
        //    chunks[i].Refresh();
        //}

        grid = GenerateGrid(mapWidth, mapHeight);
        grid = RandomWalkTopSmoothed(grid, EnvironmentSeed, 1, minDepth);
        //grid = GenerateCaves(grid, EnvironmentSeed);

        //grid = GenerateResources(grid, EnvironmentSeed);

        //tileBaseDictionary.Add(TileType.None, null);

        //int tileCount = AllTiles.Count;
        //for (int i = 0; i < tileCount; i++)
        //{
        //    tileBaseDictionary.Add((TileType)i + 1, AllTiles[i]);
        //}
    }

    public void playerSpawnSpot(Transform currentSpot)
    {
        float y = chunkResolution * chunkSize - halfSize + 5;
        float x = 0;

        currentSpot.position = new Vector2(x, y);
    }

    public void ClearMap()
    {
        if (chunks[0] != null)
        {
            int chunkCount = chunks.Length;
            for (int i = 0; i < chunkCount; i++)
            {
                Destroy(chunks[i].gameObject);
            }
        }
    }

    public void StartMapLoad()
    {
        if (!loadingMap)
        {
            loadingMap = true;
            StartCoroutine(spawnChunks());
        }
    }

    IEnumerator spawnChunks()
    {
        loadingBar.value = 0;
        loadingBar.maxValue = 100;
        LoadingCanvas.enabled = true;
        int chunkCount = chunks.Length;
        float chunkBar = 50f / chunkCount;

        for (int i = 0, y = 0; y < chunkResolution; y++)
        {
            for (int x = 0; x < chunkResolution; x++, i++)
            {
                CreateChunk(i, x, y);
                EditAll(TileType.Dirt, transform.position, i);
                yield return new WaitForEndOfFrame();

                loadingBar.value += chunkBar;
            }
        }

        loadingText.text = "Cleaning chunks.. they're dirty..";

        for (int i = 0; i < chunkCount; i++)
        {
            chunks[i].RefreshJustTriangles();
            yield return new WaitForEndOfFrame();

            loadingBar.value += chunkBar;
        }

        loadingBar.value += 100 / chunkCount;
        loadingText.text = "All done..?";

        yield return new WaitForSeconds(1.5f);

        LoadingCanvas.enabled = false;
        loadingMap = false;

        GameManager.instance.LoadPlayer();
    }

    //private void Update()
    //{
    //    if (Mouse.current.leftButton.ReadValue() > 0)
    //    {
    //        Vector2 mousePos = Mouse.current.position.ReadValue();
    //        RaycastHit2D hitInfo = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(mousePos), Mathf.Infinity);

    //        if (hitInfo.collider != null)
    //        {
    //            Vector2 center = transform.InverseTransformPoint(hitInfo.point);
    //            center.x += halfSize;
    //            center.y += halfSize;

    //            EditVoxels(TileType.None, center, 0);
    //        }
    //    }
    //}

    public void DigChunk(TileType tileSetting, Vector2 digSpot, int digRadius)
    {
        digSpot.x += halfSize;
        digSpot.y += halfSize;
        digSpot.x = ((int)(digSpot.x / voxelSize) + 0.5f) * voxelSize;
        digSpot.y = ((int)(digSpot.y / voxelSize) + 0.5f) * voxelSize;

        EditVoxels(tileSetting, digSpot, digRadius);
    }

    private void CreateChunk(int i, int x, int y)
    {
        GroundGrid chunk = Instantiate(voxelGridPrefab);
        chunk.Initialize(voxelResolution, chunkSize);
        chunk.transform.parent = transform;
        chunk.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize);
        chunks[i] = chunk;
        if (x > 0)
        {
            chunks[i - 1].xNeighbor = chunk;
        }

        if (y > 0)
        {
            chunks[i - chunkResolution].yNeighbor = chunk;
            if (x > 0)
            {
                chunks[i - chunkResolution - 1].xyNeighbor = chunk;
            }
        }
    }

    public void EditAll(TileType fillType, Vector2 center, int chunkIndex)
    {
        activeStencil.Initialize(fillType, (1000 + 0.5f) * voxelSize);
        activeStencil.SetCenter(center.x, center.y);

        //int xStart = 0;
        //int xEnd = chunkResolution - 1;

        //int yStart = 0;
        //int yEnd = chunkResolution - 1;

        chunks[chunkIndex].ApplyAll(activeStencil);

        //for (int y = yEnd; y >= yStart; y--)
        //{
        //    int i = y * chunkResolution + xEnd;
        //    for (int x = xEnd; x >= xStart; x--, i--)
        //    {
        //        chunks[chunkIndex].ApplyAll(activeStencil);
        //    }
        //}
    }

    public void EditVoxels(TileType fillType, Vector2 center, int radiusIndex)
    {
        activeStencil.Initialize(fillType, (radiusIndex + 0.5f) * voxelSize);
        activeStencil.SetCenter(center.x, center.y);

        int xStart = (int)((activeStencil.XStart - voxelSize) / chunkSize);
        if (xStart < 0)
        {
            xStart = 0;
        }
        int xEnd = (int)((activeStencil.XEnd + voxelSize) / chunkSize);
        if (xEnd >= chunkResolution)
        {
            xEnd = chunkResolution - 1;
        }
        int yStart = (int)((activeStencil.YStart - voxelSize) / chunkSize);
        if (yStart < 0)
        {
            yStart = 0;
        }
        int yEnd = (int)((activeStencil.YEnd + voxelSize) / chunkSize);
        if (yEnd >= chunkResolution)
        {
            yEnd = chunkResolution - 1;
        }

        for (int y = yEnd; y >= yStart; y--)
        {
            int i = y * chunkResolution + xEnd;
            for (int x = xEnd; x >= xStart; x--, i--)
            {
                activeStencil.SetCenter(center.x - x * chunkSize, center.y - y * chunkSize);
                chunks[i].Apply(activeStencil);
            }
        }
    }

    public void RenderNewMap()
    {
        RenderMap(grid);
        //RenderMap(grid, resourceTileMap, tileBaseDictionary);

        int[,] backgroundGrid = new int[mapWidth, minDepth];
        backgroundGrid = RandomWalkTopSmoothed(backgroundGrid, EnvironmentSeed, 1, minDepth - minDepth / 5);

        RenderMap(backgroundGrid);
    }

    public static string GenerateNewSeed()
    {
        return System.DateTime.Now.Ticks.ToString();
    }

    public static int[,] GenerateGrid(int width, int height)
    {
        int[,] newGrid = new int[width, height];
        int upper = newGrid.GetUpperBound(0);
        int lower = newGrid.GetUpperBound(1);
        for (int x = 0; x < upper; x++)
        {
            for (int y = 0; y < lower; y++)
            {
                newGrid[x, y] = 0;
            }
        }

        return newGrid;
    }

    private int[,] GenerateCaves(int[,] grid, string environmentSeed)
    {
        int upper = grid.GetUpperBound(0);
        int lower = grid.GetUpperBound(1);
        for (int x = 0; x < upper; x++)
        {
            for (int y = 0; y < lower; y++)
            {
                grid[x, y] = 0;
            }
        }

        return grid;
    }

    private int[,] GenerateResources(int[,] grid, string environmentSeed)
    {
        throw new System.NotImplementedException();
    }

    public static void RenderMap(int[,] currentGrid, Tilemap mapToRender, TileBase thisTile)
    {
        mapToRender.ClearAllTiles();

        int upper = currentGrid.GetUpperBound(0);
        int lower = currentGrid.GetUpperBound(1);
        for (int x = 0; x < upper; x++)
        {
            for (int y = 0; y < lower; y++)
            {
                mapToRender.SetTile(new Vector3Int(x, y, 0), thisTile);
            }
        }
    }

    public static void RenderMap(int[,] currentGrid)
    {
        int upper = currentGrid.GetUpperBound(0);
        int lower = currentGrid.GetUpperBound(1);
        for (int x = 0; x < upper; x++)
        {
            for (int y = 0; y < lower; y++)
            {
                //mapToRender.SetTile(new Vector3Int(x, y, 0), tileDictionary[(TileType)currentGrid[x, y]]);
            }
        }
    }

    public static int[,] RandomWalkTopSmoothed(int[,] map, string seed, int minSectionWidth, int minDepth)
    {
        //Seed our random
        System.Random rand = new System.Random(seed.GetHashCode());
        int upper = map.GetUpperBound(0);
        int lower = map.GetUpperBound(1);

        //Debug.Log(lower);

        //Determine the start position
        int lastHeight = Random.Range(minDepth, lower);

        //Used to determine which direction to go
        int nextMove;
        //Used to keep track of the current sections width
        int sectionWidth = 0;

        //Work through the array width
        for (int x = 0; x <= upper; x++)
        {
            //Determine the next move
            nextMove = rand.Next(2);

            //Only change the height if we have used the current height more than the minimum required section width
            if (nextMove == 0 && lastHeight > 0 && sectionWidth > minSectionWidth)
            {
                lastHeight--;
                sectionWidth = 0;
            }
            else if (nextMove == 1 && lastHeight < lower && sectionWidth > minSectionWidth)
            {
                lastHeight++;
                sectionWidth = 0;
            }

            //Increment the section width
            sectionWidth++;

            //Work our way from the height down to 0
            for (int y = lastHeight; y >= 0; y--)
            {
                map[x, y] = 1;
            }
        }

        //Return the modified map
        return map;
    }

    public static void UpdateMap(int[,] map, Tilemap tilemap) //Takes in our map and tilemap, setting null tiles where needed
    {
        int upper = map.GetUpperBound(0);
        int lower = map.GetUpperBound(1);
        for (int x = 0; x < upper; x++)
        {
            for (int y = 0; y < lower; y++)
            {
                //We are only going to update the map, rather than rendering again
                //This is because it uses less resources to update tiles to null
                //As opposed to re-drawing every single tile (and collision data)
                if (map[x, y] == 0)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }
    }
}
