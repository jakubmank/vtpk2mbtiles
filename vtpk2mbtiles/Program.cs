using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static System.FormattableString;


namespace vtpk2mbtiles {

	public class CancelObject {
		public bool UserCancelled = false;
	}

	class Program {


		private static HashSet<string> _tiles = new HashSet<string>();
		private static List<TileId> _tileIds = new List<TileId>();
		private static CancelObject _cancel = new CancelObject();
		private static bool _done = false;


		static int Main(string[] args) {

			Console.CancelKeyPress += (sender, evt) => {
				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine("!!!!! CANCELLED BY USER !!!!!");
				Console.WriteLine("trying to shutdown gracefully ...");
				Console.WriteLine("mbtiles file might be corrupted");
				Console.WriteLine();
				Console.WriteLine();
				_cancel.UserCancelled = true;

				while (!_done) {
					Task.Delay(100).Wait();
				}

			};

			if (args.Length != 2) {
				Console.WriteLine("wrong number of arguments, use:");
				Console.WriteLine();
				Console.WriteLine("         vtpk-directory output-directory <unzip>");
				Console.WriteLine();
				Console.WriteLine();
				return 1;
			}

			string vtpkDir = args[0];
			string destination = args[1];
			Console.WriteLine("decompressing tiles");

            VtpkFile vtpkFile = new VtpkFile(vtpkDir, destination);
			
			// Check if VtpkFile has all necessary files
			if (vtpkFile.Validate() != null)
			{
                vtpkFile.Validate().ForEach(Console.WriteLine);
                return 1;
            }

            IOutput outputWriter;
			Console.WriteLine($"folder destination: [{destination}]");
			outputWriter = new OutputFiles(destination);


			DateTime dtStart = DateTime.Now;

			// Get all tiles from TileMap json file
			dynamic tilemap = JObject.Parse(File.ReadAllText(vtpkFile.TileMapPath));
			IEnumerable<dynamic> index = tilemap.index;
			TileMap.Read(_tiles, _tileIds, index, 0, 0, 0);

			HashSet<string> bundleFiles = vtpkFile.GetBundleFiles();

			TilesConverter converter = new TilesConverter(outputWriter, _cancel);
			
			if (null == bundleFiles || 0 == bundleFiles.Count) {
				Console.WriteLine($"no bundle files found.");
			} else {
				Console.WriteLine($"using {Environment.ProcessorCount} processors");
                converter.BundlesToPbf(bundleFiles,_tileIds);
            }

			outputWriter.Dispose();
			outputWriter = null;

			DateTime dtFinished = DateTime.Now;
			TimeSpan elapsed = dtFinished.Subtract(dtStart);

			Console.WriteLine($"{dtStart:HH:mm:ss} => {dtFinished:HH:mm:ss}");
			Console.WriteLine(Invariant($"total time[min] : {elapsed.TotalMinutes:0.0}"));
			Console.WriteLine(Invariant($"total time[sec] : {elapsed.TotalSeconds:0.0}"));
			Console.WriteLine(Invariant($"tiles/sec       : {(((double)_tileIds.Count) / elapsed.TotalSeconds):0.0}"));

			Console.WriteLine("--------------------------");
			if (converter.FailedTiles.Count > 0) {
				Console.WriteLine("FAILED TILES:");
				foreach (TileId tid in converter.FailedTiles) {
					Console.WriteLine(tid);
				}
				Console.WriteLine("--------------------------");
			}
			Console.WriteLine($"tiles in tilemap : {_tileIds.Count}");
			Console.WriteLine($"processed tiles  : {converter.ProcessedTiles}");
			Console.WriteLine($"failed tiles     : {converter.FailedTiles.Count}");

			_done = true;
			return 0;
		}


	}
}
