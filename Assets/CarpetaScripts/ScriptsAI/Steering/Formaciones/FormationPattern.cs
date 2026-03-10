using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FormationPattern : MonoBehaviour
{
    protected int numberOfSlots;
    
    public abstract Location GetDriftOffset(List<SlotAssignment> slotAssignments);
    public abstract Location GetSlotLocation(int slotNumber);
    public abstract bool SupportsSlots(int slotCount);
}
