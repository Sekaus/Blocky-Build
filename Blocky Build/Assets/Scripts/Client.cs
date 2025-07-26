using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
public partial class Client : Node {
    WorldData worldData;
    public WorldData WorldData {
        get { 
            return worldData; 
        }
    }

    ChunkRenderer chunkRenderer;
    PlayerController player;
    Node3D blockHighlight;

    // Set block in world
    public void SetBlock(BlockData blockData, Vector3I blockPosition, bool runBlockUpdates = true) {
        if (blockPosition.Y < 0 || blockPosition.Y >= GameSettings.ChunkHeight)
            return;

        var chunkPos = GetRelativeChunkPosition(blockPosition);
        var chunkRes = worldData.GetChunk(chunkPos);
        if (!chunkRes.Item1)
            return;

        var chunk = chunkRes.Item2;

        if (!chunk.Blocks.ContainsKey(blockPosition)) {
            chunk.AddBlock(blockPosition, blockData);
            chunkRenderer.SetBlock(blockPosition, blockData);
        }
    }

    // Remove block in world
    public void RemoveBlock(Vector3I blockPosition, bool runBlockUpdates = true) {
        if (blockPosition.Y < 0)
            return;

        var chunkPos = GetRelativeChunkPosition(blockPosition);
        var chunkRes = worldData.GetChunk(chunkPos);
        if (!chunkRes.Item1)
            return;

        var chunk = chunkRes.Item2;

        if (chunk.Blocks.ContainsKey(blockPosition)) {
            chunkRenderer.RemoveBlock(blockPosition);
            chunk.RemoveBlock(blockPosition);
        }
    }

    // Test if there is a block at position
    public bool IsThereABlockAt(Vector3I blockPosition, Vector3I chunkPosition) {
        var chunk = worldData.GetChunk(chunkPosition);
        if (chunk.Item1) {
            if (chunk.Item2.Blocks.ContainsKey(blockPosition))
                return true;
        }

        return false;
    }

    public override void _Ready() {
        blockHighlight = GetNode<Node3D>("%BlockHighlight");
        blockHighlight.Scale *= 1.0001f;
        player = GetNode<PlayerController>("%Player");
        worldData = GetNode<WorldData>("%World");
        chunkRenderer = GetNode<ChunkRenderer>("%ChunkRenderer");
        chunkRenderer.MeshLibrary = Register.CreateMeshLibraryForBlocks();
        chunkRenderer.Load();

        Input.MouseMode = Input.MouseModeEnum.Captured;

        Vector3I selectedChunk = new Vector3I(0, 0, 0);

        // Genarate first world chunk

        Owner.Ready += async () => {
            await InitializeAsync(selectedChunk);
        };

        player.FreezeScript = false;

        int chunkDiameter = (GameSettings.ChunkRadius * 2 + 1);
        Vector3I relativeChunk = new Vector3I(
            (int)MathF.Round(player.Position.X / chunkDiameter),
            0,
            (int)MathF.Round(player.Position.Y / chunkDiameter)
        );

        if (!player.CorrentChunk.HasValue || player.CorrentChunk.Value != relativeChunk) {
            player.CorrentChunk = relativeChunk;
            Vector3I chunkToGenerate = relativeChunk;

            _ = Task.Run(async () => {
                try {
                    await GenChunks(chunkToGenerate);
                    MainThreadDispatcher.Enqueue(() => LoadChunks());
                }
                catch (Exception ex) {
                    GD.PrintErr("Error in GenChunks task: " + ex.Message);
                }
            });
        }
    }

    private async Task InitializeAsync(Vector3I selectedChunk) {
        await worldData.GenChunkAsync(selectedChunk);
        var chunk = worldData.GetChunk(selectedChunk);
        if (chunk.Item1)
            chunkRenderer.InitializeChunk(selectedChunk, chunk.Item2.Blocks);
    }

    private async Task RendererChunks() {
        Vector3I[] chunkPositions = worldData.GetChunkPositions();
        foreach(var chunkPosition in chunkPositions)
            await InitializeAsync(chunkPosition);
    }

    private async Task GenChunks(Vector3I chunkPosition) {
        List<Vector3I> chunkPositions = new List<Vector3I>(GameSettings.MaxCunksOnSceen);

        float sqmcr = (MathF.Sqrt(GameSettings.MaxCunksOnSceen) / 2 - 1);

        for (int x = -(int)sqmcr; x <= (int)sqmcr; x++) {
            for (int z = -(int)sqmcr; z <= (int)sqmcr; z++) {
                chunkPositions.Add(new Vector3I(x + chunkPosition.X, 0, z + chunkPosition.Z));
            }
        }

        var semaphore = new SemaphoreSlim(4); // Limit to 4 concurrent chunk generations
        var tasks = new List<Task>();

        foreach (var _chunkPosition in chunkPositions) {
            await semaphore.WaitAsync();
            var task = Task.Run(async () => {
                try {
                    await worldData.GenChunkAsync(_chunkPosition);
                }
                finally {
                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    private void LoadChunks() {
        RendererChunks();
    }

    public bool InteractionWithBlock(
    GodotObject collider,
    Vector3 collisionPoint,
    Vector3 collisionNormal,
    out BlockBehavior blockBehavior,
    out Vector3I blockPosition
    ) {
        blockBehavior = null;
        blockPosition = Vector3I.Zero;

        // 1) Nudge the hit‑point slightly into the block
        float halfHit = GameSettings.BlockRenderScale * 0.5f;
        var adjusted = collisionPoint - collisionNormal * (halfHit * 0.01f);

        // 2) Compute coords
        GetBlockCoords(adjusted, collisionNormal,
            out var chunkCoord,
            out var _localUnused,
            out var globalBlockCoord
        );

        // 3) Only proceed if it's a GridMap and that block exists
        if (collider is not GridMap gridmap
            || !worldData.GetChunk(chunkCoord).Item1
            || !worldData.GetChunk(chunkCoord).Item2.Blocks.ContainsKey(globalBlockCoord)
        ) {
            blockHighlight.Visible = false;
            return false;
        }

        // 4) Compute chunk's world‐space origin
        int chunkSizeXZ = GameSettings.ChunkSizeXZ;
        int chunkHeight = GameSettings.ChunkHeight;
        // each block is `scale` units
        float scale = GameSettings.BlockRenderScale;
        Vector3 chunkWorldOrigin = new Vector3(
            chunkCoord.X * chunkSizeXZ * scale,
            chunkCoord.Y * chunkHeight * scale,
            chunkCoord.Z * chunkSizeXZ * scale
        );

        // 5) Compute the *local* center of the cell inside that chunk
        Vector3I localCell = new Vector3I(
            globalBlockCoord.X - chunkCoord.X * chunkSizeXZ,
            globalBlockCoord.Y - chunkCoord.Y * chunkHeight,
            globalBlockCoord.Z - chunkCoord.Z * chunkSizeXZ
        );
        Vector3 cellSize = new Vector3(scale, scale, scale);
        Vector3 halfExtents = cellSize * 0.5f;
        Vector3 cellCenterLocal = (Vector3)localCell * cellSize + halfExtents;

        // 6) Sum them to get the *absolute* world‐space center
        Vector3 worldCenter = chunkWorldOrigin + cellCenterLocal;

        // 7) Show the highlight there
        blockHighlight.GlobalTransform = new Transform3D(Basis.Identity, worldCenter);
        blockHighlight.Visible = true;

        // 8) Instantiate behavior as before
        var chunk = worldData.GetChunk(chunkCoord).Item2;
        var data = chunk.Blocks[globalBlockCoord];
        var script = data.BehaviorScript ?? Register.DefaultBlockBehaviorScript;
        var beh = script.New().As<BlockBehavior>();
        if (beh == null) {
            GD.PrintErr($"Behavior script failed at {globalBlockCoord}");
            return false;
        }
        beh.Setup(worldData,
                  new Transform3D(Basis.Identity, worldCenter),
                  null
        );

        blockBehavior = beh;
        blockPosition = globalBlockCoord;
        return true;
    }

    /// <summary>
    /// Converts a world‐space hit point into:
    ///   * chunkCoord      — which chunk that block lives in,
    ///   * localBlockCoord — [0..ChunkSize-1] index inside that chunk,
    ///   * globalBlockCoord— the absolute block index in your world.
    /// </summary>
    public static void GetBlockCoords(
    Vector3 worldPos,
    Vector3 collisionNormal,
    out Vector3I chunkCoord,
    out Vector3I localBlockCoord,
    out Vector3I globalBlockCoord
    ) {
        // 0) Nudge the point a hair inside the block you hit
        float eps = GameSettings.BlockRenderScale * 0.01f;
        worldPos -= collisionNormal * eps;

        // 1) Un-scale into block-space
        float scale = GameSettings.BlockRenderScale;
        Vector3 blockSpace = worldPos / scale;

        // 2) Global integer coordinate of the block
        globalBlockCoord = new Vector3I(
            Mathf.FloorToInt(blockSpace.X),
            Mathf.FloorToInt(blockSpace.Y),
            Mathf.FloorToInt(blockSpace.Z)
        );

        // 3) Chunk dimensions
        int chunkSizeXZ = GameSettings.ChunkRadius * 2 + 1;
        int chunkSizeY = GameSettings.ChunkHeight;

        // 4) Which chunk contains that block?
        chunkCoord = new Vector3I(
            Mathf.FloorToInt((float)globalBlockCoord.X / chunkSizeXZ),
            Mathf.FloorToInt((float)globalBlockCoord.Y / chunkSizeY),
            Mathf.FloorToInt((float)globalBlockCoord.Z / chunkSizeXZ)
        );

        // 5) Local index inside the chunk [0..chunkSize-1]
        localBlockCoord = new Vector3I(
            Mod(globalBlockCoord.X, chunkSizeXZ),
            Mod(globalBlockCoord.Y, chunkSizeY),
            Mod(globalBlockCoord.Z, chunkSizeXZ)
        );
    }

    // C# % operator can go negative, so we fix it
    private static int Mod(int a, int m) {
        int r = a % m;
        return r < 0 ? r + m : r;
    }


    /// <summary>
    /// Converts a global block index (Vector3I) into its chunk coordinate.
    /// </summary>
    public static Vector3I GetRelativeChunkPosition(Vector3I blockPos) {
        return new Vector3I(
            Mathf.FloorToInt((float)blockPos.X / GameSettings.ChunkSizeXZ),
            Mathf.FloorToInt((float)blockPos.Y / GameSettings.ChunkHeight),
            Mathf.FloorToInt((float)blockPos.Z / GameSettings.ChunkSizeXZ)
        );
    }

    public override void _Process(double delta) {
        if (Input.IsActionPressed("exit"))
            GetTree().Quit();
    }
}