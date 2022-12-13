using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace vtpk2mbtiles
{
	public class VtpkFile
	{	
		private string vtpkDir;
		private string destination;
		
		public VtpkFile(string _vtpkDir, string _destination)
        {
            vtpkDir = _vtpkDir;
            destination = _destination;
        }

        public string TilesDirPath 
		{	
			get { return Path.Combine(vtpkDir, "p12", "tile"); }
		}

        public string TileMapPath
        {
			get { return Path.Combine(vtpkDir, "p12", "tilemap", "root.json"); }
        }

		public string[] LevelDirs
		{
			get { return  Directory.GetDirectories(TilesDirPath, "L*"); }
        }

        public List<string> Validate()
		{
			List<string> errorsList = new List<string>();
			
			if (destination.ToLower().EndsWith(".mbtiles"))
			{
				if (File.Exists(destination))
				{
                    errorsList.Add($"destination mbtiles file already exists: [{ destination}]");
				}
			}
			else
			{
				if (Directory.Exists(destination))
				{
                    errorsList.Add($"destination directory already exists: [{destination}]");
				}
			}
			
			if (!Directory.Exists(vtpkDir))
			{
                errorsList.Add("vtpk directory does not exist");
				return errorsList;
			}

			if (!Directory.Exists(TilesDirPath))
			{	
				errorsList.Add($"tile directory not found: {TilesDirPath}");
				return errorsList;
			}

			if (null == LevelDirs || LevelDirs.Length < 1)
			{
				errorsList.Add($"tile directory is empty");
			}

			if (!File.Exists(TileMapPath))
			{
				errorsList.Add($"tile map not found: {TileMapPath}");
			}

			string rootRootJson = Path.Combine(vtpkDir, "p12", "root.json");
			if (!File.Exists(rootRootJson))
			{
				errorsList.Add($"main root.json not found: {rootRootJson}");
			}

			string infoJson = Path.Combine(vtpkDir, "p12", "resources", "info", "root.json");
			if (!File.Exists(infoJson))
			{
				errorsList.Add($"resources info root.json not found: {infoJson}");
			}

			string styleJson = Path.Combine(vtpkDir, "p12", "resources", "styles", "root.json");
			if (!File.Exists(styleJson))
			{
				errorsList.Add($"resources styles root.json not found: {styleJson}");
			}


			string itemInfoXml = Path.Combine(vtpkDir, "esriinfo", "iteminfo.xml");
			if (!File.Exists(itemInfoXml))
			{
				errorsList.Add($"iteminfo not found: { itemInfoXml}");
			}

            return errorsList.Count > 0 ? errorsList : null;
        }

		public HashSet<string> GetBundleFiles()
        {
            HashSet<string> bundleFiles = new HashSet<string>();
            foreach (string levelDir in LevelDirs)
			{
				string zTxt = Path.GetFileName(levelDir).Substring(1, 2);
				int z = Convert.ToInt32(zTxt);


				bundleFiles.UnionWith(Directory.GetFiles(levelDir, "*.bundle").OrderBy(b => b).ToArray());
			}
			return bundleFiles;
		}
	}
}
