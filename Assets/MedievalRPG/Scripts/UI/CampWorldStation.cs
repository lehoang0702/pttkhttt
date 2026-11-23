using UnityEngine;

namespace MedievalRPG.UI
{
    public enum CampStationAction
    {
        Equipment = 0,
        Shop = 1,
        StartBattle = 2,
        MistakeNotebook = 3
    }

    public class CampWorldStation : MonoBehaviour
    {
        public CampStationAction stationAction;
    }
}
