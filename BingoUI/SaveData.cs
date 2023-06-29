using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.BingoUI {
    public class BingoSaveData : EverestModuleSaveData {
        public int SeekersHit = 0;
        public int OshiroHits = 0;
        public int SnowballHits = 0;
        public int SkipUsed = -1;
        public List<Binoculars> BinocularsList = new List<Binoculars>();
    }

    public class Binoculars
    {
        public int areaID;
        public int areaMode;
        public Vector2 pos;

        public Binoculars(AreaKey lv, Vector2 myPos)
        {
            areaID = lv.ID;
            areaMode = (int)lv.Mode;
            pos = myPos;
        }
    }
}
