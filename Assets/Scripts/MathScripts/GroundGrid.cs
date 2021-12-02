using Collider2DOptimization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[SelectionBase]
public class GroundGrid : MonoBehaviour {

	public float toleranceOptimization;
	public int resolution;
	public PolygonCollider2D polygon2D;

	public GroundGrid xNeighbor, yNeighbor, xyNeighbor;

	public VoxelTile[] voxels;

	private float voxelSize, gridSize;

	private Mesh mesh;
	private MeshFilter meshFilter;

	private List<int> triangles;
	private List<Vector3> vertices;

	private VoxelTile dummyX, dummyY, dummyT;
	private int[] rowCacheMax, rowCacheMin;
	private int edgeCacheMin, edgeCacheMax;

    public void Initialize (int resolution, float size) {
		this.resolution = resolution;
		gridSize = size;
		voxelSize = size / resolution;
		voxels = new VoxelTile[resolution * resolution];

		dummyX = new VoxelTile();
		dummyY = new VoxelTile();
		dummyT = new VoxelTile();

		for (int i = 0, y = 0; y < resolution; y++) {
			for (int x = 0; x < resolution; x++, i++) {
				CreateVoxel(i, x, y);
			}
		}

		meshFilter = GetComponent<MeshFilter>();

		if (meshFilter != null)
		{
			meshFilter.mesh = mesh = new Mesh();
			mesh.name = "GroundChunk Mesh";
			vertices = new List<Vector3>();
			triangles = new List<int>();
			rowCacheMax = new int[resolution * 2 + 1];
			rowCacheMin = new int[resolution * 2 + 1];

			Refresh();
		}
		else
			Debug.LogWarning("Mesh filter missing from chunk.");
	}

	private void CreateVoxel (int i, int x, int y) {
		//GameObject o = Instantiate(voxelPrefab);
		//o.transform.parent = transform;
		//o.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, -0.01f);
		//o.transform.localScale = Vector3.one * voxelSize * 0.1f;
		//voxelMaterials[i] = o.GetComponent<MeshRenderer>().material;
		voxels[i] = new VoxelTile(x, y, voxelSize);
	}

	public void Refresh () {
		//SetVoxelColors();
		Triangulate();
		UpdatePolygonCollider2D();
	}

	public void RefreshJustTriangles()
    {
		Triangulate();
    }

	private void Triangulate()
	{
		vertices.Clear();
		triangles.Clear();
		mesh.Clear();

		FillFirstRowCache();
		TriangulateCellRows();
		if (yNeighbor != null)
		{
			TriangulateGapRow();
		}

		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
	}

	private void FillFirstRowCache()
	{
		CacheFirstCorner(voxels[0]);
		int i;
		for (i = 0; i < resolution - 1; i++)
		{
			CacheNextEdgeAndCorner(i * 2, voxels[i], voxels[i + 1]);
		}
		if (xNeighbor != null)
		{
			dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize);
			CacheNextEdgeAndCorner(i * 2, voxels[i], dummyX);
		}
	}

	private void CacheFirstCorner(VoxelTile voxel)
	{
		if (voxel.state > 0)
		{
			rowCacheMax[0] = vertices.Count;
			vertices.Add(voxel.position);
		}
	}

	private void CacheNextEdgeAndCorner(int i, VoxelTile xMin, VoxelTile xMax)
	{
		if (xMin.state != xMax.state)
		{
			rowCacheMax[i + 1] = vertices.Count;
			Vector3 p;
			p.x = xMin.xEdge;
			p.y = xMin.position.y;
			p.z = 0f;
			vertices.Add(p);
		}
		if (xMax.state > 0)
		{
			rowCacheMax[i + 2] = vertices.Count;
			vertices.Add(xMax.position);
		}
	}

	private void CacheNextMiddleEdge(VoxelTile yMin, VoxelTile yMax)
	{
		edgeCacheMin = edgeCacheMax;
		if (yMin.state != yMax.state)
		{
			edgeCacheMax = vertices.Count;
			Vector3 p;
			p.x = yMin.position.x;
			p.y = yMin.yEdge;
			p.z = 0f;
			vertices.Add(p);
		}
	}

	private void TriangulateCellRows()
	{
		int cells = resolution - 1;
		for (int i = 0, y = 0; y < cells; y++, i++)
		{
			SwapRowCaches();
			CacheFirstCorner(voxels[i + resolution]);
			CacheNextMiddleEdge(voxels[i], voxels[i + resolution]);

			for (int x = 0; x < cells; x++, i++)
			{
				VoxelTile
					a = voxels[i],
					b = voxels[i + 1],
					c = voxels[i + resolution],
					d = voxels[i + resolution + 1];
				int cacheIndex = x * 2;
				CacheNextEdgeAndCorner(cacheIndex, c, d);
				CacheNextMiddleEdge(b, d);
				TriangulateCell(cacheIndex, a, b, c, d);
			}
			if (xNeighbor != null)
			{
				TriangulateGapCell(i);
			}
		}
	}

	private void SwapRowCaches()
	{
		int[] rowSwap = rowCacheMin;
		rowCacheMin = rowCacheMax;
		rowCacheMax = rowSwap;
	}

	private void TriangulateGapCell(int i)
	{
		VoxelTile dummySwap = dummyT;
		dummySwap.BecomeXDummyOf(xNeighbor.voxels[i + 1], gridSize);
		dummyT = dummyX;
		dummyX = dummySwap;
		int cacheIndex = (resolution - 1) * 2;
		CacheNextEdgeAndCorner(cacheIndex, voxels[i + resolution], dummyX);
		CacheNextMiddleEdge(dummyT, dummyX);
		TriangulateCell(cacheIndex, voxels[i], dummyT, voxels[i + resolution], dummyX);
	}

	private void TriangulateGapRow()
	{
		dummyY.BecomeYDummyOf(yNeighbor.voxels[0], gridSize);
		int cells = resolution - 1;
		int offset = cells * resolution;
		SwapRowCaches();
		CacheFirstCorner(dummyY);
		CacheNextMiddleEdge(voxels[cells * resolution], dummyY);

		for (int x = 0; x < cells; x++)
		{
			VoxelTile dummySwap = dummyT;
			dummySwap.BecomeYDummyOf(yNeighbor.voxels[x + 1], gridSize);
			dummyT = dummyY;
			dummyY = dummySwap;
			int cacheIndex = x * 2;
			CacheNextEdgeAndCorner(cacheIndex, dummyT, dummyY);
			CacheNextMiddleEdge(voxels[x + offset + 1], dummyY);
			TriangulateCell(cacheIndex, voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
		}

		if (xNeighbor != null)
		{
			dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);
			int cacheIndex = cells * 2;
			CacheNextEdgeAndCorner(cacheIndex, dummyY, dummyT);
			CacheNextMiddleEdge(dummyX, dummyT);
			TriangulateCell(cacheIndex, voxels[voxels.Length - 1], dummyX, dummyY, dummyT);
		}
	}

	private void TriangulateCell(int i, VoxelTile a, VoxelTile b, VoxelTile c, VoxelTile d)
	{
		int cellType = 0;
		if (a.state > 0)
		{
			cellType |= 1;
		}
		if (b.state > 0)
		{
			cellType |= 2;
		}
		if (c.state > 0)
		{
			cellType |= 4;
		}
		if (d.state > 0)
		{
			cellType |= 8;
		}
		switch (cellType)
		{
			case 0:
				return;
			case 1:
				AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1]);
				break;
			case 2:
				AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax);
				break;
			case 3:
				AddQuad(rowCacheMin[i], edgeCacheMin, edgeCacheMax, rowCacheMin[i + 2]);
				break;
			case 4:
				AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin);
				break;
			case 5:
				AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], rowCacheMin[i + 1]);
				break;
			case 6:
				AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax);
				AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin);
				break;
			case 7:
				AddPentagram(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMax, rowCacheMin[i + 2]);
				break;
			case 8:
				AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1]);
				break;
			case 9:
				AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1]);
				AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1]);
				break;
			case 10:
				AddQuad(rowCacheMin[i + 1], rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i + 2]);
				break;
			case 11:
				AddPentagram(rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin, rowCacheMax[i + 1], rowCacheMax[i + 2]);
				break;
			case 12:
				AddQuad(edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax);
				break;
			case 13:
				AddPentagram(rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax, rowCacheMin[i + 1], rowCacheMin[i]);
				break;
			case 14:
				AddPentagram(rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMin, rowCacheMax[i]);
				break;
			case 15:
				AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 2], rowCacheMin[i + 2]);
				break;
		}
	}

	private void AddTriangle(int a, int b, int c)
	{
		triangles.Add(a);
		triangles.Add(b);
		triangles.Add(c);
	}

	private void AddQuad(int a, int b, int c, int d)
	{
		triangles.Add(a);
		triangles.Add(b);
		triangles.Add(c);
		triangles.Add(a);
		triangles.Add(c);
		triangles.Add(d);
	}

	private void AddPentagram(int a, int b, int c, int d, int e)
	{
		triangles.Add(a);
		triangles.Add(b);
		triangles.Add(c);
		triangles.Add(a);
		triangles.Add(c);
		triangles.Add(d);
		triangles.Add(a);
		triangles.Add(d);
		triangles.Add(e);
	}

	public void Apply(VoxelStencil stencil)
	{
		int xStart = (int)(stencil.XStart / voxelSize);
		if (xStart < 0)
		{
			xStart = 0;
		}
		int xEnd = (int)(stencil.XEnd / voxelSize);
		if (xEnd >= resolution)
		{
			xEnd = resolution - 1;
		}
		int yStart = (int)(stencil.YStart / voxelSize);
		if (yStart < 0)
		{
			yStart = 0;
		}
		int yEnd = (int)(stencil.YEnd / voxelSize);
		if (yEnd >= resolution)
		{
			yEnd = resolution - 1;
		}

		for (int y = yStart; y <= yEnd; y++)
		{
			int i = y * resolution + xStart;
			for (int x = xStart; x <= xEnd; x++, i++)
			{
				stencil.Apply(voxels[i]);
			}
		}
		SetCrossings(stencil, xStart, xEnd, yStart, yEnd);
		Refresh();
	}

	public void ApplyAll(VoxelStencil stencil)
	{
		int xEnd = resolution - 1;
		int yEnd = resolution - 1;

		int voxelCount = voxels.Length;
        for (int i = 0; i < voxelCount; i++)
        {
			stencil.Apply(voxels[i]);
		}

		SetCrossings(stencil, 0, xEnd, 0, yEnd);
		Refresh();
	}

	private void SetCrossings(VoxelStencil stencil, int xStart, int xEnd, int yStart, int yEnd)
	{
		bool crossHorizontalGap = false;
		bool includeLastVerticalRow = false;
		bool crossVerticalGap = false;

		if (xStart > 0)
		{
			xStart -= 1;
		}
		if (xEnd == resolution - 1)
		{
			xEnd -= 1;
			crossHorizontalGap = xNeighbor != null;
		}
		if (yStart > 0)
		{
			yStart -= 1;
		}
		if (yEnd == resolution - 1)
		{
			yEnd -= 1;
			includeLastVerticalRow = true;
			crossVerticalGap = yNeighbor != null;
		}

		VoxelTile a, b;
		for (int y = yStart; y <= yEnd; y++)
		{
			int i = y * resolution + xStart;
			b = voxels[i];
			for (int x = xStart; x <= xEnd; x++, i++)
			{
				a = b;
				b = voxels[i + 1];
				stencil.SetHorizontalCrossing(a, b);
				stencil.SetVerticalCrossing(a, voxels[i + resolution]);
			}
			stencil.SetVerticalCrossing(b, voxels[i + resolution]);
			if (crossHorizontalGap)
			{
				dummyX.BecomeXDummyOf(xNeighbor.voxels[y * resolution], gridSize);
				stencil.SetHorizontalCrossing(b, dummyX);
			}
		}

		if (includeLastVerticalRow)
		{
			int i = voxels.Length - resolution + xStart;
			b = voxels[i];
			for (int x = xStart; x <= xEnd; x++, i++)
			{
				a = b;
				b = voxels[i + 1];
				stencil.SetHorizontalCrossing(a, b);
				if (crossVerticalGap)
				{
					dummyY.BecomeYDummyOf(yNeighbor.voxels[x], gridSize);
					stencil.SetVerticalCrossing(a, dummyY);
				}
			}
			if (crossVerticalGap)
			{
				dummyY.BecomeYDummyOf(yNeighbor.voxels[xEnd + 1], gridSize);
				stencil.SetVerticalCrossing(b, dummyY);
			}
			if (crossHorizontalGap)
			{
				dummyX.BecomeXDummyOf(xNeighbor.voxels[voxels.Length - resolution], gridSize);
				stencil.SetHorizontalCrossing(b, dummyX);
			}
		}
	}

	void UpdatePolygonCollider2D()
	{
		if (meshFilter.mesh == null)
		{
			//Debug.LogWarning(meshFilter.gameObject.name + " has no Mesh set on its MeshFilter component!");
			return;
		}

        if (vertices.Count <= 0)
        {
			//Debug.LogWarning(meshFilter.gameObject.name + " has no vertices!");
			return;
        }

        int[] triangles = mesh.triangles;
        int triangleLength = triangles.Length;

        // Get just the outer edges from the mesh's triangles (ignore or remove any shared edges)
        Dictionary<string, KeyValuePair<int, int>> edges = new Dictionary<string, KeyValuePair<int, int>>();
        for (int i = 0; i < triangleLength; i += 3)
        {
            for (int e = 0; e < 3; e++)
            {
                int vert1 = triangles[i + e];
                int vert2 = triangles[i + e + 1 > i + 2 ? i : i + e + 1];

				string edge = Mathf.Min(vert1, vert2) + ":" + Mathf.Max(vert1, vert2);
                if (edges.ContainsKey(edge))
                {
                    edges.Remove(edge);
                }
                else
                {
                    edges.Add(edge, new KeyValuePair<int, int>(vert1, vert2));
                }
            }
        }

        // Create edge lookup (Key is first vertex, Value is second vertex, of each edge)
        Dictionary<int, int> lookup = new Dictionary<int, int>();
        foreach (KeyValuePair<int, int> edge in edges.Values)
        {
            if (lookup.ContainsKey(edge.Key) == false)
            {
                lookup.Add(edge.Key, edge.Value);
            }
        }

        //// Loop through edge vertices in order
        int startVert = 0;
        int nextVert = startVert;
        int highestVert = startVert;
        List<Vector2> colliderPath = new List<Vector2>();
        while (true)
        {

            // Add vertex to collider path
            colliderPath.Add(vertices[nextVert]);

            // Get next vertex
            nextVert = lookup[nextVert];

            // Store highest vertex (to know what shape to move to next)
            if (nextVert > highestVert)
            {
                highestVert = nextVert;
            }

            // Shape complete
            if (nextVert == startVert)
            {
                // Add path to polygon collider
                polygon2D.pathCount = 1;
				colliderPath = ShapeOptimizationHelper.DouglasPeuckerReduction(colliderPath, toleranceOptimization);
				polygon2D.SetPath(0, colliderPath.ToArray());

                colliderPath.Clear();

                //// Go to next shape if one exists
                //if (lookup.ContainsKey(highestVert + 1))
                //{

                //    // Set starting and next vertices
                //    startVert = highestVert + 1;
                //    nextVert = startVert;

                //    // Continue to next loop
                //    continue;
                //}

                // No more verts
                break;
            }
        }

        //Debug.Log(meshFilter.gameObject.name + " PolygonCollider2D updated.");
    }
}