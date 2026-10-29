using UnityEngine;

namespace CPS.KitchenCatch
{
    public sealed class LevelEnvironment : MonoBehaviour
    {
        [SerializeField] private GameSessionController controller;
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private Sprite kitchenSprite;
        [SerializeField] private Sprite daySprite;
        [SerializeField] private Sprite nightSprite;
        [SerializeField] private Color kitchenTint = new Color(.91f,.88f,.84f,1);
        [SerializeField] private GameObject kitchenAmbient;
        [SerializeField] private GameObject cafeAmbient;
        [SerializeField] private Transform[] swayingProps;
        [SerializeField] private SpriteRenderer[] cafeProps;
        [SerializeField] private SpriteRenderer pendant;
        private float seconds;
        private bool night;
        private MainMenuUI menu;

        private void Awake() => Apply(DifficultyId.Easy);
        private void OnEnable()
        {
            if (controller != null) controller.RoundStarting += Apply;
            else
            {
                menu = FindFirstObjectByType<MainMenuUI>();
                if (menu != null)
                {
                    menu.DifficultyChanged += Apply;
                    Apply(menu.SelectedDifficulty);
                }
            }
        }
        private void OnDisable()
        {
            if (controller != null) controller.RoundStarting -= Apply;
            if (menu != null) menu.DifficultyChanged -= Apply;
        }
        public void Apply(DifficultyId difficulty)
        {
            bool cafe = difficulty == DifficultyId.Normal || difficulty == DifficultyId.Hard;
            night = difficulty == DifficultyId.Hard;
            seconds = 0;
            if (background != null)
            {
                background.sprite = cafe ? (night ? nightSprite : daySprite) : kitchenSprite;
                background.color = cafe ? Color.white : kitchenTint;
            }
            if (kitchenAmbient != null) kitchenAmbient.SetActive(!cafe);
            if (cafeAmbient != null) cafeAmbient.SetActive(cafe);
            if (cafeProps != null)
                foreach (var prop in cafeProps)
                    if (prop != null) prop.color = night ? new Color(.76f,.72f,.69f,1) : Color.white;
            if (pendant != null) pendant.color = night ? new Color(1,.88f,.69f,1) : Color.white;
            if (swayingProps != null)
                foreach (var prop in swayingProps)
                    if (prop != null) prop.localRotation = Quaternion.identity;
        }
        private void Update() => StepAmbient(Time.unscaledDeltaTime);
        public void StepAmbient(float elapsed)
        {
            if (!float.IsFinite(elapsed) || elapsed < 0 || cafeAmbient == null || !cafeAmbient.activeSelf) return;
            seconds += elapsed;
            if (swayingProps != null)
                for (int i=0;i<swayingProps.Length;i++)
                    if (swayingProps[i] != null)
                        swayingProps[i].localRotation = Quaternion.Euler(0,0,Mathf.Sin(seconds*(.65f+i*.17f)+i)*1.5f);
            if (night && pendant != null)
            {
                float light=.98f+.02f*Mathf.Sin(seconds*.7f);
                pendant.color=new Color(light,.88f*light,.69f*light,1);
            }
        }
    }
}
