public class GameSettings {
    static int maxCunksOnSceen = 64;

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

    public static int DefaultBedrockLevel { 
        get {
            return 0;
        } 
    }
}