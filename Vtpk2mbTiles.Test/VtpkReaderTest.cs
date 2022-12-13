using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using vtpk2mbtiles;

namespace Vtpk2mbTiles.Test
{
    [TestClass]
    public class VtpkReaderTest
    {
        [TestMethod]
        [DeploymentItem(@"arcGISMapBundle\R0b80C0780.bundle")] // Level 13
        public void TestMethod1()
        {
            VtpkReader vtpkReader = new VtpkReader();

            TileId tileId = new TileId(13, 2033, 3025); //bundlerow 2944 bundleCol 1920
            TileId tileId_2 = new TileId(13, 2033, 3026);
            
            int bundleRow = 2944;
            int bundleCol = 1920;
            
            BinaryReader bundleReader = new BinaryReader(File.OpenRead("R0b80C0780.bundle"));
            
            // Call the GetTileOffsetAndSize method
            (long offset, long size) = vtpkReader.GetTileOffsetAndSize(tileId,bundleRow, bundleCol, bundleReader);

            Assert.AreEqual(131140, offset);
            Assert.AreEqual(22752, size);

            (offset, size) = vtpkReader.GetTileOffsetAndSize(tileId_2, bundleRow, bundleCol, bundleReader);
            Assert.AreEqual(153896, offset);
            Assert.AreEqual(6143, size);

        }

    }
}
