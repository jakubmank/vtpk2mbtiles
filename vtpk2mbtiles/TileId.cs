
namespace vtpk2mbtiles {
	public class TileId {
		public int Z { get; set; }
		public long X { get; set; }
		public long Y { get; set; }

		public TileId()
		{
		}
		public TileId(int z, int x, int y)
		{
			this.Z = z;
			this.X = x;
			this.Y = y;
		}

	}
}
