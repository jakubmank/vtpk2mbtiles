using System;
using System.Collections.Generic;
using System.IO;



namespace vtpk2mbtiles {

	public class VtpkReaderPure : IDisposable {


		public const int PACKET_SIZE = 128;//TODO take this from 'root.json': resourceinfo -> cacheinfo -> storageinfo -> packetsize
		private const int HEADER_SIZE = 64;
		private const int TILE_INDEX_SIZE_INFO = 8;

		private bool _disposed = false;
		private BinaryReader _bundleReader;
		private long _bundleRow;
		private long _bundleCol;
		private IOutput _outputWriter;


		public VtpkReaderPure(string bundleFileName, IOutput outputWriter) {

			_outputWriter = outputWriter;

			(long row, long col) bundle = GetBundleRowAndSize(Path.GetFileNameWithoutExtension(bundleFileName));
			_bundleRow = bundle.row;
			_bundleCol = bundle.col;

			Console.WriteLine($"processing bundle row:{_bundleRow} col:{_bundleCol} {bundleFileName}");

			_bundleReader = new BinaryReader(File.OpenRead(bundleFileName));
		}
		public VtpkReaderPure()
        {
			
        }
		#region IDisposable

		~VtpkReaderPure() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposeManagedResources) {
			if (!_disposed) {
				if (disposeManagedResources) {
					if (null != _bundleReader) {
						_bundleReader.Close();
						_bundleReader.Dispose();
						_bundleReader = null;
					}
				}
			}
			_disposed = true;
		}

		#endregion


		public long BundleCol { get { return _bundleCol; } }
		public long BundleRow { get { return _bundleRow; } }

		public HashSet<TileId> FailedTiles { private set; get; } = new HashSet<TileId>();
		public long ProcessedTiles { private set; get; } = 0;

		public bool GetTiles(List<TileId> tileIds, CancelObject cancel)
		{
			try
			{

				FailedTiles = new HashSet<TileId>();
				ProcessedTiles = 0;
				foreach (TileId tid in tileIds)
				{
					(long offset, long size) tileInfo;
					if (cancel.UserCancelled)
					{
						Console.WriteLine($"breaking out of tile processing ...");
						return false;
					}
					
					try
                    {
						tileInfo = GetTileOffsetAndSize(tid, _bundleRow, _bundleCol, _bundleReader);

					}
					catch (Exception ex) {
						Console.WriteLine($"{tid} error getting offset and size in bundle:{Environment.NewLine}{ex}");
						FailedTiles.Add(tid);
						continue;
					}

					_bundleReader.BaseStream.Position = tileInfo.offset;
					byte[] oneTile = _bundleReader.ReadBytes((int)tileInfo.size);

					if (null == oneTile || 0 == oneTile.Length)
					{
						Console.WriteLine($"error: {tid} no tile data read. offset:{tileInfo.offset} size:{tileInfo.size}");
						FailedTiles.Add(tid);
						continue;
					}
					// decompress tile
					byte[] decompressed = Compression.Decompress(oneTile);
					if (null == decompressed || oneTile.Length == decompressed.Length)
					{
						Console.WriteLine($"error: {tid} could not be decompressd. offset:{tileInfo.offset} size:{tileInfo.size}");
						FailedTiles.Add(tid);
						continue;
					}
					oneTile = decompressed;
					
					if (!_outputWriter.Write(tid, oneTile))
					{
						FailedTiles.Add(tid);
						continue;
					}

					ProcessedTiles++;
				}

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"error getting tiles: {ex}");
				return false;
			}
		}
		
		public (long offset, long size) GetTileOffsetAndSize(TileId tid, long bundleRow, long bundleCol, BinaryReader bundleReader)
		{
			(long row, long col) relPosition = GetRelativePosition(tid, bundleRow, bundleCol);

			long tileIndexOffset = GetTileIndexOffset(relPosition);
            long tileIndexValue = GetTileIndexValue(bundleReader, tileIndexOffset);
            long tileOffset = GetTileOffset(tileIndexValue);
			long tileSize = GetTileSize(tileIndexValue);

			bundleReader.BaseStream.Seek(tileOffset - 4, 0);

            return (tileOffset, tileSize);
		}
		private (long, long) GetBundleRowAndSize(string bundleName)
        {
			string rowTxt = bundleName.Substring(1, 4);
			string colTxt = bundleName.Substring(6, 4);
			long row = Convert.ToInt64(rowTxt, 16);
			long col = Convert.ToInt64(colTxt, 16);
			return (row, col);
		}
		
		private long GetTileOffset(long tileIndexValue)
        {
			/*
			 * Extract the tile offset from tileIndexValue by shifting and masking the value. The tile offset is represented by the lower 40 bits of the value.
			 */
			return tileIndexValue & (1L << 40) - 1L;
        }
		
		private long GetTileSize(long tileIndexValue)
		{
			/*
        	 * Extract the tile size from the tileIndexValue by shifting and masking the value. The tile size is represented by the next 20 bits after the lower 40 bits have been discarded.
			*/
			return (tileIndexValue >> 40) & ((1 << 20) - 1);
		}

        private long GetTileIndexOffset((long row, long col) relativePosition)
        {
			//See https://github.com/Esri/raster-tiles-compactcache/blob/master/CompactCacheV2.md#tile-index
			long tileIndexOffset = HEADER_SIZE + TILE_INDEX_SIZE_INFO * (PACKET_SIZE * relativePosition.row + relativePosition.col); 
			return tileIndexOffset;
        }

        private (long, long) GetRelativePosition(TileId tid, long bundleRow, long bundleCol)
        {
			long row = tid.y - bundleRow;
			long col = tid.x - bundleCol;
			return (row, col);
		}
		
        private long GetTileIndexValue(BinaryReader bundleReader, long tileIndexOffset)
        {
			bundleReader.BaseStream.Seek(tileIndexOffset, 0);
			byte[] rawBytes = bundleReader.ReadBytes(8);
			return BitConverter.ToInt64(rawBytes);
		}
	
	}
}
