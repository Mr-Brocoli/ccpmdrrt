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

            public uint XPOS = 0x184;
            public uint YPOS = 0x186;

            public Pokemon(uint baseAddress)
            {
                baseAddr = baseAddress;
                HP_ADDR += baseAddr;
                MAXHP_ADDR += baseAddr;
                SPEEDUP_ADDR += baseAddr;
                SLOWDOWN_ADDR += baseAddr;
                XPOS += baseAddr;
                YPOS += baseAddr;
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


        /*struct unkStruct_806B7F8
            {
                u16 species;
                u8 unk2;
                u32 unk4;
                u16 level;
                u8 fillA[0xC - 0xA];
                struct Position pos;
                u8 unk10;
            };*/

        private bool spawnPokemon(ushort pID, ushort x, ushort y)
        {

            Connector.Read16(one.XPOS, out x);
            Connector.Read16(one.YPOS, out y);
            y -= 1;

            uint injectsite = 0x02003af4 + 0xc0;

            //if (!Connector.Read16(injectsite + 0xa, out ushort latch) || latch != 0)
            //    return false;

            bool check = Connector.Write16(injectsite, pID) && Connector.Write16(injectsite + 2, 0)
                && Connector.Write32(injectsite + 4, 0) && Connector.Write16(injectsite + 8, 1)
                && Connector.Write16(injectsite + 0xa, 1) && Connector.Write16(injectsite + 0xc, x)
                && Connector.Write16(injectsite + 0xe, y) && Connector.Write8(injectsite + 0x11, 0);

            if (!check)
                return false;

            byte[] mold = new byte[] { 0xc0, 0x22, 0x80, 0x18, 0x0a, 0x23, 0xc4, 0x5e, 0x00, 0x22, 0xc2, 0x52, 0xc0, 0x46, 0xc0, 0x46, 0x00, 0x21, 0xc0, 0x46, 0x00, 0x2c, 0x68, 0xd0, 0x26, 0xf0, 0x1e, 0xff };
            for (uint i = 0; i != mold.Length; i++)
            {
                uint j = (i / 4) * 4;
                uint k = i % 4;
                if (!Connector.Write8(0x080449a0 + j + k, mold[i]))
                    return false;
            }

            //Connector.Write16(0x080449a0, 0xc046);

            //byte[] old = new byte[] { 0xcc, 0x22, 0xd2, 0x00, 0x80, 0x18, 0x00, 0x23, 0xc0, 0x5e, 0x40, 0x00, 0x32, 0x31, 0x40, 0x18, 0x00, 0x21, 0x40, 0x5e, 0x00, 0x28, 0x68, 0xd0, 0x2d, 0xf0, 0xc6, 0xf8 };

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
                    new Effect("Spawn test", "spawntest"){Price=10},
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
                case "spawntest":
                    TryEffect(request, () => true, () => spawnPokemon(0x17c, 6, 6));
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