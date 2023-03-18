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
        /// <summary>
        /// 外部読み込み画像を上下反転したときの、ピクセル単位の左上からXY位置とRGB24の連想配列。
        /// </summary>
        /// <remarks>
        /// Image Loadingが上下反転する不具合で、不具合修正時に反転しないように。
        /// 
        /// [Build: 1282] Image loader on quest loads images upside down | Voters | VRChat
        /// https://feedback.vrchat.com/vrchat-udon-closed-alpha-bugs/p/build-1282-image-loader-on-quest-loads-images-upside-down
        /// </remarks>
        private readonly int[][] pointRGB24ForInvertedTexturePairs = new int[][]
        {
            new[] { 15, 1039, 229, 217, 197 }, // 反転した画像の1ページ目の左上のベージュ色の部分
            new[] { 15, 2033, 246, 191,  89 }, // 反転した画像の1ページ目の左下のオレンジ色の部分
        };
        /// <summary>
        /// Image Loadingが上下反転する不具合を検出する正方形領域の一辺のピクセル数。
        /// </summary>
        /// <remarks>
        /// 検出地点を中心した正方形領域の色を平均化します。
        /// </remarks>
        private const int InvertedTextureDetectionPointSize = 10;
        /// <summary>
        /// Image Loadingが上下反転する不具合を検出するためのピクセルの色の一致判定で、RGB24の各値がどれだけズレていても一致すると判定するか。
        /// </summary>
        private const int InvertedTextureDetectionColorThreshold = 5;

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

#if UNITY_ANDROID
        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            var texture = (Texture2D)this.material.GetTexture("_MainTex");
            WallNewspaper.FixTextureInverting(
                texture,
                texture.GetPixels32(),
                this.pointRGB24ForInvertedTexturePairs
            );
        }
#endif

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

        /// <summary>
        /// テクスチャが上下反転していれば、元に戻します。
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="pixels"></param>
        /// <param name="pointRGB24ForInvertedTexturePairs"><see cref="WallNewspaper.pointRGB24ForInvertedTexturePairs"/></param>
        private static void FixTextureInverting(
            Texture2D texture,
            Color32[] pixels,
            int[][] pointRGB24ForInvertedTexturePairs
        )
        {
            foreach (var pointRGB24ForInvertedTexturePair in pointRGB24ForInvertedTexturePairs)
            {
                var point = new Vector2Int(
                    pointRGB24ForInvertedTexturePair[0],
                    pointRGB24ForInvertedTexturePair[1]
                );
                var targetColor = new Color32(
                    (byte)pointRGB24ForInvertedTexturePair[2],
                    (byte)pointRGB24ForInvertedTexturePair[3],
                    (byte)pointRGB24ForInvertedTexturePair[4],
                    255
                );

                if (!WallNewspaper.IsPixelColorsMatch(texture, pixels, point, targetColor))
                {
                    return;
                }
            }

            WallNewspaper.FlipTextureUpsideDown(texture, pixels);
        }

        /// <summary>
        /// テクスチャの指定位置周辺の平均色が指定した色とほぼ一致すれば <c>true</c> を返します。
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="pixels"></param>
        /// <param name="point"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private static bool IsPixelColorsMatch(Texture2D texture, Color32[] pixels, Vector2Int point, Color32 color)
        {
            // 指定位置周辺の色を平均化
            float[] averageColor = null;
            for (var x = point.x - (WallNewspaper.InvertedTextureDetectionPointSize / 2);
                x <= point.x + (WallNewspaper.InvertedTextureDetectionPointSize / 2);
                x++)
            {
                for (var y = point.y - (WallNewspaper.InvertedTextureDetectionPointSize / 2);
                    y <= point.y + (WallNewspaper.InvertedTextureDetectionPointSize / 2);
                    y++)
                {
                    var c = pixels[(texture.height - y) * texture.width + x];
                    if (averageColor == null)
                    {
                        averageColor = new float[] { c.r, c.g, c.b };
                        continue;
                    }
                    averageColor[0] = (averageColor[0] + c.r) / 2;
                    averageColor[1] = (averageColor[1] + c.g) / 2;
                    averageColor[2] = (averageColor[2] + c.b) / 2;
                }
            }

            if (Math.Abs(averageColor[0] - color.r) > WallNewspaper.InvertedTextureDetectionColorThreshold
                || Math.Abs(averageColor[1] - color.g) > WallNewspaper.InvertedTextureDetectionColorThreshold
                || Math.Abs(averageColor[2] - color.b) > WallNewspaper.InvertedTextureDetectionColorThreshold)
            {
                // 指定位置の画像反転時の色と実際の色の差が、閾値を超えていれば
                return false;
            }

            return true;
        }

        /// <summary>
        /// テクスチャを上下反転します。
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="pixels"></param>
        private static void FlipTextureUpsideDown(Texture2D texture, Color32[] pixels)
        {
            var flippedPixels = new Color32[pixels.Length];
            for (int sourceIndex = pixels.Length - texture.width, destinationIndex = 0;
                destinationIndex < pixels.Length;
                sourceIndex -= texture.width, destinationIndex += texture.width)
            {
                Array.Copy(pixels, sourceIndex, flippedPixels, destinationIndex, texture.width);
            }
            texture.SetPixels32(flippedPixels);
            texture.Apply();
        }

        private void Start()
        {
            Debug.Log("バーチャルライフマガジン壁新聞 v2.1.0");

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
