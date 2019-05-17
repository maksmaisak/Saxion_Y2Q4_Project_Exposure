using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class DotsRegistry
{
    struct Cell
    {
        public ulong numDots;
    }

    private readonly Bounds sceneBounds;
    private readonly float cellSize;
    private readonly Cell[,,] cells;
    private readonly Vector3Int maxIndices;
    
    public int totalNumDots { get; private set; }

    public DotsRegistry(float cellSize = 0.5f)
    {
        this.cellSize = cellSize;
        sceneBounds = GetSceneBounds();
        
        cells = new Cell[
            Mathf.CeilToInt(sceneBounds.size.x / cellSize),
            Mathf.CeilToInt(sceneBounds.size.y / cellSize),
            Mathf.CeilToInt(sceneBounds.size.z / cellSize)
        ];
        
        maxIndices = new Vector3Int(
            cells.GetLength(0) - 1,
            cells.GetLength(1) - 1,
            cells.GetLength(2) - 1
        );
    }

    private static readonly Vector3Int[] indexOffsets =
    {
        new Vector3Int( 0,  0,  0),
        new Vector3Int(-1, -1, -1),
        new Vector3Int(-1, -1,  1),
        new Vector3Int(-1,  1, -1),
        new Vector3Int(-1,  1,  1),
        new Vector3Int( 1, -1, -1), 
        new Vector3Int( 1, -1,  1),
        new Vector3Int( 1,  1, -1),
        new Vector3Int( 1,  1,  1)
    };

    public void RegisterDot(Vector3 position)
    {
        Vector3Int indices = GetCellIndicesFor(position);
        Assert.IsTrue(IsValid(indices));

        ++totalNumDots;
        ++cells[indices.x, indices.y, indices.z].numDots;
    }

    public ulong GetNumDotsAround(Vector3 position)
    {
        ulong numDots = 0;
        
        Vector3Int indices = GetCellIndicesFor(position);
        foreach (Vector3Int indexOffset in indexOffsets)
        {
            var offsetIndices = indices + indexOffset;
            if (!IsValid(offsetIndices))
                continue;
            
            numDots += cells[indices.x, indices.y, indices.z].numDots;
        }

        return numDots;
    }

    public void DrawDebugInfoInEditor()
    {
        Handles.DrawWireCube(sceneBounds.center, sceneBounds.size);
    }

    private Vector3Int GetCellIndicesFor(Vector3 position)
    {
        //Assert.IsTrue(sceneBounds.Contains(position));
        var indices = Vector3Int.FloorToInt((position - sceneBounds.min) / cellSize);
        indices.Clamp(Vector3Int.zero, maxIndices);
        return indices;
    }

    private bool IsValid(Vector3Int indices)
    {
        for (int i = 0; i < 3; ++i)
            if (indices[i] < 0 || indices[i] > maxIndices[i])
                return false;

        return true;
    }

    private static Bounds GetSceneBounds()
    {
        Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
        if (renderers.Length < 1)
            return new Bounds();

        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}