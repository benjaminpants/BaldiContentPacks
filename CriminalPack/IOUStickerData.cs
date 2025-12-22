using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CriminalPack
{
    public class IOUStickerData : ExtendedStickerData
    {
        public override string GetLocalizedInventoryStickerDescription(StickerStateData data)
        {
            StickerStateData disguise = ((IOUStickerState)data).disguisingAs;
            if (disguise != null)
            {
                return StickerMetaStorage.Instance.Get(disguise.sticker).value.GetLocalizedInventoryStickerDescription(disguise);
            }
            return base.GetLocalizedInventoryStickerDescription(data);
        }

        public override Sprite GetInventorySprite(StickerStateData data)
        {
            StickerStateData disguise = ((IOUStickerState)data).disguisingAs;
            if (disguise != null)
            {
                return StickerMetaStorage.Instance.Get(disguise.sticker).value.GetInventorySprite(disguise);
            }
            return base.GetInventorySprite(data);
        }

        public override StickerStateData CreateStateData(int activeLevel, bool opened, bool sticky)
        {
            IOUStickerState state = new IOUStickerState(sticker, activeLevel, opened, sticky);
            List<WeightedSticker> potentialStickers = new List<WeightedSticker>();
            // this shouldn't ever be possible... but just incase.
            if (Singleton<BaseGameManager>.Instance == null)
            {
                potentialStickers.AddRange(StickerMetaStorage.Instance.All().Select(x => new WeightedSticker(x.type, Mathf.Max(Mathf.CeilToInt(1000 / Mathf.Max(x.value.duplicateOddsMultiplier,0.2f)), 1))));
            }
            else
            {
                if (Singleton<BaseGameManager>.Instance.InPitstop())
                {
                    potentialStickers.AddRange(Singleton<CoreGameManager>.Instance.nextLevel.potentialStickers);
                }
                else
                {
                    potentialStickers.AddRange(Singleton<CoreGameManager>.Instance.sceneObject.potentialStickers);
                }
            }
            potentialStickers.RemoveAll(x => x.selection == sticker);
            potentialStickers.RemoveAll(x => x.selection.GetMeta().tags.Contains("crmp_iou_sticker_nodisguise"));
            potentialStickers.RemoveAll(x => x.selection.GetMeta().tags.Contains("tms_daredevil"));
            potentialStickers.Shuffle();
            potentialStickers.Sort((a, b) => a.weight.CompareTo(b.weight));
            while (potentialStickers.Count > 3)
            {
                potentialStickers.RemoveAt(potentialStickers.Count - 1);
            }
            if (potentialStickers.Count == 0)
            {
                CriminalPackPlugin.Log.LogWarning("IOU sticker failed to disguise?!");
                return state; // oh well. disguise failed.
            }
            Sticker chosenSticker = potentialStickers.RandomSelection();
            state.disguisingAs = chosenSticker.GetMeta().value.CreateStateData(state.activeLevel, state.opened, state.sticky);
            return state;
        }

        public override StickerStateData CreateOrGetAppliedStateData(StickerStateData inventoryState)
        {
            inventoryState.opened = true;
            ((IOUStickerState)inventoryState).disguisingAs = null;
            return inventoryState;
        }

        public override BooleanHandshake CanStackWith(StickerStateData thisSticker, StickerStateData otherSticker)
        {
            IOUStickerState iouSticker = (IOUStickerState)thisSticker;
            if (iouSticker.disguisingAs == null)
            {
                return base.CanStackWith(thisSticker, otherSticker);
            }
            // CanStackWith is the method the API natively uses for checking stacks. We use it with our disguising state and attempt to force the return value to be the one we want.
            bool canStackAsDisguise = iouSticker.disguisingAs.CanStackWith(otherSticker);
            return canStackAsDisguise ? BooleanHandshake.AlwaysTrue : BooleanHandshake.AlwaysFalse;
        }
    }

    public class IOUStickerState : ExtendedStickerStateData
    {
        public StickerStateData disguisingAs = null;
        public IOUStickerState(Sticker sticker, int activeLevel, bool opened, bool sticky) : base(sticker, activeLevel, opened, sticky)
        {
        }

        const byte version = 0;
        public override void VirtualWrite(BinaryWriter writer)
        {
            base.VirtualWrite(writer);
            writer.Write(version);
            writer.Write(disguisingAs != null);
            if (disguisingAs != null)
            {
                writer.Write(disguisingAs.sticker.ToStringExtended());
                disguisingAs.Write(writer);
            }
        }

        public override void VirtualReadInto(BinaryReader reader)
        {
            base.VirtualReadInto(reader);
            byte version = reader.ReadByte();
            disguisingAs = null;
            if (reader.ReadBoolean())
            {
                Sticker stickerType = EnumExtensions.GetFromExtendedName<Sticker>(reader.ReadString());
                StickerStateData data = StickerMetaStorage.Instance.Get(stickerType).value.CreateStateData(activeLevel, opened, sticky);
                data.ReadInto(reader);
            }
        }
    }
}
