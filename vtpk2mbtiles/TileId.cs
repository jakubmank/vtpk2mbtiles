﻿
namespace vtpk2mbtiles {
	public class TileId {
		public int z { get; set; }
		public long x { get; set; }
		public long y { get; set; }

		public long TmsY { get { return ((1 << z) - y - 1); } }
		public TileId()
		{

		}
		public TileId(int z, int x, int y)
		{
			// init class members with params
			this.z = z;
			this.x = x;
			this.y = y;
		}

        public override string? ToString()
        {
			return $"z:{z} row/y:{y} col/x:{x}";
		}
	}
}
