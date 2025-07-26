using Godot;
using System.Collections.Concurrent;
using System.Collections.Generic;

public partial class ChunkRenderer : GridMap {
    // Map block names to library indices
    private Dictionary<string, int> _tileIndex = new();

    public void Load() {
        // Build a lookup of all tiles in the library by name
        var lib = this.MeshLibrary;
        for (int i = 0; i < lib.GetItemList().Length; i++) {
            var name = lib.GetItemName(i); 
            _tileIndex[name] = i;
        }

        AddToGroup("block");
    }

    /// <summary>
    /// Call once when loading a chunk: fill the cells.
    /// </summary>
    public void InitializeChunk(Vector3I chunkOffset, ConcurrentDictionary<Vector3I, BlockData> blocks) {
        // 1) Compute the chunk’s edge length in cells:
        //    radius*2 + 1, since a radius of 2 gives [–2..+2] → 5 cells.
        int chunkSize = GameSettings.ChunkSizeXZ;

        foreach (var kv in blocks) {
            Vector3I globalPos = kv.Key;           // e.g. (17, 0, 3)
            string blockName = kv.Value.BlockName;

            // 2) Compute the *local* cell index within this chunk
            Vector3I local = new Vector3I(
                globalPos.X - chunkOffset.X * chunkSize,
                globalPos.Y - chunkOffset.Y * chunkSize,
                globalPos.Z - chunkOffset.Z * chunkSize
            );

            // 3) Look up the correct tile ID for this block type
            if(blockName == "air")
                continue;

            int tileId = this.MeshLibrary.FindItemByName(blockName);
            if (tileId <= -1) {
                GD.PrintErr($"No tile ID for block '{blockName}'");
                continue;
            }

            // 4) Set that cell to this block’s tile
            //    layer = 0 (your only layer), item = tileId
            SetCellItem(local, tileId);
        }
    }

    /// <summary>
    /// Add or replace a single block.
    /// </summary>
    public void SetBlock(Vector3I globalPos, BlockData data) {
        int orientation = GetOrthogonalIndexFromBasis(data.Basis.Orthonormalized());

        if (!_tileIndex.TryGetValue(data.BlockName, out int tileId)) {
            GD.PrintErr($"Unknown tile: {data.BlockName}");
            return;
        }

        if (orientation == -1) {
            GD.PrintErr($"Invalid rotation basis for block {data.BlockName} @ {globalPos}");
            return;
        }

        SetCellItem(ToLocal(globalPos), tileId, orientation);
    }

    /// <summary>
    /// Remove a single block.
    /// </summary>
    public void RemoveBlock(Vector3I globalPos) {
        SetCellItem(ToLocal(globalPos), -1);
    }

    Vector3I ToLocal(Vector3I globalPos) {
        // Proper chunk size in blocks, not in world units
        int chunkSize = GameSettings.ChunkSizeXZ;

        Vector3I chunkOrigin = new Vector3I(
            (globalPos.X / chunkSize) * chunkSize,
            (globalPos.Y / chunkSize) * chunkSize,
            (globalPos.Z / chunkSize) * chunkSize
        );

        return globalPos - chunkOrigin;
    }
}