using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.BingoUI {
    public class OuiJournalBinoculars : OuiJournalPage {
        private OuiJournalPage.Table table;
        private List<Binoculars> binocularList;

        public OuiJournalBinoculars(OuiJournal journal, List<Binoculars> binocs) : base(journal) {
            this.PageTexture = "page";
            binocularList = binocs;
            this.table = new OuiJournalPage.Table().AddColumn(new OuiJournalPage.TextCell("RECORDS:\nBINOCULARS", new Vector2(1f, 0.5f), 0.7f, this.TextColor, 300f, false));
            for (int i = 0; i < SaveData.Instance.UnlockedModes; i++) {
                this.table.AddColumn(new OuiJournalPage.TextCell(Dialog.Clean("journal_mode_" + (AreaMode)i, null), this.TextJustify, 0.6f, this.TextColor, 240f, false));
            }
            bool[] array = new bool[]
            {
                true,
                SaveData.Instance.UnlockedModes >= 2,
                SaveData.Instance.UnlockedModes >= 3
            };
            int[] array2 = new int[3];
            foreach (AreaStats areaStats in SaveData.Instance.Areas_Safe) {
                AreaData areaData = AreaData.Get(areaStats.ID_Safe);
                bool interlude_Safe = areaData.Interlude_Safe;
                if (!interlude_Safe) {
                    bool flag = areaData.ID > SaveData.Instance.UnlockedAreas_Safe;
                    if (flag) {
                        array[0] = (array[1] = (array[2] = false));
                        break;
                    }
                    OuiJournalPage.Row row = this.table.AddRow();
                    row.Add(new OuiJournalPage.TextCell(Dialog.Clean(areaData.Name, null), new Vector2(1f, 0.5f), 0.6f, this.TextColor, 0f, false));
                    for (int j = 0; j < SaveData.Instance.UnlockedModes; j++) {
                        int num = CountBinoculars(areaStats.ID_Safe, j);
                        bool flag2 = num > 0;
                        if (flag2) {
                            foreach (EntityData entityData in AreaData.Areas[areaStats.ID_Safe].Mode[j].MapData.Goldenberries) {
                                EntityID item = new EntityID(entityData.Level.Name, entityData.ID);
                                bool flag3 = areaStats.Modes[j].Strawberries.Contains(item);
                                if (flag3) {
                                    num = 0;
                                }
                            }

                            row.Add(new OuiJournalPage.TextCell(Dialog.Deaths(num), this.TextJustify, 0.5f, this.TextColor, 0f, false));
                            array2[j] += num;



                        } else {
                            row.Add(new OuiJournalPage.IconCell("dot", 0f));
                            array[j] = false;
                        }
                    }

                }
            }

            table.AddRow();
            OuiJournalPage.Row totalsRow = table.AddRow().Add(new OuiJournalPage.TextCell("TOTALS", new Vector2(1f, 0.5f), 0.7f, this.TextColor, 0f, false));
            for (int j = 0; j < SaveData.Instance.UnlockedModes; j++)
                totalsRow.Add(new OuiJournalPage.TextCell(array2[j].ToString(), this.TextJustify, 0.6f, this.TextColor, 0f, false));

            bool flag4 = array[0] || array[1] || array[2];
            if (flag4) {
                this.table.AddRow();
                OuiJournalPage.Row row2 = this.table.AddRow();
                row2.Add(new OuiJournalPage.TextCell(Dialog.Clean("journal_totals", null), new Vector2(1f, 0.5f), 0.7f, this.TextColor, 0f, false));
                for (int k = 0; k < SaveData.Instance.UnlockedModes; k++) {
                    row2.Add(new OuiJournalPage.TextCell(Dialog.Deaths(array2[k]), this.TextJustify, 0.6f, this.TextColor, 0f, false));
                }
                bool flag5 = array[0] && array[1] && array[2];
                if (flag5) {
                    OuiJournalPage.TextCell textCell = new OuiJournalPage.TextCell(Dialog.Deaths(array2[0] + array2[1] + array2[2]), this.TextJustify, 0.6f, this.TextColor, 0f, false);
                    textCell.SpreadOverColumns = 3;
                    this.table.AddRow().Add(new OuiJournalPage.TextCell(Dialog.Clean("journal_grandtotal", null), new Vector2(1f, 0.5f), 0.7f, this.TextColor, 0f, false)).Add(textCell);
                }
            }
        }

        private int CountBinoculars(int stage, int mode) {
            int binocCount = 0;
            foreach (Binoculars b in binocularList)
                if (b.areaID == stage && b.areaMode == mode)
                    binocCount++;
            return binocCount;
        }

        public override void Redraw(VirtualRenderTarget buffer) {
            base.Redraw(buffer);
            Draw.SpriteBatch.Begin();
            this.table.Render(new Vector2(60f, 20f));
            RenderStamps();
            Draw.SpriteBatch.End();
        }

        internal void RenderStamps() {
            bool assistMode = SaveData.Instance.AssistMode;
            if (assistMode) {
                //GFX.Gui["fileselect/assist"].DrawCentered(new Vector2(1250f, 810f), Color.White * 0.5f, 1f, 0.2f);
            }
            bool cheatMode = SaveData.Instance.CheatMode;
            if (cheatMode) {
                //GFX.Gui["fileselect/cheatmode"].DrawCentered(new Vector2(1400f, 860f), Color.White * 0.5f, 1f, 0f);
            }
        }

        public static void Load() {
            Everest.Events.Journal.OnEnter += BinocularJournal;
        }

        public static void Unload() {
            Everest.Events.Journal.OnEnter += BinocularJournal;
        }

        public static void BinocularJournal(OuiJournal journal, Oui from) {
            if (!BingoModule.Settings.Enabled)
                return;
            int newIndex = 2;
            foreach (OuiJournalPage page in journal.Pages) {
                if (page.PageIndex >= newIndex)
                    page.PageIndex++;
            }
            OuiJournalBinoculars newPage = new OuiJournalBinoculars(journal, BingoModule.SaveData.BinocularsList);
            newPage.PageIndex = newIndex;
            journal.Pages.Insert(newIndex, newPage);
        }
    }
}
