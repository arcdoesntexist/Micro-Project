using System;
using Newtonsoft.Json;

namespace CPS.KitchenCatch
{
    [Serializable]
    public sealed class ResultUploadPayload
    {
        public int schemaVersion;
        public string sessionId;
        public string playerId;
        public string displayName;
        public string difficulty;
        public string endReason;
        public string endedAtUtc;
        public float playedSeconds;
        public int score;
        public int remainingHp;
        public int foodCaught;
        public int foodMissed;
        public int trashEaten;
        public int maxCombo;
        public string highestRank;
        public string inputSource;
        public float? leftMaxStrength01;
        public float? leftMeanStrength01;
        public float? leftPressedSeconds;
        public float? leftMaxForceN;
        public float? leftMeanForceN;
        public float? rightMaxStrength01;
        public float? rightMeanStrength01;
        public float? rightPressedSeconds;
        public float? rightMaxForceN;
        public float? rightMeanForceN;
        public float? bothPressedSeconds;
        public float? leftBalance01;

        public static ResultUploadPayload From(SessionResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            InputStatistics statistics = result.InputStatistics;
            return new ResultUploadPayload
            {
                schemaVersion = result.SchemaVersion,
                sessionId = result.SessionId,
                playerId = result.PlayerId,
                displayName = result.DisplayName,
                difficulty = result.Difficulty.ToString(),
                endReason = result.EndReason.ToString(),
                endedAtUtc = result.EndedAtUtc,
                playedSeconds = result.PlayedSeconds,
                score = result.Score,
                remainingHp = result.RemainingHp,
                foodCaught = result.FoodCaught,
                foodMissed = result.FoodMissed,
                trashEaten = result.TrashEaten,
                maxCombo = result.MaxCombo,
                highestRank = result.HighestRank.ToString(),
                inputSource = result.InputSource,
                leftMaxStrength01 = Strength(statistics?.Left, hand => hand.MaxStrength01),
                leftMeanStrength01 = Strength(statistics?.Left, hand => hand.MeanStrength01),
                leftPressedSeconds = statistics?.Left?.PressedDurationSeconds,
                leftMaxForceN = Force(statistics?.Left, hand => hand.MaxForceN),
                leftMeanForceN = Force(statistics?.Left, hand => hand.MeanForceN),
                rightMaxStrength01 = Strength(statistics?.Right, hand => hand.MaxStrength01),
                rightMeanStrength01 = Strength(statistics?.Right, hand => hand.MeanStrength01),
                rightPressedSeconds = statistics?.Right?.PressedDurationSeconds,
                rightMaxForceN = Force(statistics?.Right, hand => hand.MaxForceN),
                rightMeanForceN = Force(statistics?.Right, hand => hand.MeanForceN),
                bothPressedSeconds = statistics?.BothPressedDurationSeconds,
                leftBalance01 = statistics != null && statistics.HasBalance ? statistics.LeftBalance01 : (float?)null
            };
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        private static float? Strength(HandStatistics hand, Func<HandStatistics, float> value)
        {
            return hand != null && hand.HasStrength ? value(hand) : (float?)null;
        }

        private static float? Force(HandStatistics hand, Func<HandStatistics, float> value)
        {
            return hand != null && hand.HasForce ? value(hand) : (float?)null;
        }
    }
}
