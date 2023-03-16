#if UDONSHARP
using UnityEngine;
using UdonSharp;

namespace VLM.WallNewspaper
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class OpenPage : UdonSharpBehaviour
    {
        [SerializeField]
        private UdonSharpBehaviour wallNewsPaper;
        [SerializeField]
        private int page;

        public override void Interact()
        {
            this.wallNewsPaper.SetProgramVariable(nameof(WallNewspaper.CurrentPage), this.page);
            this.wallNewsPaper.SendCustomEvent(nameof(WallNewspaper.OpenPage));
        }
    }
}
#endif
