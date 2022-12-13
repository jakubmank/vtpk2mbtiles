
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace vtpk2mbtiles.Test
{
    [TestClass]
    public class TileMapReaderTest
    {
        [TestMethod]
        public void ReadTileMap()
        {

            List<TileId> tileIds = new List<TileId>();

            List<dynamic> L13_2 = new List<dynamic> { 0, 1, 0, 0 };
            List<dynamic> L13_1 = new List<dynamic> { 0, 0, 0, 1 };
            List<dynamic> L12 = new List<dynamic> { new JArray(L13_1), 0, new JArray(L13_2), 0 };
            List<dynamic> L11 = new List<dynamic> { new JArray(L12), 0, 0, 0 };
            List<dynamic> L10= new List<dynamic> { new JArray(L11), 0, 0, 0 };
            List<dynamic> L9 = new List<dynamic> { new JArray(L10), 0, 0, 0 };
            List<dynamic> L8 = new List<dynamic> { 0, 0, 0, new JArray(L9) };
            List<dynamic> L7 = new List<dynamic> { 0, new JArray(L8), 0, 0 };
            List<dynamic> L6 = new List<dynamic> { 0, 0, 0, new JArray(L7) };
            List<dynamic> L5 = new List<dynamic> { 0, 0, 0, new JArray(L6) };
            List<dynamic> L4 = new List<dynamic> { 0, 0, 0, new JArray(L5) };
            List<dynamic> L3 = new List<dynamic> { 0, 0, 0, new JArray(L4) };
            List<dynamic> L2 = new List<dynamic> { 0,new JArray(L3), 0, 0 };
            List<dynamic> L1 = new List<dynamic>{ 0, 0, new JArray(L2), 0 };
            List<dynamic> L0 = new List<dynamic> { new JArray(L1),0 , 0, 0 };
            TileMap.Read( tileIds, new JArray(L0), 0, 0, 0);
            Assert.AreEqual(16, tileIds.Count);
            Assert.AreEqual("1/0/0", tileIds[0].Z + "/" + tileIds[0].X + "/" + tileIds[0].Y);
            Assert.AreEqual("12/1016/1512", tileIds[11].Z + "/" + tileIds[11].X + "/" + tileIds[11].Y);
            Assert.AreEqual("13/2032/3024", tileIds[12].Z + "/" + tileIds[12].X + "/" + tileIds[12].Y);

        }


    }

}
