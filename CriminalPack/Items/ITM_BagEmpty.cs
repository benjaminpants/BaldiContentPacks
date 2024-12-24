using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace CriminalPack
{
    public class ITM_BagEmpty : ITM_DealerBag
    {
        public override bool Use(PlayerManager pm)
        {
            bool v = base.Use(pm);
            toDrop = ItemMetaStorage.Instance.FindByEnum(Items.None).value;
            return v;
        }
    }
}
