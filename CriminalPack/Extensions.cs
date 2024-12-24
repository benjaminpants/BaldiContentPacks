using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CriminalPack
{
    public static class CriminalExtensions
    {

        public static void RemoveChunkProperly(this Cell cell)
        {
            if (cell.HasChunk)
            {
                cell.Chunk.Cells.Remove(cell);
            }
            cell.RemoveChunk();
        }

        public static RoomController GetPickaxeRoom(this EnvironmentController ec)
        {
            if (ec.gameObject.GetComponent<PickaxeRoomContainer>())
            {
                return ec.gameObject.GetComponent<PickaxeRoomContainer>().room;
            }
            PickaxeRoomContainer container = ec.gameObject.AddComponent<PickaxeRoomContainer>();
            container.room = Object.Instantiate<RoomController>(CriminalPackPlugin.Instance.assetMan.Get<RoomController>("Room Controller"), ec.transform); //get the room controller prefab
            container.room.name = "Pickaxe Dug Room";
            container.room.ec = ec;
            container.room.category = RoomCategory.Special;
            container.room.type = RoomType.Hall;
            container.room.color = new Color(0.1f, 0.1f, 0f);
            container.room.florTex = CriminalPackPlugin.Instance.assetMan.Get<Texture2D>("Dirt");
            container.room.wallTex = container.room.florTex;
            container.room.ceilTex = container.room.wallTex;
            container.room.GenerateTextureAtlas();
            return container.room;
        }

        class PickaxeRoomContainer : MonoBehaviour
        {
            public RoomController room;
        }
    }
}
