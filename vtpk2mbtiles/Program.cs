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
			DateTime dtStart = DateTime.Now;

			TilesConverter converter = new TilesConverter(_cancel, vtpkDir, destination);
			converter.Start();
			
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
