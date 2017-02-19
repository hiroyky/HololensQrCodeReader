using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;

public class PhotoInput : MonoBehaviour {
    public delegate void OnCaptured(List<byte> image, int width, int height);
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
            //saveToFile();
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

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        List<byte> buffer = new List<byte>();
        photoCaptureFrame.CopyRawImageDataIntoBuffer(buffer);
        photoCapture.StopPhotoModeAsync(onStoppedPhotoMode);
        if (callback != null) {
            callback(buffer, cameraResolution.width, cameraResolution.height);
        }
    }

#if false
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
