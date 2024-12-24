using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CriminalPack
{
    [BepInPlugin("mtm101.rulerp.baldiplus.criminalpackroot", "Criminal Pack Root Mod", "1.2.0.0")]
    public class CriminalPackPlugin : BaseUnityPlugin
    {
        public static CriminalPackPlugin Instance;

        public static Character dealerEnum;

        public AssetManager assetMan = new AssetManager();

        public ConfigEntry<bool> youtuberModeEnabled;

        IEnumerator ResourcesLoaded()
        {
            yield return 6;
            yield return "Loading Sprites...";
            assetMan.Add<Texture2D>("LightMap", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.name == "LightMap"));
            assetMan.Add<Sprite>("CrowbarSmall", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "CrowbarSmall.png"), 25f));
            assetMan.Add<Sprite>("CrowbarBig", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "CrowbarBig.png"), 50f));
            assetMan.Add<Sprite>("ThiefMaskSmall", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "ThiefMaskSmall.png"), 25f));
            assetMan.Add<Sprite>("ThiefMaskBig", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "ThiefMaskBig.png"), 50f));
            assetMan.Add<Sprite>("ThiefMaskOverlay", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "ThiefMaskOverlay.png"), 1f));
            assetMan.Add<Sprite>("PickaxeSmall", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "PickaxeSmall.png"), 25f));
            assetMan.Add<Sprite>("PickaxeBig", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "PickaxeBig.png"), 50f));
            assetMan.Add<Sprite>("PouchSmall", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "PouchSmall.png"), 25f));
            assetMan.Add<Sprite>("PouchBig", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "PouchBig.png"), 50f));
            assetMan.Add<Sprite>("IOUSmall", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "IOU_Small.png"), 25f));
            assetMan.Add<Sprite>("IOUCrumpled", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "IOU_Crumpled.png"), 50f));
            assetMan.Add<Sprite>("IOUBig", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "IOU_Big.png"), 50f));
            assetMan.Add<Sprite>("IOUBOOM", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "IOU_BOOM.png"), 50f));
            assetMan.Add<Sprite>("Icon_Dealer", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Icon_Dealer.png"), 16f));
            assetMan.Add<Texture2D>("DigMask", AssetLoader.TextureFromMod(this, "DigMask.png"));
            assetMan.Add<Texture2D>("DigTex", AssetLoader.TextureFromMod(this, "DigTexture.png"));
            assetMan.Add<Texture2D>("Dirt", AssetLoader.TextureFromMod(this, "Dirt.png"));
            assetMan.Add<Texture2D>("IOU_Wall", AssetLoader.TextureFromMod(this, "IOU_Wall.png"));
            assetMan.Add<Texture2D>("IOU_WallFade", AssetLoader.TextureFromMod(this, "IOU_WallFade.png"));
            assetMan.Add<Texture2D>("dealer_poster", AssetLoader.TextureFromMod(this, "dealer_poster.png"));

            Texture2D[] textures = AssetLoader.TexturesFromMod(this, "*.png", "Dealer");
            assetMan.AddRange(textures.ToSprites(16f), (spr) =>
            {
                return spr.texture.name;
            });

            yield return "Loading Audio...";

            assetMan.Add<SoundObject>("CrowbarHit", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CrowbarHit.wav"), "Sfx_Crowbar", SoundType.Effect, Color.white));
            assetMan.Add<SoundObject>("PryDoor", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PryDoor.wav"), "Sfx_Elv_Gate", SoundType.Effect, Color.white));
            assetMan.Add<SoundObject>("DecoyInflate", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "InflateDecoy.wav"), "Sfx_Inflate", SoundType.Effect, Color.white));
            assetMan.Add<SoundObject>("DecoyBoom", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "DecoyPop.wav"), "Sfx_Effects_Pop", SoundType.Effect, Color.white));
            assetMan.Add<SoundObject>("DealerScreech", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "ScreechToHalt.wav"), "Sfx_Screech", SoundType.Effect, Color.white));

            assetMan.Add<SoundObject>("PaperCrumple", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PaperCrumple.wav"), "Sfx_PaperCrumple", SoundType.Effect, Color.white));
            assetMan.Add<SoundObject>("PaperSlap", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PaperSlap.wav"), "Sfx_Slap", SoundType.Effect, Color.white));

            assetMan.Add<SoundObject>("BounceSound1", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PouchBounce1.wav"), "Sfx_PBounce", SoundType.Effect, Color.white));
            assetMan.Add<SoundObject>("BounceSound2", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PouchBounce2.wav"), "Sfx_PBounce", SoundType.Effect, Color.white));
            assetMan.Add<SoundObject>("BounceSound3", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PouchBounce3.wav"), "Sfx_PBounceBig", SoundType.Effect, Color.white));
            assetMan.Add<SoundObject>("PouchOpen", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PouchOpen.wav"), "Sfx_POpen", SoundType.Effect, Color.white));

            assetMan.Add<SoundObject>("DealerHeyYou1", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_HeyYou1.wav"), "Dealer_HeyYou1", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerHeyYou2", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_HeyYou2.wav"), "Dealer_HeyYou2", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerHeyYou3", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_HeyYou3.wav"), "Dealer_HeyYou3", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerHeyYou4", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_HeyYou4.wav"), "Dealer_HeyYou4", SoundType.Voice, dealerColor));

            assetMan.Add<SoundObject>("DealerInteract1", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_Interact1.wav"), "Dealer_Interact1", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerDeliverTo", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_DeliverTo.wav"), "Dealer_DeliverTo", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerThrowAt", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_ThrowAt.wav"), "Dealer_ThrowAt", SoundType.Voice, dealerColor));

            assetMan.Add<SoundObject>("DealerInquire", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_Inquire.wav"), "Dealer_Inquire", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerInquireGood", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_InquireGood.wav"), "Dealer_InquireGood", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerInquireBad", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_InquireBad.wav"), "Dealer_InquireBad", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerInquireLost", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_InquireLost.wav"), "Dealer_InquireLost", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerInquireWarning", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_InquireWarning.wav"), "Dealer_InquireWarning", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerUhOh", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_UhOh.wav"), "Dealer_UhOh", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerUhOh2", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_UhOh2.wav"), "Dealer_UhOh2", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerTaxed", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_Taxed.wav"), "Dealer_Taxed", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerTaxedWithItem", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_TaxedWithItem.wav"), "Dealer_TaxedWithItem", SoundType.Voice, dealerColor));
            assetMan.Add<SoundObject>("DealerInterrupted", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_Interrupted.wav"), "Dealer_Interrupted", SoundType.Voice, dealerColor));

            assetMan.Add<SoundObject>("BaldiThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "Baldi.wav"), "CharacterHappy_Baldi", SoundType.Voice, Color.green));

            // this will be WRONG!!!
            assetMan.Add<SoundObject>("PrincipalThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "Principal.wav"), "CharacterHappy_Principal", SoundType.Voice, Color.white));
            assetMan.Add<SoundObject>("BeansThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "Beans.wav"), "CharacterHappy_Beans", SoundType.Voice, Color.white));
            assetMan.Add<SoundObject>("PlaytimeThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "Playtime.wav"), "CharacterHappy_Playtime", SoundType.Voice, Color.red));
            assetMan.Add<SoundObject>("FirstPrizeThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "FirstPrize.wav"), "CharacterHappy_FirstPrize", SoundType.Voice, Color.white));
            assetMan.Add<SoundObject>("DrReflexThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "DrReflex.wav"), "CharacterHappy_DrReflex", SoundType.Voice, Color.white));
            assetMan.Add<SoundObject>("TestThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "TheTest.wav"), "CharacterHappy_TheTest", SoundType.Effect, Color.white));

            assetMan.Add<SoundObject>("PrincipalFraud", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PRI_NoFraud.wav"), "Vfx_PRI_NoFraud", SoundType.Voice, Color.white));

            // load up the DeliveryMessages
            string[] paths = Directory.GetFiles(Path.Combine(AssetLoader.GetModPath(this), "Dealer", "DeliveryMessages"));
            foreach (string path in paths)
            {
                assetMan.Add<SoundObject>(Path.GetFileNameWithoutExtension(path), ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(path), Path.GetFileNameWithoutExtension(path), SoundType.Voice, dealerColor));
            }

            yield return "Adding dealer possible NPCs...";

            Dealer.characterChoices.Add(new WeightedCharacterChoice()
            {
                selection = new CharacterChoice(assetMan.Get<SoundObject>("Dealer_Chr_Baldi"), Character.Baldi, (NPC npc) =>
                {
                    ((Baldi)npc).AudMan.FlushQueue(true);
                    ((Baldi)npc).AudMan.PlaySingle(assetMan.Get<SoundObject>("BaldiThanks"));
                    return new Baldi_Praise(npc, (Baldi)npc, npc.behaviorStateMachine.currentState, 3f);
                }),
                weight = 25
            });
            Dealer.characterChoices.Add(new WeightedCharacterChoice()
            {
                selection = new CharacterChoice(assetMan.Get<SoundObject>("Dealer_Chr_Principal"), Character.Principal, (NPC npc) =>
                {
                    AudioManager audMan = (AudioManager)(((Principal)npc).ReflectionGetVariable("audMan"));
                    audMan.FlushQueue(true);
                    audMan.PlaySingle(assetMan.Get<SoundObject>("PrincipalThanks"));
                    return new NPCGiftState(npc, npc.behaviorStateMachine.currentState, 3f);
                }),
                weight = 75
            });
            Dealer.characterChoices.Add(new WeightedCharacterChoice()
            {
                selection = new CharacterChoice(assetMan.Get<SoundObject>("Dealer_Chr_Beans"), Character.Beans, (NPC npc) =>
                {
                    AudioManager audMan = (AudioManager)(((Beans)npc).ReflectionGetVariable("audMan"));
                    audMan.FlushQueue(true);
                    audMan.PlaySingle(assetMan.Get<SoundObject>("BeansThanks"));
                    return new NPCGiftState(npc, npc.behaviorStateMachine.currentState, 3f);
                }),
                weight = 90
            });
            Dealer.characterChoices.Add(new WeightedCharacterChoice()
            {
                selection = new CharacterChoice(assetMan.Get<SoundObject>("Dealer_Chr_Playtime"), Character.Playtime, (NPC npc) =>
                {
                    AudioManager audMan = (AudioManager)(((Playtime)npc).ReflectionGetVariable("audMan"));
                    audMan.FlushQueue(true);
                    audMan.PlaySingle(assetMan.Get<SoundObject>("PlaytimeThanks"));
                    return new NPCGiftState(npc, npc.behaviorStateMachine.currentState, 3f);
                }),
                weight = 90
            });
            Dealer.characterChoices.Add(new WeightedCharacterChoice()
            {
                selection = new CharacterChoice(assetMan.Get<SoundObject>("Dealer_Chr_FirstPrize"), Character.Prize, (NPC npc) =>
                {
                    AudioManager audMan = (AudioManager)(((FirstPrize)npc).ReflectionGetVariable("audMan"));
                    audMan.FlushQueue(true);
                    audMan.PlaySingle(assetMan.Get<SoundObject>("FirstPrizeThanks"));
                    return new NPCGiftState(npc, npc.behaviorStateMachine.currentState, 3f);
                }),
                weight = 90
            });
            /*Dealer.characterChoices.Add(new WeightedCharacterChoice()
            {
                selection = new CharacterChoice(assetMan.Get<SoundObject>("Dealer_Chr_DrReflex"), Character.DrReflex, (NPC npc) =>
                {
                    AudioManager audMan = (AudioManager)(((DrReflex)npc).ReflectionGetVariable("audioManager"));
                    audMan.FlushQueue(true);
                    audMan.PlaySingle(assetMan.Get<SoundObject>("DrReflexThanks"));
                    return new NPCGiftState(npc, npc.behaviorStateMachine.currentState, 3f);
                }),
                weight = 96
            });*/
            Dealer.characterChoices.Add(new WeightedCharacterChoice()
            {
                selection = new CharacterChoice(assetMan.Get<SoundObject>("Dealer_Chr_Sweep"), Character.Sweep, (NPC npc) =>
                {
                    AudioManager audMan = (AudioManager)(((GottaSweep)npc).ReflectionGetVariable("audMan"));
                    audMan.FlushQueue(true);
                    audMan.PlaySingle((SoundObject)(((GottaSweep)npc).ReflectionGetVariable("audSweep")));
                    return new NPCGiftState(npc, npc.behaviorStateMachine.currentState, 5f);
                }),
                weight = 70
            });
            Dealer.characterChoices.Add(new WeightedCharacterChoice()
            {
                selection = new CharacterChoice(assetMan.Get<SoundObject>("Dealer_Chr_MrsPomp"), Character.Pomp, (NPC npc) =>
                {
                    AudioManager audMan = (AudioManager)(((NoLateTeacher)npc).ReflectionGetVariable("audMan"));
                    audMan.FlushQueue(true);
                    audMan.PlaySingle((SoundObject)(((GottaSweep)npc).ReflectionGetVariable("audInTime")));
                    return new NPCGiftState(npc, npc.behaviorStateMachine.currentState, 3f);
                }),
                weight = 95
            });
            /*Dealer.characterChoices.Add(new WeightedCharacterChoice()
            {
                selection = new CharacterChoice(assetMan.Get<SoundObject>("Dealer_Chr_TheTest"), Character.LookAt, (NPC npc) =>
                {
                    AudioManager audMan = (AudioManager)(((LookAtGuy)npc).ReflectionGetVariable("audMan"));
                    audMan.PlaySingle(assetMan.Get<SoundObject>("TestThanks"));
                    return new NPCGiftState(npc, npc.behaviorStateMachine.currentState, 3f);
                }),
                weight = 15
            });*/

            yield return "Creating pouch content...";
            assetMan.AddFromResources<RoomController>(); //Room Controller
            assetMan.AddFromResources<Window>();
            WindowObject digWindow = ObjectCreators.CreateWindowObject("PickaxeDigWindow", assetMan.Get<Texture2D>("DigTex"), assetMan.Get<Texture2D>("DigTex"), assetMan.Get<Texture2D>("DigMask"));
            digWindow.windowPre = assetMan.Get<Window>("PortalPosterPre");
            ItemMetaData pouchMeta = new ItemMetaData(Info, new ItemObject[0]);
            pouchMeta.flags = ItemFlags.CreatesEntity | ItemFlags.Persists;
            Items pouchEnum = EnumExtensions.ExtendEnum<Items>("DealerPouch");


            Entity pouchEntity = new EntityBuilder()
                .SetName("ITM_DealerPouch")
                .AddDefaultRenderBaseFunction(assetMan.Get<Sprite>("PouchBig"))
                .SetLayerCollisionMask(new LayerMask() { value = 2113541 }) //todo: wtf is this
                .AddTrigger(2f)
                .Build();

            ITM_DealerBag dealerBag = pouchEntity.gameObject.AddComponent<ITM_DealerBag>();
            dealerBag.entity = pouchEntity;
            PropagatedAudioManager aum = dealerBag.gameObject.AddComponent<PropagatedAudioManager>();
            aum.ReflectionSetVariable("maxDistance", 50f);
            dealerBag.audMan = aum;
            dealerBag.bounceSounds = new SoundObject[]
            {
                assetMan.Get<SoundObject>("BounceSound1"),
                assetMan.Get<SoundObject>("BounceSound2"),
                assetMan.Get<SoundObject>("BounceSound3"),
            };
            dealerBag.openSound = assetMan.Get<SoundObject>("PouchOpen");




            ItemObject pouchObject = new ItemBuilder(Info)
                .SetEnum(pouchEnum)
                .SetNameAndDescription("Itm_DealerPouch", "If you are seeing this you are a hacker or something has gone horribly wrong.")
                .SetShopPrice(25)
                .SetGeneratorCost(int.MaxValue)
                .SetItemComponent(dealerBag)
                .SetSprites(assetMan.Get<Sprite>("PouchSmall"), assetMan.Get<Sprite>("PouchBig"))
                .SetMeta(pouchMeta)
                .Build();

            assetMan.Add<ItemObject>("Pouch", pouchObject);

            Entity emptyPouchEntity = new EntityBuilder()
                .SetName("ITM_DealerPouch_Empty")
                .AddDefaultRenderBaseFunction(assetMan.Get<Sprite>("PouchBig"))
                .SetLayerCollisionMask(new LayerMask() { value = 2113541 }) //todo: wtf is this
                .AddTrigger(2f)
                .Build();

            ITM_DealerBag emptyDealerBag = emptyPouchEntity.gameObject.AddComponent<ITM_BagEmpty>();
            emptyDealerBag.entity = emptyPouchEntity;
            PropagatedAudioManager emptyAum = emptyDealerBag.gameObject.AddComponent<PropagatedAudioManager>();
            emptyAum.ReflectionSetVariable("maxDistance", 50f);
            emptyDealerBag.audMan = emptyAum;
            emptyDealerBag.bounceSounds = new SoundObject[]
            {
                assetMan.Get<SoundObject>("BounceSound1"),
                assetMan.Get<SoundObject>("BounceSound2"),
                assetMan.Get<SoundObject>("BounceSound3"),
            };
            emptyDealerBag.openSound = assetMan.Get<SoundObject>("PouchOpen");




            ItemObject emptyPouchObject = new ItemBuilder(Info)
                .SetEnum(pouchEnum)
                .SetNameAndDescription("Itm_EmptyDealerPouch", "Desc_EmptyDealerPouch")
                .SetShopPrice(1)
                .SetGeneratorCost(int.MaxValue)
                .SetItemComponent(emptyDealerBag)
                .SetSprites(assetMan.Get<Sprite>("PouchSmall"), assetMan.Get<Sprite>("PouchBig"))
                .SetMeta(pouchMeta)
                .Build();

            assetMan.Add<ItemObject>("PouchEmpty", emptyPouchObject);


            yield return "Creating Items...";
            // crowbar
            Items crowbarEnum = EnumExtensions.ExtendEnum<Items>("Crowbar");
            ItemObject crowbarObject = new ItemBuilder(Info)
                .SetEnum(crowbarEnum)
                .SetNameAndDescription("Itm_Crowbar", "Desc_Crowbar")
                .SetShopPrice(600)
                .SetGeneratorCost(55)
                .SetItemComponent<ITM_Crowbar>()
                .SetSprites(assetMan.Get<Sprite>("CrowbarSmall"), assetMan.Get<Sprite>("CrowbarBig"))
                .Build();
            ITM_Crowbar crowbar = (ITM_Crowbar)crowbarObject.item;
            assetMan.Add<ItemObject>("Crowbar", crowbarObject);
            crowbar.gameObject.name = "Crowbar Object";
            crowbar.doorPrefab = Resources.FindObjectsOfTypeAll<SwingDoor>().Where(x => (x.name == "Door_Swinging" && x.transform.parent == null)).First();
            crowbar.useSound = assetMan.Get<SoundObject>("CrowbarHit");
            crowbar.doorPrySound = assetMan.Get<SoundObject>("PryDoor");

            // thief mask
            Canvas gumCanvasClone = GameObject.Instantiate<Canvas>(Resources.FindObjectsOfTypeAll<Gum>().First().transform.Find("GumOverlay").GetComponent<Canvas>());
            gumCanvasClone.gameObject.SetActive(false);
            gumCanvasClone.name = "ThiefMaskCanvas";
            gumCanvasClone.gameObject.GetComponentInChildren<Image>().sprite = assetMan.Get<Sprite>("ThiefMaskOverlay");

            Items maskEnum = EnumExtensions.ExtendEnum<Items>("ThiefMask");

            ItemObject maskObject = new ItemBuilder(Info)
                .SetEnum(maskEnum)
                .SetShopPrice(650)
                .SetGeneratorCost(60)
                .SetItemComponent<ITM_Mask>()
                .SetSprites(assetMan.Get<Sprite>("ThiefMaskSmall"), assetMan.Get<Sprite>("ThiefMaskBig"))
                .SetNameAndDescription("Itm_ThiefMask", "Desc_ThiefMask")
                .Build();
            gumCanvasClone.transform.SetParent(maskObject.item.transform);
            ((ITM_Mask)maskObject.item).maskCanvas = gumCanvasClone;
            assetMan.Add<ItemObject>("Mask", maskObject);

            /*
            Items pickaxeEnum = EnumExtensions.ExtendEnum<Items>("Pickaxe");
            ItemMetaData pickaxeMeta = new ItemMetaData(Info, new ItemObject[0]);
            assetMan.Add<WindowObject>("digWindow", digWindow);
            for (int i = 0; i < 3; i++)
            {
                int useCount = (i + 1);
                ItemObject pickaxeObject = new ItemBuilder(Info)
                    .SetMeta(pickaxeMeta)
                    .SetEnum(pickaxeEnum)
                    .SetNameAndDescription("Itm_Pickaxe_" + useCount, "Desc_Pickaxe_" + useCount)
                    .SetSprites(assetMan.Get<Sprite>("PickaxeSmall"), assetMan.Get<Sprite>("PickaxeBig"))
                    .SetItemComponent<ITM_Pickaxe>()
                    .SetShopPrice(300)
                    .SetGeneratorCost(55)
                    .Build();
                //ItemObject pickaxeObject = ObjectCreators.CreateItemObject("Itm_Pickaxe_" + useCount, "Desc_Pickaxe_" + useCount, assetMan.Get<Sprite>("PickaxeSmall"), assetMan.Get<Sprite>("PickaxeBig"), pickaxeEnum, 300, 60);
                ITM_Pickaxe pickaxe = (ITM_Pickaxe)pickaxeObject.item;
                pickaxe.windowToPlace = digWindow;
                pickaxe.uses = i;
                pickaxeObject.item = pickaxe;
                assetMan.Add<ItemObject>("Pickaxe" + useCount, pickaxeObject);
                pickaxe.gameObject.name = "Pickaxe Object " + useCount;
            }
            */

            //create IOU item

            Entity iouEntity = new EntityBuilder()
                .SetName("ITM_IOU")
                .AddDefaultRenderBaseFunction(assetMan.Get<Sprite>("IOUCrumpled"))
                //.SetLayerCollisionMask(new LayerMask() { value = 2363401 })
                .AddTrigger(1f)
                .Build();

            ITM_IOU iouItem = iouEntity.gameObject.AddComponent<ITM_IOU>();
            iouItem.entity = iouEntity;

            PropagatedAudioManager iouAum = iouItem.gameObject.AddComponent<PropagatedAudioManager>();
            iouAum.ReflectionSetVariable("maxDistance", 75f);
            iouItem.audMan = iouAum;

            iouItem.crumple = assetMan.Get<SoundObject>("PaperCrumple");
            iouItem.slap = assetMan.Get<SoundObject>("PaperSlap");

            ItemObject IOU = new ItemBuilder(Info)
                .SetSprites(assetMan.Get<Sprite>("IOUSmall"), assetMan.Get<Sprite>("IOUBig"))
                .SetNameAndDescription("Itm_IOU", "Desc_IOU")
                .SetShopPrice(500)
                .SetGeneratorCost(int.MaxValue) // this should never be in the generator
                .SetMeta(ItemFlags.CreatesEntity | ItemFlags.Persists, new string[0])
                .SetEnum(IOUEnum)
                .SetItemComponent(iouItem)
                .Build();

            ItemObject IOUDecoy = new ItemBuilder(Info)
                .SetSprites(assetMan.Get<Sprite>("IOUSmall"), assetMan.Get<Sprite>("IOUBig"))
                .SetNameAndDescription("erm... you shouldn't be getting this...", "erm....")
                .SetShopPrice(500)
                .SetGeneratorCost(40)
                .SetMeta(ItemFlags.NoUses | ItemFlags.InstantUse, new string[0])
                .SetEnum(IOUDecoyEnum)
                .SetItemComponent<Item>()
                .Build();

            assetMan.Add<ItemObject>("IOU", IOU);
            assetMan.Add<ItemObject>("IOUDecoy", IOUDecoy);

            assetMan.AddFromResources<Shader>();
            assetMan.AddFromResources<Mesh>();

            ITM_DealerBag.potentialItems.Add(new WeightedItemObject()
            {
                weight = 5,
                selection = IOUDecoy
            });

            yield return "Creating Dealer...";

            // dealer code
            Dealer dealer = new NPCBuilder<Dealer>(Info)
                .SetName("Dealer")
                .SetEnum("Dealer")
                .AddTrigger()
                .AddLooker()
                .SetMinMaxAudioDistance(1f, 150f)
                .SetPoster(assetMan.Get<Texture2D>("dealer_poster"), "PST_PRI_Dealer1", "PST_PRI_Dealer2")
                .AddSpawnableRoomCategories(RoomCategory.Class, RoomCategory.Office, RoomCategory.Special, RoomCategory.Faculty)
                .SetForcedSubtitleColor(dealerColor)
                .Build();

            dealerEnum = dealer.Character;

            CustomSpriteAnimator anim = dealer.gameObject.AddComponent<CustomSpriteAnimator>();
            anim.spriteRenderer = dealer.spriteRenderer[0];
            dealer.animator = anim;
            dealer.spriteRenderer[0].transform.localPosition += new Vector3(0f, 0.4f, 0f);

            dealer.audGrappled = assetMan.Get<SoundObject>("DealerScreech");

            dealer.audHey = new SoundObject[]
            {
                assetMan.Get<SoundObject>("DealerHeyYou1"),
                assetMan.Get<SoundObject>("DealerHeyYou2"),
                assetMan.Get<SoundObject>("DealerHeyYou3"),
                assetMan.Get<SoundObject>("DealerHeyYou4"),
            };

            dealer.audInteract = new SoundObject[]
            {
                assetMan.Get<SoundObject>("DealerInteract1"),
                assetMan.Get<SoundObject>("DealerDeliverTo"),
                dealer.audGrappled,
                assetMan.Get<SoundObject>("DealerThrowAt"),
            };

            dealer.audInquireWarning = new SoundObject[]
            {
                assetMan.Get<SoundObject>("DealerInquire"),
                assetMan.Get<SoundObject>("DealerInquireWarning"),
            };

            dealer.audInquireGood = new SoundObject[]
            {
                assetMan.Get<SoundObject>("DealerInquire"),
                assetMan.Get<SoundObject>("DealerInquireGood"),
            };

            dealer.audInquireBad = new SoundObject[]
            {
                assetMan.Get<SoundObject>("DealerInquire"),
                assetMan.Get<SoundObject>("DealerInquireBad"),
            };

            dealer.audInquireLost = new SoundObject[]
            {
                assetMan.Get<SoundObject>("DealerInquire"),
                assetMan.Get<SoundObject>("DealerInquireLost"),
            };

            dealer.audGottaScram = new SoundObject[]
            {
                assetMan.Get<SoundObject>("DealerUhOh"),
                assetMan.Get<SoundObject>("DealerUhOh2"),
            };

            dealer.audSteal = assetMan.Get<SoundObject>("DealerTaxedWithItem");
            dealer.audStealNoSpare = assetMan.Get<SoundObject>("DealerTaxed");
            dealer.audInterrupted = assetMan.Get<SoundObject>("DealerInterrupted");

            dealer.audMan = dealer.GetComponent<PropagatedAudioManager>();
            dealer.weightedItems = new WeightedItemObject[]
            {
                new WeightedItemObject()
                {
                    selection = assetMan.Get<ItemObject>("Pouch"),
                    weight = 100
                }
            };

            assetMan.Add<Dealer>("Dealer", dealer);

            ITM_DealerBag.potentialItems.Add(new WeightedItemObject()
            {
                weight = 70,
                selection= ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value
            });
            ITM_DealerBag.potentialItems.Add(new WeightedItemObject()
            {
                weight = 60,
                selection = ItemMetaStorage.Instance.FindByEnum(Items.PortalPoster).value
            });
            ITM_DealerBag.potentialItems.Add(new WeightedItemObject()
            {
                weight = 50,
                selection = ItemMetaStorage.Instance.FindByEnum(Items.GrapplingHook).value
            });
            ITM_DealerBag.potentialItems.Add(new WeightedItemObject()
            {
                weight = 80,
                selection = ItemMetaStorage.Instance.FindByEnum(Items.Tape).value
            });
            ITM_DealerBag.potentialItems.Add(new WeightedItemObject()
            {
                weight = 20,
                selection = ItemMetaStorage.Instance.FindByEnum(Items.Apple).value
            });
            ITM_DealerBag.potentialItems.Add(new WeightedItemObject()
            {
                weight = 80,
                selection = ItemMetaStorage.Instance.FindByEnum(Items.DetentionKey).value
            });
            ITM_DealerBag.potentialItems.Add(new WeightedItemObject()
            {
                weight = 70,
                selection = ItemMetaStorage.Instance.GetPointsObject(100, false)
            });
            
        }


        void GeneratorModifications(string levelName, int levelId, SceneObject scene)
        {
            if (levelId > 0)
            {
                scene.CustomLevelObject().forcedItems.Add(assetMan.Get<ItemObject>("IOUDecoy"));
            }
            scene.MarkAsNeverUnload();
            switch (levelName)
            {
                case "F1":
                    scene.CustomLevelObject().potentialItems = scene.CustomLevelObject().potentialItems.AddRangeToArray(new WeightedItemObject[]
                    {
                        new WeightedItemObject()
                        {
                            selection = assetMan.Get<ItemObject>("Crowbar"),
                            weight = 20
                        },
                        new WeightedItemObject()
                        {
                            selection = assetMan.Get<ItemObject>("Mask"),
                            weight = 40
                        }
                    });
                    //obj.forcedNpcs = obj.forcedNpcs.AddToArray(assetMan.Get<NPC>("Dealer"));
                    break;
                case "F2":
                    scene.CustomLevelObject().potentialItems = scene.CustomLevelObject().potentialItems.AddRangeToArray(new WeightedItemObject[]
                    {
                        new WeightedItemObject()
                        {
                            selection = assetMan.Get<ItemObject>("Crowbar"),
                            weight = 70
                        },
                        new WeightedItemObject()
                        {
                            selection = assetMan.Get<ItemObject>("Mask"),
                            weight = 60
                        }
                    });
                    scene.shopItems = scene.shopItems.AddRangeToArray(new WeightedItemObject[]
                    {
                        new WeightedItemObject()
                        {
                            selection = assetMan.Get<ItemObject>("Mask"),
                            weight = 80
                        }
                    });
                    if (!youtuberModeEnabled.Value)
                    {
                        scene.potentialNPCs.Add(new WeightedNPC()
                        {
                            selection = assetMan.Get<NPC>("Dealer"),
                            weight = 100
                        });
                    }
                    else
                    {
                        scene.forcedNpcs = scene.forcedNpcs.AddToArray(assetMan.Get<NPC>("Dealer"));
                        scene.additionalNPCs = Mathf.Max(scene.additionalNPCs - 1, 0);
                    }
                    return;
                case "F3":
                    scene.CustomLevelObject().potentialItems = scene.CustomLevelObject().potentialItems.AddRangeToArray(new WeightedItemObject[]
                    {
                        new WeightedItemObject()
                        {
                            selection = assetMan.Get<ItemObject>("Crowbar"),
                            weight = 68
                        },
                        new WeightedItemObject()
                        {
                            selection = assetMan.Get<ItemObject>("Mask"),
                            weight = 80
                        }
                    });
                    scene.shopItems = scene.shopItems.AddRangeToArray(new WeightedItemObject[]
                    {
                        new WeightedItemObject()
                        {
                            selection = assetMan.Get<ItemObject>("Crowbar"),
                            weight = 90
                        },
                        new WeightedItemObject()
                        {
                            selection = assetMan.Get<ItemObject>("Mask"),
                            weight = 65
                        }
                    });
                    if (!youtuberModeEnabled.Value)
                    {
                        scene.potentialNPCs.Add(new WeightedNPC()
                        {
                            selection = assetMan.Get<NPC>("Dealer"),
                            weight = 45
                        });
                    }
                    return;
            }
        }

        public static Items IOUEnum = EnumExtensions.ExtendEnum<Items>("IOU");

        public static Items IOUDecoyEnum = EnumExtensions.ExtendEnum<Items>("IOUDecoy");


        static Color dealerColor = new Color(174f / 255f, 94f / 255f, 144f / 255f);

        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.criminalpackroot");
            harmony.PatchAllConditionals();
            Instance = this;
            LoadingEvents.RegisterOnAssetsLoaded(Info, ResourcesLoaded(), false);
            AssetLoader.LocalizationFromMod(this);
            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorModifications);

            youtuberModeEnabled = Config.Bind<bool>("General", "Youtuber Mode", false, "If true, Dealer will always appear on Floor 2.");

            ModdedSaveGame.AddSaveHandler(new CriminalPackSaveIO());
        }
    }

    public class CriminalPackSaveIO : ModdedSaveGameIOBinary
    {
        public override PluginInfo pluginInfo => CriminalPackPlugin.Instance.Info;

        public override void Load(BinaryReader reader)
        {
            reader.ReadByte();
        }

        public override void Reset()
        {
            
        }

        public override void Save(BinaryWriter writer)
        {
            writer.Write((byte)0);
        }

        public override string[] GenerateTags()
        {
            if (CriminalPackPlugin.Instance.youtuberModeEnabled.Value)
            {
                return new string[1] { "YoutuberMode" };
            }
            return new string[0];
        }

        public override string DisplayTags(string[] tags)
        {
            if (tags.Contains("YoutuberMode"))
            {
                return "Youtuber Mode";
            }
            return "Standard Mode";
        }
    }
}
