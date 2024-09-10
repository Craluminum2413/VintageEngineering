﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VintageEngineering.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace VintageEngineering
{
    public class BECrudeOilWell : BlockEntity, IOilWell
    {
        private ICoreServerAPI sapi;
        private long _fluidportions;        
        public long RemainingPortions => _fluidportions;

        public long MaxPPS => (long)base.Block.Attributes["portionpersecond"].AsDouble(50);

        public string OilBlockCode => base.Block.Attributes["oilblockcode"].AsString("vinteng:crudeoil");

        public string OilPortionCode => base.Block.Attributes["oilportioncode"].AsString("vinteng:crudeoilportion");

        public int TricklePortions => base.Block.Attributes["trickleportions"].AsInt(100);

        public bool CanBeInfinite => base.Block.Attributes["canbeinfinite"].AsBool(true);

        public long MaxDepositBlocks => (long)base.Block.Attributes["maxdepositblocks"].AsDouble(200000);

        public long MinDepositBlocks => (long)base.Block.Attributes["mindepositblocks"].AsDouble(1500);

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Server) sapi = api as ICoreServerAPI;
            _fluidportions = 0;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            // this is called on the client, while the values are on the server, need to push values to client
            Item portion = Api.World.GetItem(new AssetLocation(OilPortionCode));
            int perliter = 100;
            if (portion != null) 
            {
                ItemStack portionstack = new ItemStack(portion);
                WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(portionstack);
                if (props != null)
                {
                    perliter = (int)props.ItemsPerLitre;
                }
            }
            if (RemainingPortions > 0) dsc.AppendLine($"{RemainingPortions / perliter} Litres Remaining");
            else 
            {
                if (CanBeInfinite) dsc.AppendLine($"Well Depleted, infinite source.");
                else dsc.AppendLine("Well Depleted");
            }
        }

        public void InitDeposit(bool isLarge, ICoreAPI api)
        {
            if (this.Api == null) Initialize(api);
            if (sapi == null) return; // this should never trigger
                                                  
            long numblocks = sapi.World.Rand.NextInt64(MinDepositBlocks, MaxDepositBlocks+1);
            if (isLarge) numblocks *= 2;
            try
            {
                AssetLocation portion = new AssetLocation(OilPortionCode);
                ItemStack portionstack = new ItemStack(sapi.World.GetItem(portion));
                WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(portionstack);
                if (props != null)
                {
                    _fluidportions = 1000 * numblocks * (int)props.ItemsPerLitre;
                }
                else
                {
                    _fluidportions = 1000 * numblocks * 100;
                }
            }
            catch (Exception ex)
            {
                sapi.Logger.Error(ex);
            }
            MarkDirty(true);
        }

        public long PumpTick(float dt)
        {
            if (Api.Side == EnumAppSide.Client) return -1;
            long amount = 0;
            if (_fluidportions > 0)
            {
                amount = (long)(MaxPPS * dt);
                if (amount > _fluidportions) amount = _fluidportions;
                _fluidportions -= amount;                
            }
            else
            {                
                amount = (long)(TricklePortions * dt);
                if (!CanBeInfinite) amount = 0;
            }
            return amount;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            _fluidportions = tree.GetLong("fluidleft", 0);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetLong("fluidleft", _fluidportions);
        }
    }
}
