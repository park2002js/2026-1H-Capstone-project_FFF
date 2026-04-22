using System.Collections.Generic;

namespace FFF.Map
{
    public class MapNode
    {
        public int Floor;
        public int Column;
        public RoomType RoomType = RoomType.None;
        public List<MapNode> Next = new List<MapNode>();
        public List<MapNode> Prev = new List<MapNode>();
    }
}
