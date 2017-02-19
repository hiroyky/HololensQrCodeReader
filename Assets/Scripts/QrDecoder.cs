using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QrDecoder : MonoBehaviour {

    public string Decode(byte[] src, int width, int height) {
#if !UNITY_EDITOR
        Debug.Log("qr decoding...");
        ZXing.IBarcodeReader reader = new ZXing.BarcodeReader();
        var res = reader.Decode(src, width, height, ZXing.BitmapFormat.BGRA32);
        if (res == null) {
            return null;
        }
        return res.Text;
#else
        return "editor debugging...";
#endif
    }

}
