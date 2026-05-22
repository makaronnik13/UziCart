using UnityEngine;

public class FPSunter : MonoBehaviour
{
    [SerializeField] private float updateInterval = 0.25f;
    [SerializeField] private int fontSize = 32;
    [SerializeField] private Vector2 offset = new Vector2(10f, 10f);

    private float _accumulatedTime;
    private int _frames;
    private float _timeLeft;
    private float _fps;

    private GUIStyle _style;

    private void Awake()
    {
        _timeLeft = updateInterval;

        _style = new GUIStyle
        {
            fontSize = fontSize,
            normal =
            {
                textColor = Color.white
            }
        };
    }

    private void Update()
    {
        _timeLeft -= Time.unscaledDeltaTime;
        _accumulatedTime += Time.unscaledDeltaTime;
        _frames++;

        if (_timeLeft <= 0f)
        {
            _fps = _frames / _accumulatedTime;

            _timeLeft = updateInterval;
            _accumulatedTime = 0f;
            _frames = 0;
        }
    }

    private void OnGUI()
    {
        GUI.Label(
            new Rect(offset.x, offset.y, 300f, 80f),
            $"FPS: {_fps:0}",
            _style
        );
    }
}