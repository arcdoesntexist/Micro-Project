using System;
using UnityEngine;

namespace CPS.KitchenCatch
{
    public sealed class AppSession : MonoBehaviour
    {
        public static AppSession Instance { get; private set; }
        public ProfileStore Store { get; private set; }
        public PlayerProfile SelectedProfile { get; private set; }
        public DifficultyId SelectedDifficulty { get; private set; } = DifficultyId.Easy;
        public GameMode SelectedGameMode { get; private set; } = GameMode.SinglePlayer;
        public VersusMatchSetup VersusSetup { get; private set; }

        public void Initialize(ProfileStore store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (Store != null) throw new InvalidOperationException("AppSession is already initialized.");
            Store = store;
            Store.Load();
        }

        public void Select(string playerId, DifficultyId difficulty)
        {
            if (!Enum.IsDefined(typeof(DifficultyId), difficulty))
                throw new ArgumentException("Unknown difficulty.", nameof(difficulty));
            if (Store == null) throw new InvalidOperationException("AppSession is not initialized.");
            PlayerProfile selected = null;
            foreach (var profile in Store.Profiles)
                if (profile.PlayerId == playerId) { selected = profile; break; }
            if (selected == null) throw new ArgumentException("Unknown player ID.", nameof(playerId));
            SelectedProfile = selected;
            SelectedDifficulty = difficulty;
            SelectedGameMode = GameMode.SinglePlayer;
            VersusSetup = null;
        }

        public void ConfigureVersus(string p1Id, string p2Id, string p1HatColorHex,
            string p1ShoeColorHex, string p2HatColorHex, string p2ShoeColorHex)
        {
            if (Store == null) throw new InvalidOperationException("AppSession is not initialized.");
            if (string.IsNullOrWhiteSpace(p1Id))
                throw new ArgumentException("Player 1 profile is required.", nameof(p1Id));
            if (string.IsNullOrWhiteSpace(p2Id))
                throw new ArgumentException("Player 2 profile is required.", nameof(p2Id));
            if (string.Equals(p1Id, p2Id, StringComparison.Ordinal))
                throw new ArgumentException("Versus players must use distinct profile IDs.", nameof(p2Id));

            var player1 = FindProfile(p1Id);
            var player2 = FindProfile(p2Id);
            if (player1 == null) throw new ArgumentException("Unknown player 1 ID.", nameof(p1Id));
            if (player2 == null) throw new ArgumentException("Unknown player 2 ID.", nameof(p2Id));

            var setup = new VersusMatchSetup(
                new VersusPlayerSetup(player1.PlayerId, player1.DisplayName,
                    VersusMatchSetup.NormalizeColor(p1HatColorHex, nameof(p1HatColorHex)),
                    VersusMatchSetup.NormalizeColor(p1ShoeColorHex, nameof(p1ShoeColorHex))),
                new VersusPlayerSetup(player2.PlayerId, player2.DisplayName,
                    VersusMatchSetup.NormalizeColor(p2HatColorHex, nameof(p2HatColorHex)),
                    VersusMatchSetup.NormalizeColor(p2ShoeColorHex, nameof(p2ShoeColorHex))));

            VersusSetup = setup;
            SelectedProfile = null;
            SelectedGameMode = GameMode.Versus;
        }

        public void ClearSelection()
        {
            SelectedProfile = null;
            VersusSetup = null;
            SelectedGameMode = GameMode.SinglePlayer;
        }

        private PlayerProfile FindProfile(string playerId)
        {
            foreach (var profile in Store.Profiles)
                if (string.Equals(profile.PlayerId, playerId, StringComparison.Ordinal)) return profile;
            return null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (Store == null) Initialize(new ProfileStore());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
