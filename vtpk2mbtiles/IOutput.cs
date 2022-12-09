using System;

namespace vtpk2mbtiles {


	public interface IOutput : IDisposable {

		public bool Write(TileId tid, byte[] data);
	}
}
