public class GameSettings {
    public const int MaxCunksOnSceen = 64;
    public const int MaxLight = 16;

    public static int ChunkRadius {  
        get {
            return 10;
        } 
    }

    public static int ChunkHeight { 
        get { 
            return 512; 
        } 
    }

    public static int BlockRenderScale {
        get {
            return 2;
        }
    }

    public static int ChunkSizeXZ {
        get {
            return GameSettings.ChunkRadius * 2 + 1;
        }
    }

    public static float ChunkSizeInRender {
        get {
            return ChunkRadius * BlockRenderScale;
        }
    }

    public static int DefaultBedrockLevel { 
        get {
            return 0;
        } 
    }
}