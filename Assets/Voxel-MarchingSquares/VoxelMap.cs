using UnityEngine;
using UnityEngine.InputSystem;

public class VoxelMap : MonoBehaviour {

	private static string[] fillTypeNames = {"Filled", "Empty"};
	private static string[] radiusNames = {"0", "1", "2", "3", "4", "5"};
	private static string[] stencilNames = {"Square", "Circle"};

	public float size = 2f;

	public int voxelResolution = 8;
	public int chunkResolution = 2;

	public VoxelGrid voxelGridPrefab;

	public Transform[] stencilVisualizations;

	public bool snapToGrid;

	private VoxelGrid[] chunks;
	
	private float chunkSize, voxelSize, halfSize;

	private int fillTypeIndex, radiusIndex, stencilIndex;

	private VoxelStencil[] stencils = {
		new VoxelStencil(),
		new VoxelStencilCircle()
	};
	
	private void Awake () {
		halfSize = size * 0.5f;
		chunkSize = size / chunkResolution;
		voxelSize = chunkSize / voxelResolution;
		
		chunks = new VoxelGrid[chunkResolution * chunkResolution];
		for (int i = 0, y = 0; y < chunkResolution; y++) {
			for (int x = 0; x < chunkResolution; x++, i++) {
				CreateChunk(i, x, y);
			}
		}
		BoxCollider box = gameObject.AddComponent<BoxCollider>();
		box.size = new Vector3(size, size);
	}

	private void CreateChunk (int i, int x, int y) {
		VoxelGrid chunk = Instantiate(voxelGridPrefab) as VoxelGrid;
		chunk.Initialize(voxelResolution, chunkSize);
		chunk.transform.parent = transform;
		chunk.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize);
		chunks[i] = chunk;
		if (x > 0) {
			chunks[i - 1].xNeighbor = chunk;
		}
		if (y > 0) {
			chunks[i - chunkResolution].yNeighbor = chunk;
			if (x > 0) {
				chunks[i - chunkResolution - 1].xyNeighbor = chunk;
			}
		}
	}

	private void Update () {
		Transform visualization = stencilVisualizations[stencilIndex];
		RaycastHit hitInfo;
		Vector2 mousePos = Mouse.current.position.ReadValue();
		if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hitInfo) &&
		    hitInfo.collider.gameObject == gameObject) {
			Vector2 center = transform.InverseTransformPoint(hitInfo.point);
			center.x += halfSize;
			center.y += halfSize;
			if (snapToGrid) {
				center.x = ((int)(center.x / voxelSize) + 0.5f) * voxelSize;
				center.y = ((int)(center.y / voxelSize) + 0.5f) * voxelSize;
			}

			if (Mouse.current.leftButton.ReadValue() > 0) {
				EditVoxels(center);
			}

			center.x -= halfSize;
			center.y -= halfSize;
			visualization.localPosition = center;
			visualization.localScale = Vector3.one * ((radiusIndex + 0.5f) * voxelSize * 2f);
			visualization.gameObject.SetActive(true);
		}
		else {
			visualization.gameObject.SetActive(false);
		}
	}

	private void EditVoxels (Vector2 center) {
		VoxelStencil activeStencil = stencils[stencilIndex];
		activeStencil.Initialize(fillTypeIndex == 0, (radiusIndex + 0.5f) * voxelSize);
		activeStencil.SetCenter(center.x, center.y);

		int xStart = (int)((activeStencil.XStart - voxelSize) / chunkSize);
		if (xStart < 0) {
			xStart = 0;
		}
		int xEnd = (int)((activeStencil.XEnd + voxelSize) / chunkSize);
		if (xEnd >= chunkResolution) {
			xEnd = chunkResolution - 1;
		}
		int yStart = (int)((activeStencil.YStart - voxelSize) / chunkSize);
		if (yStart < 0) {
			yStart = 0;
		}
		int yEnd = (int)((activeStencil.YEnd + voxelSize) / chunkSize);
		if (yEnd >= chunkResolution) {
			yEnd = chunkResolution - 1;
		}

		for (int y = yEnd; y >= yStart; y--) {
			int i = y * chunkResolution + xEnd;
			for (int x = xEnd; x >= xStart; x--, i--) {
				activeStencil.SetCenter(center.x - x * chunkSize, center.y - y * chunkSize);
				chunks[i].Apply(activeStencil);
			}
		}
	}

	private void OnGUI () {
		GUILayout.BeginArea(new Rect(4f, 4f, 150f, 500f));
		GUILayout.Label("Fill Type");
		fillTypeIndex = GUILayout.SelectionGrid(fillTypeIndex, fillTypeNames, 2);
		GUILayout.Label("Radius");
		radiusIndex = GUILayout.SelectionGrid(radiusIndex, radiusNames, 6);
		GUILayout.Label("Stencil");
		stencilIndex = GUILayout.SelectionGrid(stencilIndex, stencilNames, 2);
		GUILayout.EndArea();
	}
}