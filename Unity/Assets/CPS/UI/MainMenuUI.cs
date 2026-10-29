using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameSessionController gameplayTemplate;
        [SerializeField] private SceneNavigator navigator;
        [SerializeField] private Font licensedFont;
        [SerializeField] private InputField nicknameField;
        [SerializeField] private Dropdown profileDropdown;
        [SerializeField] private Text status;
        [SerializeField] private Button startButton;
        [SerializeField] private Button createButton;
        [SerializeField] private GameObject createProfilePanel;
        [SerializeField] private Button openCreateButton;
        [SerializeField] private Button cancelCreateButton;
        [SerializeField] private Text createStatus;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private AudioSettingsPanel settingsPanel;
        [SerializeField] private ControllerPanel controllerPanel;
        [SerializeField] private Button controllerButton;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject[] howToPlayObjects;
        [SerializeField] private GameObject profileManagerPanel;
        [SerializeField] private InputField renameField;
        [SerializeField] private GameObject deleteConfirmation;
        [SerializeField] private Text deleteConfirmationText;
        [SerializeField] private Text profileManagerStatus;
        [SerializeField] private Transform profileListContent;
        [SerializeField] private Button profileRowTemplate;
        [SerializeField] private Button manageProfilesButton;
        [SerializeField] private Button saveNameButton;
        [SerializeField] private Button useProfileButton;
        [SerializeField] private Button deleteProfileButton;
        [SerializeField] private Button backProfilesButton;
        [SerializeField] private Button confirmDeleteButton;
        [SerializeField] private Button cancelDeleteButton;
        private readonly List<string> profileIds = new List<string>();
        private readonly List<GameObject> profileRows = new List<GameObject>();
        private DifficultyId difficulty = DifficultyId.Easy;
        private string localWarning;
        private string managedProfileId;
        public DifficultyId SelectedDifficulty => difficulty;
        public event Action<DifficultyId> DifficultyChanged;

        public string ValidationError
        {
            get
            {
                if (gameplayTemplate == null) return "Gameplay template is missing.";
                if (navigator == null) return "Scene navigation is missing.";
                return gameplayTemplate.GetValidationError(AppSession.Instance);
            }
        }

        private void OnEnable()
        {
            if (nicknameField != null) nicknameField.characterLimit = 0;
            foreach (var label in GetComponentsInChildren<Text>(true))
            {
                label.supportRichText = false;
                if (licensedFont != null) label.font = licensedFont;
            }
            if (profileDropdown != null) profileDropdown.onValueChanged.AddListener(SelectProfile);
            if (createButton != null) createButton.onClick.AddListener(CreateFromButton);
            if (openCreateButton != null) openCreateButton.onClick.AddListener(ShowCreateProfile);
            if (cancelCreateButton != null) cancelCreateButton.onClick.AddListener(HideCreateProfile);
            if (startButton != null) startButton.onClick.AddListener(StartFromButton);
            if (easyButton != null) easyButton.onClick.AddListener(SelectEasy);
            if (normalButton != null) normalButton.onClick.AddListener(SelectNormal);
            if (hardButton != null) hardButton.onClick.AddListener(SelectHard);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (controllerButton != null) controllerButton.onClick.AddListener(OpenController);
            if (manageProfilesButton != null) manageProfilesButton.onClick.AddListener(ShowProfileManager);
            if (saveNameButton != null) saveNameButton.onClick.AddListener(RenameFromButton);
            if (useProfileButton != null) useProfileButton.onClick.AddListener(UseManagedProfile);
            if (deleteProfileButton != null) deleteProfileButton.onClick.AddListener(RequestDeleteManagedProfile);
            if (backProfilesButton != null) backProfilesButton.onClick.AddListener(HideProfileManager);
            if (confirmDeleteButton != null) confirmDeleteButton.onClick.AddListener(DeleteFromButton);
            if (cancelDeleteButton != null) cancelDeleteButton.onClick.AddListener(CancelDeleteManagedProfile);
            UpdateDifficultyButtons();
            RefreshProfiles();
        }

        private void OnDisable()
        {
            if (profileDropdown != null) profileDropdown.onValueChanged.RemoveListener(SelectProfile);
            if (createButton != null) createButton.onClick.RemoveListener(CreateFromButton);
            if (openCreateButton != null) openCreateButton.onClick.RemoveListener(ShowCreateProfile);
            if (cancelCreateButton != null) cancelCreateButton.onClick.RemoveListener(HideCreateProfile);
            if (startButton != null) startButton.onClick.RemoveListener(StartFromButton);
            if (easyButton != null) easyButton.onClick.RemoveListener(SelectEasy);
            if (normalButton != null) normalButton.onClick.RemoveListener(SelectNormal);
            if (hardButton != null) hardButton.onClick.RemoveListener(SelectHard);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (controllerButton != null) controllerButton.onClick.RemoveListener(OpenController);
            if (manageProfilesButton != null) manageProfilesButton.onClick.RemoveListener(ShowProfileManager);
            if (saveNameButton != null) saveNameButton.onClick.RemoveListener(RenameFromButton);
            if (useProfileButton != null) useProfileButton.onClick.RemoveListener(UseManagedProfile);
            if (deleteProfileButton != null) deleteProfileButton.onClick.RemoveListener(RequestDeleteManagedProfile);
            if (backProfilesButton != null) backProfilesButton.onClick.RemoveListener(HideProfileManager);
            if (confirmDeleteButton != null) confirmDeleteButton.onClick.RemoveListener(DeleteFromButton);
            if (cancelDeleteButton != null) cancelDeleteButton.onClick.RemoveListener(CancelDeleteManagedProfile);
        }

        private void Start()
        {
            SelectControl(openCreateButton != null ? openCreateButton : nicknameField);
        }

        public void ShowCreateProfile()
        {
            HideProfileManager();
            localWarning = null;
            if (createStatus != null) createStatus.text = "Nickname · 1–24 characters";
            if (createProfilePanel != null) createProfilePanel.SetActive(true);
            SelectControl(nicknameField);
        }

        public void HideCreateProfile()
        {
            if (createProfilePanel != null) createProfilePanel.SetActive(false);
            SelectControl(openCreateButton);
        }

        public void RefreshProfiles()
        {
            var app = AppSession.Instance;
            if (profileDropdown == null || app?.Store == null) return;
            difficulty = app.SelectedProfile == null ? difficulty : app.SelectedDifficulty;
            UpdateDifficultyButtons();
            profileIds.Clear();
            var options = new List<Dropdown.OptionData>();
            int selected = 0;
            foreach (var profile in app.Store.Profiles)
            {
                if (app.SelectedProfile?.PlayerId == profile.PlayerId) selected = profileIds.Count;
                profileIds.Add(profile.PlayerId);
                options.Add(new Dropdown.OptionData(profile.DisplayName + " · " + profile.PlayerId.Substring(0, Math.Min(6, profile.PlayerId.Length))));
            }
            if (options.Count == 0) options.Add(new Dropdown.OptionData("No profiles yet"));
            profileDropdown.ClearOptions();
            profileDropdown.AddOptions(options);
            profileDropdown.SetValueWithoutNotify(selected);
            if (app.SelectedProfile == null && profileIds.Count > 0)
                app.Select(profileIds[selected], difficulty);
            profileDropdown.interactable = profileIds.Count > 0;
            if (profileDropdown.captionText != null) profileDropdown.captionText.supportRichText = false;
            if (profileDropdown.itemText != null) profileDropdown.itemText.supportRichText = false;
            profileDropdown.RefreshShownValue();
            RefreshStatus();
        }

        public bool CreateProfile()
        {
            var app = AppSession.Instance;
            if (app?.Store == null || nicknameField == null) return false;
            nicknameField.characterLimit = 0;
            try
            {
                var profile = app.Store.Create(nicknameField.text);
                app.Select(profile.PlayerId, difficulty);
                localWarning = null;
                RefreshProfiles();
                return true;
            }
            catch (ArgumentException exception)
            {
                localWarning = exception.Message;
                if (createStatus != null) createStatus.text = "Use a nickname with 1 to 24 characters.";
                RefreshStatus();
                return false;
            }
        }

        public void ShowProfileManager()
        {
            var app = AppSession.Instance;
            if (app?.SelectedProfile != null) difficulty = app.SelectedDifficulty;
            SetHowToPlayVisible(false);
            if (profileManagerPanel != null) profileManagerPanel.SetActive(true);
            if (deleteConfirmation != null) deleteConfirmation.SetActive(false);
            var profiles = app?.Store?.Profiles;
            managedProfileId = app?.SelectedProfile?.PlayerId;
            if (managedProfileId == null && profiles != null && profiles.Count > 0)
                managedProfileId = profiles[0].PlayerId;
            RefreshProfileManager();
            SelectControl(renameField);
        }

        public void HideProfileManager()
        {
            if (profileManagerPanel != null) profileManagerPanel.SetActive(false);
            SetHowToPlayVisible(true);
            if (deleteConfirmation != null) deleteConfirmation.SetActive(false);
            SelectControl(manageProfilesButton);
        }

        public void SelectManagedProfile(string playerId)
        {
            var profile = FindProfile(playerId);
            if (profile == null) throw new ArgumentException("Unknown player ID.", nameof(playerId));
            managedProfileId = profile.PlayerId;
            localWarning = null;
            RefreshProfileManager();
        }

        public bool RenameManagedProfile()
        {
            var app = AppSession.Instance;
            if (app?.Store == null || renameField == null || managedProfileId == null) return false;
            try
            {
                if (!app.Store.Rename(managedProfileId, renameField.text))
                {
                    localWarning = app.Store.Warning;
                    RefreshProfileManager();
                    return false;
                }
                if (app.SelectedProfile?.PlayerId == managedProfileId)
                    app.Select(managedProfileId, difficulty);
                localWarning = null;
                RefreshProfiles();
                RefreshProfileManager();
                return true;
            }
            catch (ArgumentException exception)
            {
                localWarning = exception.Message;
                RefreshProfileManager();
                return false;
            }
        }

        public void UseManagedProfile()
        {
            var app = AppSession.Instance;
            if (managedProfileId == null || FindProfile(managedProfileId) == null) return;
            app.Select(managedProfileId, difficulty);
            localWarning = null;
            RefreshProfiles();
            RefreshProfileManager();
        }

        public void RequestDeleteManagedProfile()
        {
            var profile = FindProfile(managedProfileId);
            if (profile == null) return;
            if (deleteConfirmationText != null)
                deleteConfirmationText.text = "Delete profile “" + profile.DisplayName + "”?";
            if (deleteConfirmation != null) deleteConfirmation.SetActive(true);
            SelectControl(confirmDeleteButton);
        }

        public void CancelDeleteManagedProfile()
        {
            if (deleteConfirmation != null) deleteConfirmation.SetActive(false);
            SelectControl(deleteProfileButton);
        }

        public bool ConfirmDeleteManagedProfile()
        {
            var app = AppSession.Instance;
            if (app?.Store == null || managedProfileId == null) return false;
            int index = -1;
            for (int i = 0; i < app.Store.Profiles.Count; i++)
                if (app.Store.Profiles[i].PlayerId == managedProfileId) { index = i; break; }
            if (index < 0) return false;
            if (!app.Store.Delete(managedProfileId))
            {
                localWarning = app.Store.Warning;
                RefreshProfileManager();
                return false;
            }
            if (app.Store.Profiles.Count == 0)
            {
                managedProfileId = null;
                app.ClearSelection();
            }
            else
            {
                index = Math.Min(index, app.Store.Profiles.Count - 1);
                managedProfileId = app.Store.Profiles[index].PlayerId;
                app.Select(managedProfileId, difficulty);
            }
            localWarning = null;
            if (deleteConfirmation != null) deleteConfirmation.SetActive(false);
            RefreshProfiles();
            RefreshProfileManager();
            SelectControl(managedProfileId == null ? backProfilesButton : renameField);
            return true;
        }

        private PlayerProfile FindProfile(string playerId)
        {
            var profiles = AppSession.Instance?.Store?.Profiles;
            if (profiles == null) return null;
            foreach (var profile in profiles)
                if (profile.PlayerId == playerId) return profile;
            return null;
        }

        private void SetHowToPlayVisible(bool visible)
        {
            if (howToPlayPanel != null) howToPlayPanel.SetActive(visible);
            if (howToPlayObjects == null) return;
            foreach (var item in howToPlayObjects) if (item != null) item.SetActive(visible);
        }

        private static void SelectControl(Selectable control)
        {
            if (control != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(control.gameObject);
        }

        private void RefreshProfileManager()
        {
            var profile = FindProfile(managedProfileId);
            if (renameField != null) renameField.SetTextWithoutNotify(profile?.DisplayName ?? "");
            if (profileManagerStatus != null)
                profileManagerStatus.text = localWarning ?? (profile == null
                    ? "No profiles yet"
                    : profile.DisplayName + " · Played " + profile.PlayCount);
            RefreshProfileRows();
        }

        private void RefreshProfileRows()
        {
            foreach (var row in profileRows) if (row != null) Destroy(row);
            profileRows.Clear();
            var profiles = AppSession.Instance?.Store?.Profiles;
            if (profileListContent == null || profileRowTemplate == null || profiles == null) return;
            foreach (var profile in profiles)
            {
                var row = Instantiate(profileRowTemplate, profileListContent);
                row.gameObject.SetActive(true);
                var label = row.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.supportRichText = false;
                    label.text = profile.DisplayName + " · " + profile.PlayerId.Substring(0, Math.Min(6, profile.PlayerId.Length)) +
                        " · Played " + profile.PlayCount;
                }
                string playerId = profile.PlayerId;
                row.onClick.AddListener(() => SelectManagedProfile(playerId));
                profileRows.Add(row.gameObject);
            }
        }

        public void SelectDifficulty(DifficultyId selected)
        {
            difficulty = selected;
            localWarning = null;
            var app = AppSession.Instance;
            if (app?.SelectedProfile != null) app.Select(app.SelectedProfile.PlayerId, selected);
            UpdateDifficultyButtons();
            RefreshStatus();
        }

        private void UpdateDifficultyButtons()
        {
            SetDifficultyTint(easyButton, difficulty == DifficultyId.Easy);
            SetDifficultyTint(normalButton, difficulty == DifficultyId.Normal);
            SetDifficultyTint(hardButton, difficulty == DifficultyId.Hard);
            DifficultyChanged?.Invoke(difficulty);
        }

        private static void SetDifficultyTint(Button button, bool chosen)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = chosen ? Color.white : new Color(.70f, .70f, .70f);
            colors.highlightedColor = chosen ? Color.white : new Color(.85f, .85f, .85f);
            colors.selectedColor = new Color(.75f, .90f, 1f);
            button.colors = colors;
            var selected = button.transform.Find("Selected");
            if (selected != null) selected.gameObject.SetActive(chosen);
        }

        public bool StartSelectedGame()
        {
            RefreshStatus();
            return ValidationError == null && ControllerInput.EnsureExists().CanPlay(out _) && navigator != null && navigator.GoTo("Gameplay");
        }

        private void SelectProfile(int index)
        {
            var app = AppSession.Instance;
            if (app?.Store == null || index < 0 || index >= profileIds.Count) return;
            app.Select(profileIds[index], difficulty);
            localWarning = null;
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            var app = AppSession.Instance;
            string error = ValidationError;
            bool controllerReady = !Application.isPlaying || ControllerInput.EnsureExists().CanPlay(out _);
            if (startButton != null) startButton.interactable = error == null && controllerReady && navigator != null && !navigator.IsTransitioning;
            if (status != null)
            {
                status.text = localWarning ?? error ?? (!controllerReady ? "Connect controller" : app?.SelectedProfile == null
                    ? "No profiles yet"
                    : app.SelectedProfile.DisplayName + " · " + difficulty);
                if (app?.Store != null && !string.IsNullOrEmpty(app.Store.Warning)) status.text += "\n" + app.Store.Warning;
            }
        }

        private void Update() => RefreshStatus();
        private void CreateFromButton()
        {
            if (CreateProfile()) HideCreateProfile();
        }
        private void StartFromButton()
        {
            MascotPresentation.NotifyUserInteraction();
            StartSelectedGame();
        }
        private void SelectEasy() => SelectDifficulty(DifficultyId.Easy);
        private void SelectNormal() => SelectDifficulty(DifficultyId.Normal);
        private void SelectHard() => SelectDifficulty(DifficultyId.Hard);
        private void OpenSettings() => settingsPanel?.Show();
        private void OpenController() => controllerPanel?.Show();
        private void RenameFromButton() => RenameManagedProfile();
        private void DeleteFromButton() => ConfirmDeleteManagedProfile();
    }
}
