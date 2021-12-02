using UnityEngine;
using System;

[Serializable]
public class VoxelTile {

	public TileType state;
	public int x;
	public int y;
	public Vector2 position;
	public float xEdge, yEdge;

	public VoxelTile(int x, int y, float size) {
		this.x = x;
		this.y = y;
		position.x = (x + 0.5f) * size;
		position.y = (y + 0.5f) * size;

		xEdge = float.MinValue;
		yEdge = float.MinValue;

		//state = TileType.Dirt;
	}

	public VoxelTile() {}

	public void BecomeXDummyOf (VoxelTile voxel, float offset) {
		state = voxel.state;
		position = voxel.position;
		position.x += offset;
		xEdge = voxel.xEdge + offset;
		yEdge = voxel.yEdge;
	}

	public void BecomeYDummyOf (VoxelTile voxel, float offset) {
		state = voxel.state;
		position = voxel.position;
		position.y += offset;
		xEdge = voxel.xEdge;
		yEdge = voxel.yEdge + offset;
	}

	public void BecomeXYDummyOf (VoxelTile voxel, float offset) {
		state = voxel.state;
		position = voxel.position;
		position.x += offset;
		position.y += offset;
		xEdge = voxel.xEdge + offset;
		yEdge = voxel.yEdge + offset;
	}
}