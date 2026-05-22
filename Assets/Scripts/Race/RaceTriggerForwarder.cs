using UnityEngine;

public class RaceTriggerForwarder : MonoBehaviour
{
    RaceController _controller;
    int _checkpointIndex = -1;
    bool _isFinish;

    public void Initialize(RaceController controller, int checkpointIndex, bool isFinish)
    {
        _controller = controller;
        _checkpointIndex = checkpointIndex;
        _isFinish = isFinish;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_controller == null)
        {
            return;
        }

        if (_isFinish)
        {
            _controller.HandleFinishTrigger(other);
        }
        else
        {
            _controller.HandleCheckpointTrigger(other, _checkpointIndex);
        }
    }
}
