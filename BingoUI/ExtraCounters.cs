using Microsoft.Xna.Framework;
using FMOD.Studio;

namespace Celeste.Mod.BingoUI {
    public static class ExtraCounters {
        public static void Load() {
            On.Celeste.Audio.Play_string_Vector2 += BinoHud;
            On.Celeste.Seeker.RegenerateBegin += RegenerateBegin;
            On.Celeste.AngryOshiro.HurtBegin += HurtBegin;
            On.Celeste.Snowball.OnPlayerBounce += OnPlayerBounce2;
            On.Celeste.Key.OnPlayer += TrackKeys;
        }

        public static void Unload() {
            On.Celeste.Audio.Play_string_Vector2 -= BinoHud;
            On.Celeste.Seeker.RegenerateBegin -= RegenerateBegin;
            On.Celeste.AngryOshiro.HurtBegin -= HurtBegin;
            On.Celeste.Snowball.OnPlayerBounce -= OnPlayerBounce2;
            On.Celeste.Key.OnPlayer -= TrackKeys;
        }

        private static EventInstance BinoHud(On.Celeste.Audio.orig_Play_string_Vector2 orig, string path, Vector2 position)
        {
            var binos = BingoModule.SaveData.BinocularsList;
            var area = BingoModule.CurrentLevel.Session.Area;

            if (path == "event:/game/general/lookout_use")
            {
                bool matchFound = false;
                foreach (Binoculars b in binos) {
                    if (b.areaID == area.ID && b.areaMode == (int)area.Mode && b.pos.Equals(position)) {
                        matchFound = true;
                        break;
                    }
                }
                if (!matchFound) {
                    binos.Add(new Binoculars(area, position));
                }
            }
            return orig(path, position);
        }

        private static void RegenerateBegin(On.Celeste.Seeker.orig_RegenerateBegin orig, Seeker seeker)
        {

            BingoModule.SaveData.SeekersHit++;
            orig(seeker);
        }

        private static void HurtBegin(On.Celeste.AngryOshiro.orig_HurtBegin orig, AngryOshiro oshiro)
        {
            BingoModule.SaveData.OshiroHits++;
            orig(oshiro);
        }

        private static void OnPlayerBounce2(On.Celeste.Snowball.orig_OnPlayerBounce orig, Snowball snowball, Player player)
        {
            BingoModule.SaveData.SnowballHits++;
            orig(snowball, player);
        }

        private static void TrackKeys(On.Celeste.Key.orig_OnPlayer orig, Key self, Player player) {
            orig(self, player);

            var area = BingoModule.CurrentLevel.Session.Area;
            var keys = BingoModule.SaveData.KeysList;

            bool matchFound = false;
            foreach (Keys k in keys) {
                if (k.areaID == area.ID && k.areaMode == (int)area.Mode && k.entity.Key == self.ID.Key) {
                    matchFound = true;
                    break;
                }
            }
            if (!matchFound) {
                keys.Add(new Keys(area, self.ID));
            }
        }
    }
}
