using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

namespace CPS.KitchenCatch
{
    public sealed class ResultReferenceView : MonoBehaviour
    {
        public RectTransform summary;
        public RectTransform details;
        public Button toggle;
        public Button close;
        public Vector2 closedPosition;
        public Vector2 openPosition;
        private bool expanded;
        private readonly List<GameObject> hiddenHud = new List<GameObject>();

        private void OnEnable()
        {
            toggle.onClick.AddListener(Toggle); close.onClick.AddListener(Close);
            foreach(string name in new[]{"Player","Timer","Score","Rank badge","Rank callout"})
            {
                var hud=transform.parent.Find(name);
                if(hud!=null && hud.gameObject.activeSelf){hiddenHud.Add(hud.gameObject);hud.gameObject.SetActive(false);}
            }
        }
        private void OnDisable()
        {
            toggle.onClick.RemoveListener(Toggle); close.onClick.RemoveListener(Close);
            foreach(var hud in hiddenHud)if(hud!=null)hud.SetActive(true);
            hiddenHud.Clear();
        }
        private void Update() { if (expanded && Keyboard.current?.escapeKey.wasPressedThisFrame == true) Close(); }
        private static void Set(Transform parent, string name, string value) => parent.Find(name).GetComponent<Text>().text = value;
        public void Toggle() => Expand(!expanded);
        public void Close() => Expand(false);
        private void Expand(bool value)
        {
            var events = EventSystem.current;
            bool restore = !value && events != null && events.currentSelectedGameObject != null &&
                events.currentSelectedGameObject.transform.IsChildOf(details);
            expanded = value;
            details.gameObject.SetActive(value);
            summary.anchoredPosition = value ? openPosition : closedPosition;
            toggle.GetComponentInChildren<Text>().text = value ? "Round details  -" : "Round details  +";
            if (restore) events.SetSelectedGameObject(toggle.gameObject);
        }
        public void Show(SessionResult result)
        {
            foreach(string field in new[]{"Round stats","Left","Right","Balance","Force"})
                details.Find(field).GetComponent<Text>().font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bool easy = result.Difficulty == DifficultyId.Easy;
            Set(summary,"Identity",result.DisplayName);
            Set(summary,"Outcome",result.EndReason == EndReason.Triumph ? "Triumph!" : "HP depleted");
            Set(summary,"Message",result.EndReason == EndReason.Triumph ? "A delicious minute in the kitchen." : "A little spill. Another round?");
            Set(summary,"Score",result.Score.ToString("N0"));
            Set(summary,"Rank",result.HighestRank.ToString());
            string[] names={"Dull Dish","Cookin'","Bold Bite","Appetizing!","Sizzling!","Super Savory!","Supreme Chef!"};
            Set(summary,"Rank name",names[Mathf.Clamp((int)result.HighestRank,0,6)]);
            Color[] rankColors={new Color(.45f,.36f,.29f),new Color(.67f,.29f,.13f),new Color(.71f,.22f,.16f),new Color(.65f,.18f,.33f),new Color(.58f,.35f,.06f),new Color(.57f,.20f,.42f),new Color(.09f,.42f,.35f)};
            summary.Find("Rank badge").GetComponent<Image>().color=rankColors[Mathf.Clamp((int)result.HighestRank,0,6)];
            Set(summary,"Combo","Best streak  " + result.MaxCombo + "  catches in a row");
            for(int i=0;i<3;i++)
            {
                var badge=summary.Find("Difficulty " + i);
                badge.gameObject.SetActive((int)result.Difficulty == i);
            }
            bool known=result.CaughtItems != null && result.CaughtItems.Length == 5;
            int count=0;if(known)foreach(int n in result.CaughtItems)count+=n;
            known &= count == result.FoodCaught + result.TrashEaten;
            for(int i=0;i<5;i++)
                Set(summary,"Count " + i,known ? "x " + result.CaughtItems[i].ToString("N0") : "N/A");
            foreach(string name in new[]{"Trash heading","Trash divider","Item 3","Item 4","Count 3","Count 4","Name 3","Name 4"})
                summary.Find(name).gameObject.SetActive(!easy);
            float actionsY=easy?-235:-310;
            foreach(string name in new[]{"Restart","Menu"})
            {var r=(RectTransform)summary.Find(name);r.anchoredPosition=new Vector2(r.anchoredPosition.x,actionsY*1.2f);}
            var toggleRect=(RectTransform)toggle.transform;
            toggleRect.anchoredPosition=new Vector2(toggleRect.anchoredPosition.x,(easy?-308:-363)*1.2f);
            Set(details,"Round stats",$"Time played                     {result.PlayedSeconds:0.0} s\nHP remaining                     {result.RemainingHp} / 3\nFood caught                         {result.FoodCaught}\nFood missed                        {result.FoodMissed}" +
                (easy ? "" : "\nTrash eaten                         " + result.TrashEaten));
            var stats=result.InputStatistics;
            Set(details,"Input source",result.InputSource == "LoadCell" ? "Load Cell - Estimated Force (N)" :
                result.InputSource == "FSR" ? "FSR - relative press level" : "Keyboard - simulated input");
            Set(details,"Left",Hand("LEFT",stats?.Left));Set(details,"Right",Hand("RIGHT",stats?.Right));
            Set(details,"Balance","Both pressed  " + (stats?.BothPressedDurationSeconds ?? 0).ToString("0.0") + " s\nLeft / right balance  " +
                (stats?.HasBalance == true ? (stats.LeftBalance01*100).ToString("0") + "% / " + ((1-stats.LeftBalance01)*100).ToString("0") + "%" : "N/A"));
            Set(details,"Force",Force(stats,result.InputSource));
            Expand(false);
        }
        private static string Hand(string name,HandStatistics hand) => hand?.HasForce == true && !hand.HasStrength ?
            $"{name}\nAverage   {hand.MeanForceN:0.00} N\nMaximum   {hand.MaxForceN:0.00} N\nPressed   {hand.PressedDurationSeconds:0.0} s" : hand?.HasStrength == true ?
            $"{name}\nAverage   {hand.MeanStrength01*100:0}%\nMaximum   {hand.MaxStrength01*100:0}%\nPressed   {hand.PressedDurationSeconds:0.0} s" : name + "\nAverage   N/A\nMaximum   N/A\nPressed   0.0 s";
        private static string Force(InputStatistics stats,string source)
        {
            if(source == "FSR") return "N/A - not measured in FSR mode";
            if(source == "Keyboard") return "N/A - keyboard input is simulated";
            if(string.IsNullOrEmpty(source)) return "N/A - Sensor not connected";
            string Value(HandStatistics hand,bool maximum) => hand?.HasForce == true ? (maximum ? hand.MaxForceN : hand.MeanForceN).ToString("0.00") : "N/A";
            return "                 LEFT       RIGHT\nAverage     " + Value(stats?.Left,false) + "        " + Value(stats?.Right,false) +
                "\nMaximum    " + Value(stats?.Left,true) + "        " + Value(stats?.Right,true);
        }
    }
}
