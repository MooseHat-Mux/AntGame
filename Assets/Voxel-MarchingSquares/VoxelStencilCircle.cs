﻿using UnityEngine;

public class VoxelStencilCircle : VoxelStencil {
	
	private float sqrRadius;

	public override void Initialize(TileType fillType, float radius)
	{
		base.Initialize(fillType, radius);
		sqrRadius = radius * radius;
	}

	public override void Initialize (bool fillType, float radius) {
		base.Initialize (fillType, radius);
		sqrRadius = radius * radius;
	}
	
	public override void Apply (VoxelTile voxel) {
		float x = voxel.position.x - centerX;
		float y = voxel.position.y - centerY;
		if (x * x + y * y <= sqrRadius) {
			voxel.state = fillTileType;
		}
	}

	protected override void FindHorizontalCrossing (VoxelTile xMin, VoxelTile xMax) {
		float y2 = xMin.position.y - centerY;
		y2 *= y2;
		// Circle edge: x * x + y * y = sqrRadius.
		if (xMin.state == fillTileType) {
			// Possibly on right side of circle.
			float x = xMin.position.x - centerX;
			if (x * x + y2 <= sqrRadius) {
				// Left is inside, right must be outside.
				// We want to find x * x + y2 = sqrRadius.
				// Or x * x = sqrRadius - y2.
				x = centerX + Mathf.Sqrt(sqrRadius - y2);
				if (xMin.xEdge == float.MinValue || xMin.xEdge < x) {
					xMin.xEdge = x;
				}
			}
		}
		else if (xMax.state == fillTileType) {
			float x = xMax.position.x - centerX;
			if (x * x + y2 <= sqrRadius) {
				x = centerX - Mathf.Sqrt(sqrRadius - y2);
				if (xMin.xEdge == float.MinValue || xMin.xEdge > x) {
					xMin.xEdge = x;
				}
			}
		}
	}

	protected override void FindVerticalCrossing (VoxelTile yMin, VoxelTile yMax) {
		float x2 = yMin.position.x - centerX;
		x2 *= x2;
		// Circle edge: x * x + y * y = sqrRadius.
		if (yMin.state == fillTileType) {
			// Possibly on top side of circle.
			float y = yMin.position.y - centerY;
			if (y * y + x2 <= sqrRadius) {
				// Bottom is inside, top must be outside.
				// We want to find y * y + x2 = sqrRadius.
				// Or y * y = sqrRadius - x2.
				y = centerY + Mathf.Sqrt(sqrRadius - x2);
				if (yMin.yEdge == float.MinValue || yMin.yEdge < y) {
					yMin.yEdge = y;
				}
			}
		}
		else if (yMax.state == fillTileType) {
			float y = yMax.position.y - centerY;
			if (y * y + x2 <= sqrRadius) {
				y = centerY - Mathf.Sqrt(sqrRadius - x2);
				if (yMin.yEdge == float.MinValue || yMin.yEdge > y) {
					yMin.yEdge = y;
				}
			}
		}
	}
}