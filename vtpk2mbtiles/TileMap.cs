using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace vtpk2mbtiles
{
    public class TileMap
    {
        /**
		 *	Recursively read tiles from a tilemap
		 */
        public static void Read(List<TileId> tileIds, IEnumerable<dynamic> index, int zoomLevel, long parentRow, long parentCol)
		{
			zoomLevel++;

			List<dynamic> children = index.ToList();

			long col = parentCol * 2; 
			long row = parentRow * 2; 

			if (children[0] is JArray || 1 == ((JValue)children[0]).Value<int>())
			{
				tileIds.Add(new TileId { Z = zoomLevel, X = col, Y = row });
			}

			if (children[1] is JArray || 1 == ((JValue)children[1]).Value<int>())
			{
				tileIds.Add(new TileId { Z = zoomLevel, X = col + 1, Y = row });
			}

			if (children[2] is JArray || 1 == ((JValue)children[2]).Value<int>())
			{
				tileIds.Add(new TileId { Z = zoomLevel, X = col, Y = row + 1 });
			}

			if (children[3] is JArray || 1 == ((JValue)children[3]).Value<int>())
			{
				tileIds.Add(new TileId { Z = zoomLevel, X = col + 1, Y = row + 1 });
			}
			

			if (children[0] is JArray) { Read(tileIds, children[0], zoomLevel, row, col); }
			if (children[1] is JArray) { Read(tileIds, children[1], zoomLevel, row, col + 1); }
			if (children[2] is JArray) { Read(tileIds, children[2], zoomLevel, row + 1, col); }
			if (children[3] is JArray) { Read(tileIds,children[3], zoomLevel, row + 1, col + 1); }
		}
	}
}
