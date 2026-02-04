using System.Linq;

namespace Server.Custom.SpawnSystem
{
    internal static class SpawnSysTileInfo
    {
        internal static string GetTileName(int id)
        {
            if (InvalidTiles.Contains(id)) return string.Empty;

            if (GrassTiles.Contains(id)) return "grass";

            if (SandTiles.Contains(id)) return "sand";

            if (JungleTiles.Contains(id)) return "jungle";

            if (SnowTiles.Contains(id)) return "snow";

            if (DirtTiles.Contains(id)) return "dirt";

            if (SwampTiles.Contains(id)) return "swamp";

            if (TileTiles.Contains(id)) return "tile";

            if (MarbleTiles.Contains(id)) return "marble";

            if (WoodenFloorTiles.Contains(id)) return "wooden floor";

            if (SandStoneTiles.Contains(id)) return "sand stone";

            if (CloudTiles.Contains(id)) return "cloud";

            return string.Empty;
        }

        //Invalid
        private static readonly int[] InvalidTiles = new int[]
        {
            0x01B3, 0x01B4,
            0x01B5,
        };

        //Grass
        private static readonly int[] GrassTiles = new int[]
        {
            0x3EF0, 0x3FF6,
            0x3FFC, 0x3FFD,
            0x3FFE, 0x3FFF,
        };

        //Sand
        private static readonly int[] SandTiles = new int[]
        {
            0x0192, 0x3FF5, 0x3FF7,
        };

        //Jungle
        private static readonly int[] JungleTiles = new int[]
        {
            0x00B2, 0x00B4,
            0x00B5, 0x00B7,
            0x00B8, 0x00BA,
            0x00BB,
            0x073F, 0x0740,
            0x0741, 0x0742,

        };

        //Snow
        private static readonly int[] SnowTiles = new int[]
        {
            0x0119,
            0x3FEC, 0x3FED,
            0x3FEE, 0x3FEF,
        };

        //Dirt
        private static readonly int[] DirtTiles = new int[]
        {
            0x03F8,
            0x079A, 0x079B,
            0x079C, 0x079D,
            0x079E, 0x079F,
            0x07A0, 0x07A1,
            0x07A2, 0x07A3,
            0x07A4, 0x07A5,
            0x07A6, 0x07A7,
            0x07A8, 0x07A9,
            0x07AA, 0x07AB,
            0x07AC, 0x07AD,
            0x07AE, 0x07AF,
            0x07B0, 0x07B1,
            0x3FF4, 0x3FF8,
            0x3FF9, 0x3FFA,
            0x3FFB,
        };

        //Swamp
        private static readonly int[] SwampTiles = new int[]
        {
            0x09C4, 0x09C5,
            0x09C6, 0x09C7,
            0x09C8, 0x09C9,
            0x09CA, 0x09CB,
            0x09CC, 0x09CD,
            0x09CE, 0x09CF,
            0x09D0, 0x09D1,
            0x09D2, 0x09D3,
            0x09D4, 0x09D5,
            0x09D6, 0x09D7,
            0x09D8, 0x09D9,
            0x09DA, 0x09DB,
            0x09DC, 0x09DD,
            0x09DE, 0x09DF,
            0x09E0, 0x09E1,
            0x09E2, 0x09E3,
            0x09E4, 0x09E5,
            0x09E6, 0x09E7,
            0x09E8, 0x09E9,
            0x09EA, 0x09EB,

            0x3D65,
            //3DC
            0x3DC0, 0x3DC1,
            0x3DC2, 0x3DC3,
            0x3DC4, 0x3DC5,
            0x3DC6, 0x3DC7,
            0x3DC8, 0x3DC9,
            0x3DCA, 0x3DCB,
            0x3DCC, 0x3DCD,
            0x3DCE, 0x3DCF,
            //3DD
            0x3DD0, 0x3DD1,
            0x3DD2, 0x3DD3,
            0x3DD4, 0x3DD5,
            0x3DD6, 0x3DD7,
            0x3DD8, 0x3DD9,
            0x3DDA, 0x3DDB,
            0x3DDC, 0x3DDD,
            0x3DDE, 0x3DDF,
            //3DE
            0x3DE0, 0x3DE1,
            0x3DE2, 0x3DE3,
            0x3DE4, 0x3DE5,
            0x3DE6, 0x3DE7,
            0x3DE8, 0x3DE9,
            0x3DEA, 0x3DEB,
            0x3DEC, 0x3DED,
            0x3DEE, 0x3DEF,
            //3DF
            0x3DF0, 0x3DF1,
            0x3DF2, 0x3DF3,
            0x3DF4, 0x3DF5,
            0x3DF6, 0x3DF7,
            0x3DF8, 0x3DF9,
            0x3DFA, 0x3DFB,
            0x3DFC, 0x3DFD,
            0x3DFE, 0x3DFF,
            //E00
            0x3E00, 0x3E01,
            0x3E02, 0x3E03,
            0x3E04, 0x3E05,
            0x3E06, 0x3E07,
            0x3E08, 0x3E09,
            0x3E0A, 0x3E0B,
            0x3E0C, 0x3E0D,
            0x3E0E, 0x3E0F,
            //E10
            0x3E10, 0x3E11,
            0x3E12, 0x3E13,
            0x3E14, 0x3E15,
            0x3E16, 0x3E17,
            0x3E18, 0x3E19,
            0x3E1A, 0x3E1B,
            0x3E1C, 0x3E1D,
            0x3E1E, 0x3E1F,
            //E20
            0x3E20, 0x3E21,
            0x3E22, 0x3E23,
            0x3E24, 0x3E25,
            0x3E26, 0x3E27,
            0x3E28, 0x3E29,
            0x3E2A, 0x3E2B,
            0x3E2C, 0x3E2D,
            0x3E2E, 0x3E2F,
            //E30
            0x3E30, 0x3E31,
            0x3E32, 0x3E33,
            0x3E34, 0x3E35,
            0x3E36, 0x3E37,
            0x3E38, 0x3E39,
            0x3E3A, 0x3E3B,
            0x3E3C, 0x3E3D,
            0x3E3E, 0x3E3F,
            //E40
            0x3E40, 0x3E41,
            0x3E42, 0x3E43,
            0x3E44, 0x3E45,
            //E60
            0x3E66, 0x3E67,
            0x3E68, 0x3E69,
            0x3E6A, 0x3E6B,
            0x3E6C, 0x3E6D,
            0x3E6E, 0x3E6F,
            //E70
            0x3E70, 0x3E71,
        };

        //Tile
        private static readonly int[] TileTiles = new int[]
        {
            0x0200, 0x0205,
            0x0206, 0x0207,
            0x0208, 0x0209,
            0x020A, 0x020B,
            0x020C,
            0x0210, 0x0211,
            0x0212, 0x0213,
            0x0214, 0x0215,
            0x0216, 0x0217,
            0x0218,
        };

        //Marble
        private static readonly int[] MarbleTiles = new int[]
        {
            0x04E7, 0x04E8,
        };

        //Wooden Floor
        private static readonly int[] WoodenFloorTiles = new int[]
        {
            0x04D6, 0x04D7,
            0x04DD, 0x04DF,
            0x04E0, 0x3CDD,
        };

        //Sand Stone
        private static readonly int[] SandStoneTiles = new int[]
        {
            0x0501, 0x0502,
            0x0503, 0x0504,
            0x0505, 0x0506,
            0x0507, 0x0508,
            0x0509, 0x050A,
            0x050B, 0x050C,
            0x050D, 0x050E,
            0x050F, 0x0510,
            0x0511, 0x0512,
            0x0513, 0x0514,
            0x0515, 0x0516,
            0x0517, 0x0518,
            0x0519, 0x051A,
            0x051B, 0x051C,
        };

        //cloud
        private static readonly int[] CloudTiles = new int[]
        {
            0x3EED, 0x3EEE,
            0x3EEF, 0x3EF1,
        };

        //Water Sand
        internal static readonly int[] WaterSandTiles = new int[]
        {
            0x004C, 0x004D,
            0x004E, 0x004F,
            0x0050, 0x0051,
            0x0052, 0x0053,
            0x0054, 0x0055,
            0x0056, 0x0057,
            0x0058, 0x0059,
            0x005A, 0x005B,
            0x005C, 0x005D,
            0x005E, 0x005F,
            0x0060, 0x0061,
            0x0062, 0x0063,
            0x0064, 0x0065,
            0x0066,
            0x0068, 0x0069,
            0x006A, 0x006B,
            0x006C, 0x006D,
            0x006E, 0x006F,
            0x07BD, 0x07BE,
            0x07BF, 0x07C0,
        };

        //Invalid Names
        internal static readonly string[] InvalidTileNames = new string[]
        {
            "ED",
            "VOID!!!!!!!",
            "NODRAW",
            "NoName"
        };
    }
}
