using UnityEngine;

namespace ChaosRider.Animals
{
    [RequireComponent(typeof(GaitEngine))]
    public class GaitDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private int fontSize = 48;
        [SerializeField] private Vector2 screenPosition = new Vector2(24f, 20f);

        private GaitEngine gaitEngine;
        private GUIStyle labelStyle;

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

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft,
                };
                labelStyle.normal.textColor = Color.white;
            }

            GUI.Label(
                new Rect(screenPosition.x, screenPosition.y, 500f, 80f),
                gaitEngine.CurrentGaitLabel.ToUpperInvariant(),
                labelStyle);
        }
    }
}
