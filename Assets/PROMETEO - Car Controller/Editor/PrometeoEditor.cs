using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PrometeoCarController))]
public class PrometeoEditor : Editor
{
    SerializedProperty _maxSpeed;
    SerializedProperty _maxReverseSpeed;
    SerializedProperty _accelerationMultiplier;
    SerializedProperty _maxSteeringAngle;
    SerializedProperty _steeringSpeed;
    SerializedProperty _brakeForce;
    SerializedProperty _decelerationMultiplier;
    SerializedProperty _handbrakeDriftMultiplier;
    SerializedProperty _bodyMassCenter;

    SerializedProperty _frontLeftMesh;
    SerializedProperty _frontLeftCollider;
    SerializedProperty _frontRightMesh;
    SerializedProperty _frontRightCollider;
    SerializedProperty _rearLeftMesh;
    SerializedProperty _rearLeftCollider;
    SerializedProperty _rearRightMesh;
    SerializedProperty _rearRightCollider;

    SerializedProperty _useEffects;
    SerializedProperty _rlwParticleSystem;
    SerializedProperty _rrwParticleSystem;
    SerializedProperty _rlwTireSkid;
    SerializedProperty _rrwTireSkid;

    SerializedProperty _useSounds;
    SerializedProperty _carEngineSound;
    SerializedProperty _tireScreechSound;
    bool _hasValidTargets;

    void OnEnable()
    {
        _hasValidTargets = HasValidTargets();
        if (!_hasValidTargets)
        {
            return;
        }

        _maxSpeed = serializedObject.FindProperty("maxSpeed");
        _maxReverseSpeed = serializedObject.FindProperty("maxReverseSpeed");
        _accelerationMultiplier = serializedObject.FindProperty("accelerationMultiplier");
        _maxSteeringAngle = serializedObject.FindProperty("maxSteeringAngle");
        _steeringSpeed = serializedObject.FindProperty("steeringSpeed");
        _brakeForce = serializedObject.FindProperty("brakeForce");
        _decelerationMultiplier = serializedObject.FindProperty("decelerationMultiplier");
        _handbrakeDriftMultiplier = serializedObject.FindProperty("handbrakeDriftMultiplier");
        _bodyMassCenter = serializedObject.FindProperty("bodyMassCenter");

        _frontLeftMesh = serializedObject.FindProperty("frontLeftMesh");
        _frontLeftCollider = serializedObject.FindProperty("frontLeftCollider");
        _frontRightMesh = serializedObject.FindProperty("frontRightMesh");
        _frontRightCollider = serializedObject.FindProperty("frontRightCollider");
        _rearLeftMesh = serializedObject.FindProperty("rearLeftMesh");
        _rearLeftCollider = serializedObject.FindProperty("rearLeftCollider");
        _rearRightMesh = serializedObject.FindProperty("rearRightMesh");
        _rearRightCollider = serializedObject.FindProperty("rearRightCollider");

        _useEffects = serializedObject.FindProperty("useEffects");
        _rlwParticleSystem = serializedObject.FindProperty("RLWParticleSystem");
        _rrwParticleSystem = serializedObject.FindProperty("RRWParticleSystem");
        _rlwTireSkid = serializedObject.FindProperty("RLWTireSkid");
        _rrwTireSkid = serializedObject.FindProperty("RRWTireSkid");

        _useSounds = serializedObject.FindProperty("useSounds");
        _carEngineSound = serializedObject.FindProperty("carEngineSound");
        _tireScreechSound = serializedObject.FindProperty("tireScreechSound");
    }

    public override void OnInspectorGUI()
    {
        if (!_hasValidTargets || !HasValidTargets())
        {
            EditorGUILayout.HelpBox("Prometeo target is missing. Select an existing car object.", MessageType.Warning);
            return;
        }

        serializedObject.Update();

        DrawCarSetup();
        DrawWheels();
        DrawEffects();
        DrawSounds();
        DrawRuntimeInfo();

        serializedObject.ApplyModifiedProperties();
    }

    bool HasValidTargets()
    {
        if (target == null || targets == null || targets.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    void DrawCarSetup()
    {
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("CAR SETUP", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        _maxSpeed.intValue = EditorGUILayout.IntSlider("Max Speed", _maxSpeed.intValue, 20, 190);
        _maxReverseSpeed.intValue = EditorGUILayout.IntSlider("Max Reverse Speed", _maxReverseSpeed.intValue, 10, 120);
        _accelerationMultiplier.intValue = EditorGUILayout.IntSlider("Acceleration Multiplier", _accelerationMultiplier.intValue, 1, 10);
        _maxSteeringAngle.intValue = EditorGUILayout.IntSlider("Max Steering Angle", _maxSteeringAngle.intValue, 10, 45);
        _steeringSpeed.floatValue = EditorGUILayout.Slider("Steering Speed", _steeringSpeed.floatValue, 0.1f, 1f);
        _brakeForce.intValue = EditorGUILayout.IntSlider("Brake Force", _brakeForce.intValue, 100, 600);
        _decelerationMultiplier.intValue = EditorGUILayout.IntSlider("Deceleration Multiplier", _decelerationMultiplier.intValue, 1, 10);
        _handbrakeDriftMultiplier.intValue = EditorGUILayout.IntSlider("Drift Multiplier", _handbrakeDriftMultiplier.intValue, 1, 10);
        EditorGUILayout.PropertyField(_bodyMassCenter, new GUIContent("Mass Center"));
    }

    void DrawWheels()
    {
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("WHEELS", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(_frontLeftMesh, new GUIContent("Front Left Mesh"));
        EditorGUILayout.PropertyField(_frontLeftCollider, new GUIContent("Front Left Collider"));
        EditorGUILayout.PropertyField(_frontRightMesh, new GUIContent("Front Right Mesh"));
        EditorGUILayout.PropertyField(_frontRightCollider, new GUIContent("Front Right Collider"));
        EditorGUILayout.PropertyField(_rearLeftMesh, new GUIContent("Rear Left Mesh"));
        EditorGUILayout.PropertyField(_rearLeftCollider, new GUIContent("Rear Left Collider"));
        EditorGUILayout.PropertyField(_rearRightMesh, new GUIContent("Rear Right Mesh"));
        EditorGUILayout.PropertyField(_rearRightCollider, new GUIContent("Rear Right Collider"));
    }

    void DrawEffects()
    {
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("EFFECTS", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        _useEffects.boolValue = EditorGUILayout.BeginToggleGroup("Use effects", _useEffects.boolValue);
        EditorGUILayout.PropertyField(_rlwParticleSystem, new GUIContent("Rear Left Particle System"));
        EditorGUILayout.PropertyField(_rrwParticleSystem, new GUIContent("Rear Right Particle System"));
        EditorGUILayout.PropertyField(_rlwTireSkid, new GUIContent("Rear Left Trail Renderer"));
        EditorGUILayout.PropertyField(_rrwTireSkid, new GUIContent("Rear Right Trail Renderer"));
        EditorGUILayout.EndToggleGroup();
    }

    void DrawSounds()
    {
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("SOUNDS", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        _useSounds.boolValue = EditorGUILayout.BeginToggleGroup("Use sounds", _useSounds.boolValue);
        EditorGUILayout.PropertyField(_carEngineSound, new GUIContent("Car Engine Sound"));
        EditorGUILayout.PropertyField(_tireScreechSound, new GUIContent("Tire Screech Sound"));
        EditorGUILayout.EndToggleGroup();
    }

    void DrawRuntimeInfo()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        PrometeoCarController controller = target as PrometeoCarController;
        if (controller == null)
        {
            return;
        }

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("RUNTIME", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Speed", $"{controller.SpeedKmh.Value:0} km/h");
        EditorGUILayout.LabelField("Control Enabled", controller.ControlEnabled.ToString());
    }
}
