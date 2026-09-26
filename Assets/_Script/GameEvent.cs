namespace _Script
{
    public struct GridLoadedEvent
    {
    }

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

    public struct CheckRulesEvent { }

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