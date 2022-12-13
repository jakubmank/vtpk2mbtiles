using Newtonsoft.Json.Linq;
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
		private readonly string _OutputPath; // where tiles will be saved
		private List<TileId> _TileIds;
		private readonly string _VtpkPath; // path to Vtpk file
		
        public TilesConverter(string vtpkPath, string outputPath)
        {
			_OutputPath = outputPath;
			_VtpkPath = vtpkPath;
			InitConverterVariables();
		}
		
		public void Start()
        {
			VtpkFile vtpkFile = new VtpkFile(_VtpkPath, _OutputPath);

			// Check if VtpkFile has all necessary files
			if (vtpkFile.Validate() != null)
			{
				vtpkFile.Validate().ForEach(Console.WriteLine);
				return; // TODO add error logging
			}
			InitConverterVariables();
			_OutputWriter = new OutputFiles(_OutputPath); // Create output directory

			// Get all tiles from TileMap json file
			dynamic tilemap = JObject.Parse(File.ReadAllText(vtpkFile.TileMapPath));
			IEnumerable<dynamic> index = tilemap.index;
			TileMap.Read(_TileIds, index, 0, 0, 0);

			HashSet<string> bundleFiles = vtpkFile.GetBundleFiles();


			if (null == bundleFiles || 0 == bundleFiles.Count)
			{
				Console.WriteLine($"no bundle files found.");
			}
			else
			{
				Console.WriteLine($"using {Environment.ProcessorCount} processors");
				BundlesToPbf(bundleFiles, _TileIds);
			}

			_OutputWriter.Dispose();
			_OutputWriter = null;
		}
		
        private void BundlesToPbf(HashSet<string> bundleFiles, List<TileId> tileIds)
        {
			Parallel.ForEach(
				bundleFiles
				, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }
				, (bundleFile, loopState) => {


					string zTxt = Path.GetFileName(Path.GetDirectoryName(bundleFile)).Substring(1, 2);
					int z = Convert.ToInt32(zTxt);

					using (VtpkReaderPure vtpkReader = new VtpkReaderPure(bundleFile, _OutputWriter))
					{

						if (!vtpkReader.GetTiles(
							tileIds.Where(
								ti => ti.Z == z
								&& ti.X >= vtpkReader.BundleCol
								&& ti.X < vtpkReader.BundleCol + VtpkReaderPure.PACKET_SIZE
								&& ti.Y >= vtpkReader.BundleRow
								&& ti.Y < vtpkReader.BundleRow + VtpkReaderPure.PACKET_SIZE
							).ToList()
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

		private void InitConverterVariables()
        {
			FailedTiles = new ConcurrentBag<TileId>();
			ProcessedTiles = 0;
			_TileIds = new List<TileId>();
		}

    }
}
