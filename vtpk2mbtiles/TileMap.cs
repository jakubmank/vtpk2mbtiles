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
        public static void Read(HashSet<string> tiles,List<TileId> tileIds, IEnumerable<dynamic> index, int zoomLevel, long parentRow, long parentCol)
		{
			zoomLevel++;

			List<dynamic> children = index.ToList();

			long col = parentCol * 2; 
			long row = parentRow * 2; 

			if (children[0] is JArray || 1 == ((JValue)children[0]).Value<int>())
			{
				tiles.Add($"{zoomLevel}/{col}/{row}");
				tileIds.Add(new TileId { z = zoomLevel, x = col, y = row });
			}

			if (children[1] is JArray || 1 == ((JValue)children[1]).Value<int>())
			{
				tiles.Add($"{zoomLevel}/{col + 1}/{row}");
				tileIds.Add(new TileId { z = zoomLevel, x = col + 1, y = row });
			}

			if (children[2] is JArray || 1 == ((JValue)children[2]).Value<int>())
			{
				tiles.Add($"{zoomLevel}/{col}/{row + 1}");
				tileIds.Add(new TileId { z = zoomLevel, x = col, y = row + 1 });
			}

			if (children[3] is JArray || 1 == ((JValue)children[3]).Value<int>())
			{
				tiles.Add($"{zoomLevel}/{col + 1}/{row + 1}");
				tileIds.Add(new TileId { z = zoomLevel, x = col + 1, y = row + 1 });
			}
			

			if (children[0] is JArray) { Read(tiles,tileIds, children[0], zoomLevel, row, col); }
			if (children[1] is JArray) { Read(tiles,tileIds, children[1], zoomLevel, row, col + 1); }
			if (children[2] is JArray) { Read(tiles,tileIds, children[2], zoomLevel, row + 1, col); }
			if (children[3] is JArray) { Read(tiles,tileIds,children[3], zoomLevel, row + 1, col + 1); }
		}
	}
}
