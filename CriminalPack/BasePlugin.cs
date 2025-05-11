using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.PlusExtensions;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using MTM101BaldAPI.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CriminalPack
{
    [BepInPlugin("mtm101.rulerp.baldiplus.criminalpackroot", "Criminal Pack Root Mod", "2.0.0.0")]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInDependency("mtm101.rulerp.baldiplus.leveltyped", BepInDependency.DependencyFlags.SoftDependency)]
    public class CriminalPackPlugin : BaseUnityPlugin
    {
        public static CriminalPackPlugin Instance;

        public static ManualLogSource Log;

        public static Character dealerEnum;
        public static LevelType prisonType;

        public AssetManager assetMan = new AssetManager();

        public ConfigEntry<bool> youtuberModeEnabled;

        IEnumerator ResourcesLoaded()
        {
            yield return 11 + (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.leveltyped") ? 1 : 0);
            yield return "Fetching existing assets...";
            SoundObject[] foundSoundObjects = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.GetInstanceID() >= 0).ToArray();
            assetMan.Add<SoundObject>("CorrectBuzz", foundSoundObjects.First(x => x.name == "Activity_Correct"));
            assetMan.Add<SoundObject>("WrongBuzz", foundSoundObjects.First(x => x.name == "Activity_Incorrect"));

            yield return "Loading Sprites and Textures...";
            assetMan.Add<Texture2D>("LightMap", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.name == "LightMap"));
            assetMan.Add<Sprite>("CrowbarSmall", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "CrowbarSmall.png"), 25f));
            assetMan.Add<Sprite>("CrowbarBig", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "CrowbarBig.png"), 50f));
            assetMan.Add<Sprite>("ThiefMaskSmall", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "ThiefMaskSmall.png"), 25f));
            assetMan.Add<Sprite>("ThiefMaskBig", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "ThiefMaskBig.png"), 50f));
            assetMan.Add<Sprite>("ThiefMaskOverlay", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "ThiefMaskOverlay.png"), 1f));
            assetMan.Add<Sprite>("PouchSmall", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "PouchSmall.png"), 25f));
            assetMan.Add<Sprite>("PouchBig", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "PouchBig.png"), 50f));
            assetMan.Add<Sprite>("IOUSmall", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "IOU_Small.png"), 25f));
            assetMan.Add<Sprite>("IOUCrumpled", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "IOU_Crumpled.png"), 50f));
            assetMan.Add<Sprite>("IOUBig", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "IOU_Big.png"), 50f));
            assetMan.Add<Sprite>("IOUBOOM", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "IOU_BOOM.png"), 50f));
            assetMan.Add<Texture2D>("IOU_Wall", AssetLoader.TextureFromMod(this, "IOU_Wall.png"));
            assetMan.Add<Texture2D>("IOU_WallFade", AssetLoader.TextureFromMod(this, "IOU_WallFade.png"));
            assetMan.Add<Texture2D>("dealer_poster", AssetLoader.TextureFromMod(this, "dealer_poster.png"));
            assetMan.Add<Texture2D>("WarningPosterImage", AssetLoader.TextureFromMod(this, "Posters", "Poster_Warning.png"));
            assetMan.Add<Texture2D>("WarningCross", AssetLoader.TextureFromMod(this, "Posters", "Poster_WarningCross.png"));

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
            assetMan.Add<SoundObject>("ScannerProcess", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "ScannerScan.wav"), "Sfx_Scan", SoundType.Effect, Color.white));

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
            assetMan.Add<SoundObject>("DealerInterrupted", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Dealer", "Dealer_Interrupted.wav"), "Dealer_Interrupted", SoundType.Voice, dealerColor));

            assetMan.Add<SoundObject>("BaldiThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "Baldi.wav"), "CharacterHappy_Baldi", SoundType.Voice, Color.green));

            assetMan.Add<SoundObject>("PrincipalThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "Principal.wav"), "CharacterHappy_Principal", SoundType.Voice, Color.white));
            assetMan.Add<SoundObject>("BeansThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "Beans.wav"), "CharacterHappy_Beans", SoundType.Voice, Color.white));
            assetMan.Add<SoundObject>("PlaytimeThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "Playtime.wav"), "CharacterHappy_Playtime", SoundType.Voice, Color.red));
            assetMan.Add<SoundObject>("FirstPrizeThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "FirstPrize.wav"), "CharacterHappy_FirstPrize", SoundType.Voice, Color.white));
            assetMan.Add<SoundObject>("DrReflexThanks", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CharacterHappy", "DrReflex.wav"), "CharacterHappy_DrReflex", SoundType.Voice, Color.white));

            assetMan.Add<SoundObject>("PrincipalFraud", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PRI_NoFraud.wav"), "Vfx_PRI_NoFraud", SoundType.Voice, Color.white));
            assetMan.Add<SoundObject>("PrincipalContraband", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PRI_NoContraband.wav"), "Vfx_PRI_NoContraband", SoundType.Voice, Color.white));

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
                    audMan.PlaySingle((SoundObject)(((NoLateTeacher)npc).ReflectionGetVariable("audInTime")));
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
                .SetNameAndDescription("Itm_DealerPouch", "Desc_DealerPouch")
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
                .SetMeta(ItemFlags.None, new string[2] { "crmp_contraband", "sharp" })
                .SetSprites(assetMan.Get<Sprite>("CrowbarSmall"), assetMan.Get<Sprite>("CrowbarBig"))
                .Build();
            ITM_Crowbar crowbar = (ITM_Crowbar)crowbarObject.item;
            assetMan.Add<ItemObject>("Crowbar", crowbarObject);
            crowbar.gameObject.name = "Crowbar Object";
            crowbar.doorPrefab = Resources.FindObjectsOfTypeAll<SwingDoor>().Where(x => (x.name == "Door_Swinging" && x.transform.parent == null)).First();
            crowbar.useSound = assetMan.Get<SoundObject>("CrowbarHit");
            crowbar.doorPrySound = assetMan.Get<SoundObject>("PryDoor");

            // thief mask
            Canvas gumCanvasClone = GameObject.Instantiate<Canvas>(Resources.FindObjectsOfTypeAll<Gum>().First(x => x.GetInstanceID() >= 0).transform.Find("GumOverlay").GetComponent<Canvas>());
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
                .SetMeta(ItemFlags.Persists, new string[1] { "crmp_contraband" })
                .Build();
            gumCanvasClone.transform.SetParent(maskObject.item.transform);
            ((ITM_Mask)maskObject.item).maskCanvas = gumCanvasClone;
            assetMan.Add<ItemObject>("Mask", maskObject);

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
                .SetMeta(ItemFlags.CreatesEntity | ItemFlags.Persists, new string[1] { "cann_like" }) // lol
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

            dealer.audSteal = assetMan.Get<SoundObject>("DealerTaxed");
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

            yield return "Creating Scanner...";

            // load scanner materials
            Material materialBase = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "TileBase" && x.GetInstanceID() >= 0);

            Material scannerBaseMat = new Material(materialBase) { name = "ScannerBaseMat" };
            scannerBaseMat.SetMainTexture(AssetLoader.TextureFromMod(this, "Models", "ScannerTex1.png"));
            Material scannerLightGreenMat = new Material(materialBase) { name = "ScannerLightGreenMat" };
            scannerLightGreenMat.SetMainTexture(AssetLoader.TextureFromMod(this, "Models", "ScannerTex2.png"));
            scannerLightGreenMat.SetTexture("_LightGuide", AssetLoader.TextureFromMod(this, "Models", "ScannerTexLightmap.png"));

            Dictionary<string, Material> materials = new Dictionary<string, Material>
            {
                { "m_scannerbase", scannerBaseMat },
                { "m_scannerlight", scannerLightGreenMat }
            };

            GameObject scannerObject = AssetLoader.ModelFromModManualMaterials(this, materials, "Models", "scanner.obj");
            scannerObject.transform.localScale = Vector3.one * 10f;
            scannerObject.ConvertToPrefab(true);
            scannerObject.name = "Scanner";
            ItemScanner scanner = scannerObject.AddComponent<ItemScanner>();
            scanner.greenLight = scannerLightGreenMat;
            scanner.audMan = scannerObject.AddComponent<PropagatedAudioManager>();
            scanner.scanGood = assetMan.Get<SoundObject>("CorrectBuzz");
            scanner.scanBad = assetMan.Get<SoundObject>("WrongBuzz");
            scanner.scanStart = assetMan.Get<SoundObject>("ScannerProcess");

            scanner.yellowLight = new Material(scannerLightGreenMat) { 
                name = "ScannerLightYellowMat"
            };
            scanner.yellowLight.SetMainTexture(AssetLoader.TextureFromMod(this, "Models", "ScannerTex2Yellow.png"));

            scanner.redLight = new Material(scannerLightGreenMat)
            {
                name = "ScannerLightRedMat"
            };
            scanner.redLight.SetMainTexture(AssetLoader.TextureFromMod(this, "Models", "ScannerTex2Red.png"));

            scanner.blackLight = new Material(scannerBaseMat)
            {
                name = "ScannerLightBlackMat"
            };
            scanner.blackLight.SetMainTexture(AssetLoader.TextureFromMod(this, "Models", "ScannerTex2Off.png"));
            scanner.lightMeshes = scannerObject.GetComponentsInChildren<MeshRenderer>().Where(x => x.materials.Count(z => z.name.Contains("ScannerLight")) > 0).ToArray();

            // add trigger
            BoxCollider scannerCollider = scannerObject.AddComponent<BoxCollider>();
            scannerCollider.size = new Vector3(1f, 1f, 0.1f); // i forgot scale
            scannerCollider.isTrigger = true; 

            // create the structure builder
            GameObject scannerBuilderObject = new GameObject("ScannerBuilder");
            scannerBuilderObject.ConvertToPrefab(true);
            Structure_Scanner scannerBuilder = scannerBuilderObject.AddComponent<Structure_Scanner>();
            scannerBuilder.prefab = scanner;
            assetMan.Add<Structure_Scanner>("scanner", scannerBuilder);

            yield return "Loading Prison Style assets...";
            assetMan.Add<Transform>("CagedLight", Resources.FindObjectsOfTypeAll<Transform>().First(x => x.GetInstanceID() >= 0 && x.name == "CagedLight"));
            prisonType = EnumExtensions.ExtendEnum<LevelType>("Prison");
            assetMan.Add<Texture2D>("PrisonWall", AssetLoader.TextureFromMod(this, "Prison", "PrisonWall.png"));
            assetMan.Add<Texture2D>("PrisonFloor", AssetLoader.TextureFromMod(this, "Prison", "PrisonFloor.png"));
            assetMan.AddFromResourcesNoClones<RoomAsset>();

            WindowObject cellWindow = ObjectCreators.CreateWindowObject("CellWindow", AssetLoader.TextureFromMod(this, "Prison", "CellWindow.png"), AssetLoader.TextureFromMod(this, "Prison", "CellWindowBroken.png"), AssetLoader.TextureFromMod(this, "Prison", "CellWindow_Mask.png"));

            StandardDoorMats cellMat = ObjectCreators.CreateDoorDataObject("CellDoor", AssetLoader.TextureFromMod(this, "Prison", "CellDoor_Open.png"), AssetLoader.TextureFromMod(this, "Prison", "CellDoor_Closed.png"));

            RoomAsset officeAsset = assetMan.Get<RoomAsset>("Room_Office_0");

            RoomFunctionContainer prisonCellContainer = GameObject.Instantiate<RoomFunctionContainer>(Resources.FindObjectsOfTypeAll<RoomFunctionContainer>().First(x => x.GetInstanceID() >= 0 && x.name == "OfficeRoomFunction"), MTM101BaldiDevAPI.prefabTransform);

            prisonCellContainer.name = "CellOfficeRoomFunction";
            Destroy(prisonCellContainer.GetComponent<CharacterPostersRoomFunction>());
            ((List<RoomFunction>)prisonCellContainer.ReflectionGetVariable("functions")).RemoveAll(x => x is CharacterPostersRoomFunction);
            JailDoorRoomFunction jdrf = prisonCellContainer.gameObject.AddComponent<JailDoorRoomFunction>();
            jdrf.doorMat = cellMat;
            prisonCellContainer.AddFunction(jdrf);

            // create cellblock
            RoomAsset cellRoom = ScriptableObject.CreateInstance<RoomAsset>();

            cellRoom.mapMaterial = officeAsset.mapMaterial;
            cellRoom.color = officeAsset.color;

            cellRoom.name = "CellBlock";
            ((UnityEngine.Object)cellRoom).name = "CellBlock";
            cellRoom.category = RoomCategory.Office;
            cellRoom.hasActivity = false;
            cellRoom.activity = new ActivityData();
            cellRoom.ceilTex = assetMan.Get<Texture2D>("PrisonFloor");
            cellRoom.florTex = assetMan.Get<Texture2D>("PrisonFloor");
            cellRoom.wallTex = assetMan.Get<Texture2D>("PrisonWall");
            cellRoom.itemSpawnPoints = new List<ItemSpawnPoint>
            {
                new ItemSpawnPoint()
                {
                    chance = 0.5f,
                    minValue = 1,
                    maxValue = 25,
                    position = new Vector2(5f, 5f),
                    weight = 25
                }
            };
            cellRoom.maxItemValue = 25;
            cellRoom.minItemValue = 1;
            cellRoom.doorMats = cellMat;
            cellRoom.roomFunctionContainer = prisonCellContainer;
            cellRoom.potentialDoorPositions = new List<IntVector2>() { new IntVector2(1, 0), new IntVector2(2, 0) };
            cellRoom.cells.Add(new CellData()
            {
                pos = new IntVector2(0, 0),
                type = 12
            });
            cellRoom.cells.Add(new CellData()
            {
                pos = new IntVector2(0, 1),
                type = 9
            });
            cellRoom.cells.Add(new CellData()
            {
                pos = new IntVector2(1, 0),
                type = 4
            });
            cellRoom.cells.Add(new CellData()
            {
                pos = new IntVector2(1, 1),
                type = 1
            });
            cellRoom.cells.Add(new CellData()
            {
                pos = new IntVector2(2, 0),
                type = 6
            });
            cellRoom.cells.Add(new CellData()
            {
                pos = new IntVector2(2, 1),
                type = 3
            });
            cellRoom.standardLightCells.Add(new IntVector2(2, 1));
            cellRoom.entitySafeCells.Add(new IntVector2(2, 1));
            cellRoom.eventSafeCells.Add(new IntVector2(2, 1));
            cellRoom.windowChance = 0.8f;
            cellRoom.windowObject = cellWindow;
            assetMan.Add<RoomAsset>("CellBlock", cellRoom);

            yield return "Setting up keycards...";
            HudManager hudMan = Resources.FindObjectsOfTypeAll<HudManager>().First(x => x.GetInstanceID() >= 0 && x.name == "MainHud");
            KeycardHud keyHud = hudMan.gameObject.AddComponent<KeycardHud>();
            Transform itemSlotsTransform = hudMan.transform.Find("ItemSlots");
            Transform keyCardClone = GameObject.Instantiate<Transform>(itemSlotsTransform.transform.Find("ItemSlot (0)"), MTM101BaldiDevAPI.prefabTransform);
            keyCardClone.SetParent(itemSlotsTransform);
            GameObject.DestroyImmediate(keyCardClone.transform.Find("ItemIcon (0)").gameObject); // nope
            keyCardClone.GetComponent<RectTransform>().anchoredPosition += Vector2.left * 40; //go back
            keyCardClone.name = "KeyDisplay1";
            RawImage keyCardimage = keyCardClone.GetComponent<RawImage>();
            keyCardimage.texture = AssetLoader.TextureFromMod(this, "Keycards", "IconCard1.png");
            keyCardClone.localScale = Vector3.one;
            keyCardClone.GetComponent<RectTransform>().sizeDelta = new Vector2(40f,35f);
            keyCardimage.enabled = false;
            keyHud.renderers[0] = keyCardimage;
            for (int i = 2; i <= 3; i++)
            {
                RawImage keyIconClone = GameObject.Instantiate<Transform>(keyCardClone, MTM101BaldiDevAPI.prefabTransform).GetComponent<RawImage>();
                keyIconClone.name = "KeyDisplay" + i;
                keyIconClone.texture = AssetLoader.TextureFromMod(this, "Keycards", "IconCard" + i + ".png");
                keyIconClone.transform.SetParent(itemSlotsTransform);
                keyIconClone.transform.localScale = Vector3.one;
                keyHud.renderers[i - 1] = keyIconClone;
            }

            GameObject keycardBuilderObject = new GameObject("KeycardDoorBuilder");
            keycardBuilderObject.transform.SetParent(MTM101BaldiDevAPI.prefabTransform);
            Structure_KeycardDoors keycardBuilder = keycardBuilderObject.AddComponent<Structure_KeycardDoors>();
            assetMan.Add<Structure_KeycardDoors>("Structure_KeycardDoors", keycardBuilder);

            Color[] keyDoorColors = new Color[3]
            {
                new Color(0f,1f,0f),
                new Color(0f,0f,1f),
                new Color(1f,0f,0f)
            };

            Sprite keycardDoorOpen = AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 16f, "Keycards", "Icon_KeyDoor_Open.png");
            Sprite keycardDoorClosed = AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 16f, "Keycards", "Icon_KeyDoor_Closed.png");

            LockdownDoor doorTemplate = Resources.FindObjectsOfTypeAll<LockdownDoor>().First(x => x.GetInstanceID() >= 0 && x.name == "LockdownDoor_TrapCheck");
            for (int i = 0; i < 3; i++)
            {
                LockdownDoor keyDoorOld = GameObject.Instantiate<LockdownDoor>(doorTemplate, MTM101BaldiDevAPI.prefabTransform);
                keyDoorOld.name = "Keycard" + (i + 1) + "LockdownDoor";
                KeycardLockdownDoor keyLockDoor = keyDoorOld.gameObject.AddComponent<KeycardLockdownDoor>();
                FieldInfo[] fields = typeof(LockdownDoor).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int j = 0; j < fields.Length; j++)
                {
                    fields[j].SetValue(keyLockDoor, fields[j].GetValue(keyDoorOld));
                }
                keyLockDoor.ReflectionSetVariable("shutAtGameStart", true);
                keyLockDoor.ReflectionSetVariable("speed", 5f);
                keyLockDoor.ReflectionSetVariable("mapUnlockedSprite", keycardDoorOpen);
                keyLockDoor.ReflectionSetVariable("mapLockedSprite", keycardDoorClosed);
                keyLockDoor.myValue = i;
                keyLockDoor.myColor = keyDoorColors[i];
                Destroy(keyDoorOld); // remove the old component
                MeshRenderer renderer = keyDoorOld.transform.Find("LockdownDoor_Model").GetComponent<MeshRenderer>();

                Material[] rendMats = renderer.materials;

                Material newWarningStripeMaterial = new Material(rendMats[1]);
                newWarningStripeMaterial.name = "ClearanceStripes" + (i + 1);
                newWarningStripeMaterial.SetMainTexture(AssetLoader.TextureFromMod(this, "Keycards", "ClearanceStripes" + (i + 1) + ".png"));
                rendMats[1] = newWarningStripeMaterial;

                Material newLockdownMaterial = new Material(rendMats[2]);
                newLockdownMaterial.name = "LockdownKeyMaterial" + (i + 1);
                newLockdownMaterial.SetMainTexture(AssetLoader.TextureFromMod(this, "Keycards", "LockdownClearance" + (i + 1) + ".png"));
                rendMats[2] = newLockdownMaterial;

                renderer.materials = rendMats;

                keycardBuilder.doorPrefabs[i] = keyLockDoor;
            }

            string[] keycardEnums = new string[] { "Green", "Blue", "Red" };

            SoundObject cardPickupSound = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CardPickup.wav"), "", SoundType.Effect, Color.white);
            cardPickupSound.subtitle = false;
            for (int i = 0; i < 3; i++)
            {
                Sprite cardSprite = AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 50f, "Keycards", "Keycard" + (i + 1) + ".png");
                ItemObject cardObject = new ItemBuilder(Info)
                    .SetEnum("Keycard" + keycardEnums[i])
                    .SetShopPrice(1000)
                    .SetGeneratorCost(100)
                    .SetItemComponent<ITM_Keycard>()
                    .SetSprites(cardSprite, cardSprite)
                    .SetNameAndDescription("Itm_KeyCard" + keycardEnums[i], "Desc_KeyCardBad")
                    .SetAsInstantUse()
                    .SetPickupSound(cardPickupSound)
                    .Build();

                ((ITM_Keycard)cardObject.item).myValue = i;

                keycardBuilder.keycardItems[i] = cardObject;
            }

            keycardBuilder.windowObj = cellWindow;

            yield return "Modifying meta...";
            ItemMetaStorage.Instance.FindByEnum(Items.GrapplingHook).tags.Add("crmp_contraband"); // reasoning: dangerous
            ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).tags.Add("crmp_contraband"); // reasoning: dangerous
            ItemMetaStorage.Instance.FindByEnum(Items.DetentionKey).tags.Add("crmp_contraband"); // reasoning: belongs to principal (they are called principal's keys)
            ItemMetaStorage.Instance.FindByEnum(Items.CircleKey).tags.AddRange(new string[] { "crmp_contraband", "crmp_scanner_no_poster" }); // reasoning: they only ever open faculty rooms
            ItemMetaStorage.Instance.FindByEnum(Items.HexagonKey).tags.AddRange(new string[] { "crmp_contraband", "crmp_scanner_no_poster" }); // reasoning: they only ever open faculty rooms
            ItemMetaStorage.Instance.FindByEnum(Items.PentagonKey).tags.AddRange(new string[] { "crmp_contraband", "crmp_scanner_no_poster" }); // reasoning: they only ever open faculty rooms
            ItemMetaStorage.Instance.FindByEnum(Items.SquareKey).tags.AddRange(new string[] { "crmp_contraband", "crmp_scanner_no_poster" }); // reasoning: they only ever open faculty rooms
            ItemMetaStorage.Instance.FindByEnum(Items.WeirdKey).tags.AddRange(new string[] { "crmp_contraband", "crmp_scanner_no_poster" }); // reasoning: they only ever open faculty rooms
            ItemMetaStorage.Instance.FindByEnum(Items.TriangleKey).tags.AddRange(new string[] { "crmp_contraband", "crmp_scanner_no_poster" }); // reasoning: they only ever open faculty rooms

            NPCMetaStorage.Instance.Get(Character.Crafters).tags.Add("crmp_no_keycard");
            NPCMetaStorage.Instance.Get(Character.Chalkles).tags.Add("crmp_no_keycard");
            NPCMetaStorage.Instance.Get(Character.LookAt).tags.Add("crmp_no_keycard");
            NPCMetaStorage.Instance.Get(Character.Bully).tags.Add("crmp_no_keycard");
            NPCMetaStorage.Instance.Get(Character.Sweep).tags.Add("crmp_no_keycard");


            // we move it all into a seperate class so that we don't add an invalid using to this file when leveltyped isn't installed
            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.leveltyped"))
            {
                yield return "Adding Level Typed support...";
                LevelTypedAdder.Add();
            }
        }

        public static List<ExtendedPosterObject> itemPosters = new List<ExtendedPosterObject>();

        public void CreateScannerPoster(string itemName, Texture2D itemTexture, string posterIdName)
        {
            ExtendedPosterObject poster = ScriptableObject.CreateInstance<ExtendedPosterObject>();
            poster.baseTexture = assetMan.Get<Texture2D>("WarningPosterImage");
            poster.overlayData = new PosterImageData[]
            {
                    new PosterImageData(itemTexture, new IntVector2(100,-163 + 8), new IntVector2(64,64)),
                    new PosterImageData(assetMan.Get<Texture2D>("WarningCross"), new IntVector2(100-32,-163 + 32 + 8), new IntVector2(128,128))
            };
            poster.name = "Scanner_Poster_" + posterIdName;
            poster.textData = new PosterTextData[]
            {
                    new PosterTextData()
                    {
                        font = BaldiFonts.BoldComicSans24.FontAsset(),
                        fontSize = (int)BaldiFonts.BoldComicSans24.FontSize(),
                        alignment = TMPro.TextAlignmentOptions.Center,
                        color = Color.black,
                        textKey = "PST_Scn_Warn",
                        style = TMPro.FontStyles.Underline,
                        position = new IntVector2(57,195),
                        size = new IntVector2(144,64)
                    },
                    new ExtendedPosterTextData()
                    {
                        font = BaldiFonts.ComicSans12.FontAsset(),
                        fontSize = (int)BaldiFonts.ComicSans12.FontSize(),
                        alignment = TMPro.TextAlignmentOptions.Center,
                        color = Color.black,
                        textKey = Singleton<LocalizationManager>.Instance.GetLocalizedText(itemName).EndsWith("s") ? "PST_Scn_Itm_S" : "PST_Scn_Itm_NoS",
                        style = TMPro.FontStyles.Normal,
                        position = new IntVector2(57,96),
                        size = new IntVector2(144,128),
                        formats = new string[1] { itemName },
                        replacementRegex = new string[1][] // remove any (5) or other such counters if they exist
                        {
                            new string[2] { "\\([^)]*\\)", "" }
                        }
                    }
            };

            itemPosters.Add(poster);
        }

        IEnumerator ResourcesLoadedPost()
        {
            ItemMetaData[] allItems = ItemMetaStorage.Instance.FindAllWithTags(true, "crmp_contraband").Where(x => !x.tags.Contains("crmp_scanner_no_poster")).ToArray();
            yield return allItems.Length + 1;
            for (int i = 0; i < allItems.Length; i++)
            {
                yield return "Generating scanner poster for " + EnumExtensions.ToStringExtended(allItems[i].id);

                ItemObject currentItem = allItems[i].value;

                CreateScannerPoster(currentItem.nameKey, currentItem.itemSpriteLarge.texture, EnumExtensions.ToStringExtended(allItems[i].id));
            }

            yield return "Generating scanner poster for shape keys";
            CreateScannerPoster("PST_Scn_Itm_ShapeKeys", AssetLoader.TextureFromMod(this, "ShapeKeysAll.png"), "ShapeKeys");
        }

        /// <summary>
        /// Turns the specified LevelObject into a prison variant.
        /// </summary>
        /// <param name="toModify"></param>
        /// <returns></returns>
        public void ModifyIntoPrison(LevelObject toModify, int levelId)
        {
            // dont delete locked room this time
            //toModify.roomGroup = toModify.roomGroup.Where(x => x.name != "LockedRoom").ToArray();

            toModify.randomEvents.RemoveAll(x => x.selection.Type == RandomEventType.Party); // no.

            toModify.potentialPostPlotSpecialHalls = new WeightedRoomAsset[]
            {
                new WeightedRoomAsset()
                {
                    selection=assetMan.Get<RoomAsset>("Room_HallFormation_0"),
                    weight=100
                }
            };

            toModify.minPostPlotSpecialHalls = 15;
            toModify.maxPostPlotSpecialHalls = 25;

            toModify.potentialPrePlotSpecialHalls = new WeightedRoomAsset[]
            {
                new WeightedRoomAsset()
                {
                    selection=assetMan.Get<RoomAsset>("Room_HallFormation_0"),
                    weight=100
                }
            };


            toModify.maxItemValue += 50;

            RoomGroup facultyGroup = toModify.roomGroup.First(x => x.name == "Faculty");
            facultyGroup.maxRooms += 4;
            facultyGroup.minRooms += 2;
            facultyGroup.stickToHallChance = 1f;

            RoomGroup principalOfficeGroup = new RoomGroup();

            RoomGroup officeGroup = toModify.roomGroup.First(x => x.name == "Office");

            principalOfficeGroup.minRooms = 1;
            principalOfficeGroup.maxRooms = 1;
            principalOfficeGroup.stickToHallChance = 1f;
            principalOfficeGroup.floorTexture = officeGroup.floorTexture;
            principalOfficeGroup.wallTexture = officeGroup.wallTexture;
            principalOfficeGroup.ceilingTexture = officeGroup.ceilingTexture;
            principalOfficeGroup.potentialRooms = officeGroup.potentialRooms;
            principalOfficeGroup.name = "FacultyOffice";

            officeGroup.name = "CellBlocks"; // so other mods don't fuck it up
            officeGroup.minRooms = 25;
            officeGroup.maxRooms = 40;
            officeGroup.potentialRooms = new WeightedRoomAsset[1]
            {
                new WeightedRoomAsset()
                {
                    selection = assetMan.Get<RoomAsset>("CellBlock"),
                    weight = 100
                }
            };

            toModify.roomGroup = toModify.roomGroup.AddItem(principalOfficeGroup).ToArray();

            officeGroup.wallTexture = new WeightedTexture2D[]
            {
                new WeightedTexture2D()
                {
                    selection=assetMan.Get<Texture2D>("PrisonWall"),
                    weight=100
                }
            };

            officeGroup.floorTexture = new WeightedTexture2D[]
            {
                new WeightedTexture2D()
                {
                    selection=assetMan.Get<Texture2D>("PrisonFloor"),
                    weight=100
                }
            };

            officeGroup.ceilingTexture = new WeightedTexture2D[]
            {
                new WeightedTexture2D()
                {
                    selection=assetMan.Get<Texture2D>("PrisonFloor"),
                    weight=100
                }
            };

            toModify.minPrePlotSpecialHalls = 5;
            toModify.maxPrePlotSpecialHalls = 5;

            toModify.minPlots = 8;
            toModify.maxPlots = 12;

            toModify.standardLightColor = new Color(249f/255f, 241f/255f, 199f/255f);
            toModify.standardLightStrength = 6;

            toModify.hallLights = new WeightedTransform[]
            {
                new WeightedTransform()
                {
                    selection=assetMan.Get<Transform>("CagedLight"),
                    weight=100,
                }
            };

            for (int i = 0; i < toModify.roomGroup.Length; i++)
            {
                toModify.roomGroup[i].light = new WeightedTransform[]
                {
                    new WeightedTransform()
                    {
                        selection=assetMan.Get<Transform>("CagedLight"),
                        weight=100,
                    }
                };
            }

            toModify.hallFloorTexs = new WeightedTexture2D[]
            {
                new WeightedTexture2D()
                {
                    selection=assetMan.Get<Texture2D>("PrisonFloor"),
                    weight=100
                }
            };
            toModify.hallCeilingTexs = new WeightedTexture2D[]
            {
                new WeightedTexture2D()
                {
                    selection=assetMan.Get<Texture2D>("PrisonFloor"),
                    weight=100
                }
            };
            toModify.hallWallTexs = new WeightedTexture2D[]
            {
                new WeightedTexture2D()
                {
                    selection=assetMan.Get<Texture2D>("PrisonWall"),
                    weight=100
                }
            };

            toModify.timeLimit += 120f;

            List<StructureWithParameters> structures = toModify.forcedStructures.ToList();
            structures.Add(new StructureWithParameters()
            {
                parameters = new StructureParameters()
                {
                    minMax = new IntVector2[]
                    {
                        new IntVector2(6,9),
                        new IntVector2(12,16)
                    }
                },
                prefab = assetMan.Get<Structure_Scanner>("scanner")
            });

            switch (levelId)
            {
                case 0:
                    structures.Add(new StructureWithParameters()
                    {
                        parameters = new StructureParameters()
                        {
                            minMax = new IntVector2[]
                            {
                                new IntVector2(4,4),
                                new IntVector2(0,0),
                                new IntVector2(0,0)
                            }
                        },
                        prefab = assetMan.Get<Structure_KeycardDoors>("Structure_KeycardDoors")
                    });
                    break;
                case 1:
                    structures.Add(new StructureWithParameters()
                    {
                        parameters = new StructureParameters()
                        {
                            minMax = new IntVector2[]
                            {
                                new IntVector2(2,5),
                                new IntVector2(2,2),
                                new IntVector2(0,0)
                            }
                        },
                        prefab = assetMan.Get<Structure_KeycardDoors>("Structure_KeycardDoors")
                    });
                    break;
                case 2:
                    structures.Add(new StructureWithParameters()
                    {
                        parameters = new StructureParameters()
                        {
                            minMax = new IntVector2[]
                            {
                                new IntVector2(3,4),
                                new IntVector2(2,2),
                                new IntVector2(2,3)
                            }
                        },
                        prefab = assetMan.Get<Structure_KeycardDoors>("Structure_KeycardDoors")
                    });
                    break;
                case 3:
                    structures.Add(new StructureWithParameters()
                    {
                        parameters = new StructureParameters()
                        {
                            minMax = new IntVector2[]
                            {
                                new IntVector2(2,2),
                                new IntVector2(2,3),
                                new IntVector2(2,2)
                            }
                        },
                        prefab = assetMan.Get<Structure_KeycardDoors>("Structure_KeycardDoors")
                    });
                    break;
                default: // 4 and above or 3 and below
                    structures.Add(new StructureWithParameters()
                    {
                        parameters = new StructureParameters()
                        {
                            minMax = new IntVector2[]
                            {
                                new IntVector2(1,2),
                                new IntVector2(2,4),
                                new IntVector2(3,3)
                            }
                        },
                        prefab = assetMan.Get<Structure_KeycardDoors>("Structure_KeycardDoors")
                    });
                    break;
            }

            toModify.forcedStructures = structures.ToArray();
        }

        public bool ShouldGeneratePrisonType(string levelName, int levelId, SceneObject scene)
        {
            if (levelName != "F4" && levelName != "F5") return false; // we dont want to add this type to anything we dont want to
            return true;
        }
        void PrisonLevelTypeCreator(string levelName, int levelId, SceneObject scene)
        {
            if (!ShouldGeneratePrisonType(levelName, levelId, scene)) return;
            CustomLevelObject[] supportedObjects = scene.GetCustomLevelObjects();
            CustomLevelObject factoryLevel = supportedObjects.First(x => x.type == LevelType.Factory);
            if (factoryLevel == null) return;
            CustomLevelObject prisonCopy = factoryLevel.MakeClone();
            prisonCopy.type = prisonType;
            prisonCopy.name = prisonCopy.name.Replace("(Clone)", "").Replace("Factory", "Prison");
            List<StructureWithParameters> structures = prisonCopy.forcedStructures.ToList();
            structures.RemoveAll(x => x.prefab is Structure_Rotohalls);
            structures.RemoveAll(x => x.prefab is Structure_ConveyorBelt);
            structures.RemoveAll(x => x.prefab.name == "LockdownDoorConstructor");
            structures.RemoveAll(x => x.prefab is Structure_LevelBox);
            prisonCopy.forcedStructures = structures.ToArray();
            prisonCopy.potentialSpecialRooms = new WeightedRoomAsset[0];
            prisonCopy.minSpecialRooms = 0;
            prisonCopy.maxSpecialRooms = 0;
            ModifyIntoPrison(prisonCopy, levelId);
            scene.randomizedLevelObject = scene.randomizedLevelObject.AddToArray(new WeightedLevelObject()
            {
                selection = prisonCopy,
                weight = 90
            });
        }
        void GeneratorModifications(string levelName, int levelId, SceneObject scene)
        {
            CustomLevelObject[] objects = scene.GetCustomLevelObjects();
            scene.MarkAsNeverUnload();

            // not level object specific
            switch (levelName)
            {
                case "F2":
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
                    break;
                case "F3":
                    if (!youtuberModeEnabled.Value)
                    {
                        scene.potentialNPCs.Add(new WeightedNPC()
                        {
                            selection = assetMan.Get<NPC>("Dealer"),
                            weight = 45
                        });
                    }
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
                    break;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                CustomLevelObject obj = objects[i];
                if ((levelId > 0) && (obj.type == LevelType.Schoolhouse || obj.type == LevelType.Maintenance || obj.type == prisonType))
                {
                    obj.forcedItems.Add(assetMan.Get<ItemObject>("IOUDecoy"));
                }

                switch (levelName)
                {
                    case "F1":
                        obj.potentialItems = obj.potentialItems.AddRangeToArray(new WeightedItemObject[]
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
                        obj.potentialItems = obj.potentialItems.AddRangeToArray(new WeightedItemObject[]
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
                        /*
                        if (obj.type == LevelType.Schoolhouse)
                        {
                            obj.potentialStructures = obj.potentialStructures.AddToArray(new WeightedStructureWithParameters()
                            {
                                selection = new StructureWithParameters()
                                {
                                    parameters = new StructureParameters()
                                    {
                                        minMax = new IntVector2[]
                                        {
                                        new IntVector2(1,3),
                                        new IntVector2(4,8)
                                        }
                                    },
                                    prefab = assetMan.Get<Structure_Scanner>("scanner")
                                },
                                weight = 60
                            });
                        }*/
                        break;
                    case "F3":
                        obj.potentialItems = obj.potentialItems.AddRangeToArray(new WeightedItemObject[]
                        {
                            new WeightedItemObject()
                            {
                                selection = assetMan.Get<ItemObject>("Crowbar"),
                                weight = 65
                            },
                            new WeightedItemObject()
                            {
                                selection = assetMan.Get<ItemObject>("Mask"),
                                weight = 80
                            }
                        });
                        /*
                        if (obj.type == LevelType.Schoolhouse)
                        {
                            obj.potentialStructures = obj.potentialStructures.AddToArray(new WeightedStructureWithParameters()
                            {
                                selection = new StructureWithParameters()
                                {
                                    parameters = new StructureParameters()
                                    {
                                        minMax = new IntVector2[]
                                        {
                                        new IntVector2(2,6),
                                        new IntVector2(12,16)
                                        }
                                    },
                                    prefab = assetMan.Get<Structure_Scanner>("scanner")
                                },
                                weight = 80
                            });
                        }*/
                        break;
                    case "F4":
                        obj.potentialItems = obj.potentialItems.AddRangeToArray(new WeightedItemObject[]
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
                        break;
                    case "F5":
                        obj.potentialItems = obj.potentialItems.AddRangeToArray(new WeightedItemObject[]
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
                        break;
                    default:
                        return;
                }
                objects[i].MarkAsNeverUnload();
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
            LoadingEvents.RegisterOnAssetsLoaded(Info, ResourcesLoadedPost(), true);
            AssetLoader.LocalizationFromMod(this);
            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorModifications);
            GeneratorManagement.Register(this, GenerationModType.Preparation, PrisonLevelTypeCreator);

            youtuberModeEnabled = Config.Bind<bool>("General", "Youtuber Mode", false, "If true, Dealer will always appear on Floor 2.");

            ModdedSaveGame.AddSaveHandler(new CriminalPackSaveIO());
            CriminalPackPlugin.Log = this.Logger;
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
