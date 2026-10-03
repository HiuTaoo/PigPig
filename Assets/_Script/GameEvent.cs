namespace _Script
{
    public struct CellClickedEvent
    {
        public Node ClickedNode;

        public CellClickedEvent(Node node)
        {
            ClickedNode = node;
        }
    }
    
    public struct CellDoubleClickedEvent
    {
        public Node ClickedNode { get; }

        public CellDoubleClickedEvent(Node node)
        {
            ClickedNode = node;
        }
    }

    public struct CellCorrectClickEvent
    {
        public Node ClickedNode { get; }

        public CellCorrectClickEvent(Node node)
        {
            ClickedNode = node;
        }
    }
    
    public struct CellInCorrectClickEvent
    {
        public Node ClickedNode { get; }

        public CellInCorrectClickEvent(Node node)
        {
            ClickedNode = node;
        }
    }
    
    public struct GameOverEvent
    {
        public int Level;
        public GameOverEvent(int level){
            Level = level;
        }
    }

    public struct LevelCompletedEvent
    {
        public int LevelIndex;
        public LevelCompletedEvent(int levelIndex)
        {
            LevelIndex = levelIndex;
        }
    }

    public struct RestartLevelEvent { }
}