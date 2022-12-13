using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace vtpk2mbtiles
{
    public class TilesConverter
    {	
		public long ProcessedTiles;
		public ConcurrentBag<TileId> FailedTiles;
		
		private IOutput _OutputWriter;
        private CancelObject _CancelProcess;
		
        public TilesConverter(IOutput outputWriter, CancelObject cancelProcess)
        {
            _OutputWriter = outputWriter;
            _CancelProcess = cancelProcess;
            FailedTiles = new ConcurrentBag<TileId>();
			ProcessedTiles = 0;
        }
		
        public void BundlesToPbf(HashSet<string> bundleFiles, List<TileId> tileIds)
        {
			Parallel.ForEach(
				bundleFiles
				, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }
				, (bundleFile, loopState) => {

					if (_CancelProcess.UserCancelled) { Console.WriteLine("shutting down thread ..."); loopState.Break(); }

					string zTxt = Path.GetFileName(Path.GetDirectoryName(bundleFile)).Substring(1, 2);
					int z = Convert.ToInt32(zTxt);

					using (VtpkReaderPure vtpkReader = new VtpkReaderPure(bundleFile, _OutputWriter))
					{

						if (!vtpkReader.GetTiles(
							tileIds.Where(
								ti => ti.z == z
								&& ti.x >= vtpkReader.BundleCol
								&& ti.x < vtpkReader.BundleCol + VtpkReaderPure.PACKET_SIZE
								&& ti.y >= vtpkReader.BundleRow
								&& ti.y < vtpkReader.BundleRow + VtpkReaderPure.PACKET_SIZE
							).ToList()
							, _CancelProcess
							)
						)
						{
							return;
						}
						foreach (var failedTile in vtpkReader.FailedTiles)
						{
							FailedTiles.Add(failedTile);
						}

						ProcessedTiles = Interlocked.Add(ref ProcessedTiles, vtpkReader.ProcessedTiles);
					}
				}
			);
		}
    }
}
