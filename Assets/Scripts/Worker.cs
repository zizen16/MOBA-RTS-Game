using UnityEngine;
using UnityEngine.AI;

public class Worker : BaseUnit
{
    public enum WorkerState { Idle, MovingToBuild, Building, MovingToResource, Gathering, ReturningToBase }
    public WorkerState currentState = WorkerState.Idle;
    public bool isPlacing; // True when construction preview is following cursor
    public bool CanBeMoved() => currentState != WorkerState.Building && currentState != WorkerState.Gathering;
}
