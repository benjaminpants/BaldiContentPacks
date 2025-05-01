using System;
using System.Collections.Generic;
using System.Text;
using MTM101BaldAPI;
using MTM101BaldAPI.PlusExtensions;

namespace CriminalPack
{
    public class JailDoorRoomFunction : RoomFunction
    {
        public StandardDoorMats doorMat;

        public override void OnGenerationFinished()
        {
            base.OnGenerationFinished();
            foreach (Door door in room.doors)
            {
                if (door is StandardDoor)
                {
                    ((StandardDoor)door).ApplyDoorMaterials(doorMat);
                }
            }
        }
    }
}
