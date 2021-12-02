using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileObject : IQuadTreeObject
{
	private Vector2 m_vPosition;
	public TileObject(Vector2 position)
	{
		m_vPosition = position;
	}

	public Vector2 GetPosition()
	{
		return m_vPosition;
	}
}
