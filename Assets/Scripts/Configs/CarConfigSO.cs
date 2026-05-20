using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "CarConfig", menuName = "Meta/Car Config")]
public class CarConfigSO : ScriptableObject
{
    public GameObject Prefab;
    public Sprite Preview;
    [FormerlySerializedAs("CharacterName")]
    public string CarName = "Car";
    [Range(0, 5)] public int Speed = 3;
    [Range(0, 5)] public int Handling = 3;
    [Range(0, 5)] public int Lethality = 3;
}
