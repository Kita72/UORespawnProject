using System;
using System.Collections.Generic;

using Server.Custom.UORespawnServer.Entities;

namespace Server.Custom.UORespawnServer.Managers;
/// <summary>
/// Provides O(1) spatial lookups for box spawns using a chunked 2D grid.
/// Each chunk is 16x16 tiles, storing the index of the highest-priority box covering that area.
/// </summary>
internal static class SpatialGridManager
{
    private const int CHUNK_SIZE = 16; // 16x16 tiles per chunk
    private const short NO_BOX = -1;   // Sentinel value for empty chunks

    // Grid per map: short[chunkX, chunkY] = box index (-1 if none)
    private static Dictionary<Map, short[,]> _MapGrids;

    // Box lists per map for index-based retrieval
    private static Dictionary<Map, List<BoxEntity>> _BoxLists;

    /// <summary>
    /// Initialize spatial grids from loaded box spawns.
    /// Call this AFTER SpawnManager.LoadBoxSpawnData() completes.
    /// </summary>
    internal static void Initialize(Dictionary<Map, List<BoxEntity>> boxSpawns)
    {
        _MapGrids = [];
        _BoxLists = [];

        if (boxSpawns == null || boxSpawns.Count == 0)
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, "SPATIAL GRID-[No boxes to index]");

            return;
        }

        int totalBoxes = 0;
        int totalChunks = 0;

        foreach (var kvp in boxSpawns)
        {
            Map map = kvp.Key;

            List<BoxEntity> boxes = kvp.Value;

            if (boxes == null || boxes.Count == 0)
            {
                continue;
            }

            // Store box list for index-based retrieval
            _BoxLists[map] = boxes;

            // Calculate grid dimensions based on map size
            int gridWidth = (map.Width / CHUNK_SIZE) + 1;
            int gridHeight = (map.Height / CHUNK_SIZE) + 1;

            // Create grid initialized to NO_BOX
            short[,] grid = new short[gridWidth, gridHeight];
            int[,] priorities = new int[gridWidth, gridHeight]; // Track priority per chunk

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = NO_BOX;
                    priorities[x, y] = int.MinValue;
                }
            }

            // Map each box to its chunks (highest priority wins)
            for (short boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
            {
                BoxEntity box = boxes[boxIndex];
                Rectangle2D bounds = box.SpawnBox;

                // Calculate chunk range for this box
                int startChunkX = bounds.X / CHUNK_SIZE;
                int startChunkY = bounds.Y / CHUNK_SIZE;
                int endChunkX = (bounds.X + bounds.Width - 1) / CHUNK_SIZE;
                int endChunkY = (bounds.Y + bounds.Height - 1) / CHUNK_SIZE;

                // Clamp to grid bounds
                startChunkX = Math.Max(0, startChunkX);
                startChunkY = Math.Max(0, startChunkY);
                endChunkX = Math.Min(gridWidth - 1, endChunkX);
                endChunkY = Math.Min(gridHeight - 1, endChunkY);

                // Fill chunks with box index (only if higher priority)
                for (int cx = startChunkX; cx <= endChunkX; cx++)
                {
                    for (int cy = startChunkY; cy <= endChunkY; cy++)
                    {
                        if (box.SpawnPriority > priorities[cx, cy])
                        {
                            grid[cx, cy] = boxIndex;
                            priorities[cx, cy] = box.SpawnPriority;
                            totalChunks++;
                        }
                    }
                }
            }

            _MapGrids[map] = grid;
            totalBoxes += boxes.Count;
        }

        UOR_Utility.SendMsg(ConsoleColor.Green, $"SPATIAL GRID-[Indexed {totalBoxes} boxes across {_MapGrids.Count} maps]");
    }

    /// <summary>
    /// O(1) lookup: Get the BoxEntity at a world location, or null if none.
    /// </summary>
    internal static BoxEntity GetBoxAt(Map map, Point3D location)
    {
        return GetBoxAt(map, location.X, location.Y);
    }

    /// <summary>
    /// O(1) lookup: Get the BoxEntity at world coordinates, or null if none.
    /// </summary>
    internal static BoxEntity GetBoxAt(Map map, int x, int y)
    {
        if (map == null || !_MapGrids.TryGetValue(map, out var grid))
        {
            return null;
        }

        int chunkX = x / CHUNK_SIZE;
        int chunkY = y / CHUNK_SIZE;

        // Bounds check
        if (chunkX < 0 || chunkX >= grid.GetLength(0) ||
            chunkY < 0 || chunkY >= grid.GetLength(1))
        {
            return null;
        }

        short boxIndex = grid[chunkX, chunkY];

        if (boxIndex == NO_BOX)
        {
            return null;
        }

        // Verify the point is actually inside the box (chunk may partially overlap)
        if (_BoxLists.TryGetValue(map, out var boxes) && boxIndex < boxes.Count)
        {
            BoxEntity box = boxes[boxIndex];

            // Final precise check - point must be inside box bounds
            if (box.SpawnBox.Contains(new Point2D(x, y)))
            {
                return box;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if a location has any box coverage.
    /// </summary>
    internal static bool HasBoxAt(Map map, int x, int y)
    {
        return GetBoxAt(map, x, y) != null;
    }

    /// <summary>
    /// Clear all grids (for reload scenarios).
    /// </summary>
    internal static void Clear()
    {
        int boxCount = 0;

        if (_BoxLists != null)
        {
            foreach (var kvp in _BoxLists)
            {
                boxCount += kvp.Value?.Count ?? 0;
            }
        }

        _MapGrids?.Clear();
        _BoxLists?.Clear();
    }
}
