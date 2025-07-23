public class GameSettings {
    public const int maxCunksOnSceen = 64;
    public const float BlockSize = 0.5f;

    public static int MaxCunksOnSceen {
        get {
            return maxCunksOnSceen;
        }
    }

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

    public static float ChunkSize {
        get {
            return ChunkRadius * BlockSize * BlockRenderScale;
        }
    }

    public static int BlockRenderScale {
        get {
            return 2;
        }
    }

    public static int DefaultBedrockLevel { 
        get {
            return 0;
        } 
    }
}