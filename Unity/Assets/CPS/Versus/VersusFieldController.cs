using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class VersusFieldController : MonoBehaviour
    {
        private sealed class Drop
        {
            public Image Image;
            public VersusSpawnRecord Record;
            public FallingObjectDefinition Definition;
            public float Y;
        }

        [SerializeField, Min(0.1f)] private float movementSpeed = 7;
        [SerializeField, Range(0, 7.1f)] private float movementLimit = 7.1f;
        [SerializeField] private Vector2 catchHalfExtents = new Vector2(1, .4f);
        private readonly List<Drop> drops = new List<Drop>(32);
        private readonly Stack<Image> pool = new Stack<Image>(32);
        private readonly List<VersusObjectResolution> resolutions = new List<VersusObjectResolution>(8);
        private readonly List<RectTransform> decorations = new List<RectTransform>();
        private readonly List<Vector2> decorationOrigins = new List<Vector2>();
        private VersusMatchController owner;
        private RectTransform field;
        private RectTransform mascot;
        private RectTransform hatRect;
        private Image body;
        private Image hat;
        private Sprite rightHat;
        private Sprite leftHat;
        private float x;
        private float animationSeconds;
        private float reactionSeconds;
        private float ambientSeconds;
        private bool hurt;
        private bool facesRight = true;
        private string shoeColorHex;
        private readonly List<Image> deathPieces = new List<Image>(16);
        public int ActiveCount => drops.Count;
        public float PositionX => x;
        public int MovementDirection { get; private set; }
        private float PixelsPerUnit => field.rect.width / 16;

        public void Initialize(VersusMatchController controller, RectTransform playfield, RectTransform actor,
            Image bodyImage, Image hatImage, VersusPlayerSetup setup)
        {
            owner = controller;
            movementSpeed = owner.MovementSpeed;
            catchHalfExtents = owner.CatchHalfExtents;
            field = playfield;
            mascot = actor;
            body = bodyImage;
            hat = hatImage;
            hatRect = hat.rectTransform;
            owner.GetHat(setup.HatColorHex, out rightHat, out leftHat);
            shoeColorHex = setup.HatColorHex;
            body.material = null;
            SetPose(0);
        }

        public void AddDecoration(RectTransform decoration)
        {
            decorations.Add(decoration);
            decorationOrigins.Add(decoration.anchoredPosition);
        }

        public void Spawn(VersusSpawnRecord record)
        {
            var definition = owner.Definitions[record.Kind == ObjectKind.Food ? record.DefinitionIndex : record.DefinitionIndex + 3];
            Image image;
            if (pool.Count > 0) image = pool.Pop();
            else image = VersusGameplayUI.Picture(field, "Falling item", null, Vector2.zero, Vector2.one);
            image.sprite = definition.sprite;
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = Vector2.one * PixelsPerUnit * .85f;
            image.gameObject.SetActive(true);
            image.transform.SetAsLastSibling();
            var drop = new Drop { Image = image, Definition = definition, Record = record, Y = 4.9f };
            drops.Add(drop);
            Place(drop);
        }

        public IReadOnlyList<VersusObjectResolution> Step(InputSnapshot snapshot, float seconds)
        {
            resolutions.Clear();
            int direction = (snapshot.Right.Valid && snapshot.Right.Pressed ? 1 : 0) -
                (snapshot.Left.Valid && snapshot.Left.Pressed ? 1 : 0);
            x = Mathf.Clamp(x + direction * movementSpeed * seconds, -movementLimit, movementLimit);
            animationSeconds += seconds;
            ambientSeconds += seconds;
            reactionSeconds = Mathf.Max(0, reactionSeconds - seconds);
            SetPose(direction);
            for (int i = 0; i < decorations.Count; i++)
            {
                decorations[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(ambientSeconds * 1.5f + i) * 2);
                decorations[i].anchoredPosition = decorationOrigins[i];
            }
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var drop = drops[i];
                float previous = drop.Y;
                drop.Y -= drop.Record.FallSpeed * seconds;
                Place(drop);
                bool caught = Mathf.Abs(drop.Record.NormalizedX * movementLimit - x) <= catchHalfExtents.x &&
                    previous >= -2 - catchHalfExtents.y && drop.Y <= -2 + catchHalfExtents.y;
                if (!caught && drop.Y >= -2 - catchHalfExtents.y) continue;
                resolutions.Add(new VersusObjectResolution(drop.Record.Id, drop.Record.Kind,
                    caught ? ObjectOutcome.Caught : ObjectOutcome.Missed, drop.Definition.points, drop.Definition.itemIndex));
                if (caught)
                {
                    hurt = drop.Record.Kind == ObjectKind.Trash;
                    reactionSeconds = .24f;
                    owner.PlayCatch(hurt);
                }
                drop.Image.gameObject.SetActive(false);
                pool.Push(drop.Image);
                drops.RemoveAt(i);
            }
            return resolutions;
        }

        private void Place(Drop drop)
        {
            drop.Image.rectTransform.anchoredPosition = new Vector2(drop.Record.NormalizedX * movementLimit, drop.Y) * PixelsPerUnit;
            drop.Image.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin((float)drop.Record.Id) * 12);
        }

        public void SetPose(int direction)
        {
            MovementDirection = direction;
            if (direction != 0) facesRight = direction > 0;
            mascot.anchoredPosition = new Vector2(x, -2.5f) * PixelsPerUnit;
            body.rectTransform.localScale = new Vector3(facesRight ? 1 : -1, 1, 1);
            hat.sprite = facesRight ? rightHat : leftHat;
            hat.color = Color.white;
            hatRect.anchoredPosition = new Vector2(facesRight ? -.14f : .14f, .627778f) * PixelsPerUnit;
            int frame = reactionSeconds > 0 ? (hurt ? 7 : 6) : direction == 0 ? 0 : 1 + Mathf.FloorToInt(animationSeconds * 10) % 4;
            SetBody(owner.BodyFrames[frame]);
            body.color = reactionSeconds > 0 && hurt ? new Color(1, .5f, .4f) : Color.white;
        }

        private void SetBody(Sprite sprite)
        {
            body.sprite = MascotBodyPalette.ForFrame(sprite, shoeColorHex);
        }

        public void Terminal(bool lost, float seconds)
        {
            SetBody(owner.BodyFrames[lost ? 8 : 9]);
            if (lost)
            {
                if (deathPieces.Count == 0)
                    for (int i = 0; i < 16; i++)
                    {
                        var piece = VersusGameplayUI.Picture(field, "Death fragment", i < 4 ? owner.Definitions[i % 3].sprite : null,
                            new Vector2(x, -2.5f) * PixelsPerUnit, Vector2.one * PixelsPerUnit * (i < 4 ? .5f : .09f));
                        piece.preserveAspect = true;
                        deathPieces.Add(piece);
                    }
                for (int i = 0; i < deathPieces.Count; i++)
                {
                    float vx = Mathf.Cos(i * 2.4f) * 2;
                    float vy = 2.6f + Mathf.Sin(i * 1.7f);
                    var piece = deathPieces[i];
                    piece.rectTransform.anchoredPosition = new Vector2(x + vx * seconds, -2.5f + vy * seconds - 3 * seconds * seconds) * PixelsPerUnit;
                    piece.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 33 + seconds * 160);
                    piece.color = new Color(1, i < 4 ? 1 : .45f, i < 4 ? 1 : .3f, Mathf.Clamp01(1.5f - seconds));
                }
                mascot.localRotation = Quaternion.Euler(0, 0, Mathf.Min(seconds * 140, 80));
                float alpha = Mathf.Clamp01((1.5f - seconds) / .7f);
                body.color = new Color(1, seconds < .2f ? .3f : 1, seconds < .2f ? .2f : 1, alpha);
                hat.color = new Color(1, 1, 1, alpha);
            }
            else mascot.anchoredPosition = new Vector2(x, -2.5f + Mathf.Abs(Mathf.Sin(seconds * 7)) * .3f) * PixelsPerUnit;
        }

        public void ResetField()
        {
            foreach (var piece in deathPieces) if (piece != null) Destroy(piece.gameObject);
            deathPieces.Clear();
            foreach (var drop in drops) { drop.Image.gameObject.SetActive(false); pool.Push(drop.Image); }
            drops.Clear();
            x = animationSeconds = reactionSeconds = ambientSeconds = 0;
            mascot.localRotation = Quaternion.identity;
            body.color = hat.color = Color.white;
            SetPose(0);
        }

    }
}
