using UnityEngine;

public class ActorInfoWindow : BaseWindow
{
    public Transform CurrentSelectionTransform { get; protected set; }
}

public class RoomSelectionWindow : BaseWindow
{
    public Component CurrentRoom { get; protected set; }
}

public class RoomObjectSelectionWindow : BaseWindow
{
    public Component CurrentObject { get; protected set; }
}
