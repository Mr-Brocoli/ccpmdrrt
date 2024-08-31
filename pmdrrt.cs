using System;
using System.Collections.Generic;
//using System.Runtime.Remoting.Metadata.W3cXsd2001;
using CrowdControl.Common;
using JetBrains.Annotations;
using ConnectorType = CrowdControl.Common.ConnectorType;

namespace CrowdControl.Games.Packs
{
    [UsedImplicitly]
    public class PokemonMysterDungeonRedRescueTeam : GBAEffectPack
    {

        public PokemonMysterDungeonRedRescueTeam([NotNull] UserRecord player, [NotNull] Func<CrowdControlBlock, bool> responseHandler, [NotNull] Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler)
        {
        }


        public class Pokemon
        {
            private uint baseAddr;

            public uint HP_ADDR = 0xe;
            public ushort REMEMBER_HP;

            public uint MAXHP_ADDR = 0x10;

            public uint SPEEDUP_ADDR = 0x108;
            public uint SLOWDOWN_ADDR = 0x10D;

            public Pokemon(uint baseAddress)
            {
                baseAddr = baseAddress;
                HP_ADDR += baseAddr;
                MAXHP_ADDR += baseAddr;
                SPEEDUP_ADDR += baseAddr;
                SLOWDOWN_ADDR += baseAddr;
            }


        }

        public bool canSpeedOrSlow(Pokemon p)
        {
            for (uint i = 0; i != 10; i++)
            {
                if (!Connector.Read8(p.SPEEDUP_ADDR + i, out byte zeroCheck))
                    return false;
                if (zeroCheck != 0)
                    return false;

            }
            return true;
        }


        private Pokemon one = new Pokemon(0x02004190);


        public override EffectList Effects
        {
            get
            {
                List<Effect> effects = new List<Effect>
                {
                    new Effect("OHKO", "ohko"){Price=10, Duration=10},
                    new Effect("Speedup (50 movements)", "speedup_50"){Price=10},
                    new Effect("Slowdown (50 movements)", "slowdown_50"){Price=10},
                };
                return effects;
            }
        }

        public override Game Game { get; } = new Game(999, "Pokemon Red Mystery Dungeon", "Pokemon Red Mystery Dungeon", "GBA", ConnectorType.GBAConnector);
        protected override bool IsReady(EffectRequest request) => true;

        protected override void StartEffect(EffectRequest request)
        {
            if (!IsReady(request))
            {
                DelayEffect(request, TimeSpan.FromSeconds(5));
                return;
            }
            string[] codeParams = request.EffectID.Split('_');
            switch (codeParams[0])
            {
                case "ohko":
                    StartTimed(request, () => true, () => Connector.Read16(one.HP_ADDR, out one.REMEMBER_HP) &&  Connector.Write16(one.HP_ADDR, 1));
                    return;
                case "speedup":
                    TryEffect(request, () => canSpeedOrSlow(one), () => Connector.Write8(one.SPEEDUP_ADDR, byte.Parse(codeParams[1])));
                    return;
                case "slowdown":
                    TryEffect(request, () => canSpeedOrSlow(one), () => Connector.Write8(one.SLOWDOWN_ADDR, byte.Parse(codeParams[1])));
                    return;
            }
        }
        protected override bool StopEffect(EffectRequest request)
        {
            string[] codeParams = request.EffectID.Split('_');
            switch (codeParams[0])
            {
                case "ohko":
                    return Connector.Write16(one.HP_ADDR, one.REMEMBER_HP);
                default:
                    return true;
            }
        }
    }
}