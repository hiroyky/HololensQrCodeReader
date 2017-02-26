using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using System.IO;

public class PhotoInput : MonoBehaviour {
    public delegate void OnCaptured(List<byte> image, int width, int height);
    public GameObject QrSight;
    PhotoCapture photoCapture;
    CameraParameters cameraParameters;
    OnCaptured callback = null;

    void Start() {
    }

    public void CapturePhotoAsync(OnCaptured _callback) {
        callback = _callback;

        PhotoCapture.CreateAsync(false, (_photoCapture) => {
            Debug.Log("PhotoInput start");
            this.photoCapture = _photoCapture;
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

            CameraParameters c = new CameraParameters();
            c.hologramOpacity = 0.0f;
            c.cameraResolutionWidth = cameraResolution.width;
            c.cameraResolutionHeight = cameraResolution.height;
            c.pixelFormat = CapturePixelFormat.BGRA32;
            c.hologramOpacity = 0;
            this.cameraParameters = c;
            photoCapture.StartPhotoModeAsync(cameraParameters, onPhotoModeStarted);
        });
    }

    void onPhotoModeStarted(PhotoCapture.PhotoCaptureResult result) {
        if (result.success) {
            saveToFile();
            photoCapture.TakePhotoAsync(onCapturedPhotoToMemory);
        } else {
            Debug.LogError("Unable to start photo mode");
        }
    }

    void onCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame) {
        if (!result.success) {
            Debug.LogError("Error CapturedPhotoToMemory");
            return;
        }

        // 撮影画像の取得
        List<byte> buffer = new List<byte>();
        photoCaptureFrame.CopyRawImageDataIntoBuffer(buffer);
        photoCapture.StopPhotoModeAsync(onStoppedPhotoMode);

        // QR照準内のみを切り取る
        List<byte> trimmedBuffer = trimmingQrSight(buffer, 4);

        // QR照準内の画像を保存
        //Texture2D tex = createTexture(trimmedBuffer, cameraParameters.cameraResolutionWidth, cameraParameters.cameraResolutionHeight);
        //saveToFile(tex);

        if (callback != null) {
            callback(new List<byte>(trimmedBuffer), cameraParameters.cameraResolutionWidth, cameraParameters.cameraResolutionHeight);
        }
    }

    List<byte> trimmingQrSight(List<byte> src, int stride) {
        var position = QrSight.transform.position;
        var direction = QrSight.transform.forward;
        var scale = QrSight.transform.localScale;

        // ワールド座標系でのQR照準の座標を求めます．
        var leftTop = new Vector3(
                position.x - scale.x / 2,
                position.y + scale.y / 2,
                position.z);
        var rightTop = new Vector3(
                position.x + scale.x / 2,
                position.y + scale.y / 2,
                position.z);
        var rightBottom = new Vector3(
                position.x + scale.x / 2,
                position.y - scale.y / 2,
                position.z);
        var leftBottom = new Vector3(
                position.x - scale.x / 2,
                position.y - scale.y / 2,
                position.z);

        RaycastHit leftTopHit, rightTopHit, leftBottomHit, rightBottomHit;
        Physics.Raycast(leftTop, direction, out leftTopHit);
        Physics.Raycast(rightTop, direction, out rightTopHit);
        Physics.Raycast(leftBottom, direction, out leftBottomHit);
        Physics.Raycast(rightBottom, direction, out rightBottomHit);

        // ワールド座標系を投影座標系に変換
        var leftTopScreen = Camera.main.WorldToScreenPoint(leftTopHit.point);
        var rightTopScreen = Camera.main.WorldToScreenPoint(rightTopHit.point);
        var leftBottomScreen = Camera.main.WorldToScreenPoint(leftBottomHit.point);
        var rightBottomScreen = Camera.main.WorldToScreenPoint(rightBottomHit.point);

        // 投影座標系を，PhotoCaptureが撮影する画像上での座標に変換
        int leftSide = (int)(leftTopScreen.x / (float)Camera.main.pixelWidth * cameraParameters.cameraResolutionWidth);
        int rightSide = (int)(rightTopScreen.x / (float)Camera.main.pixelWidth * cameraParameters.cameraResolutionWidth);
        int bottomSide = (int)(leftBottomScreen.y / (float)Camera.main.pixelHeight * cameraParameters.cameraResolutionHeight);
        int topSide = (int)(leftTopScreen.y / (float)Camera.main.pixelHeight * cameraParameters.cameraResolutionHeight);

        // 上下反転
        List<byte> flippedBuffer = flipVertical(src, cameraParameters.cameraResolutionWidth, cameraParameters.cameraResolutionHeight, stride);

        byte[] dst = new byte[src.Count];
        for (int y = 0; y < cameraParameters.cameraResolutionHeight; ++y) {
            for (int x = 0; x < cameraParameters.cameraResolutionWidth; ++x) {
                int px = (y * cameraParameters.cameraResolutionWidth + x) * stride;
                if (x >= leftSide && x <= rightSide && y >= bottomSide && y <= topSide) {
                    for (int i = 0; i < stride; ++i) {
                        dst[px + i] = flippedBuffer[px + i];
                    }
                } else {
                }
            }
        }
        return new List<byte>(dst);
    }

    /// <summary>
    /// 上下反転します．
    /// </summary>
    List<byte> flipVertical(List<byte> src, int width, int height, int stride) {
        byte[] dst = new byte[src.Count];
        for (int y = 0; y < height; ++y) {
            for (int x = 0; x < width; ++x) {
                int invY = (height - 1) - y;
                int pxel = (y * width + x) * stride;
                int invPxel = (invY * width + x) * stride;
                for(int i = 0; i < stride; ++i) {
                    dst[invPxel + i] = src[pxel + i];
                }                
            }
        }
        return new List<byte>(dst);
    }

#if true
    Texture2D createTexture(List<byte> rawData, int width, int height) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.BGRA32, false);
        tex.LoadRawTextureData(rawData.ToArray());
        tex.Apply();
        return tex;
    }

    string saveToFile(Texture2D tex) {
        string filename = string.Format(@"QrSightImage{0}_n.png", Time.time);
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);

        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        return filePath;
    }
#endif
#if true
    string saveToFile() {
        string filename = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
        Debug.Log(string.Format("{0} {1}", filePath, filename));
        photoCapture.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, onCapturedPhotoToDisk);
        return filePath;

    }

    void onCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result) {
        if (result.success) {
            Debug.Log("Saved Photo to disk!");
            //photoCapture.StopPhotoModeAsync(onStoppedPhotoMode);
        } else {
            Debug.Log("Failed to save Photo to disk");
        }
    }
#endif
    void onStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result) {
        photoCapture.Dispose();
        photoCapture = null;
    }
}
