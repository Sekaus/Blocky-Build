using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

public class Chunk {
    public readonly Vector3I Position;
    // Pure data: which block type belongs at which coordinate
    private readonly ConcurrentDictionary<Vector3I, BlockData> _rawBlocks = new();
    public ConcurrentDictionary<Vector3I, BlockData> Blocks {
        get {
            return _rawBlocks;
        }
    }

    private readonly TaskCompletionSource<bool> _dataReady = new();
    public Task DataReady => _dataReady.Task;

    public Chunk(Vector3I chunkPosition, WorldData worldData, WorldData.WorldLayer[][] worldLayers) {
        Position = chunkPosition;
        // Kick off only your pure-data work in the threadpool:
        ThreadPool.QueueUserWorkItem(_ => GenerateRawData(worldData, worldLayers));
    }

    private void GenerateRawData(WorldData worldData, WorldData.WorldLayer[][] worldLayers) {
        var offset = Position * GameSettings.ChunkRadius;
        int atLayer = GameSettings.DefaultBedrockLevel;

        var layerDefs = worldLayers[(int)worldData.Type];

        foreach (var layer in layerDefs) {
            var blockData = Register.BlockDataMap[layer.blockName];
            for (int y = atLayer; y < atLayer + layer.height; y++) {
                for (int x = -GameSettings.ChunkRadius + offset.X;
                     x < GameSettings.ChunkRadius + offset.X; x++) {
                    for (int z = -GameSettings.ChunkRadius + offset.Z;
                         z < GameSettings.ChunkRadius + offset.Z; z++) {
                        Vector3I pos = new Vector3I(x, y, z);
                        _rawBlocks.TryAdd(pos, (BlockData)blockData.To<BlockData>());
                    }
                }
            }
            atLayer += layer.height;
        }

        var airBlock = new BlockData();

        _dataReady.TrySetResult(true);
    }

    public bool IsAirAt(Vector3I pos) {
        return !_rawBlocks.TryGetValue(pos, out var data) || data.BlockName == "air";
    }

    public void AddBlock(Vector3I pos, BlockData blockData) {
        if (IsAirAt(pos)) {
            _rawBlocks[pos] = blockData;
        }
        else if (_rawBlocks.TryGetValue(pos, out var oldData)) {
            _rawBlocks.TryUpdate(pos, blockData, oldData);
        }
    }

    public void RemoveBlock(Vector3I pos) {
        if (_rawBlocks.TryGetValue(pos, out var oldData)) {
            // Replace with air by removing the key entirely
            _rawBlocks.TryRemove(pos, out _);
        }
    }

    // Run at single connected block update
    void UpdateConnectedBlock(Vector3I at, BlockData blockThatExecute) {
        System.Collections.Generic.Dictionary<string, BlockData> blocksToUpdate = CollectConnnectedBlocks(at);

        string blockName = blockThatExecute.BlockName;

        bool connectionToBlockRight = false;
        bool connectionToBlockLeft = false;
        bool connectionToBlockForward = false;
        bool connectionToBlockBackward = false;

        if (blockThatExecute.Type == Block.BlockType.Fence) {
            if (blocksToUpdate["Right"].BlockName != "" && blocksToUpdate["Right"].CanBeConnected)
                connectionToBlockRight = true;
            if (blocksToUpdate["Left"].BlockName != "" && blocksToUpdate["Left"].CanBeConnected)
                connectionToBlockLeft = true;
            if (blocksToUpdate["Forward"].BlockName != "" && blocksToUpdate["Forward"].CanBeConnected)
                connectionToBlockForward = true;
            if (blocksToUpdate["Backward"].BlockName != "" && blocksToUpdate["Backward"].CanBeConnected)
                connectionToBlockBackward = true;

            if (connectionToBlockRight && connectionToBlockLeft && connectionToBlockForward && connectionToBlockBackward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedC"].To<BlockData>();
            }
            else if (connectionToBlockRight && connectionToBlockLeft && connectionToBlockForward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedB"].To<BlockData>();
                blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(-90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Forward;
            }
            else if (connectionToBlockRight && connectionToBlockLeft && connectionToBlockBackward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedB"].To<BlockData>();
                blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Backward;
            }
            else if (connectionToBlockRight && connectionToBlockForward && connectionToBlockBackward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedB"].To<BlockData>();
                blockThatExecute.FacingDirection = Block.FacingDirections.Right;
            }
            else if (connectionToBlockLeft && connectionToBlockForward && connectionToBlockBackward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedB"].To<BlockData>();
                blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(180));
                blockThatExecute.FacingDirection = Block.FacingDirections.Left;
            }
            else if (connectionToBlockForward && connectionToBlockBackward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedMeddel"].To<BlockData>();
                blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(90));
            }
            else if (connectionToBlockRight && connectionToBlockLeft) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedMeddel"].To<BlockData>();
            }
            else if (connectionToBlockLeft && connectionToBlockForward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedD"].To<BlockData>();
                blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(-90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Left;
            }
            else if (connectionToBlockRight && connectionToBlockForward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedD"].To<BlockData>();
                blockThatExecute.FacingDirection = Block.FacingDirections.Forward;
            }
            else if (connectionToBlockLeft && connectionToBlockBackward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedD"].To<BlockData>();
                blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(180));
                blockThatExecute.FacingDirection = Block.FacingDirections.Backward;
            }
            else if (connectionToBlockRight && connectionToBlockBackward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedD"].To<BlockData>();
                blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Right;
            }
            else if (connectionToBlockForward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedA"].To<BlockData>();
                blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(-90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Forward;
            }
            else if (connectionToBlockBackward) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedA"].To<BlockData>();
                blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Backward;
            }
            else if (connectionToBlockRight) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedA"].To<BlockData>();
                blockThatExecute.FacingDirection = Block.FacingDirections.Right;
            }
            else if (connectionToBlockLeft) {
                blockThatExecute = Register.BlockDataMap[blockName]["ConnectedA"].To<BlockData>();
                blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(180));
                blockThatExecute.FacingDirection = Block.FacingDirections.Left;
            }
        }
        else if (blockThatExecute.Type == Block.BlockType.Roof || blockThatExecute.Type == Block.BlockType.Stairs) {
            Block.BlockType type = blockThatExecute.Type;
            int turnUp = blockThatExecute.UpsideDown ? 1 : 0;


            if (blocksToUpdate["Right"].BlockName != "" && blocksToUpdate["Right"].Type == type)
                connectionToBlockRight = true;
            if (blocksToUpdate["Left"].BlockName != "" && blocksToUpdate["Left"].Type == type)
                connectionToBlockLeft = true;
            if (blocksToUpdate["Forward"].BlockName != "" && blocksToUpdate["Forward"].Type == type)
                connectionToBlockForward = true;
            if (blocksToUpdate["Backward"].BlockName != "" && blocksToUpdate["Backward"].Type == type)
                connectionToBlockBackward = true;

            if (
                (connectionToBlockRight && connectionToBlockForward) ||
                (connectionToBlockRight && connectionToBlockBackward) ||
                (connectionToBlockLeft && connectionToBlockForward) ||
                (connectionToBlockLeft && connectionToBlockBackward)
               ) {
                if (blocksToUpdate["Forward"].FacingDirection == Block.FacingDirections.Right && blocksToUpdate["Left"].FacingDirection == Block.FacingDirections.Forward) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedA"].To<BlockData>();
                    blockThatExecute.Rotate(new(0, turnUp, 1));
                }
                else if (blocksToUpdate["Backward"].FacingDirection == Block.FacingDirections.Right && blocksToUpdate["Left"].FacingDirection == Block.FacingDirections.Backward) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedA"].To<BlockData>();
                    blockThatExecute.Rotate(new(-1, turnUp, 0));
                }
                else if (blocksToUpdate["Right"].FacingDirection == Block.FacingDirections.Backward && blocksToUpdate["Backward"].FacingDirection == Block.FacingDirections.Left) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedA"].To<BlockData>();
                    blockThatExecute.Rotate(new(0, turnUp, -1));
                }
                else if (blocksToUpdate["Right"].FacingDirection == Block.FacingDirections.Forward && blocksToUpdate["Forward"].FacingDirection == Block.FacingDirections.Left) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedA"].To<BlockData>();
                    blockThatExecute.Rotate(new(0, turnUp, 0));
                }
                else if (blocksToUpdate["Left"].FacingDirection == Block.FacingDirections.Backward && blocksToUpdate["Forward"].FacingDirection == Block.FacingDirections.Left) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedB"].To<BlockData>();
                    blockThatExecute.Rotate(new(0, turnUp, -1));
                }
                else if (blocksToUpdate["Left"].FacingDirection == Block.FacingDirections.Forward && blocksToUpdate["Backward"].FacingDirection == Block.FacingDirections.Left) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedB"].To<BlockData>();
                    blockThatExecute.Rotate(new(0, turnUp, 0));
                }
                else if (blocksToUpdate["Right"].FacingDirection == Block.FacingDirections.Forward && blocksToUpdate["Backward"].FacingDirection == Block.FacingDirections.Right) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedB"].To<BlockData>();
                    blockThatExecute.Rotate(new(0, turnUp, 1));
                }
                else if (blocksToUpdate["Right"].FacingDirection == Block.FacingDirections.Backward && blocksToUpdate["Forward"].FacingDirection == Block.FacingDirections.Right) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedB"].To<BlockData>();
                    blockThatExecute.Rotate(new(-1, turnUp, 0));
                }

                if (turnUp == 1)
                    blockThatExecute.Basis.Rotated(Vector3.Up, Mathf.DegToRad(90));
            }
        }
    }

    // Collect all connected Blocks
    public System.Collections.Generic.Dictionary<string, BlockData> CollectConnnectedBlocks(Vector3I at) {
        System.Collections.Generic.Dictionary<string, BlockData> connectedBlocks = new();

        BlockData nextToBlockRight = _rawBlocks[new(at.X + 1, at.Y, at.Z)];
        BlockData nextToBlockLeft = _rawBlocks[new(at.X - 1, at.Y, at.Z)];
        BlockData nextToBlockForward = _rawBlocks[new(at.X, at.Y, at.Z + 1)];
        BlockData nextToBlockBackward = _rawBlocks[new(at.X, at.Y, at.Z - 1)];

        connectedBlocks.Add("Right", nextToBlockRight);
        connectedBlocks.Add("Left", nextToBlockLeft);
        connectedBlocks.Add("Forward", nextToBlockForward);
        connectedBlocks.Add("Backward", nextToBlockBackward);

        return connectedBlocks;
    }

    // Run at multiple block updates
    public void UpdateBlocks(System.Collections.Generic.Dictionary<Vector3I, BlockData> blocksToUpdate) {
        if (blocksToUpdate != null || blocksToUpdate.Count > 0) {
            foreach (var block in blocksToUpdate) {
                if (block.Value.Type == Block.BlockType.Fence || block.Value.Type == Block.BlockType.Roof || block.Value.Type == Block.BlockType.Stairs) {
                    Vector3I blockPosition = new Vector3I(Mathf.RoundToInt(block.Key.X), Mathf.RoundToInt(block.Key.Y), Mathf.RoundToInt(block.Key.Z));

                    block.Value.Basis = new Basis(block.Value.Basis.GetRotationQuaternion());
                    block.Value.UpsideDown = block.Value.UpsideDown;
                    block.Value.FacingDirection = block.Value.FacingDirection;

                    UpdateConnectedBlock(blockPosition, block.Value);
                }
            }
        }
    }
}