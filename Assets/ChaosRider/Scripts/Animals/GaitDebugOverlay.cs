using UnityEngine;

namespace ChaosRider.Animals
{
    [RequireComponent(typeof(GaitEngine))]
    public class GaitDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private int fontSize = 28;
        [SerializeField] private float topPadding = 18f;
        [SerializeField] private float horizontalGap = 26f;

        private GaitEngine gaitEngine;
        private GUIStyle activeStyle;
        private GUIStyle inactiveStyle;
        private readonly GaitType[] gaitOrder =
        {
            GaitType.Idle,
            GaitType.DogWalk,
            GaitType.DogTrot,
            GaitType.DogCanter,
            GaitType.DogGallop,
        };

        private void Awake()
        {
            gaitEngine = GetComponent<GaitEngine>();
        }

        private void OnGUI()
        {
            if (!showOverlay || gaitEngine == null)
            {
                return;
            }

            EnsureStyles();

            var labels = new string[gaitOrder.Length];
            var totalWidth = 0f;
            for (var index = 0; index < gaitOrder.Length; index++)
            {
                labels[index] = FormatLabel(gaitOrder[index]);
                totalWidth += activeStyle.CalcSize(new GUIContent(labels[index])).x;
            }

            totalWidth += horizontalGap * Mathf.Max(0, gaitOrder.Length - 1);
            var startX = Mathf.Max(20f, (Screen.width - totalWidth) * 0.5f);
            var y = topPadding;

            for (var index = 0; index < gaitOrder.Length; index++)
            {
                var gaitType = gaitOrder[index];
                var label = labels[index];
                var style = gaitEngine.SelectedGait == gaitType ? activeStyle : inactiveStyle;
                var size = style.CalcSize(new GUIContent(label));
                var rect = new Rect(startX, y, size.x, size.y + 8f);

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    gaitEngine.SetSelectedGait(gaitType);
                }

                GUI.Label(rect, label, style);
                startX += size.x + horizontalGap;
            }
        }

        private void EnsureStyles()
        {
            if (activeStyle != null && inactiveStyle != null)
            {
                return;
            }

            activeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
            };
            activeStyle.normal.textColor = Color.white;

            inactiveStyle = new GUIStyle(activeStyle);
            inactiveStyle.normal.textColor = new Color(0.68f, 0.68f, 0.68f, 1f);
        }

        private static string FormatLabel(GaitType gaitType)
        {
            return gaitType switch
            {
                GaitType.DogWalk => "WALK",
                GaitType.DogTrot => "TROT",
                GaitType.DogCanter => "CANTER",
                GaitType.DogGallop => "GALLOP",
                _ => "IDLE",
            };
        }
    }
}
