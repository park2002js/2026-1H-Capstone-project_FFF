using System.Collections.Generic;

namespace FFF.Map
{
    public class MapData
    {
        public const int FLOORS = 15;
        public const int COLUMNS = 7;

        public MapNode[,] Nodes = new MapNode[FLOORS, COLUMNS];
        public MapNode BossNode;
        public int Seed;

        public MapNode GetNode(int floor, int column)
        {
            if (floor < 0 || floor >= FLOORS || column < 0 || column >= COLUMNS) return null;
            return Nodes[floor, column];
        }

        public List<MapNode> GetFloor(int floor)
        {
            var list = new List<MapNode>();
            for (int col = 0; col < COLUMNS; col++)
            {
                if (Nodes[floor, col] != null)
                    list.Add(Nodes[floor, col]);
            }
            return list;
        }
    }
}
