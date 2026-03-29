using System.Runtime.Serialization;
using UnityModManagerNet;

namespace SwitchNormalizer
{
    public class Settings : UnityModManager.ModSettings
    {
        [DataMember]
        public string[] ThrownSwitches { get; set; } = [];

        public override void Save(UnityModManager.ModEntry modEntry) {
            Save(this, modEntry);
        }

        public void OnChange() {
        }
    }
}
