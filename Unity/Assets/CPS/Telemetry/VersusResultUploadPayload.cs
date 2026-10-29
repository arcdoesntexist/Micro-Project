using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CPS.KitchenCatch
{
    public sealed class VersusResultUploadPayload
    {
        public string MatchId { get; private set; }
        public JObject Data { get; private set; }

        public static VersusResultUploadPayload Create(string matchId, VersusMatchSetup setup,
            VersusMatchResult result, InputStatistics first, InputStatistics second,
            string firstSource = "Keyboard P1", string secondSource = "Keyboard P2")
        {
            if (string.IsNullOrWhiteSpace(matchId) || setup == null || result == null)
                throw new ArgumentException("A completed match and its setup are required.");
            return new VersusResultUploadPayload
            {
                MatchId = matchId,
                Data = new JObject
                {
                    ["schemaVersion"] = 1,
                    ["mode"] = "multiplayer",
                    ["matchId"] = matchId,
                    ["winnerSlot"] = result.Winner.ToString(),
                    ["endReason"] = "PlayerDefeated",
                    ["endedAtUtc"] = DateTime.UtcNow.ToString("O"),
                    ["playedSeconds"] = result.PlayedSeconds,
                    ["players"] = new JArray(Player("P1", setup.Player1, result.Player1, first, firstSource),
                        Player("P2", setup.Player2, result.Player2, second, secondSource))
                }
            };
        }

        private static JObject Player(string slot, VersusPlayerSetup setup, VersusPlayerResult result, InputStatistics stats, string source)
        {
            var data = JObject.FromObject(ResultUploadPayload.From(new SessionResult
            {
                PlayerId = setup.PlayerId, DisplayName = setup.DisplayName,
                Score = result.Score, RemainingHp = result.RemainingHp,
                FoodCaught = result.FoodCaught, FoodMissed = result.FoodMissed,
                TrashEaten = result.TrashEaten, MaxCombo = result.MaxCombo,
                HighestRank = result.HighestRank, InputSource = source,
                InputStatistics = stats
            }));
            foreach (string key in new[] { "schemaVersion", "sessionId", "difficulty", "endReason", "endedAtUtc", "playedSeconds" })
                data.Remove(key);
            data["playerSlot"] = slot;
            data["hatColorHex"] = setup.HatColorHex;
            data["shoeColorHex"] = setup.ShoeColorHex;
            data["caughtItems"] = new JArray(result.CaughtItems);
            return data;
        }

        public string ToJson() => Data.ToString(Formatting.None);
    }
}
