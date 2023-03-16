#if VRC_SDK_VRCSDK3
using System;
using UnityEngine;
using Random = UnityEngine.Random;
using VRC.SDKBase;
using VRC.SDK3.Image;
using VRC.Udon;
using UdonSharp;

namespace VLM.WallNewspaper
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WallNewspaper : UdonSharpBehaviour
    {
        private const int HorizontallyPageCount = 2;
        private const int VerticallyPageCount = 2;

        [NonSerialized]
        internal int CurrentPage = 0;

        [SerializeField]
        private VRCUrl url;
        [SerializeField]
        private Material material;
        [SerializeField]
        private Transform[] openingPageButtonBones;

        [UdonSynced, FieldChangeCallback(nameof(WallNewspaper.RandomizedStartPage))]
        private int randomizedStartPage;
        private int RandomizedStartPage
        {
            get => this.randomizedStartPage;
            set
            {
                this.randomizedStartPage = value;
                this.OpenPage();
            }
        }

        private VRCImageDownloader imageDownloader;

        public void OpenPage()
        {
            for (var i = 0; i < this.openingPageButtonBones.Length; i++)
            {
                this.openingPageButtonBones[i].localScale = i == this.CurrentPage ? Vector3.one : Vector3.zero;
            }

            var page = (this.CurrentPage + this.RandomizedStartPage) % this.openingPageButtonBones.Length;
            this.material.SetTextureOffset("_MainTex", new Vector2(
                (float)(page % WallNewspaper.HorizontallyPageCount) / WallNewspaper.HorizontallyPageCount,
                -Mathf.Floor((float)page / WallNewspaper.HorizontallyPageCount + 1) / WallNewspaper.VerticallyPageCount
            ));
        }

        private void Start()
        {
            Debug.Log("バーチャルライフマガジン壁新聞 v2.0.0");

            this.imageDownloader = new VRCImageDownloader();
            this.imageDownloader.DownloadImage(this.url, this.material, this.GetComponent<UdonBehaviour>());

            if (Networking.IsOwner(this.gameObject))
            {
                this.RandomizedStartPage = Random.Range(0, this.openingPageButtonBones.Length - 1);
                this.RequestSerialization();
            }

            this.material.SetTextureScale(
                "_MainTex",
                new Vector2(1.0f / WallNewspaper.HorizontallyPageCount, 1.0f / WallNewspaper.VerticallyPageCount)
            );
            this.OpenPage();
        }
    }
}
#endif
