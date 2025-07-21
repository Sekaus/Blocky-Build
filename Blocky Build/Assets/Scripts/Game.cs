using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
public partial class Game : Node {
    [Export]
    public WorldData worldData;

    [Export]
    public PlayerController[] players;

    // Set block in world at xyz
    public void SetBlock(Block newBlock, int x, int y, int z, bool runBlockUpdates = true, Vector3 rotation = new Vector3()) {
        /*if (!IsThereABlockAt(x, y, z)) {
            if (newBlock != null) {
                newBlock.BlockName = newBlock.Name;

                newBlock = RotateBlock(newBlock, rotation);
            }

            if (runBlockUpdates)
                newBlock = UpdateBlock(x, y, z, newBlock);

            newBlock.Position = new Vector3I(x, y, z);

            newBlock.Name = "block_" + x + "_" + y + "_" + z;

            //map.AddChild(newBlock);

            if (runBlockUpdates) {
                Godot.Collections.Dictionary<string, Block> blocksToUpdate = CollectConnnectedBlocks(x, y, z);

                UpdateBlocks(blocksToUpdate);
            }
        }*/
    }

    // Rotate a block at xyz (hole turns)
    public Block RotateBlock(Block block, Vector3 rotation, bool doubleRotationUp = false) {
        bool upsideDown = rotation.Y > 0;
        int rotationUp = doubleRotationUp ? 180 : 90;

        if (rotation == Vector3.Zero) {
            return block;
        }

        if (rotation.X > 0) {
            block.FacingDirection = Block.FacingDirections.Left;
            if (upsideDown) {
                block.Rotate(Vector3.Forward, Mathf.DegToRad(rotationUp));
            }
        }
        else if (rotation.X < 0) {
            block.Rotate(Vector3.Up, Mathf.DegToRad(180));
            block.FacingDirection = Block.FacingDirections.Right;
            if (upsideDown) {
                block.Rotate(Vector3.Back, Mathf.DegToRad(rotationUp));
            }
        }
        else if (rotation.Z > 0) {
            block.Rotate(Vector3.Up, Mathf.DegToRad(-90));
            block.FacingDirection = Block.FacingDirections.Forward;
            if (upsideDown) {
                block.Rotate(Vector3.Right, Mathf.DegToRad(rotationUp));
            }
        }
        else if (rotation.Z < 0) {
            block.Rotate(Vector3.Up, Mathf.DegToRad(90));
            block.FacingDirection = Block.FacingDirections.Backward;
            if (upsideDown) {
                block.Rotate(Vector3.Left, Mathf.DegToRad(rotationUp));
            }
        }
        else if (rotation == Vector3.Up || rotation == Vector3.Zero)
            block.Rotate(Vector3.Forward, Mathf.DegToRad(rotationUp));

        if (upsideDown)
            block.UpsideDown = true;

        return block;
    }


    // Remove block in world at xyz
    public void RemoveBlock(int x, int y, int z, bool runBlockUpdates = true) {
        /*if (IsThereABlockAt(x, y, z)) {
            string blockName = "block_" + x + "_" + y + "_" + z;
            Block block = getBlockAsObject(x, y, z);

            //map.RemoveChild(block);
            block.QueueFree();

            if (runBlockUpdates) {
                Godot.Collections.Dictionary<string, Block> blocksToUpdate = CollectConnnectedBlocks(x, y, z);

                UpdateBlocks(blocksToUpdate);
            }
        }*/
    }

    // Run at block single update
    public Block UpdateBlock(int atX, int atY, int atZ, Block blockThatExecute) {
        blockThatExecute = UpdateConnectedBlock(atX, atY, atZ, blockThatExecute);

        return blockThatExecute;
    }

    // Collect all connected Blocks
    public Godot.Collections.Dictionary<string, Block> CollectConnnectedBlocks(int atX, int atY, int atZ) {
        Godot.Collections.Dictionary<string, Block> connectedBlocks = new Godot.Collections.Dictionary<string, Block>();

        /*Block nextToBlockRight = getBlockAsObject(atX + 1, atY, atZ);
        Block nextToBlockLeft = getBlockAsObject(atX - 1, atY, atZ);
        Block nextToBlockForward = getBlockAsObject(atX, atY, atZ + 1);
        Block nextToBlockBackward = getBlockAsObject(atX, atY, atZ - 1);

        connectedBlocks.Add("Right", nextToBlockRight);
        connectedBlocks.Add("Left", nextToBlockLeft);
        connectedBlocks.Add("Forward", nextToBlockForward);
        connectedBlocks.Add("Backward", nextToBlockBackward);*/

        return connectedBlocks;
    }

    // Run at multiple block updates
    public void UpdateBlocks(Godot.Collections.Dictionary<string, Block> blocksToUpdate) {
        if (blocksToUpdate != null || blocksToUpdate.Count > 0) {
            foreach (var block in blocksToUpdate) {
                if (block.Value.Type == Block.BlockType.Fence || block.Value.Type == Block.BlockType.Roof || block.Value.Type == Block.BlockType.Stairs) {
                    Block updatedBlock = Register.Blocks[block.Value.BlockName].Instantiate<Block>();

                    updatedBlock.BlockName = block.Value.BlockName;

                    Vector3I blockPosition = new Vector3I(Mathf.RoundToInt(block.Value.Position.X), Mathf.RoundToInt(block.Value.Position.Y), Mathf.RoundToInt(block.Value.Position.Z));

                    updatedBlock.Rotation = block.Value.Rotation;
                    updatedBlock.UpsideDown = block.Value.UpsideDown;
                    updatedBlock.FacingDirection = block.Value.FacingDirection;
                    
                    updatedBlock = UpdateBlock(blockPosition.X, blockPosition.Y, blockPosition.Z, updatedBlock);

                    updatedBlock.Position = blockPosition;

                    //map.RemoveChild(block.Value);
                    block.Value.QueueFree();

                    updatedBlock.Name = "block_" + updatedBlock.Position.X + "_" + updatedBlock.Position.Y + "_" + updatedBlock.Position.Z;

                    //map.AddChild(updatedBlock);
                }
            }
        }
    }

    // Run at single connected block update
    public Block UpdateConnectedBlock(int atX, int atY, int atZ, Block blockThatExecute) {
        Godot.Collections.Dictionary<string, Block> blocksToUpdate = CollectConnnectedBlocks(atX, atY, atZ);

        string newBlock = blockThatExecute.BlockName;

        bool connectionToBlockRight = false;
        bool connectionToBlockLeft = false;
        bool connectionToBlockForward = false;
        bool connectionToBlockBackward = false;

        if (blockThatExecute.Type == Block.BlockType.Fence) {
            if (blocksToUpdate["Right"].BlockName != "" && blocksToUpdate["Right"].canBeConnected)
                connectionToBlockRight = true;
            if (blocksToUpdate["Left"].BlockName != "" && blocksToUpdate["Left"].canBeConnected)
                connectionToBlockLeft = true;
            if (blocksToUpdate["Forward"].BlockName != "" && blocksToUpdate["Forward"].canBeConnected)
                connectionToBlockForward = true;
            if (blocksToUpdate["Backward"].BlockName != "" && blocksToUpdate["Backward"].canBeConnected)
                connectionToBlockBackward = true;

            if (connectionToBlockRight && connectionToBlockLeft && connectionToBlockForward && connectionToBlockBackward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedC"].Instantiate<Block>();
            }
            else if (connectionToBlockRight && connectionToBlockLeft && connectionToBlockForward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedB"].Instantiate<Block>();
                blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(-90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Forward;
            }
            else if (connectionToBlockRight && connectionToBlockLeft && connectionToBlockBackward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedB"].Instantiate<Block>();
                blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Backward;
            }
            else if (connectionToBlockRight && connectionToBlockForward && connectionToBlockBackward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedB"].Instantiate<Block>();
                blockThatExecute.FacingDirection = Block.FacingDirections.Right;
            }
            else if (connectionToBlockLeft && connectionToBlockForward && connectionToBlockBackward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedB"].Instantiate<Block>();
                blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(180));
                blockThatExecute.FacingDirection = Block.FacingDirections.Left;
            }
            else if (connectionToBlockForward && connectionToBlockBackward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedMeddel"].Instantiate<Block>();
                blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(90));
            }
            else if (connectionToBlockRight && connectionToBlockLeft) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedMeddel"].Instantiate<Block>();
            }
            else if (connectionToBlockLeft && connectionToBlockForward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedD"].Instantiate<Block>();
                blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(-90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Left;
            }
            else if (connectionToBlockRight && connectionToBlockForward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedD"].Instantiate<Block>();
                blockThatExecute.FacingDirection = Block.FacingDirections.Forward;
            }
            else if (connectionToBlockLeft && connectionToBlockBackward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedD"].Instantiate<Block>();
                blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(180));
                blockThatExecute.FacingDirection = Block.FacingDirections.Backward;
            }
            else if (connectionToBlockRight && connectionToBlockBackward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedD"].Instantiate<Block>();
                blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Right;
            }
            else if (connectionToBlockForward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedA"].Instantiate<Block>();
                blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(-90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Forward;
            }
            else if (connectionToBlockBackward) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedA"].Instantiate<Block>();
                blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(90));
                blockThatExecute.FacingDirection = Block.FacingDirections.Backward;
            }
            else if (connectionToBlockRight) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedA"].Instantiate<Block>();
                blockThatExecute.FacingDirection = Block.FacingDirections.Right;
            }
            else if (connectionToBlockLeft) {
                blockThatExecute = Register.Blocks["StoneFence"]["ConnectedA"].Instantiate<Block>();
                blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(180));
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
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedA"].Instantiate<Block>();
                    blockThatExecute = RotateBlock(blockThatExecute, new(0, turnUp, 1), true);
                }
                else if (blocksToUpdate["Backward"].FacingDirection == Block.FacingDirections.Right && blocksToUpdate["Left"].FacingDirection == Block.FacingDirections.Backward) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedA"].Instantiate<Block>();
                    blockThatExecute = RotateBlock(blockThatExecute, new(-1, turnUp, 0), true);
                }
                else if (blocksToUpdate["Right"].FacingDirection == Block.FacingDirections.Backward && blocksToUpdate["Backward"].FacingDirection == Block.FacingDirections.Left) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedA"].Instantiate<Block>();
                    blockThatExecute = RotateBlock(blockThatExecute, new(0, turnUp, -1), true);
                }
                else if (blocksToUpdate["Right"].FacingDirection == Block.FacingDirections.Forward && blocksToUpdate["Forward"].FacingDirection == Block.FacingDirections.Left) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedA"].Instantiate<Block>();
                    blockThatExecute = RotateBlock(blockThatExecute, new(0, turnUp, 0), true);
                }
                else if (blocksToUpdate["Left"].FacingDirection == Block.FacingDirections.Backward && blocksToUpdate["Forward"].FacingDirection == Block.FacingDirections.Left) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedB"].Instantiate<Block>();
                    blockThatExecute = RotateBlock(blockThatExecute, new(0, turnUp, -1), true);
                }
                else if (blocksToUpdate["Left"].FacingDirection == Block.FacingDirections.Forward && blocksToUpdate["Backward"].FacingDirection == Block.FacingDirections.Left) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedB"].Instantiate<Block>();
                    blockThatExecute = RotateBlock(blockThatExecute, new(0, turnUp, 0), true);
                }
                else if (blocksToUpdate["Right"].FacingDirection == Block.FacingDirections.Forward && blocksToUpdate["Backward"].FacingDirection == Block.FacingDirections.Right) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedB"].Instantiate<Block>();
                    blockThatExecute = RotateBlock(blockThatExecute, new(0, turnUp, 1), true);
                }
                else if (blocksToUpdate["Right"].FacingDirection == Block.FacingDirections.Backward && blocksToUpdate["Forward"].FacingDirection == Block.FacingDirections.Right) {
                    blockThatExecute = Register.Blocks[blockThatExecute.BlockName]["ConnectedB"].Instantiate<Block>();
                    blockThatExecute = RotateBlock(blockThatExecute, new(-1, turnUp, 0), true);
                }

                if (turnUp == 1)
                    blockThatExecute.Rotate(Vector3.Up, Mathf.DegToRad(90));
            }
        }

        blockThatExecute.BlockName = newBlock;

        return blockThatExecute;
    }

    // Test if there is a block at xyz
    /*public bool IsThereABlockAt(int x, int y, int z) {
        string blockName = "block_" + x + "_" + y + "_" + z;
        return map.GetNodeOrNull(blockName) is not null;
    }

    // Get block as object at xyz
    public Block getBlockAsObject(int x, int y, int z) {
        string blockName = "block_" + x + "_" + y + "_" + z;
        if(IsThereABlockAt(x, y, z))
            return map.GetNode<Block>(blockName);
        else
            return new Block(true);
    }*/

    Thread t;
    public override async void _Ready() {
        Input.MouseMode = Input.MouseModeEnum.Captured;

        Vector3I selectedChunk = new Vector3I(0, 0, 0);

        // Genarate first world chunk

        await worldData.GenChunk(selectedChunk);

        await worldData.LoadChunk(selectedChunk);

        foreach (var player in players)
            player.Freeze = false;

        // and the rest

        t = new Thread(async () => await GenChunks(selectedChunk));

        t.Start();
    }

    public async Task GenChunks(Vector3I chunkPosition) {
        List<Vector3I> chunkPositions = new List<Vector3I>(GameSettings.MaxCunksOnSceen);

        float sqmcr = (MathF.Sqrt(GameSettings.MaxCunksOnSceen) / 2 - 1);

        Vector3I startChunk = new Vector3I(-(int)sqmcr, 0, -(int)sqmcr);
        for(Vector3I correntChunkX = startChunk; correntChunkX.X <= -startChunk.X; correntChunkX.X++) {
            for (Vector3I correntChunkZ = correntChunkX; correntChunkZ.Z <= -startChunk.Z; correntChunkZ.Z++) {
                chunkPositions.Add(correntChunkZ);
            }
        }

        var semaphore = new SemaphoreSlim(4); // Limit to 4 concurrent chunk generations
        var tasks = new List<Task>();

        foreach (var _chunkPosition in chunkPositions) {
            await semaphore.WaitAsync();
            var task = Task.Run(async () => {
                try {
                    await worldData.GenChunk(_chunkPosition);
                }
                finally {
                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        MainThreadDispatcher.Enqueue(() => {
            LoadChunks();
        });
    }

    async void LoadChunks() {
        await worldData.LoadChunks();
    }

    public override void _Process(double delta) {
        if (Input.IsActionPressed("exit"))
            GetTree().Quit();

        LoadChunks();
    }
}