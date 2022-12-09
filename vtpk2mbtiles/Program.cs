﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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




			if (!Directory.Exists(vtpkDir)) {
				Console.WriteLine($"vtpk directory does not exist: {vtpkDir}");
				return 1;
			}

			if (destination.ToLower().EndsWith(".mbtiles")) {
				if (File.Exists(destination)) {
					Console.WriteLine($"destination mbtiles file already exists: [{destination}]");
					return 1;
				}
			} else {
				if (Directory.Exists(destination)) {
					Console.WriteLine($"destination directory already exists: {destination}");
					return 1;
				}
			}

			string tilesDir = Path.Combine(vtpkDir, "p12", "tile");
			if (!Directory.Exists(tilesDir)) {
				Console.WriteLine($"tile directory not found: {tilesDir}");
				return 1;
			}

			string[] levelDirs = Directory.GetDirectories(tilesDir, "L*");
			if (null == levelDirs || levelDirs.Length < 1) {
				Console.WriteLine($"tile directory is empty");
				return 1;
			}

			string tileMapPath = Path.Combine(vtpkDir, "p12", "tilemap", "root.json");
			if (!File.Exists(tileMapPath)) {
				Console.WriteLine($"tile map not found: {tileMapPath}");
				return 1;
			}

			string rootRootJson = Path.Combine(vtpkDir, "p12", "root.json");
			if (!File.Exists(rootRootJson)) {
				Console.WriteLine($"main root.json not found: {rootRootJson}");
				return 1;
			}

			string infoJson = Path.Combine(vtpkDir, "p12", "resources", "info", "root.json");
			if (!File.Exists(infoJson)) {
				Console.WriteLine($"resources info root.json not found: {infoJson}");
				return 1;
			}

			string styleJson = Path.Combine(vtpkDir, "p12", "resources", "styles", "root.json");
			if (!File.Exists(styleJson)) {
				Console.WriteLine($"resources styles root.json not found: {styleJson}");
				return 1;
			}


			string itemInfoXml = Path.Combine(vtpkDir, "esriinfo", "iteminfo.xml");
			if (!File.Exists(itemInfoXml)) {
				Console.WriteLine($"iteminfo not found: {itemInfoXml}");
				return 1;
			}

			dynamic style = JObject.Parse(File.ReadAllText(styleJson, Encoding.UTF8));
			IEnumerable<dynamic> styleLayers = style.layers;


			IOutput outputWriter;
			Console.WriteLine($"folder destination: [{destination}]");
			outputWriter = new OutputFiles(destination);


			DateTime dtStart = DateTime.Now;


			dynamic tilemap = JObject.Parse(File.ReadAllText(tileMapPath));
			IEnumerable<dynamic> index = tilemap.index;

			traverseLevels(index, 0, 0, 0);

			ConcurrentBag<TileId> failedTiles = new ConcurrentBag<TileId>();
			long processedTiles = 0;

			HashSet<string> bundleFiles = new HashSet<string>();

			foreach (string levelDir in levelDirs) {
				string zTxt = Path.GetFileName(levelDir).Substring(1, 2);
				int z = Convert.ToInt32(zTxt);


				bundleFiles.UnionWith(Directory.GetFiles(levelDir, "*.bundle").OrderBy(b => b).ToArray());
			}

			if (null == bundleFiles || 0 == bundleFiles.Count) {
				Console.WriteLine($"no bundle files found.");
			} else {

				Console.WriteLine($"using {Environment.ProcessorCount} processors");

				Parallel.ForEach(
					bundleFiles
					, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }
					, (bundleFile, loopState) => {

						if (_cancel.UserCancelled) { Console.WriteLine("shutting down thread ..."); loopState.Break(); }

						string zTxt = Path.GetFileName(Path.GetDirectoryName(bundleFile)).Substring(1, 2);
						int z = Convert.ToInt32(zTxt);

						using (VtpkReaderPure vtpkReader = new VtpkReaderPure(bundleFile, outputWriter)) {

							if (!vtpkReader.GetTiles(
								_tileIds.Where(
									ti => ti.z == z
									&& ti.x >= vtpkReader.BundleCol
									&& ti.x < vtpkReader.BundleCol + VtpkReaderPure.PACKET_SIZE
									&& ti.y >= vtpkReader.BundleRow
									&& ti.y < vtpkReader.BundleRow + VtpkReaderPure.PACKET_SIZE
								).ToList()
								, _cancel
								)
							) {
								return;
							}
							foreach (var failedTile in vtpkReader.FailedTiles) {
								failedTiles.Add(failedTile);
							}

							processedTiles = Interlocked.Add(ref processedTiles, vtpkReader.ProcessedTiles);
						}
					}
				);
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
			if (failedTiles.Count > 0) {
				Console.WriteLine("FAILED TILES:");
				foreach (TileId tid in failedTiles) {
					Console.WriteLine(tid);
				}
				Console.WriteLine("--------------------------");
			}
			Console.WriteLine($"tiles in tilemap : {_tileIds.Count}");
			Console.WriteLine($"processed tiles  : {processedTiles}");
			Console.WriteLine($"failed tiles     : {failedTiles.Count}");

			_done = true;
			return 0;
		}


		//TODO Fix not working LVL 0
		private static void traverseLevels(IEnumerable<dynamic> index, int zoomLevel, long parentRow, long parentCol) {

			zoomLevel++;

			List<dynamic> children = index.ToList();

			long col = parentCol * 2;
			long row = parentRow * 2;

			if (children[0] is JArray || 1 == ((JValue)children[0]).Value<int>()) {
				_tiles.Add($"{zoomLevel}/{col}/{row}");
				_tileIds.Add(new TileId { z = zoomLevel, x = col, y = row });
			}

			if (children[1] is JArray || 1 == ((JValue)children[1]).Value<int>()) {
				_tiles.Add($"{zoomLevel}/{col + 1}/{row}");
				_tileIds.Add(new TileId { z = zoomLevel, x = col + 1, y = row });
			}

			if (children[3] is JArray || 1 == ((JValue)children[3]).Value<int>()) {
				_tiles.Add($"{zoomLevel}/{col + 1}/{row + 1}");
				_tileIds.Add(new TileId { z = zoomLevel, x = col + 1, y = row + 1 });
			}

			if (children[2] is JArray || 1 == ((JValue)children[2]).Value<int>()) {
				_tiles.Add($"{zoomLevel}/{col}/{row + 1}");
				_tileIds.Add(new TileId { z = zoomLevel, x = col, y = row + 1 });
			}



			if (children[0] is JArray) { traverseLevels(children[0], zoomLevel, row, col); }
			if (children[1] is JArray) { traverseLevels(children[1], zoomLevel, row, col + 1); }
			if (children[2] is JArray) { traverseLevels(children[2], zoomLevel, row + 1, col); }
			if (children[3] is JArray) { traverseLevels(children[3], zoomLevel, row + 1, col + 1); }
		}


	}
}
