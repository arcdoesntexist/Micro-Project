using System;
using System.Globalization;

namespace CPS.KitchenCatch
{
    [Serializable]
    public sealed class VersusPlayerSetup
    {
        public string PlayerId { get; }
        public string DisplayName { get; }
        public string HatColorHex { get; }
        public string ShoeColorHex { get; }

        internal VersusPlayerSetup(string playerId, string displayName, string hatColorHex, string shoeColorHex)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            HatColorHex = hatColorHex;
            ShoeColorHex = shoeColorHex;
        }

        internal VersusPlayerSetup Copy() =>
            new VersusPlayerSetup(PlayerId, DisplayName, HatColorHex, ShoeColorHex);
    }

    [Serializable]
    public sealed class VersusMatchSetup
    {
        public VersusPlayerSetup Player1 { get; }
        public VersusPlayerSetup Player2 { get; }

        internal VersusMatchSetup(VersusPlayerSetup player1, VersusPlayerSetup player2)
        {
            Player1 = player1 ?? throw new ArgumentNullException(nameof(player1));
            Player2 = player2 ?? throw new ArgumentNullException(nameof(player2));
        }

        public VersusMatchSetup Copy() => new VersusMatchSetup(Player1.Copy(), Player2.Copy());

        internal static string NormalizeColor(string value, string parameterName)
        {
            if (value == null || value.Length != 7 || value[0] != '#')
                throw new ArgumentException("Color must use #RRGGBB.", parameterName);
            for (int index = 1; index < value.Length; index += 2)
                if (!byte.TryParse(value.Substring(index, 2), NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out _))
                    throw new ArgumentException("Color must use #RRGGBB.", parameterName);
            return value.ToUpperInvariant();
        }
    }
}
